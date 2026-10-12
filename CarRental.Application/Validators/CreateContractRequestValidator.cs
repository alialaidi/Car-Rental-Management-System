using _CarRental.Application.DTOs.Contracts;
using _CarRental.Domain.Exceptions;

namespace _CarRental.Application.Validators;

public sealed class CreateContractRequestValidator
{
    public void Validate(CreateContractRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ReservationId <= 0 || request.PickupMileage is < 0)
            throw new BusinessRuleException("VALIDATION_FAILED", "Reservation id must be positive and pickup mileage cannot be negative.");
        if (!Enum.IsDefined(request.PickupFuelLevel))
            throw new BusinessRuleException("VALIDATION_FAILED", "Pickup fuel level is invalid.");
        if (request.ConditionNotes?.Length > 1000)
            throw new BusinessRuleException("VALIDATION_FAILED", "Condition notes cannot exceed 1000 characters.");
        if (request.DiscountAmount < 0 || request.AdditionalFees < 0)
            throw new BusinessRuleException("VALIDATION_FAILED", "Discount and additional fees cannot be negative.");
    }
}
