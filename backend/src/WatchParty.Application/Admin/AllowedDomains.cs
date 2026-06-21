using FluentValidation;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Contracts.Admin;
using WatchParty.Domain.Admin;
using WatchParty.Domain.Common;

namespace WatchParty.Application.Admin;

public sealed record AddAllowedDomainCommand(Guid AdminUserId, string Host) : ICommand<Result<AllowedDomainDto>>;

public sealed class AddAllowedDomainValidator : AbstractValidator<AddAllowedDomainCommand>
{
    public AddAllowedDomainValidator() => RuleFor(x => x.Host).NotEmpty();
}

public sealed class AddAllowedDomainCommandHandler(
    IAllowedDomainRepository allowedDomainRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AddAllowedDomainCommand, Result<AllowedDomainDto>>
{
    public async Task<Result<AllowedDomainDto>> Handle(AddAllowedDomainCommand command, CancellationToken cancellationToken)
    {
        var normalized = AllowedDomain.Normalize(command.Host);
        if (normalized is null)
        {
            return DomainErrors.Admin.DomainInvalid;
        }

        if (await allowedDomainRepository.GetByHostAsync(normalized, cancellationToken) is not null)
        {
            return DomainErrors.Admin.DomainAlreadyAllowed;
        }

        var domainResult = AllowedDomain.Create(command.Host, command.AdminUserId);
        if (domainResult.IsFailure)
        {
            return domainResult.Error;
        }

        await allowedDomainRepository.AddAsync(domainResult.Value, cancellationToken);
        await auditLogRepository.AddAsync(
            AuditLog.Admin("domain_added", command.AdminUserId, "AllowedDomain", domainResult.Value.Id.ToString(), normalized),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return domainResult.Value.ToDto();
    }
}

public sealed record ToggleAllowedDomainCommand(Guid AdminUserId, Guid DomainId, bool Enable) : ICommand<Result>;

public sealed class ToggleAllowedDomainCommandHandler(
    IAllowedDomainRepository allowedDomainRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ToggleAllowedDomainCommand, Result>
{
    public async Task<Result> Handle(ToggleAllowedDomainCommand command, CancellationToken cancellationToken)
    {
        var domain = await allowedDomainRepository.GetByIdAsync(command.DomainId, cancellationToken);
        if (domain is null)
        {
            return DomainErrors.Admin.DomainNotFound;
        }

        if (command.Enable)
        {
            domain.Enable();
        }
        else
        {
            domain.Disable();
        }

        allowedDomainRepository.Update(domain);
        await auditLogRepository.AddAsync(
            AuditLog.Admin(command.Enable ? "domain_enabled" : "domain_disabled", command.AdminUserId, "AllowedDomain", domain.Id.ToString(), domain.Host),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
