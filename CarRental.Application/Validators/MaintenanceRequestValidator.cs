using _CarRental.Application.DTOs.Maintenance;
using _CarRental.Domain.Exceptions;

namespace _CarRental.Application.Validators;

public sealed class MaintenanceRequestValidator
{
    public void Validate(CreateMaintenanceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateDetails(request.VehicleId, request.MaintenanceType, request.ProblemDescription, request.StartDate, request.ExpectedEndDate);
    }

    public void Validate(UpdateMaintenanceRequest request, DateOnly startDate)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateDetails(1, request.MaintenanceType, request.ProblemDescription, startDate, request.ExpectedEndDate);
    }

    public void Validate(CompleteMaintenanceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Cost < 0) throw new BusinessRuleException("VALIDATION_FAILED", "Maintenance cost cannot be negative.");
    }

    private static void ValidateDetails(int vehicleId, string? type, string? description, DateOnly start, DateOnly? end)
    {
        if (vehicleId <= 0 || string.IsNullOrWhiteSpace(type) || type.Trim().Length > 100
            || description?.Length > 1000 || start == default || (end.HasValue && end.Value < start))
            throw new BusinessRuleException("VALIDATION_FAILED", "Maintenance details are invalid.");
    }
}
