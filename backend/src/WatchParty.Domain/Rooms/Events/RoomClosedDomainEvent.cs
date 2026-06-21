using WatchParty.Domain.Common;

namespace WatchParty.Domain.Rooms.Events;

public sealed record RoomClosedDomainEvent(Guid RoomId, Guid ClosedByUserId) : IDomainEvent;
