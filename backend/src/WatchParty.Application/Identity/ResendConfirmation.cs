using FluentValidation;
using Microsoft.Extensions.Options;
using WatchParty.Application.Abstractions;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.Security;
using WatchParty.Application.Abstractions.Services;
using WatchParty.Application.Common;
using WatchParty.Domain.Common;
using WatchParty.Domain.Identity;

namespace WatchParty.Application.Identity;

public sealed record ResendConfirmationCommand(string Email) : ICommand<Result>;

public sealed class ResendConfirmationValidator : AbstractValidator<ResendConfirmationCommand>
{
    public ResendConfirmationValidator() => RuleFor(x => x.Email).NotEmpty();
}

public sealed class ResendConfirmationCommandHandler(
    IUserRepository userRepository,
    IEmailVerificationTokenRepository verificationTokenRepository,
    ITokenHasher tokenHasher,
    ISecureTokenGenerator secureTokenGenerator,
    IEmailSender emailSender,
    IClock clock,
    IUnitOfWork unitOfWork,
    IOptions<SecurityOptions> securityOptions)
    : ICommandHandler<ResendConfirmationCommand, Result>
{
    public async Task<Result> Handle(ResendConfirmationCommand command, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(command.Email);
        if (emailResult.IsSuccess)
        {
            var user = await userRepository.GetByEmailAsync(emailResult.Value.Value, cancellationToken);
            if (user is not null && !user.EmailConfirmed)
            {
                var rawToken = secureTokenGenerator.Generate();
                var token = EmailVerificationToken.Issue(
                    user.Id,
                    tokenHasher.Hash(rawToken),
                    clock.UtcNow.AddHours(securityOptions.Value.EmailConfirmationHours));
                await verificationTokenRepository.AddAsync(token, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await emailSender.SendEmailConfirmationAsync(user.Email.Value, user.DisplayName, rawToken, cancellationToken);
            }
        }

        // Always succeed to avoid leaking which emails are registered.
        return Result.Success();
    }
}
