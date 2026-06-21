using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchParty.Api.Common;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Playback;
using WatchParty.Contracts.Playback;

namespace WatchParty.Api.Controllers;

/// <summary>
/// REST playback endpoints. Realtime clients normally drive playback over the
/// SignalR hub; these mirror the same use cases for non-realtime clients/testing.
/// </summary>
[Authorize]
[Route("api/rooms/{roomId:guid}/playback")]
public sealed class PlaybackController(IDispatcher dispatcher) : ApiControllerBase(dispatcher)
{
    [HttpGet("state")]
    public async Task<IActionResult> State(Guid roomId, CancellationToken cancellationToken) =>
        (await Dispatcher.Query(new GetPlaybackStateQuery(roomId, UserId), cancellationToken)).ToActionResult();

    [HttpPost("media")]
    public async Task<IActionResult> ChangeMedia(Guid roomId, ChangeMediaRequest request, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new ChangeMediaCommand(UserId, roomId, request.Url, request.Title), cancellationToken)).ToActionResult();

    [HttpPost("play")]
    public async Task<IActionResult> Play(Guid roomId, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new PlaybackPlayCommand(UserId, roomId), cancellationToken)).ToActionResult();

    [HttpPost("pause")]
    public async Task<IActionResult> Pause(Guid roomId, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new PlaybackPauseCommand(UserId, roomId), cancellationToken)).ToActionResult();

    [HttpPost("seek")]
    public async Task<IActionResult> Seek(Guid roomId, SeekRequest request, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new PlaybackSeekCommand(UserId, roomId, request.PositionSeconds), cancellationToken)).ToActionResult();
}
