using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchParty.Api.Common;
using WatchParty.Api.Controllers;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Admin;

namespace WatchParty.Api.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Route("api/admin/rooms")]
public sealed class AdminRoomsController(IDispatcher dispatcher) : ApiControllerBase(dispatcher)
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        (await Dispatcher.Query(new GetRoomsQuery(status, page, pageSize), cancellationToken)).ToActionResult();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken cancellationToken) =>
        (await Dispatcher.Query(new GetAdminRoomDetailQuery(id), cancellationToken)).ToActionResult();

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new AdminCloseRoomCommand(UserId, id), cancellationToken)).ToActionResult();
}
