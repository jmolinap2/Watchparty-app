using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchParty.Api.Common;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Identity;
using WatchParty.Contracts.Identity;

namespace WatchParty.Api.Controllers;

[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController(IDispatcher dispatcher) : ApiControllerBase(dispatcher)
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new RegisterUserCommand(request.Email, request.Password, request.DisplayName, IpAddress), cancellationToken)).ToActionResult();

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new LoginUserCommand(request.LoginIdentifier, request.Password, IpAddress), cancellationToken)).ToActionResult();

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new RefreshTokenCommand(request.RefreshToken, IpAddress), cancellationToken)).ToActionResult();

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new LogoutCommand(request.RefreshToken), cancellationToken)).ToActionResult();

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new ConfirmEmailCommand(request.Token), cancellationToken)).ToActionResult();

    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation(ResendConfirmationRequest request, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new ResendConfirmationCommand(request.Email), cancellationToken)).ToActionResult();

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new ForgotPasswordCommand(request.Email), cancellationToken)).ToActionResult();

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new ResetPasswordCommand(request.Token, request.NewPassword), cancellationToken)).ToActionResult();
}
