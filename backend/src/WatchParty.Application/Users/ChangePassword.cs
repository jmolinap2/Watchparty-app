using FluentValidation;
using Microsoft.Extensions.Options;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.Security;
using WatchParty.Application.Common;
using WatchParty.Domain.Common;

namespace WatchParty.Application.Users;

public sealed record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : ICommand<Result>;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator(IOptions<SecurityOptions> options)
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(options.Value.MinPasswordLength)
            .MaximumLength(128);
    }
}

public sealed class ChangePasswordCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ChangePasswordCommand, Result>
{
    public async Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return DomainErrors.Users.NotFound;
        }

        if (!passwordHasher.Verify(command.CurrentPassword, user.PasswordHash))
        {
            return DomainErrors.Identity.InvalidCredentials;
        }

        user.ChangePassword(passwordHasher.Hash(command.NewPassword));
        userRepository.Update(user);

        // Revoke existing sessions; the current client must re-authenticate.
        await refreshTokenRepository.RevokeAllForUserAsync(user.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
