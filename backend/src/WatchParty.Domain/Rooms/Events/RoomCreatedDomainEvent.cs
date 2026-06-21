using WatchParty.Domain.Common;

namespace WatchParty.Domain.Rooms.Events;

public sealed record RoomCreatedDomainEvent(Guid RoomId, Guid HostUserId, string Code) : IDomainEvent;
