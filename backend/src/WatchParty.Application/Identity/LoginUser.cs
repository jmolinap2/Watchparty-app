using FluentValidation;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.Security;
using WatchParty.Contracts.Identity;
using WatchParty.Domain.Admin;
using WatchParty.Domain.Common;
using WatchParty.Domain.Identity;

namespace WatchParty.Application.Identity;

/// <summary><paramref name="Identifier"/> may be an email address or a username.</summary>
public sealed record LoginUserCommand(string Identifier, string Password, string? IpAddress)
    : ICommand<Result<AuthResponse>>;

public sealed class LoginUserValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserValidator()
    {
        RuleFor(x => x.Identifier).NotEmpty();
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
        // The identifier may be an email or a username. A valid email is looked up by
        // email; anything else is treated as a username. Misses are reported as bad
        // credentials (no account enumeration).
        var user = await ResolveUserAsync(command.Identifier, cancellationToken);

        if (user is null || !passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            await auditLogRepository.AddAsync(
                AuditLog.Security("login_failed", user?.Id, command.Identifier, command.IpAddress),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return DomainErrors.Identity.InvalidCredentials;
        }

        if (user.IsBlocked)
        {
            await auditLogRepository.AddAsync(
                AuditLog.Security("login_blocked", user.Id, command.Identifier, command.IpAddress),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return DomainErrors.Identity.AccountBlocked;
        }

        user.RecordLogin();
        userRepository.Update(user);

        var response = await authTokenIssuer.IssueAsync(user, command.IpAddress, cancellationToken);
        await auditLogRepository.AddAsync(
            AuditLog.Security("login_success", user.Id, command.Identifier, command.IpAddress),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }

    private async Task<User?> ResolveUserAsync(string identifier, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(identifier);
        if (emailResult.IsSuccess)
        {
            return await userRepository.GetByEmailAsync(emailResult.Value.Value, cancellationToken);
        }

        var username = User.NormalizeUsername(identifier);
        return username is null
            ? null
            : await userRepository.GetByUsernameAsync(username, cancellationToken);
    }
}
