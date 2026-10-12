using _CarRental.Domain.Entities;

namespace _CarRental.Domain.Interfaces.Repositories;

public interface IAuditLogRepository
{
    Task InsertAsync(AuditLog auditLog);

    Task<IReadOnlyList<AuditLog>> QueryAsync(
        int page,
        int pageSize,
        int? actorUserId = null,
        string? entityName = null);
}
