using _CarRental.Domain.Entities;
using _CarRental.Domain.Enums;
using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence.Mappers;

internal static class PaymentMapper
{
    public static Payment Map(SqlDataReader reader) => Payment.FromPersistence(
        reader.GetInt32(reader.GetOrdinal("PaymentId")), reader.GetInt32(reader.GetOrdinal("ContractId")),
        reader.GetInt32(reader.GetOrdinal("CustomerId")), Enum.Parse<PaymentMethod>(reader.GetString(reader.GetOrdinal("PaymentMethod"))),
        reader.GetDecimal(reader.GetOrdinal("Amount")), NullableString(reader, "TransactionReference"),
        Enum.Parse<PaymentStatus>(reader.GetString(reader.GetOrdinal("PaymentStatus"))), reader.GetDateTime(reader.GetOrdinal("PaymentDate")));

    private static string? NullableString(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
