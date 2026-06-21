namespace WatchParty.Domain.Common;

/// <summary>
/// Marker for domain events. Raised by aggregates and dispatched after persistence.
/// </summary>
public interface IDomainEvent
{
    Guid EventId => Guid.NewGuid();
    DateTimeOffset OccurredOnUtc => DateTimeOffset.UtcNow;
}
