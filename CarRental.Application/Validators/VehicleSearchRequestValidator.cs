using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.DTOs.Vehicles;
using _CarRental.Domain.Exceptions;

namespace _CarRental.Application.Validators;

public sealed class VehicleSearchRequestValidator(IDateTimeProvider dateTimeProvider)
{
    private static readonly HashSet<string> AllowedSortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "price", "year", "make", "model"
    };

    public VehicleSearchRequest Validate(VehicleSearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.CategoryId is <= 0 || request.FuelTypeId is <= 0 || request.TransmissionTypeId is <= 0 || request.BranchId is <= 0)
            throw Invalid("Lookup and branch filters must be positive.");
        if (request.MinimumSeats is <= 0)
            throw Invalid("MinimumSeats must be positive.");
        if (request.MinimumDailyPrice is < 0 or > 9999999999999999.99m || request.MaximumDailyPrice is < 0 or > 9999999999999999.99m)
            throw Invalid("Price filters must fit DECIMAL(18,2) and cannot be negative.");
        if (request.MinimumDailyPrice is not null && decimal.Round(request.MinimumDailyPrice.Value, 2) != request.MinimumDailyPrice.Value ||
            request.MaximumDailyPrice is not null && decimal.Round(request.MaximumDailyPrice.Value, 2) != request.MaximumDailyPrice.Value)
            throw Invalid("Price filters can have at most two decimal places.");
        if (request.MinimumDailyPrice > request.MaximumDailyPrice)
            throw Invalid("MinimumDailyPrice cannot exceed MaximumDailyPrice.");

        if (request.PickupDateTime.HasValue != request.ExpectedReturnDateTime.HasValue)
            throw Invalid("PickupDateTime and ExpectedReturnDateTime must be supplied together.");

        DateTime? pickupUtc = null;
        DateTime? returnUtc = null;
        if (request.PickupDateTime is { } pickup && request.ExpectedReturnDateTime is { } expectedReturn)
        {
            if (pickup <= new DateTimeOffset(DateTime.SpecifyKind(dateTimeProvider.UtcNow, DateTimeKind.Utc)))
                throw Invalid("PickupDateTime must be in the future.");
            if (expectedReturn <= pickup)
                throw Invalid("ExpectedReturnDateTime must be after PickupDateTime.");
            pickupUtc = pickup.UtcDateTime;
            returnUtc = expectedReturn.UtcDateTime;
        }

        var sortBy = string.IsNullOrWhiteSpace(request.SortBy) ? "price" : request.SortBy.Trim().ToLowerInvariant();
        if (!AllowedSortFields.Contains(sortBy))
            throw Invalid("SortBy must be one of: price, year, make, model.");
        var sortDirection = string.IsNullOrWhiteSpace(request.SortDirection) ? "asc" : request.SortDirection.Trim().ToLowerInvariant();
        if (sortDirection is not ("asc" or "desc"))
            throw Invalid("SortDirection must be 'asc' or 'desc'.");

        return new VehicleSearchRequest
        {
            CategoryId = request.CategoryId,
            FuelTypeId = request.FuelTypeId,
            TransmissionTypeId = request.TransmissionTypeId,
            BranchId = request.BranchId,
            MinimumDailyPrice = request.MinimumDailyPrice,
            MaximumDailyPrice = request.MaximumDailyPrice,
            MinimumSeats = request.MinimumSeats,
            PickupDateTime = pickupUtc is null ? null : new DateTimeOffset(pickupUtc.Value, TimeSpan.Zero),
            ExpectedReturnDateTime = returnUtc is null ? null : new DateTimeOffset(returnUtc.Value, TimeSpan.Zero),
            Page = request.Page,
            PageSize = request.PageSize,
            SortBy = sortBy,
            SortDirection = sortDirection
        };
    }

    private static BusinessRuleException Invalid(string message) => new("VALIDATION_FAILED", message);


}
