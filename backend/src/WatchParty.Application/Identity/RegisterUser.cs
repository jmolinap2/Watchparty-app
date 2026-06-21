using FluentValidation;
using Microsoft.Extensions.Options;
using WatchParty.Application.Abstractions;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.Security;
using WatchParty.Application.Abstractions.Services;
using WatchParty.Application.Common;
using WatchParty.Contracts.Identity;
using WatchParty.Domain.Common;
using WatchParty.Domain.Identity;

namespace WatchParty.Application.Identity;

public sealed record RegisterUserCommand(string Email, string Password, string DisplayName, string? IpAddress)
    : ICommand<Result<AuthResponse>>;

public sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator(IOptions<SecurityOptions> options)
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(Email.MaxLength);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(User.MaxDisplayNameLength);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(options.Value.MinPasswordLength)
            .MaximumLength(128);
    }
}

public sealed class RegisterUserCommandHandler(
    IUserRepository userRepository,
    IEmailVerificationTokenRepository verificationTokenRepository,
    IPasswordHasher passwordHasher,
    ITokenHasher tokenHasher,
    ISecureTokenGenerator secureTokenGenerator,
    IEmailSender emailSender,
    AuthTokenIssuer authTokenIssuer,
    IUnitOfWork unitOfWork,
    IClock clock,
    IOptions<SecurityOptions> securityOptions)
    : ICommandHandler<RegisterUserCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(command.Email);
        if (emailResult.IsFailure)
        {
            return emailResult.Error;
        }

        var email = emailResult.Value;
        if (await userRepository.EmailExistsAsync(email.Value, cancellationToken))
        {
            return DomainErrors.Identity.EmailAlreadyInUse;
        }

        var passwordHash = passwordHasher.Hash(command.Password);
        var userResult = User.Register(email, passwordHash, command.DisplayName);
        if (userResult.IsFailure)
        {
            return userResult.Error;
        }

        var user = userResult.Value;
        await userRepository.AddAsync(user, cancellationToken);

        // Issue an email confirmation token (the raw token goes only to the user's inbox).
        var rawToken = secureTokenGenerator.Generate();
        var verificationToken = EmailVerificationToken.Issue(
            user.Id,
            tokenHasher.Hash(rawToken),
            clock.UtcNow.AddHours(securityOptions.Value.EmailConfirmationHours));
        await verificationTokenRepository.AddAsync(verificationToken, cancellationToken);

        var response = await authTokenIssuer.IssueAsync(user, command.IpAddress, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await emailSender.SendEmailConfirmationAsync(email.Value, user.DisplayName, rawToken, cancellationToken);

        return response;
    }
}
