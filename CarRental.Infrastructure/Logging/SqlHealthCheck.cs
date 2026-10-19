using System.Data;
using _CarRental.Application.Common.Interfaces;
using _CarRental.Infrastructure.Persistence;

namespace _CarRental.Infrastructure.Logging;

internal sealed class SqlHealthCheck(IDbConnectionFactory connectionFactory) : IDatabaseHealthProbe
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);
        try
        {
            await using var connection = connectionFactory.CreateConnection();
            await connection.OpenAsync(timeout.Token);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandType = CommandType.Text;
            command.CommandTimeout = (int)Timeout.TotalSeconds;
            var result = await command.ExecuteScalarAsync(timeout.Token);
            return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture) == 1;
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }
}
