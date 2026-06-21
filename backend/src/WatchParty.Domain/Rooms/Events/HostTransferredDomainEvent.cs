using WatchParty.Domain.Common;

namespace WatchParty.Domain.Rooms.Events;

public sealed record HostTransferredDomainEvent(Guid RoomId, Guid FromUserId, Guid ToUserId) : IDomainEvent;
