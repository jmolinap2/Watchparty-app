using WatchParty.Contracts.Chat;
using WatchParty.Contracts.Playback;
using WatchParty.Contracts.Rooms;

namespace WatchParty.Contracts.Realtime;

// Payloads pushed from the server to clients over SignalR. One record per event in RealtimeEvents.

public sealed record MemberJoinedEvent(Guid RoomId, RoomMemberDto Member, int OnlineCount);

public sealed record MemberLeftEvent(Guid RoomId, Guid UserId, int OnlineCount);

public sealed record PresenceUpdatedEvent(Guid RoomId, IReadOnlyList<Guid> OnlineUserIds);

public sealed record HostTransferredEvent(Guid RoomId, Guid FromUserId, Guid ToUserId);

public sealed record MemberKickedEvent(Guid RoomId, Guid UserId);

public sealed record RoomClosedEvent(Guid RoomId);

public sealed record MediaChangedEvent(Guid RoomId, MediaDto Media, PlaybackStateDto Playback);

public sealed record ChatMessageDeletedEvent(Guid RoomId, Guid MessageId, Guid DeletedByUserId);

public sealed record HubErrorEvent(string Code, string Message);
