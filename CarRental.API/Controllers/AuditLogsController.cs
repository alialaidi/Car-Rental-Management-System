using _CarRental.Application.Common.Interfaces;
using _CarRental.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _CarRental.API.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class AuditLogsController(IAuditQueryService auditQuery) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] int? actorUserId = null, [FromQuery] string? action = null,
        [FromQuery] string? entityName = null, [FromQuery] DateOnly? from = null, [FromQuery] DateOnly? to = null) =>
        Ok(await auditQuery.SearchAsync(page, pageSize, actorUserId, action, entityName, from, to));
}
