using System.Globalization;
using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Common.Models;
using _CarRental.Application.DTOs.Contracts;
using _CarRental.Application.Validators;
using _CarRental.Domain.Constants;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Enums;
using _CarRental.Domain.Exceptions;
using _CarRental.Domain.Interfaces.Repositories;
using _CarRental.Domain.Services;

namespace _CarRental.Application.Services;

public sealed class RentalContractService(
    IUnitOfWork unitOfWork, ICurrentUser currentUser, IAuditLogger auditLogger,
    IDateTimeProvider dateTimeProvider, CreateContractRequestValidator validator,
    ReturnVehicleRequestValidator returnValidator) : IRentalContractService
{
    public async Task<ContractDto> CreateAsync(CreateContractRequest request)
    {
        validator.Validate(request);
        var staff = await RequireStaffAsync(requireEmployee: true);
        var reservation = await unitOfWork.Reservations.GetByIdAsync(request.ReservationId)
            ?? throw new NotFoundException("Reservation was not found.");
        if (staff.BranchId.HasValue && reservation.PickupBranchId != staff.BranchId.Value)
            throw new ForbiddenException("You are not allowed to create contracts for another branch.");
        if (reservation.Status != ReservationStatus.Confirmed)
            throw new ConflictException("RESERVATION_NOT_CONFIRMED", "Only confirmed reservations can be converted to a rental contract.");

        var vehicle = await unitOfWork.Vehicles.GetByIdAsync(reservation.VehicleId)
            ?? throw new NotFoundException("Vehicle was not found.");
        if (vehicle.Status != VehicleStatus.Reserved.ToString())
            throw new ConflictException("VEHICLE_NOT_RESERVED", "The vehicle must be reserved before pickup.");

        var pickupAt = dateTimeProvider.UtcNow;
        var mileage = request.PickupMileage ?? vehicle.CurrentMileage;
        if (mileage < vehicle.CurrentMileage)
            throw new BusinessRuleException("INVALID_PICKUP_MILEAGE", "Pickup mileage cannot be less than the vehicle's current mileage.");
        var days = RentalPriceCalculator.CalculateDays(pickupAt, reservation.ExpectedReturnDateTime);
        var basic = RentalPriceCalculator.CalculateBasic(vehicle.DailyPrice, days);
        var contract = RentalContract.Create(reservation.ReservationId, reservation.CustomerId, reservation.VehicleId,
            staff.EmployeeId!.Value, pickupAt, mileage, request.PickupFuelLevel, request.ConditionNotes,
            days, basic, request.DiscountAmount, request.AdditionalFees);
        contract.PickupBranchId = reservation.PickupBranchId;

        await unitOfWork.BeginTransactionAsync();
        try
        {
            contract.ContractId = await unitOfWork.Contracts.CreateFromReservationAsync(contract);
            await unitOfWork.CommitAsync();
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }

        var persisted = await unitOfWork.Contracts.GetByIdAsync(contract.ContractId)
            ?? throw new InvalidOperationException("The created rental contract could not be reloaded.");
        await auditLogger.LogAsync(AuditActions.RentalContractCreated, "RentalContracts",
            persisted.ContractId.ToString(CultureInfo.InvariantCulture), null, ToDto(persisted), currentUser.UserId);
        return ToDto(persisted);
    }

    public async Task<ContractDto> GetByIdAsync(int contractId)
    {
        if (contractId <= 0) throw new NotFoundException("Contract was not found.");
        var contract = await unitOfWork.Contracts.GetByIdAsync(contractId)
            ?? throw new NotFoundException("Contract was not found.");
        if (currentUser.IsInRole(RoleNames.Customer))
        {
            var customerId = await currentUser.GetCustomerIdAsync();
            if (contract.CustomerId != customerId) throw new NotFoundException("Contract was not found.");
        }
        else
        {
            var staff = await RequireStaffAsync(requireEmployee: false);
            if (staff.BranchId.HasValue && contract.PickupBranchId != staff.BranchId.Value)
                throw new NotFoundException("Contract was not found.");
        }
        return ToDto(contract);
    }

    public async Task<ContractDto> ReturnAsync(int contractId, ReturnVehicleRequest request)
    {
        if (contractId <= 0) throw new NotFoundException("Contract was not found.");
        returnValidator.Validate(request);
        var staff = await RequireStaffAsync(requireEmployee: true);
        var contract = await unitOfWork.Contracts.GetByIdAsync(contractId)
            ?? throw new NotFoundException("Contract was not found.");
        if (contract.ReservationId is not int reservationId)
            throw new ConflictException("CONTRACT_RESERVATION_REQUIRED", "Only reservation-based contracts can be returned through this operation.");
        var reservation = await unitOfWork.Reservations.GetByIdAsync(reservationId)
            ?? throw new NotFoundException("The contract reservation was not found.");
        if (staff.BranchId.HasValue && reservation.ReturnBranchId != staff.BranchId.Value)
            throw new ForbiddenException("You are not allowed to return vehicles to another branch.");
        if (contract.Status != ContractStatus.Active)
            throw new ConflictException("CONTRACT_NOT_ACTIVE", "Only active contracts can be returned.");
        if (reservation.Status != ReservationStatus.Confirmed)
            throw new ConflictException("RESERVATION_STATE_CONFLICT", "The reservation is not in a returnable state.");

        var requestedReturnAt = request.ActualReturnDate?.UtcDateTime ?? dateTimeProvider.UtcNow;
        var returnAt = DateTime.SpecifyKind(
            new DateTime(requestedReturnAt.Ticks - requestedReturnAt.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
        if (returnAt < contract.ActualPickupDate)
            throw new BusinessRuleException("RETURN_BEFORE_PICKUP", "Actual return date cannot be before pickup.");
        var finalDays = RentalPriceCalculator.CalculateDays(contract.ActualPickupDate, returnAt);
        var finalBasic = RentalPriceCalculator.CalculateFinalBasic(contract.BasicRentalPrice, contract.RentalDays, finalDays);
        var oldValue = ToDto(contract);
        contract.Complete(staff.EmployeeId!.Value, returnAt, request.ReturnMileage, request.ReturnFuelLevel,
            request.ConditionNotes, finalDays, finalBasic, request.FuelCharges, request.DamageCharges);

        await unitOfWork.BeginTransactionAsync();
        try
        {
            await unitOfWork.Contracts.CompleteAsync(contract, reservation.ReturnBranchId, request.RequiresMaintenance);
            await unitOfWork.CommitAsync();
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }

        var persisted = await unitOfWork.Contracts.GetByIdAsync(contractId)
            ?? throw new InvalidOperationException("The returned contract could not be reloaded.");
        await auditLogger.LogAsync(AuditActions.RentalContractReturned, "RentalContracts",
            persisted.ContractId.ToString(CultureInfo.InvariantCulture), oldValue, ToDto(persisted), currentUser.UserId);
        return ToDto(persisted);
    }

    public async Task<PagedResult<ContractDto>> ListAsync(PagedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var staff = await RequireStaffAsync(requireEmployee: false);
        var (items, count) = await unitOfWork.Contracts.ListAsync(staff.BranchId, request.Page, request.PageSize);
        return new PagedResult<ContractDto>(items.Select(ToDto).ToArray(), request.Page, request.PageSize, count);
    }

    public async Task<PagedResult<ContractDto>> ListMineAsync(PagedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var customerId = await currentUser.GetCustomerIdAsync();
        var (items, count) = await unitOfWork.Contracts.ListByCustomerAsync(customerId, request.Page, request.PageSize);
        return new PagedResult<ContractDto>(items.Select(ToDto).ToArray(), request.Page, request.PageSize, count);
    }

    private async Task<StaffContext> RequireStaffAsync(bool requireEmployee)
    {
        if (!currentUser.IsInRole(RoleNames.Admin) && !currentUser.IsInRole(RoleNames.Employee) && !currentUser.IsInRole(RoleNames.BranchManager))
            throw new ForbiddenException("Only staff can access this operation.");
        int? employeeId = null;
        if (requireEmployee)
            employeeId = await currentUser.GetEmployeeIdAsync();
        else if (currentUser.IsInRole(RoleNames.Employee) || currentUser.IsInRole(RoleNames.BranchManager))
            employeeId = await currentUser.GetEmployeeIdAsync();

        int? branchId = currentUser.IsInRole(RoleNames.Admin) ? null : await currentUser.GetBranchIdAsync();
        return new StaffContext(branchId, employeeId);
    }

    private static ContractDto ToDto(RentalContract contract) => new(contract.ContractId, contract.ReservationId,
        contract.ContractNumber, contract.CustomerId, contract.VehicleId, contract.EmployeeId,
        DateTime.SpecifyKind(contract.ActualPickupDate, DateTimeKind.Utc), contract.PickupMileage,
        contract.PickupFuelLevel.ToString(), contract.ConditionNotes, contract.RentalDays,
        contract.BasicRentalPrice, contract.DiscountAmount, contract.AdditionalFees, contract.FuelCharges,
        contract.DamageCharges, contract.TotalAmount, contract.Status.ToString(), contract.PickupBranchId,
        contract.ActualReturnDate is null ? null : DateTime.SpecifyKind(contract.ActualReturnDate.Value, DateTimeKind.Utc),
        contract.ReturnMileage, contract.ReturnFuelLevel?.ToString());

    private sealed record StaffContext(int? BranchId, int? EmployeeId);
}
