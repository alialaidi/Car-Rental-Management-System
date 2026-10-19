using _CarRental.Domain.Entities;
using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence.Mappers;

internal static class ReviewMapper
{
    public static Review Map(SqlDataReader reader)
    {
        var commentOrdinal = reader.GetOrdinal("Comment");
        return Review.FromPersistence(reader.GetInt32(reader.GetOrdinal("ReviewId")),
            reader.GetInt32(reader.GetOrdinal("ContractId")), reader.GetInt32(reader.GetOrdinal("CustomerId")),
            reader.GetInt32(reader.GetOrdinal("VehicleId")), reader.GetString(reader.GetOrdinal("CustomerName")),
            reader.GetInt32(reader.GetOrdinal("Rating")), reader.IsDBNull(commentOrdinal) ? null : reader.GetString(commentOrdinal),
            reader.GetDateTime(reader.GetOrdinal("CreatedAt")));
    }
}
