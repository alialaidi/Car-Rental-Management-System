namespace _CarRental.Domain.Services;

public static class RentalPriceCalculator
{
    public static int CalculateDays(DateTime actualPickup, DateTime expectedReturn)
    {
        return Math.Max(1, checked((int)Math.Ceiling((expectedReturn - actualPickup).TotalDays)));
    }

    public static decimal CalculateBasic(decimal dailyPrice, int rentalDays)
    {
        if (dailyPrice < 0) throw new ArgumentOutOfRangeException(nameof(dailyPrice));
        if (rentalDays <= 0) throw new ArgumentOutOfRangeException(nameof(rentalDays));
        return decimal.Round(dailyPrice * rentalDays, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal CalculateTotal(decimal basic, decimal additionalFees, decimal discount)
    {
        if (basic < 0 || additionalFees < 0 || discount < 0)
            throw new ArgumentOutOfRangeException(nameof(discount), "Amounts cannot be negative.");
        var total = decimal.Round(basic + additionalFees - discount, 2, MidpointRounding.AwayFromZero);
        if (total < 0) throw new ArgumentOutOfRangeException(nameof(discount), "Discount cannot exceed the rental price and fees.");
        return total;
    }

    public static decimal CalculateFinalBasic(decimal originalBasic, int originalDays, int finalDays)
    {
        if (originalBasic < 0) throw new ArgumentOutOfRangeException(nameof(originalBasic));
        if (originalDays <= 0) throw new ArgumentOutOfRangeException(nameof(originalDays));
        if (finalDays <= 0) throw new ArgumentOutOfRangeException(nameof(finalDays));

        var dailyPriceSnapshot = originalBasic / originalDays;
        return decimal.Round(dailyPriceSnapshot * finalDays, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal CalculateFinalTotal(
        decimal basic, decimal additionalFees, decimal fuelCharges, decimal damageCharges, decimal discount)
    {
        if (basic < 0 || additionalFees < 0 || fuelCharges < 0 || damageCharges < 0 || discount < 0)
            throw new ArgumentOutOfRangeException(nameof(discount), "Amounts cannot be negative.");

        var total = decimal.Round(basic + additionalFees + fuelCharges + damageCharges - discount,
            2, MidpointRounding.AwayFromZero);
        if (total < 0)
            throw new ArgumentOutOfRangeException(nameof(discount), "Discount cannot exceed the final rental charges.");
        return total;
    }
}
