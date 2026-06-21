using FluentValidation;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.Security;
using WatchParty.Domain.Common;

namespace WatchParty.Application.Identity;

public sealed record ConfirmEmailCommand(string Token) : ICommand<Result>;

public sealed class ConfirmEmailValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailValidator() => RuleFor(x => x.Token).NotEmpty();
}

public sealed class ConfirmEmailCommandHandler(
    IEmailVerificationTokenRepository verificationTokenRepository,
    IUserRepository userRepository,
    ITokenHasher tokenHasher,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ConfirmEmailCommand, Result>
{
    public async Task<Result> Handle(ConfirmEmailCommand command, CancellationToken cancellationToken)
    {
        var token = await verificationTokenRepository.GetByHashAsync(tokenHasher.Hash(command.Token), cancellationToken);
        if (token is null || !token.IsUsable)
        {
            return DomainErrors.Identity.InvalidConfirmationToken;
        }

        var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null)
        {
            return DomainErrors.Identity.InvalidConfirmationToken;
        }

        user.ConfirmEmail();
        userRepository.Update(user);
        token.Consume();
        verificationTokenRepository.Update(token);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
