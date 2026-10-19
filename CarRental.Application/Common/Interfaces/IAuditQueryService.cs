using _CarRental.Application.DTOs.Reports;

namespace _CarRental.Application.Common.Interfaces;

public interface IAuditQueryService
{
    Task<AuditLogPageDto> SearchAsync(int page, int pageSize, int? actorUserId, string? action,
        string? entityName, DateOnly? from, DateOnly? to);
}
