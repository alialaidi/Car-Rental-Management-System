using Xunit;

namespace _CarRental.Tests;

public sealed class SqlIntegrationFactAttribute : FactAttribute
{
    public SqlIntegrationFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CAR_RENTAL_TEST_CONNECTION_STRING")))
        {
            Skip = "Set CAR_RENTAL_TEST_CONNECTION_STRING to a disposable database to run SQL integration tests.";
        }
    }
}
