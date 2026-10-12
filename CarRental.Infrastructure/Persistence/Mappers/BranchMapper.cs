using _CarRental.Domain.Entities;
using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence.Mappers;

internal static class BranchMapper
{
    public static Branch Map(SqlDataReader reader) => new()
    {
        BranchId = reader.GetInt32(reader.GetOrdinal("BranchId")),
        Name = reader.GetString(reader.GetOrdinal("Name")),
        Address = reader.GetString(reader.GetOrdinal("Address")),
        City = reader.GetString(reader.GetOrdinal("City")),
        Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? null : reader.GetString(reader.GetOrdinal("Phone")),
        ManagerEmployeeId = reader.IsDBNull(reader.GetOrdinal("ManagerEmployeeId")) ? null : reader.GetInt32(reader.GetOrdinal("ManagerEmployeeId"))
    };
}
