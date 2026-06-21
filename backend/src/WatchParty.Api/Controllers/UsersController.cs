using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchParty.Api.Common;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Users;
using WatchParty.Contracts.Users;

namespace WatchParty.Api.Controllers;

[Authorize]
[Route("api/users")]
public sealed class UsersController(IDispatcher dispatcher) : ApiControllerBase(dispatcher)
{
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken) =>
        (await Dispatcher.Query(new GetMyProfileQuery(UserId), cancellationToken)).ToActionResult();

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new UpdateProfileCommand(UserId, request.DisplayName, request.IsPrivate), cancellationToken)).ToActionResult();

    [HttpPut("me/avatar")]
    public async Task<IActionResult> SetAvatar(SetAvatarRequest request, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new SetAvatarCommand(UserId, request.AvatarUrl), cancellationToken)).ToActionResult();

    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new ChangePasswordCommand(UserId, request.CurrentPassword, request.NewPassword), cancellationToken)).ToActionResult();

    [HttpGet("me/blocked")]
    public async Task<IActionResult> Blocked(CancellationToken cancellationToken) =>
        (await Dispatcher.Query(new GetBlockedUsersQuery(UserId), cancellationToken)).ToActionResult();

    [HttpPost("blocks")]
    public async Task<IActionResult> Block(BlockUserRequest request, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new BlockUserCommand(UserId, request.UserId), cancellationToken)).ToActionResult();

    [HttpDelete("blocks/{userId:guid}")]
    public async Task<IActionResult> Unblock(Guid userId, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new UnblockUserCommand(UserId, userId), cancellationToken)).ToActionResult();
}
