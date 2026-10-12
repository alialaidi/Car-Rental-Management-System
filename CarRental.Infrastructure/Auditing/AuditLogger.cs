using _CarRental.Application.Common.Interfaces;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace _CarRental.Infrastructure.Auditing;

public sealed class AuditLogger(
    IAuditLogRepository repository,
    IRequestContext requestContext,
    IDateTimeProvider dateTimeProvider,
    ILogger<AuditLogger> logger) : IAuditLogger
{
    public async Task LogAsync(
        string action,
        string entityName,
        string? entityId,
        object? oldValue,
        object? newValue,
        int? actorUserId = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                ActorUserId = actorUserId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                OldValue = AuditValueSerializer.Serialize(oldValue),
                NewValue = AuditValueSerializer.Serialize(newValue),
                IpAddress = requestContext.IpAddress,
                DeviceInfo = requestContext.DeviceInfo,
                Timestamp = dateTimeProvider.UtcNow
            };

            await repository.InsertAsync(auditLog);
        }
        catch (Exception exception)
        {
            // Audit failures must not change the outcome of the business operation.
            logger.LogError(exception, "Failed to write audit event {Action} for {EntityName}.", action, entityName);
        }
    }
}
