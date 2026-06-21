using Microsoft.AspNetCore.SignalR;
using WatchParty.Application.Abstractions.Realtime;
using WatchParty.Contracts.Chat;
using WatchParty.Contracts.Playback;
using WatchParty.Contracts.Realtime;

namespace WatchParty.Api.Realtime;

/// <summary>
/// Publishes server-to-client realtime events through SignalR. Implements the
/// application abstraction so use cases stay free of SignalR (architecture §4, §11).
/// </summary>
public sealed class RoomRealtimeNotifier(IHubContext<RoomHub> hubContext) : IRoomRealtimeNotifier
{
    private IHubClients Clients => hubContext.Clients;

    public Task PlaybackStateChangedAsync(Guid roomId, PlaybackStateDto state, CancellationToken cancellationToken) =>
        Clients.Group(RoomGroups.Name(roomId)).SendAsync(RealtimeEvents.PlaybackStateChanged, state, cancellationToken);

    public Task MediaChangedAsync(MediaChangedEvent payload, CancellationToken cancellationToken) =>
        Clients.Group(RoomGroups.Name(payload.RoomId)).SendAsync(RealtimeEvents.MediaChanged, payload, cancellationToken);

    public Task MemberJoinedAsync(MemberJoinedEvent payload, CancellationToken cancellationToken) =>
        Clients.Group(RoomGroups.Name(payload.RoomId)).SendAsync(RealtimeEvents.MemberJoined, payload, cancellationToken);

    public Task MemberLeftAsync(MemberLeftEvent payload, CancellationToken cancellationToken) =>
        Clients.Group(RoomGroups.Name(payload.RoomId)).SendAsync(RealtimeEvents.MemberLeft, payload, cancellationToken);

    public Task PresenceUpdatedAsync(PresenceUpdatedEvent payload, CancellationToken cancellationToken) =>
        Clients.Group(RoomGroups.Name(payload.RoomId)).SendAsync(RealtimeEvents.PresenceUpdated, payload, cancellationToken);

    public Task HostTransferredAsync(HostTransferredEvent payload, CancellationToken cancellationToken) =>
        Clients.Group(RoomGroups.Name(payload.RoomId)).SendAsync(RealtimeEvents.HostTransferred, payload, cancellationToken);

    public Task MemberKickedAsync(MemberKickedEvent payload, CancellationToken cancellationToken) =>
        Clients.Group(RoomGroups.Name(payload.RoomId)).SendAsync(RealtimeEvents.MemberKicked, payload, cancellationToken);

    public Task RoomClosedAsync(RoomClosedEvent payload, CancellationToken cancellationToken) =>
        Clients.Group(RoomGroups.Name(payload.RoomId)).SendAsync(RealtimeEvents.RoomClosed, payload, cancellationToken);

    public Task ChatMessageReceivedAsync(Guid roomId, ChatMessageDto message, CancellationToken cancellationToken) =>
        Clients.Group(RoomGroups.Name(roomId)).SendAsync(RealtimeEvents.ChatMessageReceived, message, cancellationToken);

    public Task ChatMessageDeletedAsync(ChatMessageDeletedEvent payload, CancellationToken cancellationToken) =>
        Clients.Group(RoomGroups.Name(payload.RoomId)).SendAsync(RealtimeEvents.ChatMessageDeleted, payload, cancellationToken);

    public Task NotifyUserKickedAsync(Guid roomId, Guid userId, CancellationToken cancellationToken) =>
        Clients.User(userId.ToString()).SendAsync(RealtimeEvents.YouWereKicked, new MemberKickedEvent(roomId, userId), cancellationToken);
}
