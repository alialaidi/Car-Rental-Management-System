using _CarRental.Application.Common.Interfaces;

namespace _CarRental.Infrastructure.Persistence;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
