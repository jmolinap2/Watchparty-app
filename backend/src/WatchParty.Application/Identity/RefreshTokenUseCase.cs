using FluentValidation;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.Security;
using WatchParty.Contracts.Identity;
using WatchParty.Domain.Common;

namespace WatchParty.Application.Identity;

public sealed record RefreshTokenCommand(string RefreshToken, string? IpAddress)
    : ICommand<Result<AuthResponse>>;

public sealed class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}

public sealed class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    ITokenHasher tokenHasher,
    AuthTokenIssuer authTokenIssuer,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var hash = tokenHasher.Hash(command.RefreshToken);
        var token = await refreshTokenRepository.GetByHashAsync(hash, cancellationToken);

        if (token is null || !token.IsActive)
        {
            return DomainErrors.Identity.InvalidRefreshToken;
        }

        var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null)
        {
            return DomainErrors.Identity.InvalidRefreshToken;
        }

        if (user.IsBlocked)
        {
            token.Revoke();
            refreshTokenRepository.Update(token);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return DomainErrors.Identity.AccountBlocked;
        }

        var response = await authTokenIssuer.RotateAsync(user, token, command.IpAddress, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return response;
    }
}
