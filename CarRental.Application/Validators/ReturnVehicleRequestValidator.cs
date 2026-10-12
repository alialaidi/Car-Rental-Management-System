using _CarRental.Application.DTOs.Contracts;
using _CarRental.Domain.Exceptions;

namespace _CarRental.Application.Validators;

public sealed class ReturnVehicleRequestValidator
{
    public void Validate(ReturnVehicleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ReturnMileage < 0)
            throw new BusinessRuleException("VALIDATION_FAILED", "Return mileage cannot be negative.");
        if (!Enum.IsDefined(request.ReturnFuelLevel))
            throw new BusinessRuleException("VALIDATION_FAILED", "Return fuel level is invalid.");
        if (request.FuelCharges < 0 || request.DamageCharges < 0)
            throw new BusinessRuleException("VALIDATION_FAILED", "Fuel and damage charges cannot be negative.");
        if (request.ConditionNotes?.Length > 1000)
            throw new BusinessRuleException("VALIDATION_FAILED", "Return condition notes cannot exceed 1000 characters.");
    }
}
