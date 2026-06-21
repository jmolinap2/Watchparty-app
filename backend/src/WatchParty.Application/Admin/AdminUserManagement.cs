using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Domain.Admin;
using WatchParty.Domain.Common;
using WatchParty.Domain.Identity;

namespace WatchParty.Application.Admin;

// Admin user-management use cases. Every action is audited (architecture §18).

public sealed record BlockUserAdminCommand(Guid AdminUserId, Guid TargetUserId, string? Reason) : ICommand<Result>;

public sealed class BlockUserAdminCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<BlockUserAdminCommand, Result>
{
    public async Task<Result> Handle(BlockUserAdminCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.TargetUserId, cancellationToken);
        if (user is null)
        {
            return DomainErrors.Users.NotFound;
        }

        user.BlockByAdmin(command.Reason);
        userRepository.Update(user);
        await refreshTokenRepository.RevokeAllForUserAsync(user.Id, cancellationToken);
        await auditLogRepository.AddAsync(
            AuditLog.Admin("user_blocked", command.AdminUserId, "User", user.Id.ToString(), command.Reason),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record UnblockUserAdminCommand(Guid AdminUserId, Guid TargetUserId) : ICommand<Result>;

public sealed class UnblockUserAdminCommandHandler(
    IUserRepository userRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UnblockUserAdminCommand, Result>
{
    public async Task<Result> Handle(UnblockUserAdminCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.TargetUserId, cancellationToken);
        if (user is null)
        {
            return DomainErrors.Users.NotFound;
        }

        user.UnblockByAdmin();
        userRepository.Update(user);
        await auditLogRepository.AddAsync(
            AuditLog.Admin("user_unblocked", command.AdminUserId, "User", user.Id.ToString()),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record SetUserRoleCommand(Guid AdminUserId, Guid TargetUserId, string Role) : ICommand<Result>;

public sealed class SetUserRoleCommandHandler(
    IUserRepository userRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SetUserRoleCommand, Result>
{
    public async Task<Result> Handle(SetUserRoleCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<UserRole>(command.Role, ignoreCase: true, out var role))
        {
            return Error.Validation("admin.invalid_role", "Role must be 'User' or 'Admin'.");
        }

        var user = await userRepository.GetByIdAsync(command.TargetUserId, cancellationToken);
        if (user is null)
        {
            return DomainErrors.Users.NotFound;
        }

        user.SetRole(role);
        userRepository.Update(user);
        await auditLogRepository.AddAsync(
            AuditLog.Admin("user_role_changed", command.AdminUserId, "User", user.Id.ToString(), role.ToString()),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
