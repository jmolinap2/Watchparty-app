using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchParty.Api.Common;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Chat;
using WatchParty.Contracts.Chat;

namespace WatchParty.Api.Controllers;

[Authorize]
[Route("api/rooms/{roomId:guid}/chat")]
public sealed class ChatController(IDispatcher dispatcher) : ApiControllerBase(dispatcher)
{
    [HttpGet("messages")]
    public async Task<IActionResult> History(Guid roomId, [FromQuery] DateTimeOffset? before, [FromQuery] int limit, CancellationToken cancellationToken) =>
        (await Dispatcher.Query(new GetChatHistoryQuery(roomId, UserId, before, limit), cancellationToken)).ToActionResult();

    [HttpPost("messages")]
    public async Task<IActionResult> Send(Guid roomId, SendMessageRequest request, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new SendMessageCommand(UserId, roomId, request.Content), cancellationToken)).ToActionResult();

    [HttpDelete("messages/{messageId:guid}")]
    public async Task<IActionResult> Delete(Guid roomId, Guid messageId, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new DeleteMessageCommand(UserId, messageId, IsAdmin), cancellationToken)).ToActionResult();
}
