using System.Data;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Exceptions;
using _CarRental.Domain.Interfaces.Repositories;
using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence.Repositories;

internal sealed class AuditLogRepository(IDbConnectionFactory connectionFactory) : IAuditLogRepository
{
    public async Task InsertAsync(AuditLog auditLog)
    {
        ArgumentNullException.ThrowIfNull(auditLog);
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "usp_AuditLogs_Insert";
        command.CommandType = CommandType.StoredProcedure;
        AddNullableInt32(command.Parameters, "@ActorUserId", auditLog.ActorUserId);
        AddNVarChar(command.Parameters, "@Action", 100, auditLog.Action);
        AddNVarChar(command.Parameters, "@EntityName", 100, auditLog.EntityName);
        AddNVarChar(command.Parameters, "@EntityId", 50, auditLog.EntityId);
        AddNVarChar(command.Parameters, "@OldValue", -1, auditLog.OldValue);
        AddNVarChar(command.Parameters, "@NewValue", -1, auditLog.NewValue);
        AddNVarChar(command.Parameters, "@IPAddress", 50, auditLog.IpAddress);
        AddNVarChar(command.Parameters, "@DeviceInfo", 255, auditLog.DeviceInfo);
        command.Parameters.Add("@Timestamp", SqlDbType.DateTime2).Value = auditLog.Timestamp;

        try
        {
            await command.ExecuteNonQueryAsync();
        }
        catch (SqlException exception)
        {
            SqlErrorTranslator.ThrowIfKnown(exception);
            throw;
        }
    }

    public async Task<IReadOnlyList<AuditLog>> QueryAsync(int page, int pageSize, int? actorUserId = null, string? entityName = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var offset = (long)(page - 1) * pageSize;

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "usp_AuditLogs_Query";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add("@Offset", SqlDbType.BigInt).Value = offset;
        command.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;
        AddNullableInt32(command.Parameters, "@ActorUserId", actorUserId);
        AddNVarChar(command.Parameters, "@EntityName", 100, entityName);

        try
        {
            await using var reader = await command.ExecuteReaderAsync();
            var results = new List<AuditLog>();
            while (await reader.ReadAsync())
            {
                results.Add(Map(reader));
            }

            return results;
        }
        catch (SqlException exception)
        {
            SqlErrorTranslator.ThrowIfKnown(exception);
            throw;
        }
    }

    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> SearchAsync(int page, int pageSize,
        int? actorUserId, string? action, string? entityName, DateTime? fromUtc, DateTime? toUtcExclusive)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var offset = (long)(page - 1) * pageSize;
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "usp_AuditLogs_Search";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add("@Offset", SqlDbType.BigInt).Value = offset;
        command.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;
        AddNullableInt32(command.Parameters, "@ActorUserId", actorUserId);
        AddNVarChar(command.Parameters, "@Action", 100, action);
        AddNVarChar(command.Parameters, "@EntityName", 100, entityName);
        command.Parameters.Add("@FromUtc", SqlDbType.DateTime2).Value = (object?)fromUtc ?? DBNull.Value;
        command.Parameters.Add("@ToUtcExclusive", SqlDbType.DateTime2).Value = (object?)toUtcExclusive ?? DBNull.Value;
        try
        {
            await using var reader = await command.ExecuteReaderAsync();
            var total = await reader.ReadAsync() ? reader.GetInt32(reader.GetOrdinal("TotalCount")) : 0;
            var results = new List<AuditLog>();
            if (await reader.NextResultAsync())
                while (await reader.ReadAsync()) results.Add(Map(reader));
            return (results, total);
        }
        catch (SqlException exception)
        {
            SqlErrorTranslator.ThrowIfKnown(exception);
            throw;
        }
    }

    private static AuditLog Map(SqlDataReader reader) => new()
    {
        AuditId = reader.GetInt32(reader.GetOrdinal("AuditId")),
        ActorUserId = ReadNullableInt32(reader, "ActorUserId"),
        Action = reader.GetString(reader.GetOrdinal("Action")),
        EntityName = reader.GetString(reader.GetOrdinal("EntityName")),
        EntityId = ReadNullableString(reader, "EntityId"),
        OldValue = ReadNullableString(reader, "OldValue"),
        NewValue = ReadNullableString(reader, "NewValue"),
        IpAddress = ReadNullableString(reader, "IPAddress"),
        DeviceInfo = ReadNullableString(reader, "DeviceInfo"),
        Timestamp = reader.GetDateTime(reader.GetOrdinal("Timestamp"))
    };

    private static int? ReadNullableInt32(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    private static string? ReadNullableString(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static void AddNullableInt32(SqlParameterCollection parameters, string name, int? value) =>
        parameters.Add(name, SqlDbType.Int).Value = value.HasValue ? value.Value : DBNull.Value;

    private static void AddNVarChar(SqlParameterCollection parameters, string name, int size, string? value) =>
        parameters.Add(name, SqlDbType.NVarChar, size).Value = (object?)value ?? DBNull.Value;


}
