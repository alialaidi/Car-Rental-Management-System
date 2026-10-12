namespace _CarRental.Application.DTOs.Reservations;

public sealed class CreateReservationRequest
{
    public int VehicleId { get; init; }
    public int PickupBranchId { get; init; }
    public int ReturnBranchId { get; init; }
    public DateTimeOffset PickupDateTime { get; init; }
    public DateTimeOffset ExpectedReturnDateTime { get; init; }
}
