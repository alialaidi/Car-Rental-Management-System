namespace _CarRental.Application.Common.Interfaces;

public interface IAuditLogger
{
    Task LogAsync(
        string action,
        string entityName,
        string? entityId,
        object? oldValue,
        object? newValue,
        int? actorUserId = null);
}
