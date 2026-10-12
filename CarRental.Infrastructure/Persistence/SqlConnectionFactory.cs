using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence;

internal sealed class SqlConnectionFactory(DatabaseOptions options) : IDbConnectionFactory
{
    public SqlConnection CreateConnection()
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'CarRentalDb' is not configured. Set it through user-secrets or an environment variable.");
        }

        return new SqlConnection(options.ConnectionString);
    }
}
