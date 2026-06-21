using WatchParty.Contracts.Admin;

namespace WatchParty.Application.Abstractions.Persistence;

/// <summary>
/// Admin read models that don't belong to another module's query service.
/// Audit-log reads are served by <see cref="Admin.IAuditLogReader"/>.
/// </summary>
public interface IAdminQueries
{
    Task<IReadOnlyList<AllowedDomainDto>> GetAllowedDomainsAsync(CancellationToken cancellationToken);
}
