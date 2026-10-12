using _CarRental.Domain.Entities;
using _CarRental.Domain.Enums;
using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence.Mappers;

internal static class RentalContractMapper
{
    public static RentalContract Map(SqlDataReader reader) => new()
    {
        ContractId = reader.GetInt32(reader.GetOrdinal("ContractId")),
        ReservationId = NullableInt(reader, "ReservationId"),
        ContractNumber = reader.GetString(reader.GetOrdinal("ContractNumber")),
        CustomerId = reader.GetInt32(reader.GetOrdinal("CustomerId")),
        VehicleId = reader.GetInt32(reader.GetOrdinal("VehicleId")),
        EmployeeId = reader.GetInt32(reader.GetOrdinal("EmployeeId")),
        ActualPickupDate = reader.GetDateTime(reader.GetOrdinal("ActualPickupDate")),
        ActualReturnDate = NullableDateTime(reader, "ActualReturnDate"),
        PickupMileage = reader.GetInt32(reader.GetOrdinal("PickupMileage")),
        ReturnMileage = NullableInt(reader, "ReturnMileage"),
        PickupFuelLevel = Enum.Parse<FuelLevel>(reader.GetString(reader.GetOrdinal("PickupFuelLevel")), false),
        ReturnFuelLevel = NullableString(reader, "ReturnFuelLevel") is { } returnFuelLevel
            ? Enum.Parse<FuelLevel>(returnFuelLevel, false) : null,
        ConditionNotes = NullableString(reader, "ConditionNotes"),
        RentalDays = reader.GetInt32(reader.GetOrdinal("RentalDays")),
        BasicRentalPrice = reader.GetDecimal(reader.GetOrdinal("BasicRentalPrice")),
        DiscountAmount = reader.GetDecimal(reader.GetOrdinal("DiscountAmount")),
        AdditionalFees = reader.GetDecimal(reader.GetOrdinal("AdditionalFees")),
        FuelCharges = reader.GetDecimal(reader.GetOrdinal("FuelCharges")),
        DamageCharges = reader.GetDecimal(reader.GetOrdinal("DamageCharges")),
        TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
        Status = Enum.Parse<ContractStatus>(reader.GetString(reader.GetOrdinal("Status")), false),
        PickupBranchId = reader.GetInt32(reader.GetOrdinal("PickupBranchId"))
    };

    private static int? NullableInt(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    private static string? NullableString(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static DateTime? NullableDateTime(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }
}
