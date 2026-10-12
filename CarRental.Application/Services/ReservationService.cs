using System.Globalization;
using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Common.Models;
using _CarRental.Application.DTOs.Reservations;
using _CarRental.Application.Validators;
using _CarRental.Domain.Constants;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Enums;
using _CarRental.Domain.Exceptions;
using _CarRental.Domain.Interfaces.Repositories;

namespace _CarRental.Application.Services;

public sealed class ReservationService(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IAuditLogger auditLogger,
    CreateReservationRequestValidator validator,
    IDateTimeProvider dateTimeProvider) : IReservationService
{
    public async Task<ReservationDto> CreateAsync(CreateReservationRequest request)
    {
        var validated = validator.Validate(request);
        var customerId = await currentUser.GetCustomerIdAsync();
        var customer = await unitOfWork.Customers.GetByUserIdAsync(currentUser.UserId)
            ?? throw new NotFoundException("Customer profile was not found.");
        if (customer.DrivingLicenseExpiry.Date < validated.ExpectedReturnDateTimeUtc.Date)
            throw new BusinessRuleException("DRIVING_LICENSE_EXPIRED", "The driving license must remain valid through the expected return date.");

        var vehicle = await unitOfWork.Vehicles.GetByIdAsync(validated.VehicleId)
            ?? throw new NotFoundException("Vehicle was not found.");
        if (vehicle.BranchId != validated.PickupBranchId)
            throw new ConflictException("PICKUP_BRANCH_MISMATCH", "The pickup branch must be the vehicle's current branch.");
        if (await unitOfWork.Branches.GetByIdAsync(validated.PickupBranchId) is null ||
            await unitOfWork.Branches.GetByIdAsync(validated.ReturnBranchId) is null)
            throw new NotFoundException("Pickup or return branch was not found.");
        if (!await unitOfWork.Vehicles.IsAvailableAsync(vehicle.VehicleId, validated.PickupDateTimeUtc, validated.ExpectedReturnDateTimeUtc))
            throw new ConflictException("VEHICLE_NOT_AVAILABLE", "The selected vehicle is not available for the requested dates.");

        var reservation = new Reservation
        {
            CustomerId = customerId,
            VehicleId = validated.VehicleId,
            PickupBranchId = validated.PickupBranchId,
            ReturnBranchId = validated.ReturnBranchId,
            PickupDateTime = validated.PickupDateTimeUtc,
            ExpectedReturnDateTime = validated.ExpectedReturnDateTimeUtc,
            CreatedAt = dateTimeProvider.UtcNow
        };
        reservation.ReservationId = await unitOfWork.Reservations.CreateAsync(reservation);

        var dto = ToDto(reservation);
        await auditLogger.LogAsync(AuditActions.ReservationCreated, "Reservations",
            reservation.ReservationId.ToString(CultureInfo.InvariantCulture), null, dto, currentUser.UserId);
        return dto;
    }

    public async Task<PagedResult<ReservationDto>> ListMineAsync(PagedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var customerId = await RequireCurrentCustomerIdAsync();
        var (items, count) = await unitOfWork.Reservations.ListByCustomerAsync(customerId, request.Page, request.PageSize);
        return new PagedResult<ReservationDto>(items.Select(ToDto).ToArray(), request.Page, request.PageSize, count);
    }

    public async Task<ReservationDto> GetMineAsync(int reservationId)
    {
        var reservation = await RequireOwnedReservationAsync(reservationId);
        return ToDto(reservation);
    }

    public async Task<ReservationDto> CancelMineAsync(int reservationId)
    {
        var reservation = await RequireOwnedReservationAsync(reservationId);
        if (reservation.Status == ReservationStatus.Confirmed)
            throw new ConflictException("RESERVATION_CONFIRMED_CONTACT_BRANCH", "A confirmed reservation cannot be cancelled here. Please contact the branch.");

        reservation.Cancel();
        await unitOfWork.Reservations.UpdateStatusAsync(reservation.ReservationId, ReservationStatus.Pending, reservation.Status);
        await auditLogger.LogAsync(AuditActions.ReservationStatusChanged, "Reservations",
            reservation.ReservationId.ToString(CultureInfo.InvariantCulture), new { Status = ReservationStatus.Pending },
            new { Status = reservation.Status }, currentUser.UserId);
        return ToDto(reservation);
    }

    public async Task<PagedResult<ReservationDto>> ListForStaffAsync(ReservationFilterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var staff = await GetStaffContextAsync();
        var from = request.PickupFrom?.UtcDateTime;
        var to = request.PickupTo?.UtcDateTime;
        if (from.HasValue && to.HasValue && from.Value > to.Value)
            throw new BusinessRuleException("VALIDATION_FAILED", "PickupFrom must be earlier than or equal to PickupTo.");

        var (items, count) = await unitOfWork.Reservations.ListForBranchAsync(
            staff.BranchId, request.Status, from, to, request.Page, request.PageSize);
        return new PagedResult<ReservationDto>(items.Select(ToDto).ToArray(), request.Page, request.PageSize, count);
    }

    public async Task<ReservationDto> ConfirmAsync(int reservationId)
    {
        var staff = await GetStaffContextAsync();
        var reservation = await RequireStaffReservationAsync(reservationId, staff.BranchId);
        EnsurePending(reservation);
        reservation.Confirm(staff.EmployeeId);

        await unitOfWork.BeginTransactionAsync();
        try
        {
            await unitOfWork.Vehicles.LockForReservationReviewAsync(reservation.VehicleId);
            await unitOfWork.Reservations.UpdateStatusAsync(reservation.ReservationId,
                ReservationStatus.Pending, ReservationStatus.Confirmed, staff.EmployeeId);
            await unitOfWork.Vehicles.SetStatusAsync(reservation.VehicleId, VehicleStatus.Reserved);
            await unitOfWork.CommitAsync();
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }

        await AuditStaffActionAsync(reservation, ReservationStatus.Pending, reservation.Status);
        return ToDto(reservation);
    }

    public async Task<ReservationDto> RejectAsync(int reservationId, RejectReservationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Reason?.Length > 500)
            throw new BusinessRuleException("VALIDATION_FAILED", "Reason cannot exceed 500 characters.");

        var staff = await GetStaffContextAsync();
        var reservation = await RequireStaffReservationAsync(reservationId, staff.BranchId);
        EnsurePending(reservation);
        reservation.Reject();

        await unitOfWork.BeginTransactionAsync();
        try
        {
            await unitOfWork.Vehicles.LockForReservationReviewAsync(reservation.VehicleId);
            await unitOfWork.Reservations.UpdateStatusAsync(reservation.ReservationId,
                ReservationStatus.Pending, ReservationStatus.Rejected);
            await ReleaseVehicleIfFreeAsync(reservation);
            await unitOfWork.CommitAsync();
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }

        await AuditStaffActionAsync(reservation, ReservationStatus.Pending, reservation.Status);
        return ToDto(reservation);
    }

    public async Task<ReservationDto> CancelByStaffAsync(int reservationId)
    {
        var staff = await GetStaffContextAsync();
        var reservation = await RequireStaffReservationAsync(reservationId, staff.BranchId);
        var oldStatus = reservation.Status;
        if (oldStatus is not (ReservationStatus.Pending or ReservationStatus.Confirmed))
            throw new ConflictException("RESERVATION_NOT_CANCELLABLE", "Only pending or confirmed reservations can be cancelled by staff.");
        reservation.CancelByStaff();

        await unitOfWork.BeginTransactionAsync();
        try
        {
            await unitOfWork.Vehicles.LockForReservationReviewAsync(reservation.VehicleId);
            await unitOfWork.Reservations.UpdateStatusAsync(reservation.ReservationId, oldStatus, reservation.Status);
            await ReleaseVehicleIfFreeAsync(reservation);
            await unitOfWork.CommitAsync();
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }

        await AuditStaffActionAsync(reservation, oldStatus, reservation.Status);
        return ToDto(reservation);
    }

    private async Task ReleaseVehicleIfFreeAsync(Reservation reservation)
    {
        if (await unitOfWork.Vehicles.HasOtherConfirmedReservationAsync(reservation.VehicleId, reservation.ReservationId))
            return;

        var vehicle = await unitOfWork.Vehicles.GetByIdAsync(reservation.VehicleId);
        if (vehicle?.Status == VehicleStatus.Reserved.ToString())
            await unitOfWork.Vehicles.SetStatusAsync(vehicle.VehicleId, VehicleStatus.Available);
    }

    private async Task<StaffContext> GetStaffContextAsync()
    {
        if (currentUser.IsInRole(RoleNames.Admin))
            return new StaffContext(null, null);
        if (!currentUser.IsInRole(RoleNames.Employee) && !currentUser.IsInRole(RoleNames.BranchManager))
            throw new ForbiddenException("Only employees, branch managers, or administrators can review reservations.");

        return new StaffContext(await currentUser.GetBranchIdAsync(), await currentUser.GetEmployeeIdAsync());
    }

    private async Task<Reservation> RequireStaffReservationAsync(int reservationId, int? branchId)
    {
        if (reservationId <= 0)
            throw new NotFoundException("Reservation was not found.");
        var reservation = await unitOfWork.Reservations.GetByIdAsync(reservationId)
            ?? throw new NotFoundException("Reservation was not found.");
        if (branchId.HasValue && reservation.PickupBranchId != branchId.Value)
            throw new ForbiddenException("You are not allowed to review reservations for another branch.");
        return reservation;
    }

    private static void EnsurePending(Reservation reservation)
    {
        if (reservation.Status != ReservationStatus.Pending)
            throw new ConflictException("RESERVATION_NOT_PENDING", "Only pending reservations can be confirmed or rejected.");
    }

    private Task AuditStaffActionAsync(Reservation reservation, ReservationStatus oldStatus, ReservationStatus newStatus) =>
        auditLogger.LogAsync(AuditActions.ReservationStatusChanged, "Reservations",
            reservation.ReservationId.ToString(CultureInfo.InvariantCulture), new { Status = oldStatus },
            new { Status = newStatus, reservation.ConfirmedByEmployeeId }, currentUser.UserId);

    private async Task<int> RequireCurrentCustomerIdAsync()
    {
        return await currentUser.GetCustomerIdAsync();
    }

    private async Task<Reservation> RequireOwnedReservationAsync(int reservationId)
    {
        if (reservationId <= 0)
            throw new NotFoundException("Reservation was not found.");
        var customerId = await RequireCurrentCustomerIdAsync();
        var reservation = await unitOfWork.Reservations.GetByIdAsync(reservationId);
        if (reservation is null || reservation.CustomerId != customerId)
            throw new NotFoundException("Reservation was not found.");
        return reservation;
    }

    private static ReservationDto ToDto(Reservation reservation) => new(
        reservation.ReservationId,
        reservation.CustomerId,
        reservation.VehicleId,
        reservation.PickupBranchId,
        reservation.ReturnBranchId,
        DateTime.SpecifyKind(reservation.PickupDateTime, DateTimeKind.Utc),
        DateTime.SpecifyKind(reservation.ExpectedReturnDateTime, DateTimeKind.Utc),
        reservation.Status.ToString(),
        DateTime.SpecifyKind(reservation.CreatedAt, DateTimeKind.Utc));

    private sealed record StaffContext(int? BranchId, int? EmployeeId);

}
