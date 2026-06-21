using FluentValidation;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.Security;
using WatchParty.Domain.Common;

namespace WatchParty.Application.Identity;

public sealed record LogoutCommand(string RefreshToken) : ICommand<Result>;

public sealed class LogoutValidator : AbstractValidator<LogoutCommand>
{
    public LogoutValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}

public sealed class LogoutCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    ITokenHasher tokenHasher,
    IUnitOfWork unitOfWork)
    : ICommandHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var token = await refreshTokenRepository.GetByHashAsync(tokenHasher.Hash(command.RefreshToken), cancellationToken);
        if (token is not null && token.IsActive)
        {
            token.Revoke();
            refreshTokenRepository.Update(token);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // Always succeed: logging out an unknown/expired token is a no-op.
        return Result.Success();
    }
}
