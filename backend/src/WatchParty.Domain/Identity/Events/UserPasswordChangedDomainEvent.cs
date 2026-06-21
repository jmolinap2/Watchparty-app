using WatchParty.Domain.Common;

namespace WatchParty.Domain.Identity.Events;

public sealed record UserPasswordChangedDomainEvent(Guid UserId) : IDomainEvent;
