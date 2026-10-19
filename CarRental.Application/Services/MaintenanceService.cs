using System.Globalization;
using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Common.Models;
using _CarRental.Application.DTOs.Maintenance;
using _CarRental.Application.Validators;
using _CarRental.Domain.Constants;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Enums;
using _CarRental.Domain.Exceptions;
using _CarRental.Domain.Interfaces.Repositories;

namespace _CarRental.Application.Services;

public sealed class MaintenanceService(IUnitOfWork unitOfWork, ICurrentUser currentUser, IAuditLogger auditLogger,
    IDateTimeProvider dateTimeProvider, MaintenanceRequestValidator validator) : IMaintenanceService
{
    public async Task<MaintenanceDto> CreateAsync(CreateMaintenanceRequest request)
    {
        validator.Validate(request);
        var actor = await RequireStaffAsync(requireEmployee: true);
        var vehicle = await unitOfWork.Vehicles.GetByIdAsync(request.VehicleId)
            ?? throw new NotFoundException("Vehicle was not found.");
        EnsureBranchScope(actor, vehicle.BranchId);
        if (vehicle.Status == VehicleStatus.Rented.ToString())
            throw new ConflictException("VEHICLE_RENTED", "Maintenance cannot begin while the vehicle is rented.");
        if (vehicle.Status is not ("Available" or "Maintenance"))
            throw new ConflictException("VEHICLE_STATE_CONFLICT", "Maintenance can begin only for an available vehicle or a vehicle already flagged for maintenance.");

        var record = MaintenanceRecord.Open(vehicle.VehicleId, vehicle.BranchId, actor.EmployeeId,
            request.MaintenanceType, request.ProblemDescription, request.StartDate, request.ExpectedEndDate);
        await unitOfWork.BeginTransactionAsync();
        try
        {
            record.MaintenanceId = await unitOfWork.Maintenance.CreateAsync(record);
            await unitOfWork.CommitAsync();
        }
        catch { await unitOfWork.RollbackAsync(); throw; }

        var saved = await RequireRecordAsync(record.MaintenanceId);
        await AuditAsync(AuditActions.MaintenanceCreated, saved.MaintenanceId, null, ToDto(saved));
        return ToDto(saved);
    }

    public async Task<MaintenanceDto> UpdateAsync(int maintenanceId, UpdateMaintenanceRequest request)
    {
        var actor = await RequireStaffAsync(requireEmployee: false);
        var record = await RequireRecordAsync(maintenanceId);
        EnsureBranchScope(actor, record.BranchId);
        validator.Validate(request, record.StartDate);
        var old = ToDto(record);
        record.UpdateDetails(request.MaintenanceType, request.ProblemDescription, request.ExpectedEndDate);
        await InTransactionAsync(() => unitOfWork.Maintenance.UpdateAsync(record));
        var updated = await RequireRecordAsync(maintenanceId);
        await AuditAsync(AuditActions.MaintenanceUpdated, maintenanceId, old, ToDto(updated));
        return ToDto(updated);
    }

    public async Task<MaintenanceDto> CompleteAsync(int maintenanceId, CompleteMaintenanceRequest request)
    {
        validator.Validate(request);
        var actor = await RequireStaffAsync(requireEmployee: false);
        var record = await RequireRecordAsync(maintenanceId);
        EnsureBranchScope(actor, record.BranchId);
        var old = ToDto(record);
        var actualEndDate = DateOnly.FromDateTime(dateTimeProvider.UtcNow);
        record.Complete(actualEndDate, request.Cost);
        await InTransactionAsync(() => unitOfWork.Maintenance.CompleteAsync(maintenanceId, actualEndDate, request.Cost));
        var updated = await RequireRecordAsync(maintenanceId);
        await AuditAsync(AuditActions.MaintenanceCompleted, maintenanceId, old, ToDto(updated));
        return ToDto(updated);
    }

    public async Task<MaintenanceDto> CancelAsync(int maintenanceId)
    {
        var actor = await RequireStaffAsync(requireEmployee: false);
        var record = await RequireRecordAsync(maintenanceId);
        EnsureBranchScope(actor, record.BranchId);
        var old = ToDto(record);
        record.Cancel();
        await InTransactionAsync(() => unitOfWork.Maintenance.CancelAsync(maintenanceId));
        var updated = await RequireRecordAsync(maintenanceId);
        await AuditAsync(AuditActions.MaintenanceCancelled, maintenanceId, old, ToDto(updated));
        return ToDto(updated);
    }

    public async Task<MaintenanceDto> GetAsync(int maintenanceId)
    {
        var actor = await RequireStaffAsync(requireEmployee: false);
        var record = await RequireRecordAsync(maintenanceId);
        EnsureBranchScope(actor, record.BranchId);
        return ToDto(record);
    }

    public async Task<IReadOnlyList<MaintenanceDto>> ListByVehicleAsync(int vehicleId)
    {
        var actor = await RequireStaffAsync(requireEmployee: false);
        var vehicle = await unitOfWork.Vehicles.GetByIdAsync(vehicleId)
            ?? throw new NotFoundException("Vehicle was not found.");
        EnsureBranchScope(actor, vehicle.BranchId);
        return (await unitOfWork.Maintenance.ListByVehicleAsync(vehicleId)).Select(ToDto).ToArray();
    }

    public async Task<PagedResult<MaintenanceDto>> ListByBranchAsync(int branchId, PagedRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var actor = await RequireStaffAsync(requireEmployee: false);
        EnsureBranchScope(actor, branchId);
        if (await unitOfWork.Branches.GetByIdAsync(branchId) is null)
            throw new NotFoundException("Branch was not found.");
        var (items, count) = await unitOfWork.Maintenance.ListByBranchAsync(branchId, request.Page, request.PageSize);
        return new PagedResult<MaintenanceDto>(items.Select(ToDto).ToArray(), request.Page, request.PageSize, count);
    }

    private async Task<MaintenanceRecord> RequireRecordAsync(int id)
    {
        if (id <= 0) throw new NotFoundException("Maintenance record was not found.");
        return await unitOfWork.Maintenance.GetByIdAsync(id)
            ?? throw new NotFoundException("Maintenance record was not found.");
    }

    private async Task<StaffContext> RequireStaffAsync(bool requireEmployee)
    {
        if (!currentUser.IsInRole(RoleNames.Admin) && !currentUser.IsInRole(RoleNames.Employee) && !currentUser.IsInRole(RoleNames.BranchManager))
            throw new ForbiddenException("Only staff can manage vehicle maintenance.");
        var employeeId = 0;
        if (requireEmployee)
        {
            if (currentUser.IsInRole(RoleNames.Admin))
            {
                var employee = await unitOfWork.Employees.GetByUserIdAsync(currentUser.UserId);
                employeeId = employee?.EmployeeId ?? throw new ForbiddenException("Maintenance creation requires an employee profile.");
            }
            else employeeId = await currentUser.GetEmployeeIdAsync();
        }
        var branchId = currentUser.IsInRole(RoleNames.Admin) ? (int?)null : await currentUser.GetBranchIdAsync();
        return new StaffContext(employeeId, branchId, currentUser.IsInRole(RoleNames.Admin));
    }

    private static void EnsureBranchScope(StaffContext actor, int branchId)
    {
        if (!actor.IsAdmin && actor.BranchId != branchId)
            throw new NotFoundException("Maintenance record was not found.");
    }

    private async Task InTransactionAsync(Func<Task> operation)
    {
        await unitOfWork.BeginTransactionAsync();
        try { await operation(); await unitOfWork.CommitAsync(); }
        catch { await unitOfWork.RollbackAsync(); throw; }
    }

    private Task AuditAsync(string action, int id, object? oldValue, object? newValue) =>
        auditLogger.LogAsync(action, "MaintenanceRecords", id.ToString(CultureInfo.InvariantCulture), oldValue, newValue, currentUser.UserId);

    private static MaintenanceDto ToDto(MaintenanceRecord r) => new(r.MaintenanceId, r.VehicleId, r.BranchId,
        r.CreatedByEmployeeId, r.MaintenanceType, r.ProblemDescription, r.StartDate, r.ExpectedEndDate,
        r.ActualEndDate, r.Cost, r.Status.ToString());

    private sealed record StaffContext(int EmployeeId, int? BranchId, bool IsAdmin);
}
