using WatchParty.Domain.Admin;

namespace WatchParty.Application.Abstractions.Persistence;

public interface IAllowedDomainRepository
{
    Task<AllowedDomain?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<AllowedDomain?> GetByHostAsync(string host, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<string>> GetEnabledHostsAsync(CancellationToken cancellationToken);
    Task AddAsync(AllowedDomain domain, CancellationToken cancellationToken);
    void Update(AllowedDomain domain);
}
