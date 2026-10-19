using _CarRental.Domain.Enums;
using _CarRental.Domain.Exceptions;

namespace _CarRental.Domain.Entities;

public sealed class MaintenanceRecord
{
    public int MaintenanceId { get; set; }
    public int VehicleId { get; set; }
    public int BranchId { get; set; }
    public int CreatedByEmployeeId { get; set; }
    public string MaintenanceType { get; private set; } = string.Empty;
    public string? ProblemDescription { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? ExpectedEndDate { get; private set; }
    public DateOnly? ActualEndDate { get; private set; }
    public decimal? Cost { get; private set; }
    public MaintenanceStatus Status { get; private set; } = MaintenanceStatus.InProgress;

    public static MaintenanceRecord Open(int vehicleId, int branchId, int createdByEmployeeId,
        string maintenanceType, string? problemDescription, DateOnly startDate, DateOnly? expectedEndDate)
    {
        ValidateDetails(maintenanceType, problemDescription, startDate, expectedEndDate);
        if (vehicleId <= 0 || branchId <= 0 || createdByEmployeeId <= 0)
            throw new BusinessRuleException("VALIDATION_FAILED", "Vehicle, branch, and employee identifiers must be positive.");
        return new MaintenanceRecord
        {
            VehicleId = vehicleId, BranchId = branchId, CreatedByEmployeeId = createdByEmployeeId,
            MaintenanceType = maintenanceType.Trim(), ProblemDescription = Normalize(problemDescription),
            StartDate = startDate, ExpectedEndDate = expectedEndDate, Status = MaintenanceStatus.InProgress
        };
    }

    public void UpdateDetails(string maintenanceType, string? problemDescription, DateOnly? expectedEndDate)
    {
        EnsureInProgress();
        ValidateDetails(maintenanceType, problemDescription, StartDate, expectedEndDate);
        MaintenanceType = maintenanceType.Trim();
        ProblemDescription = Normalize(problemDescription);
        ExpectedEndDate = expectedEndDate;
    }

    public void Complete(DateOnly actualEndDate, decimal? cost)
    {
        EnsureInProgress();
        if (actualEndDate < StartDate || cost < 0)
            throw new BusinessRuleException("VALIDATION_FAILED", "Actual end date cannot precede start date and cost cannot be negative.");
        ActualEndDate = actualEndDate;
        Cost = cost;
        Status = MaintenanceStatus.Completed;
    }

    public void Cancel()
    {
        EnsureInProgress();
        Status = MaintenanceStatus.Cancelled;
    }

    public static MaintenanceRecord FromPersistence(int maintenanceId, int vehicleId, int branchId,
        int createdByEmployeeId, string maintenanceType, string? problemDescription, DateOnly startDate,
        DateOnly? expectedEndDate, DateOnly? actualEndDate, decimal? cost, MaintenanceStatus status) => new()
    {
        MaintenanceId = maintenanceId, VehicleId = vehicleId, BranchId = branchId,
        CreatedByEmployeeId = createdByEmployeeId, MaintenanceType = maintenanceType,
        ProblemDescription = problemDescription, StartDate = startDate, ExpectedEndDate = expectedEndDate,
        ActualEndDate = actualEndDate, Cost = cost, Status = status
    };

    private void EnsureInProgress()
    {
        if (Status != MaintenanceStatus.InProgress)
            throw new ConflictException("MAINTENANCE_NOT_IN_PROGRESS", "Completed or cancelled maintenance records cannot be changed.");
    }

    private static void ValidateDetails(string maintenanceType, string? problemDescription, DateOnly startDate, DateOnly? expectedEndDate)
    {
        if (string.IsNullOrWhiteSpace(maintenanceType) || maintenanceType.Trim().Length > 100
            || problemDescription?.Length > 1000 || (expectedEndDate.HasValue && expectedEndDate.Value < startDate))
            throw new BusinessRuleException("VALIDATION_FAILED", "Maintenance details are invalid.");
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
