using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.DTOs.Reservations;
using _CarRental.Application.Validators;
using _CarRental.Domain.Exceptions;
using Xunit;

namespace _CarRental.Tests;

public sealed class CreateReservationRequestValidatorTests
{
    private static readonly DateTime Now = new(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc);
    private readonly CreateReservationRequestValidator _validator = new(new FixedDateTimeProvider(Now));

    [Fact]
    public void NormalizesValidDatesToUtc()
    {
        var pickup = new DateTimeOffset(2026, 10, 12, 15, 0, 0, TimeSpan.FromHours(3));
        var result = _validator.Validate(new CreateReservationRequest
        {
            VehicleId = 1,
            PickupBranchId = 2,
            ReturnBranchId = 3,
            PickupDateTime = pickup,
            ExpectedReturnDateTime = pickup.AddDays(2)
        });

        Assert.Equal(pickup.UtcDateTime, result.PickupDateTimeUtc);
        Assert.Equal(pickup.AddDays(2).UtcDateTime, result.ExpectedReturnDateTimeUtc);
    }

    [Fact]
    public void RejectsInvalidIdsAndInvalidOrTooSoonDates()
    {
        Assert.Throws<BusinessRuleException>(() => _validator.Validate(new CreateReservationRequest
        {
            VehicleId = 0, PickupBranchId = 2, ReturnBranchId = 3,
            PickupDateTime = new DateTimeOffset(Now.AddHours(1)),
            ExpectedReturnDateTime = new DateTimeOffset(Now.AddDays(1))
        }));
        Assert.Throws<BusinessRuleException>(() => _validator.Validate(new CreateReservationRequest
        {
            VehicleId = 1, PickupBranchId = 2, ReturnBranchId = 3,
            PickupDateTime = new DateTimeOffset(Now.AddMinutes(4)),
            ExpectedReturnDateTime = new DateTimeOffset(Now.AddDays(1))
        }));
        Assert.Throws<BusinessRuleException>(() => _validator.Validate(new CreateReservationRequest
        {
            VehicleId = 1, PickupBranchId = 2, ReturnBranchId = 3,
            PickupDateTime = new DateTimeOffset(Now.AddDays(1)),
            ExpectedReturnDateTime = new DateTimeOffset(Now.AddDays(1))
        }));
    }

    private sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
