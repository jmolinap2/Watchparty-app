using WatchParty.Domain.Common;

namespace WatchParty.Domain.Identity.Events;

public sealed record UserRegisteredDomainEvent(Guid UserId, string Email, string DisplayName) : IDomainEvent;
