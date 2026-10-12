using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.DTOs.Vehicles;
using _CarRental.Application.Validators;
using _CarRental.Domain.Exceptions;
using Xunit;

namespace _CarRental.Tests;

public sealed class VehicleSearchRequestValidatorTests
{
    private static readonly DateTime Now = new(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc);
    private readonly VehicleSearchRequestValidator _validator = new(new FixedDateTimeProvider(Now));

    [Fact]
    public void AppliesDefaultSortAndNormalizesAllowedSortValues()
    {
        var request = _validator.Validate(new VehicleSearchRequest());
        Assert.Equal("price", request.SortBy);
        Assert.Equal("asc", request.SortDirection);
        Assert.Equal(1, request.Page);
        Assert.Equal(20, request.PageSize);

        var sorted = _validator.Validate(new VehicleSearchRequest { SortBy = " MAKE ", SortDirection = "DESC" });
        Assert.Equal("make", sorted.SortBy);
        Assert.Equal("desc", sorted.SortDirection);
    }

    [Fact]
    public void RejectsInvalidPriceRange()
    {
        Assert.Throws<BusinessRuleException>(() => _validator.Validate(new VehicleSearchRequest
        {
            MinimumDailyPrice = 50,
            MaximumDailyPrice = 49
        }));
    }

    [Fact]
    public void RejectsNegativeFiltersAndUnsupportedSortFields()
    {
        Assert.Throws<BusinessRuleException>(() => _validator.Validate(new VehicleSearchRequest { MinimumSeats = 0 }));
        Assert.Throws<BusinessRuleException>(() => _validator.Validate(new VehicleSearchRequest { MinimumDailyPrice = -1 }));
        Assert.Throws<BusinessRuleException>(() => _validator.Validate(new VehicleSearchRequest { SortBy = "dailyPrice DESC; DROP TABLE Vehicles" }));
    }

    [Fact]
    public void RequiresBothDatesAndAnIncreasingFutureWindow()
    {
        var pickup = new DateTimeOffset(Now.AddDays(1));
        Assert.Throws<BusinessRuleException>(() => _validator.Validate(new VehicleSearchRequest { PickupDateTime = pickup }));
        Assert.Throws<BusinessRuleException>(() => _validator.Validate(new VehicleSearchRequest
        {
            PickupDateTime = new DateTimeOffset(Now.AddHours(-1)),
            ExpectedReturnDateTime = new DateTimeOffset(Now.AddDays(1))
        }));
        Assert.Throws<BusinessRuleException>(() => _validator.Validate(new VehicleSearchRequest
        {
            PickupDateTime = pickup,
            ExpectedReturnDateTime = pickup
        }));
    }

    [Fact]
    public void NormalizesValidWindowToUtc()
    {
        var pickup = new DateTimeOffset(2026, 10, 12, 15, 0, 0, TimeSpan.FromHours(3));
        var result = _validator.Validate(new VehicleSearchRequest
        {
            PickupDateTime = pickup,
            ExpectedReturnDateTime = pickup.AddDays(2)
        });

        Assert.Equal(pickup.UtcDateTime, result.PickupDateTime!.Value.UtcDateTime);
        Assert.Equal(pickup.AddDays(2).UtcDateTime, result.ExpectedReturnDateTime!.Value.UtcDateTime);
    }

    private sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
