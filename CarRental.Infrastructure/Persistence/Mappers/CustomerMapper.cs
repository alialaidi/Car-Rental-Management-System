using _CarRental.Domain.Entities;
using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence.Mappers;

internal static class CustomerMapper
{
    public static Customer Map(SqlDataReader reader) => new()
    {
        CustomerId = reader.GetInt32(reader.GetOrdinal("CustomerId")),
        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
        FullName = reader.GetString(reader.GetOrdinal("FullName")),
        DrivingLicenseNumber = reader.GetString(reader.GetOrdinal("DrivingLicenseNumber")),
        DrivingLicenseExpiry = reader.GetDateTime(reader.GetOrdinal("DrivingLicenseExpiry")),
        Address = ReadNullableString(reader, "Address")
    };

    private static string? ReadNullableString(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
