using _CarRental.Domain.Enums;
using _CarRental.Domain.Exceptions;
using _CarRental.Domain.Services;

namespace _CarRental.Domain.Entities;

public sealed class RentalContract
{
    public int ContractId { get; set; }
    public int? ReservationId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public int VehicleId { get; set; }
    public int EmployeeId { get; set; }
    public int? ReturnedByEmployeeId { get; set; }
    public DateTime ActualPickupDate { get; set; }
    public DateTime? ActualReturnDate { get; set; }
    public int PickupMileage { get; set; }
    public int? ReturnMileage { get; set; }
    public FuelLevel PickupFuelLevel { get; set; }
    public FuelLevel? ReturnFuelLevel { get; set; }
    public string? ConditionNotes { get; set; }
    public int RentalDays { get; set; }
    public decimal BasicRentalPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal AdditionalFees { get; set; }
    public decimal FuelCharges { get; set; }
    public decimal DamageCharges { get; set; }
    public decimal TotalAmount { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.Active;
    public int PickupBranchId { get; set; }

    public static RentalContract Create(int reservationId, int customerId, int vehicleId, int employeeId,
        DateTime pickupAt, int pickupMileage, FuelLevel fuelLevel, string? conditionNotes,
        int rentalDays, decimal basic, decimal discount, decimal additionalFees)
    {
        if (reservationId <= 0 || customerId <= 0 || vehicleId <= 0 || employeeId <= 0)
            throw new BusinessRuleException("Contract identifiers must be positive.");
        if (pickupMileage < 0 || rentalDays <= 0)
            throw new BusinessRuleException("Pickup mileage must be non-negative and rental days must be positive.");
        if (conditionNotes?.Length > 1000)
            throw new BusinessRuleException("Condition notes cannot exceed 1000 characters.");
        decimal total;
        try { total = RentalPriceCalculator.CalculateTotal(basic, additionalFees, discount); }
        catch (ArgumentOutOfRangeException ex) { throw new BusinessRuleException("INVALID_CONTRACT_AMOUNT", ex.Message); }

        return new RentalContract
        {
            ReservationId = reservationId, CustomerId = customerId, VehicleId = vehicleId, EmployeeId = employeeId,
            ActualPickupDate = pickupAt, PickupMileage = pickupMileage, PickupFuelLevel = fuelLevel,
            ConditionNotes = conditionNotes, RentalDays = rentalDays, BasicRentalPrice = basic,
            DiscountAmount = discount, AdditionalFees = additionalFees, TotalAmount = total,
            Status = ContractStatus.Active
        };
    }

    public void Complete(int returnedByEmployeeId, DateTime returnedAt, int returnMileage,
        FuelLevel returnFuelLevel, string? returnConditionNotes, int finalRentalDays,
        decimal finalBasicRentalPrice, decimal fuelCharges, decimal damageCharges)
    {
        if (Status != ContractStatus.Active)
            throw new BusinessRuleException("CONTRACT_NOT_ACTIVE", "Only active contracts can be returned.");
        if (returnedByEmployeeId <= 0)
            throw new BusinessRuleException("INVALID_EMPLOYEE", "Returning employee id must be positive.");
        if (returnedAt < ActualPickupDate)
            throw new BusinessRuleException("RETURN_BEFORE_PICKUP", "Actual return date cannot be before pickup.");
        if (returnMileage < PickupMileage)
            throw new BusinessRuleException("INVALID_RETURN_MILEAGE", "Return mileage cannot be less than pickup mileage.");
        if (finalRentalDays <= 0)
            throw new BusinessRuleException("INVALID_RENTAL_DAYS", "Final rental days must be positive.");
        if (returnConditionNotes?.Length > 1000)
            throw new BusinessRuleException("VALIDATION_FAILED", "Return condition notes cannot exceed 1000 characters.");

        var combinedNotes = CombineConditionNotes(ConditionNotes, returnConditionNotes);
        if (combinedNotes?.Length > 1000)
            throw new BusinessRuleException("VALIDATION_FAILED", "Combined pickup and return condition notes cannot exceed 1000 characters.");
        decimal total;
        try { total = RentalPriceCalculator.CalculateFinalTotal(finalBasicRentalPrice, AdditionalFees, fuelCharges, damageCharges, DiscountAmount); }
        catch (ArgumentOutOfRangeException ex) { throw new BusinessRuleException("INVALID_CONTRACT_AMOUNT", ex.Message); }

        RentalDays = finalRentalDays;
        BasicRentalPrice = finalBasicRentalPrice;
        FuelCharges = fuelCharges;
        DamageCharges = damageCharges;
        TotalAmount = total;
        ActualReturnDate = returnedAt;
        ReturnMileage = returnMileage;
        ReturnFuelLevel = returnFuelLevel;
        ReturnedByEmployeeId = returnedByEmployeeId;
        ConditionNotes = combinedNotes;
        Status = ContractStatus.Completed;
    }

    private static string? CombineConditionNotes(string? pickupNotes, string? returnNotes)
    {
        if (string.IsNullOrWhiteSpace(returnNotes)) return pickupNotes;
        if (string.IsNullOrWhiteSpace(pickupNotes)) return $"Return: {returnNotes}";
        return $"{pickupNotes}\nReturn: {returnNotes}";
    }
}
