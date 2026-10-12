namespace _CarRental.Application.DTOs.Contracts;

public sealed record ContractDto(
    int ContractId, int? ReservationId, string ContractNumber, int CustomerId, int VehicleId,
    int EmployeeId, DateTime ActualPickupDateUtc, int PickupMileage, string PickupFuelLevel,
    string? ConditionNotes, int RentalDays, decimal BasicRentalPrice, decimal DiscountAmount,
    decimal AdditionalFees, decimal FuelCharges, decimal DamageCharges, decimal TotalAmount,
    string Status, int PickupBranchId, DateTime? ActualReturnDateUtc = null,
    int? ReturnMileage = null, string? ReturnFuelLevel = null);
