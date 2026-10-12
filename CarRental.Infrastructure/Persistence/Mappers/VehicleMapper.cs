using _CarRental.Domain.Entities;
using _CarRental.Domain.Enums;
using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence.Mappers;

internal static class VehicleMapper
{
    public static Vehicle Map(SqlDataReader reader) => new()
    {
        VehicleId = reader.GetInt32(reader.GetOrdinal("VehicleId")),
        PlateNumber = reader.GetString(reader.GetOrdinal("PlateNumber")),
        Make = reader.GetString(reader.GetOrdinal("Make")),
        Model = reader.GetString(reader.GetOrdinal("Model")),
        Year = reader.GetInt32(reader.GetOrdinal("Year")),
        CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
        FuelTypeId = reader.GetInt32(reader.GetOrdinal("FuelTypeId")),
        TransmissionTypeId = reader.GetInt32(reader.GetOrdinal("TransmissionTypeId")),
        SeatsCount = reader.GetInt32(reader.GetOrdinal("SeatsCount")),
        DailyPrice = reader.GetDecimal(reader.GetOrdinal("DailyPrice")),
        BranchId = reader.GetInt32(reader.GetOrdinal("BranchId")),
        Status = Enum.Parse<VehicleStatus>(reader.GetString(reader.GetOrdinal("Status")), ignoreCase: false).ToString(),
        CurrentMileage = reader.GetInt32(reader.GetOrdinal("CurrentMileage"))
    };
}
