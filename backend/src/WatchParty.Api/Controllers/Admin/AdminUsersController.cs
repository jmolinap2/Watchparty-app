using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchParty.Api.Common;
using WatchParty.Api.Controllers;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Admin;
using WatchParty.Contracts.Admin;

namespace WatchParty.Api.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Route("api/admin/users")]
public sealed class AdminUsersController(IDispatcher dispatcher) : ApiControllerBase(dispatcher)
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        (await Dispatcher.Query(new GetUsersQuery(search, page, pageSize), cancellationToken)).ToActionResult();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken cancellationToken) =>
        (await Dispatcher.Query(new GetUserDetailQuery(id), cancellationToken)).ToActionResult();

    [HttpPost("{id:guid}/block")]
    public async Task<IActionResult> Block(Guid id, BlockUserAdminRequest request, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new BlockUserAdminCommand(UserId, id, request.Reason), cancellationToken)).ToActionResult();

    [HttpPost("{id:guid}/unblock")]
    public async Task<IActionResult> Unblock(Guid id, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new UnblockUserAdminCommand(UserId, id), cancellationToken)).ToActionResult();

    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> SetRole(Guid id, SetUserRoleRequest request, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new SetUserRoleCommand(UserId, id, request.Role), cancellationToken)).ToActionResult();
}
