using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchParty.Api.Common;
using WatchParty.Api.Controllers;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Reports;
using WatchParty.Contracts.Reports;

namespace WatchParty.Api.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Route("api/admin/reports")]
public sealed class AdminReportsController(IDispatcher dispatcher) : ApiControllerBase(dispatcher)
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        (await Dispatcher.Query(new GetReportsQuery(status, page, pageSize), cancellationToken)).ToActionResult();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken cancellationToken) =>
        (await Dispatcher.Query(new GetReportDetailQuery(id), cancellationToken)).ToActionResult();

    [HttpPost("{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id, ResolveReportRequest request, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new ResolveReportCommand(UserId, id, request.Note), cancellationToken)).ToActionResult();

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, RejectReportRequest request, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new RejectReportCommand(UserId, id, request.Note), cancellationToken)).ToActionResult();
}
