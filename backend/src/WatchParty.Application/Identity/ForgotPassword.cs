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

public sealed record ForgotPasswordCommand(string Email) : ICommand<Result>;

public sealed class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator() => RuleFor(x => x.Email).NotEmpty();
}

public sealed class ForgotPasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordResetTokenRepository resetTokenRepository,
    ITokenHasher tokenHasher,
    ISecureTokenGenerator secureTokenGenerator,
    IEmailSender emailSender,
    IClock clock,
    IUnitOfWork unitOfWork,
    IOptions<SecurityOptions> securityOptions)
    : ICommandHandler<ForgotPasswordCommand, Result>
{
    public async Task<Result> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(command.Email);
        if (emailResult.IsSuccess)
        {
            var user = await userRepository.GetByEmailAsync(emailResult.Value.Value, cancellationToken);
            if (user is not null)
            {
                var rawToken = secureTokenGenerator.Generate();
                var token = PasswordResetToken.Issue(
                    user.Id,
                    tokenHasher.Hash(rawToken),
                    clock.UtcNow.AddHours(securityOptions.Value.PasswordResetHours));
                await resetTokenRepository.AddAsync(token, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await emailSender.SendPasswordResetAsync(user.Email.Value, user.DisplayName, rawToken, cancellationToken);
            }
        }

        // Always succeed to avoid leaking which emails are registered.
        return Result.Success();
    }
}
