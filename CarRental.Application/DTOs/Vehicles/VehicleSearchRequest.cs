using _CarRental.Application.Common.Models;

namespace _CarRental.Application.DTOs.Vehicles;

public sealed class VehicleSearchRequest : PagedRequest
{
    public int? CategoryId { get; init; }
    public int? FuelTypeId { get; init; }
    public int? TransmissionTypeId { get; init; }
    public int? BranchId { get; init; }
    public decimal? MinimumDailyPrice { get; init; }
    public decimal? MaximumDailyPrice { get; init; }
    public int? MinimumSeats { get; init; }
    public DateTimeOffset? PickupDateTime { get; init; }
    public DateTimeOffset? ExpectedReturnDateTime { get; init; }
    public string? SortBy { get; init; }
    public string? SortDirection { get; init; }
}
