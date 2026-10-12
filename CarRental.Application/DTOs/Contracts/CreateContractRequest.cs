using _CarRental.Domain.Enums;

namespace _CarRental.Application.DTOs.Contracts;

public sealed record CreateContractRequest(
    int ReservationId,
    int? PickupMileage,
    FuelLevel PickupFuelLevel,
    string? ConditionNotes,
    decimal DiscountAmount = 0,
    decimal AdditionalFees = 0);
