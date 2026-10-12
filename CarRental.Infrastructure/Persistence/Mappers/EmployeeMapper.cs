using _CarRental.Domain.Entities;
using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence.Mappers;

internal static class EmployeeMapper
{
    public static Employee Map(SqlDataReader reader) => new()
    {
        EmployeeId = reader.GetInt32(reader.GetOrdinal("EmployeeId")),
        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
        BranchId = reader.GetInt32(reader.GetOrdinal("BranchId")),
        Position = reader.GetString(reader.GetOrdinal("Position")),
        HireDate = reader.GetDateTime(reader.GetOrdinal("HireDate"))
    };
}
