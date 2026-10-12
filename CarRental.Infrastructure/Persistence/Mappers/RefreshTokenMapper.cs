using _CarRental.Domain.Entities;
using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence.Mappers;

internal static class RefreshTokenMapper
{
    public static RefreshToken Map(SqlDataReader reader)
    {
        var revokedAtOrdinal = reader.GetOrdinal("RevokedAt");
        return new RefreshToken
        {
            TokenId = reader.GetInt32(reader.GetOrdinal("TokenId")),
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            Token = reader.GetString(reader.GetOrdinal("Token")),
            ExpiresAt = reader.GetDateTime(reader.GetOrdinal("ExpiresAt")),
            RevokedAt = reader.IsDBNull(revokedAtOrdinal) ? null : reader.GetDateTime(revokedAtOrdinal),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }
}
