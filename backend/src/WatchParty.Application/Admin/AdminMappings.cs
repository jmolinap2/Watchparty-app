using WatchParty.Contracts.Admin;
using WatchParty.Domain.Admin;

namespace WatchParty.Application.Admin;

public static class AdminMappings
{
    public static AllowedDomainDto ToDto(this AllowedDomain domain) =>
        new(domain.Id, domain.Host, domain.IsEnabled, domain.CreatedAtUtc);
}
