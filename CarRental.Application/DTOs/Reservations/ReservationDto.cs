namespace _CarRental.Application.DTOs.Reservations;

public sealed record ReservationDto(
    int ReservationId,
    int CustomerId,
    int VehicleId,
    int PickupBranchId,
    int ReturnBranchId,
    DateTime PickupDateTimeUtc,
    DateTime ExpectedReturnDateTimeUtc,
    string Status,
    DateTime CreatedAtUtc);
