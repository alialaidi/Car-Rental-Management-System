using _CarRental.Domain.Entities;

namespace _CarRental.Domain.ValueObjects;

public sealed record VehicleSearchCriteria(
    int? CategoryId,
    int? FuelTypeId,
    int? TransmissionTypeId,
    int? BranchId,
    decimal? MinimumDailyPrice,
    decimal? MaximumDailyPrice,
    int? MinimumSeats,
    DateTime? PickupDateTimeUtc,
    DateTime? ExpectedReturnDateTimeUtc,
    int Page,
    int PageSize,
    string SortBy,
    string SortDirection);

public sealed record VehicleCatalogItem(
    Vehicle Vehicle,
    string CategoryName,
    string FuelTypeName,
    string TransmissionTypeName,
    string? PrimaryImageUrl);

public sealed record VehicleCatalogPage(IReadOnlyList<VehicleCatalogItem> Items, int TotalCount);

public sealed record VehicleCatalogDetails(VehicleCatalogItem CatalogItem, IReadOnlyList<VehicleImage> Images);
