using WatchParty.Domain.Common;

namespace WatchParty.Domain.Rooms.Events;

public sealed record MemberKickedDomainEvent(Guid RoomId, Guid KickedUserId, Guid ByUserId) : IDomainEvent;
