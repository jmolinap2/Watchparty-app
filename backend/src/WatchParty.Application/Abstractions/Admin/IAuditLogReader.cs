using WatchParty.Contracts.Admin;
using WatchParty.Contracts.Common;

namespace WatchParty.Application.Abstractions.Admin;

public interface IAuditLogReader
{
    Task<PagedResult<AuditLogDto>> SearchAsync(AuditLogSearchRequest request, CancellationToken cancellationToken);

    Task<AuditLogDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
