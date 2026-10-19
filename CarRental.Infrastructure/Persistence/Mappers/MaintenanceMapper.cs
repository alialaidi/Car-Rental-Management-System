using _CarRental.Domain.Entities;
using _CarRental.Domain.Enums;
using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence.Mappers;

internal static class MaintenanceMapper
{
    public static MaintenanceRecord Map(SqlDataReader reader) => MaintenanceRecord.FromPersistence(
        reader.GetInt32(reader.GetOrdinal("MaintenanceId")), reader.GetInt32(reader.GetOrdinal("VehicleId")),
        reader.GetInt32(reader.GetOrdinal("BranchId")), reader.GetInt32(reader.GetOrdinal("CreatedByEmployeeId")),
        reader.GetString(reader.GetOrdinal("MaintenanceType")), NullableString(reader, "ProblemDescription"),
        DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("StartDate"))), NullableDate(reader, "ExpectedEndDate"),
        NullableDate(reader, "ActualEndDate"), NullableDecimal(reader, "Cost"),
        Enum.Parse<MaintenanceStatus>(reader.GetString(reader.GetOrdinal("Status")), false));

    private static string? NullableString(SqlDataReader r, string n) { var i = r.GetOrdinal(n); return r.IsDBNull(i) ? null : r.GetString(i); }
    private static DateOnly? NullableDate(SqlDataReader r, string n) { var i = r.GetOrdinal(n); return r.IsDBNull(i) ? null : DateOnly.FromDateTime(r.GetDateTime(i)); }
    private static decimal? NullableDecimal(SqlDataReader r, string n) { var i = r.GetOrdinal(n); return r.IsDBNull(i) ? null : r.GetDecimal(i); }
}
