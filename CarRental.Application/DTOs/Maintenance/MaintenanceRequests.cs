namespace _CarRental.Application.DTOs.Maintenance;

public sealed record CreateMaintenanceRequest(int VehicleId, string MaintenanceType, string? ProblemDescription,
    DateOnly StartDate, DateOnly? ExpectedEndDate);
public sealed record UpdateMaintenanceRequest(string MaintenanceType, string? ProblemDescription, DateOnly? ExpectedEndDate);
public sealed record CompleteMaintenanceRequest(decimal? Cost);
public sealed record MaintenanceDto(int MaintenanceId, int VehicleId, int BranchId, int CreatedByEmployeeId,
    string MaintenanceType, string? ProblemDescription, DateOnly StartDate, DateOnly? ExpectedEndDate,
    DateOnly? ActualEndDate, decimal? Cost, string Status);
