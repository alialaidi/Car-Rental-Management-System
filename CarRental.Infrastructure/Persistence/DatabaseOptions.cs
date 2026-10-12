namespace _CarRental.Infrastructure.Persistence;

public sealed class DatabaseOptions(string? connectionString)
{
    public const string ConnectionStringName = "CarRentalDb";

    public string ConnectionString { get; } = connectionString ?? string.Empty;
}
