using WatchParty.Contracts.Chat;
using WatchParty.Contracts.Playback;
using WatchParty.Contracts.Realtime;

namespace WatchParty.Application.Abstractions.Realtime;

/// <summary>
/// Publishes server-to-client realtime events to a room's SignalR group. Use cases
/// depend on this abstraction; the implementation (API layer) wraps IHubContext,
/// keeping SignalR out of the application/domain layers.
/// </summary>
public interface IRoomRealtimeNotifier
{
    Task PlaybackStateChangedAsync(Guid roomId, PlaybackStateDto state, CancellationToken cancellationToken);
    Task MediaChangedAsync(MediaChangedEvent payload, CancellationToken cancellationToken);
    Task MemberJoinedAsync(MemberJoinedEvent payload, CancellationToken cancellationToken);
    Task MemberLeftAsync(MemberLeftEvent payload, CancellationToken cancellationToken);
    Task PresenceUpdatedAsync(PresenceUpdatedEvent payload, CancellationToken cancellationToken);
    Task HostTransferredAsync(HostTransferredEvent payload, CancellationToken cancellationToken);
    Task MemberKickedAsync(MemberKickedEvent payload, CancellationToken cancellationToken);
    Task RoomClosedAsync(RoomClosedEvent payload, CancellationToken cancellationToken);
    Task ChatMessageReceivedAsync(Guid roomId, ChatMessageDto message, CancellationToken cancellationToken);
    Task ChatMessageDeletedAsync(ChatMessageDeletedEvent payload, CancellationToken cancellationToken);

    /// <summary>Notify a specific user (all their connections) that they were kicked, so their client leaves.</summary>
    Task NotifyUserKickedAsync(Guid roomId, Guid userId, CancellationToken cancellationToken);
}
