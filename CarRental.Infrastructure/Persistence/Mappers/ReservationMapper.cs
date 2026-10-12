using _CarRental.Domain.Entities;
using _CarRental.Domain.Enums;
using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence.Mappers;

internal static class ReservationMapper
{
    public static Reservation Map(SqlDataReader reader)
    {
        var status = Enum.Parse<ReservationStatus>(reader.GetString(reader.GetOrdinal("Status")), ignoreCase: false);
        return new Reservation(status)
        {
            ReservationId = reader.GetInt32(reader.GetOrdinal("ReservationId")),
            CustomerId = reader.GetInt32(reader.GetOrdinal("CustomerId")),
            VehicleId = reader.GetInt32(reader.GetOrdinal("VehicleId")),
            PickupBranchId = reader.GetInt32(reader.GetOrdinal("PickupBranchId")),
            ReturnBranchId = reader.GetInt32(reader.GetOrdinal("ReturnBranchId")),
            PickupDateTime = reader.GetDateTime(reader.GetOrdinal("PickupDateTime")),
            ExpectedReturnDateTime = reader.GetDateTime(reader.GetOrdinal("ExpectedReturnDateTime")),
            ConfirmedByEmployeeId = reader.IsDBNull(reader.GetOrdinal("ConfirmedByEmployeeId"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("ConfirmedByEmployeeId")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }
}
