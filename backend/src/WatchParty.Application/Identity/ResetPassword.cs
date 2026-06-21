using FluentValidation;
using Microsoft.Extensions.Options;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.Security;
using WatchParty.Application.Common;
using WatchParty.Domain.Common;

namespace WatchParty.Application.Identity;

public sealed record ResetPasswordCommand(string Token, string NewPassword) : ICommand<Result>;

public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator(IOptions<SecurityOptions> options)
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(options.Value.MinPasswordLength)
            .MaximumLength(128);
    }
}

public sealed class ResetPasswordCommandHandler(
    IPasswordResetTokenRepository resetTokenRepository,
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    ITokenHasher tokenHasher,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var token = await resetTokenRepository.GetByHashAsync(tokenHasher.Hash(command.Token), cancellationToken);
        if (token is null || !token.IsUsable)
        {
            return DomainErrors.Identity.InvalidResetToken;
        }

        var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null)
        {
            return DomainErrors.Identity.InvalidResetToken;
        }

        user.ChangePassword(passwordHasher.Hash(command.NewPassword));
        userRepository.Update(user);
        token.Consume();
        resetTokenRepository.Update(token);

        // Invalidate all existing sessions after a password reset.
        await refreshTokenRepository.RevokeAllForUserAsync(user.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
