using Microsoft.Data.SqlClient;

namespace _CarRental.Infrastructure.Persistence;

internal interface IDbConnectionFactory
{
    SqlConnection CreateConnection();
}
