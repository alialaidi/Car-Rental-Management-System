using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.DTOs.Reports;
using _CarRental.Application.Common.Models;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Exceptions;
using _CarRental.Domain.Interfaces.Repositories;

namespace _CarRental.Application.Services;

public sealed class AuditQueryService(IAuditLogRepository repository, ICurrentUser currentUser) : IAuditQueryService
{
    public async Task<AuditLogPageDto> SearchAsync(int page, int pageSize, int? actorUserId, string? action,
        string? entityName, DateOnly? from, DateOnly? to)
    {
        if (!currentUser.IsInRole(_CarRental.Domain.Constants.RoleNames.Admin))
            throw new ForbiddenException("Only administrators can search audit logs.");
        if (actorUserId is <= 0) throw new BusinessRuleException("Actor user id must be positive.");
        if (from.HasValue != to.HasValue) throw new BusinessRuleException("Both audit date filters must be provided together.");
        if (from.HasValue)
        {
            ReportService.ValidateRange(from.Value, to!.Value);
        }
        var request = new PagedRequest { Page = page, PageSize = pageSize };
        DateTime? fromUtc = from?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        DateTime? toExclusive = to == DateOnly.MaxValue ? null : to?.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var (items, total) = await repository.SearchAsync(request.Page, request.PageSize, actorUserId,
            Normalize(action), Normalize(entityName), fromUtc, toExclusive);
        return new AuditLogPageDto(items.Select(ToDto).ToArray(), request.Page, request.PageSize, total);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static AuditLogDto ToDto(AuditLog x) => new(x.AuditId, x.ActorUserId, x.Action, x.EntityName, x.EntityId,
        x.OldValue, x.NewValue, x.IpAddress, x.DeviceInfo, x.Timestamp);
}
