using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.DTOs.Reservations;
using _CarRental.Domain.Exceptions;

namespace _CarRental.Application.Validators;

public sealed class CreateReservationRequestValidator(IDateTimeProvider dateTimeProvider)
{
    private static readonly TimeSpan MinimumPickupNotice = TimeSpan.FromMinutes(5);

    public ValidatedReservationRequest Validate(CreateReservationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.VehicleId <= 0 || request.PickupBranchId <= 0 || request.ReturnBranchId <= 0)
            throw new BusinessRuleException("VALIDATION_FAILED", "Vehicle and branch identifiers must be positive.");

        var pickup = request.PickupDateTime.UtcDateTime;
        var expectedReturn = request.ExpectedReturnDateTime.UtcDateTime;
        if (pickup < dateTimeProvider.UtcNow.Add(MinimumPickupNotice))
            throw new BusinessRuleException("VALIDATION_FAILED", "Pickup must be at least five minutes in the future.");
        if (expectedReturn <= pickup)
            throw new BusinessRuleException("VALIDATION_FAILED", "Expected return must be after pickup.");

        return new ValidatedReservationRequest(request.VehicleId, request.PickupBranchId, request.ReturnBranchId, pickup, expectedReturn);
    }
}

public sealed record ValidatedReservationRequest(
    int VehicleId,
    int PickupBranchId,
    int ReturnBranchId,
    DateTime PickupDateTimeUtc,
    DateTime ExpectedReturnDateTimeUtc);
