using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchParty.Api.Common;
using WatchParty.Api.Controllers;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Admin;

namespace WatchParty.Api.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Route("api/admin/metrics")]
public sealed class AdminMetricsController(IDispatcher dispatcher) : ApiControllerBase(dispatcher)
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        (await Dispatcher.Query(new GetMetricsQuery(), cancellationToken)).ToActionResult();
}
