using Microsoft.EntityFrameworkCore;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Domain.Admin;

namespace WatchParty.Infrastructure.Persistence.Repositories;

public sealed class AllowedDomainRepository(WatchPartyDbContext dbContext) : IAllowedDomainRepository
{
    public Task<AllowedDomain?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.AllowedDomains.FirstOrDefaultAsync(domain => domain.Id == id, cancellationToken);

    public Task<AllowedDomain?> GetByHostAsync(string host, CancellationToken cancellationToken) =>
        dbContext.AllowedDomains.FirstOrDefaultAsync(domain => domain.Host == host, cancellationToken);

    public async Task<IReadOnlyCollection<string>> GetEnabledHostsAsync(CancellationToken cancellationToken) =>
        await dbContext.AllowedDomains
            .AsNoTracking()
            .Where(domain => domain.IsEnabled)
            .Select(domain => domain.Host)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(AllowedDomain domain, CancellationToken cancellationToken) =>
        await dbContext.AllowedDomains.AddAsync(domain, cancellationToken);

    public void Update(AllowedDomain domain) => dbContext.AllowedDomains.Update(domain);
}
