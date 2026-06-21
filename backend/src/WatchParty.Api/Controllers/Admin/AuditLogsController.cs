using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchParty.Api.Auditing;
using WatchParty.Application.Abstractions.Admin;
using WatchParty.Contracts.Admin;
using WatchParty.Contracts.Common;

namespace WatchParty.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[DisableRequestAuditing]
[Route("api/admin/audit-logs")]
public sealed class AuditLogsController(IAuditLogReader auditLogReader) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetAll(
        [FromQuery] AuditLogSearchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await auditLogReader.SearchAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AuditLogDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await auditLogReader.GetByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
