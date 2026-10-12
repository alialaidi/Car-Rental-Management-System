using System.Data;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Interfaces.Repositories;
using _CarRental.Infrastructure.Persistence.Mappers;
using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence.Repositories;

internal sealed class RefreshTokenRepository(SqlExecutor executor) : IRefreshTokenRepository
{
    public async Task<int> CreateAsync(RefreshToken refreshToken)
    {
        ArgumentNullException.ThrowIfNull(refreshToken);
        try
        {
            return await executor.ExecuteScalarInt32Async("usp_RefreshTokens_Create", parameters =>
            {
                parameters.Add("@UserId", SqlDbType.Int).Value = refreshToken.UserId;
                parameters.Add("@Token", SqlDbType.NVarChar, 500).Value = refreshToken.Token;
                parameters.Add("@ExpiresAt", SqlDbType.DateTime2).Value = refreshToken.ExpiresAt;
            });
        }
        catch (SqlException exception)
        {
            SqlErrorTranslator.ThrowIfKnown(exception);
            throw;
        }
    }

    public Task<RefreshToken?> GetByTokenAsync(string token) => executor.QuerySingleOrDefaultAsync(
        "usp_RefreshTokens_GetByToken",
        RefreshTokenMapper.Map,
        parameters => parameters.Add("@Token", SqlDbType.NVarChar, 500).Value = token);

    public async Task RevokeAsync(int tokenId, DateTime revokedAtUtc)
    {
        try
        {
            await executor.ExecuteNonQueryAsync("usp_RefreshTokens_Revoke", parameters =>
            {
                parameters.Add("@TokenId", SqlDbType.Int).Value = tokenId;
                parameters.Add("@RevokedAt", SqlDbType.DateTime2).Value = revokedAtUtc;
            });
        }
        catch (SqlException exception)
        {
            SqlErrorTranslator.ThrowIfKnown(exception);
            throw;
        }
    }

    public async Task<bool> TryRevokeAsync(int tokenId, DateTime revokedAtUtc)
    {
        try
        {
            return await executor.ExecuteScalarBooleanAsync("usp_RefreshTokens_TryRevoke", parameters =>
            {
                parameters.Add("@TokenId", SqlDbType.Int).Value = tokenId;
                parameters.Add("@RevokedAt", SqlDbType.DateTime2).Value = revokedAtUtc;
            });
        }
        catch (SqlException exception)
        {
            SqlErrorTranslator.ThrowIfKnown(exception);
            throw;
        }
    }

    public async Task RevokeAllForUserAsync(int userId, DateTime revokedAtUtc)
    {
        try
        {
            await executor.ExecuteNonQueryAsync("usp_RefreshTokens_RevokeAllForUser", parameters =>
            {
                parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                parameters.Add("@RevokedAt", SqlDbType.DateTime2).Value = revokedAtUtc;
            });
        }
        catch (SqlException exception)
        {
            SqlErrorTranslator.ThrowIfKnown(exception);
            throw;
        }
    }
}
