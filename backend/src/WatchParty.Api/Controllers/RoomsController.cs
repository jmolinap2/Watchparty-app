using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchParty.Api.Common;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Rooms;
using WatchParty.Contracts.Rooms;

namespace WatchParty.Api.Controllers;

[Authorize]
[Route("api/rooms")]
public sealed class RoomsController(IDispatcher dispatcher) : ApiControllerBase(dispatcher)
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateRoomRequest request, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new CreateRoomCommand(UserId, request.Name, request.IsPrivate, request.MaxMembers), cancellationToken)).ToCreatedResult();

    [HttpGet("history")]
    public async Task<IActionResult> History(CancellationToken cancellationToken) =>
        (await Dispatcher.Query(new GetMyRoomHistoryQuery(UserId), cancellationToken)).ToActionResult();

    [HttpGet("by-code/{code}")]
    public async Task<IActionResult> GetByCode(string code, CancellationToken cancellationToken) =>
        (await Dispatcher.Query(new GetRoomByCodeQuery(code), cancellationToken)).ToActionResult();

    [HttpPost("join")]
    public async Task<IActionResult> Join(JoinRoomRequest request, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new JoinRoomByCodeCommand(UserId, request.Code), cancellationToken)).ToActionResult();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken cancellationToken) =>
        (await Dispatcher.Query(new GetRoomDetailQuery(id, UserId), cancellationToken)).ToActionResult();

    [HttpPost("{id:guid}/leave")]
    public async Task<IActionResult> Leave(Guid id, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new LeaveRoomCommand(UserId, id), cancellationToken)).ToActionResult();

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new CloseRoomCommand(UserId, id), cancellationToken)).ToActionResult();

    [HttpPost("{id:guid}/transfer-host")]
    public async Task<IActionResult> TransferHost(Guid id, TransferHostRequest request, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new TransferHostCommand(UserId, id, request.ToUserId), cancellationToken)).ToActionResult();

    [HttpPost("{id:guid}/kick")]
    public async Task<IActionResult> Kick(Guid id, KickMemberRequest request, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new KickMemberCommand(UserId, id, request.UserId), cancellationToken)).ToActionResult();
}
