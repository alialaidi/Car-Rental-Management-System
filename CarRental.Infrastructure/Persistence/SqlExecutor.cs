using System.Data;
using Microsoft.Data.SqlClient;
using _CarRental.Infrastructure.Persistence.UnitOfWork;

namespace _CarRental.Infrastructure.Persistence;

internal sealed class SqlExecutor(DbSession session)
{
    public async Task<int> ExecuteNonQueryAsync(string procedureName, Action<SqlParameterCollection>? configureParameters = null)
    {
        await using var command = await CreateCommandAsync(procedureName, configureParameters);
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<int> ExecuteScalarInt32Async(string procedureName, Action<SqlParameterCollection>? configureParameters = null)
    {
        await using var command = await CreateCommandAsync(procedureName, configureParameters);
        var result = await command.ExecuteScalarAsync();
        if (result is null or DBNull)
        {
            throw new InvalidOperationException($"Stored procedure '{procedureName}' did not return an integer value.");
        }

        return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task<int?> ExecuteScalarNullableInt32Async(string procedureName, Action<SqlParameterCollection>? configureParameters = null)
    {
        await using var command = await CreateCommandAsync(procedureName, configureParameters);
        var result = await command.ExecuteScalarAsync();
        return result is null or DBNull ? null : Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task<bool> ExecuteScalarBooleanAsync(string procedureName, Action<SqlParameterCollection>? configureParameters = null)
    {
        await using var command = await CreateCommandAsync(procedureName, configureParameters);
        var result = await command.ExecuteScalarAsync();
        return result is not null and not DBNull && Convert.ToBoolean(result, System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(
        string procedureName,
        Func<SqlDataReader, T> map,
        Action<SqlParameterCollection>? configureParameters = null)
    {
        await using var command = await CreateCommandAsync(procedureName, configureParameters);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? map(reader) : default;
    }

    public async Task<IReadOnlyList<T>> QueryAsync<T>(
        string procedureName,
        Func<SqlDataReader, T> map,
        Action<SqlParameterCollection>? configureParameters = null)
    {
        await using var command = await CreateCommandAsync(procedureName, configureParameters);
        await using var reader = await command.ExecuteReaderAsync();
        var results = new List<T>();
        while (await reader.ReadAsync())
        {
            results.Add(map(reader));
        }

        return results;
    }

    public async Task<(IReadOnlyList<TFirst> First, IReadOnlyList<TSecond> Second)> QueryTwoResultSetsAsync<TFirst, TSecond>(
        string procedureName,
        Func<SqlDataReader, TFirst> mapFirst,
        Func<SqlDataReader, TSecond> mapSecond,
        Action<SqlParameterCollection>? configureParameters = null)
    {
        await using var command = await CreateCommandAsync(procedureName, configureParameters);
        await using var reader = await command.ExecuteReaderAsync();
        var first = new List<TFirst>();
        while (await reader.ReadAsync())
            first.Add(mapFirst(reader));

        var second = new List<TSecond>();
        if (await reader.NextResultAsync())
            while (await reader.ReadAsync())
                second.Add(mapSecond(reader));

        return (first, second);
    }

    private async Task<SqlCommand> CreateCommandAsync(string procedureName, Action<SqlParameterCollection>? configureParameters)
    {
        var connection = await session.GetOpenConnectionAsync();
        var command = connection.CreateCommand();
        command.CommandText = procedureName;
        command.CommandType = CommandType.StoredProcedure;
        command.Transaction = session.Transaction;
        configureParameters?.Invoke(command.Parameters);
        return command;
    }
}
