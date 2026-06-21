using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchParty.Api.Common;
using WatchParty.Api.Controllers;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Admin;
using WatchParty.Contracts.Admin;

namespace WatchParty.Api.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Route("api/admin/allowed-domains")]
public sealed class AdminDomainsController(IDispatcher dispatcher) : ApiControllerBase(dispatcher)
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        (await Dispatcher.Query(new GetAllowedDomainsQuery(), cancellationToken)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> Add(AddAllowedDomainRequest request, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new AddAllowedDomainCommand(UserId, request.Host), cancellationToken)).ToCreatedResult();

    [HttpPost("{id:guid}/enable")]
    public async Task<IActionResult> Enable(Guid id, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new ToggleAllowedDomainCommand(UserId, id, true), cancellationToken)).ToActionResult();

    [HttpPost("{id:guid}/disable")]
    public async Task<IActionResult> Disable(Guid id, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new ToggleAllowedDomainCommand(UserId, id, false), cancellationToken)).ToActionResult();
}
