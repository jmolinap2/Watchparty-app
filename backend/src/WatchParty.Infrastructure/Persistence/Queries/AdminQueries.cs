using Microsoft.EntityFrameworkCore;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Admin;
using WatchParty.Contracts.Admin;

namespace WatchParty.Infrastructure.Persistence.Queries;

public sealed class AdminQueries(WatchPartyDbContext dbContext) : IAdminQueries
{
    public async Task<IReadOnlyList<AllowedDomainDto>> GetAllowedDomainsAsync(CancellationToken cancellationToken)
    {
        var domains = await dbContext.AllowedDomains.AsNoTracking()
            .OrderBy(d => d.Host)
            .ToListAsync(cancellationToken);
        return domains.Select(d => d.ToDto()).ToList();
    }
}
