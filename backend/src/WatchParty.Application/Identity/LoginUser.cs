using FluentValidation;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.Security;
using WatchParty.Contracts.Identity;
using WatchParty.Domain.Admin;
using WatchParty.Domain.Common;
using WatchParty.Domain.Identity;

namespace WatchParty.Application.Identity;

public sealed record LoginUserCommand(string Email, string Password, string? IpAddress)
    : ICommand<Result<AuthResponse>>;

public sealed class LoginUserValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IAuditLogRepository auditLogRepository,
    AuthTokenIssuer authTokenIssuer,
    IUnitOfWork unitOfWork)
    : ICommandHandler<LoginUserCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        // Normalise the email; an invalid format is treated as bad credentials (no enumeration).
        var emailResult = Email.Create(command.Email);
        var user = emailResult.IsSuccess
            ? await userRepository.GetByEmailAsync(emailResult.Value.Value, cancellationToken)
            : null;

        if (user is null || !passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            await auditLogRepository.AddAsync(
                AuditLog.Security("login_failed", user?.Id, command.Email, command.IpAddress),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return DomainErrors.Identity.InvalidCredentials;
        }

        if (user.IsBlocked)
        {
            await auditLogRepository.AddAsync(
                AuditLog.Security("login_blocked", user.Id, command.Email, command.IpAddress),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return DomainErrors.Identity.AccountBlocked;
        }

        user.RecordLogin();
        userRepository.Update(user);

        var response = await authTokenIssuer.IssueAsync(user, command.IpAddress, cancellationToken);
        await auditLogRepository.AddAsync(
            AuditLog.Security("login_success", user.Id, command.Email, command.IpAddress),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }
}
