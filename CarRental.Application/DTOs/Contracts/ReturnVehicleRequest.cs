using _CarRental.Domain.Enums;

namespace _CarRental.Application.DTOs.Contracts;

public sealed record ReturnVehicleRequest(
    int ReturnMileage,
    FuelLevel ReturnFuelLevel,
    decimal FuelCharges = 0,
    decimal DamageCharges = 0,
    string? ConditionNotes = null,
    bool RequiresMaintenance = false,
    DateTimeOffset? ActualReturnDate = null);
