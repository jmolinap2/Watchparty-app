using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WatchParty.Api.Common;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.Realtime;
using WatchParty.Application.Abstractions.State;
using WatchParty.Application.Chat;
using WatchParty.Application.Playback;
using WatchParty.Contracts.Realtime;
using WatchParty.Domain.Common;

namespace WatchParty.Api.Realtime;

/// <summary>
/// The realtime entry point (architecture §11). Hubs authenticate, manage groups
/// and presence, and delegate all business rules to use cases; success events are
/// broadcast by the handlers via <see cref="IRoomRealtimeNotifier"/>.
/// </summary>
[Authorize]
public sealed class RoomHub(
    IDispatcher dispatcher,
    IRoomQueries roomQueries,
    IPresenceStore presenceStore,
    IRoomRealtimeNotifier notifier,
    IMetricsCounter metricsCounter) : Hub
{
    private const string RoomItemKey = "roomId";

    public override async Task OnConnectedAsync()
    {
        // Operational metric: SignalR connection establishments / reconnects.
        await metricsCounter.IncrementAsync(MetricKeys.SignalrReconnections, Context.ConnectionAborted);
        await base.OnConnectedAsync();
    }

    public async Task JoinRoom(Guid roomId)
    {
        var userId = Context.User!.GetUserId();

        if (!await roomQueries.IsActiveMemberAsync(roomId, userId, Context.ConnectionAborted))
        {
            await SendErrorAsync(DomainErrors.Rooms.NotMember);
            return;
        }

        // One room per connection: leave any previous room first.
        if (Context.Items.TryGetValue(RoomItemKey, out var existing) && existing is Guid previous && previous != roomId)
        {
            await LeaveCurrentRoomAsync(previous, userId);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, RoomGroups.Name(roomId));
        await presenceStore.AddAsync(roomId, userId, Context.ConnectionId, Context.ConnectionAborted);
        Context.Items[RoomItemKey] = roomId;

        var onlineIds = await presenceStore.GetOnlineUserIdsAsync(roomId, Context.ConnectionAborted);
        var members = await roomQueries.GetActiveMembersAsync(roomId, Context.ConnectionAborted);
        var member = members.FirstOrDefault(m => m.UserId == userId);
        if (member is not null)
        {
            await notifier.MemberJoinedAsync(
                new MemberJoinedEvent(roomId, member with { IsOnline = true }, onlineIds.Count),
                Context.ConnectionAborted);
        }

        await notifier.PresenceUpdatedAsync(new PresenceUpdatedEvent(roomId, onlineIds), Context.ConnectionAborted);
    }

    public async Task LeaveRoom()
    {
        var userId = Context.User!.GetUserId();
        if (Context.Items.TryGetValue(RoomItemKey, out var existing) && existing is Guid roomId)
        {
            await LeaveCurrentRoomAsync(roomId, userId);
            Context.Items.Remove(RoomItemKey);
        }
    }

    public Task Heartbeat() => presenceStore.HeartbeatAsync(Context.ConnectionId, Context.ConnectionAborted);

    public Task Play(Guid roomId) => DispatchAsync(new PlaybackPlayCommand(Context.User!.GetUserId(), roomId));

    public Task Pause(Guid roomId) => DispatchAsync(new PlaybackPauseCommand(Context.User!.GetUserId(), roomId));

    public Task Seek(Guid roomId, double positionSeconds) =>
        DispatchAsync(new PlaybackSeekCommand(Context.User!.GetUserId(), roomId, positionSeconds));

    public Task ChangeMedia(Guid roomId, string url, string? title) =>
        DispatchAsync(new ChangeMediaCommand(Context.User!.GetUserId(), roomId, url, title));

    public Task SendMessage(Guid roomId, string content) =>
        DispatchAsync(new SendMessageCommand(Context.User!.GetUserId(), roomId, content));

    public Task DeleteMessage(Guid messageId) =>
        DispatchAsync(new DeleteMessageCommand(Context.User!.GetUserId(), messageId, Context.User!.IsAdmin()));

    /// <summary>Called by clients when their player raises an error (playback-errors metric).</summary>
    public Task ReportPlaybackError() => metricsCounter.IncrementAsync(MetricKeys.PlaybackErrors, Context.ConnectionAborted);

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var removal = await presenceStore.RemoveAsync(Context.ConnectionId, CancellationToken.None);
        if (removal is not null && !removal.UserStillConnected)
        {
            await BroadcastLeftAsync(removal.RoomId, removal.UserId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task LeaveCurrentRoomAsync(Guid roomId, Guid userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, RoomGroups.Name(roomId));
        var removal = await presenceStore.RemoveAsync(Context.ConnectionId, Context.ConnectionAborted);
        if (removal is not null && !removal.UserStillConnected)
        {
            await BroadcastLeftAsync(roomId, userId);
        }
    }

    private async Task BroadcastLeftAsync(Guid roomId, Guid userId)
    {
        var onlineIds = await presenceStore.GetOnlineUserIdsAsync(roomId, CancellationToken.None);
        await notifier.MemberLeftAsync(new MemberLeftEvent(roomId, userId, onlineIds.Count), CancellationToken.None);
        await notifier.PresenceUpdatedAsync(new PresenceUpdatedEvent(roomId, onlineIds), CancellationToken.None);
    }

    private async Task DispatchAsync<TResponse>(ICommand<Result<TResponse>> command)
    {
        var result = await dispatcher.Send(command, Context.ConnectionAborted);
        if (result.IsFailure)
        {
            await SendErrorAsync(result.Error);
        }
    }

    private async Task DispatchAsync(ICommand<Result> command)
    {
        var result = await dispatcher.Send(command, Context.ConnectionAborted);
        if (result.IsFailure)
        {
            await SendErrorAsync(result.Error);
        }
    }

    private Task SendErrorAsync(Error error) =>
        Clients.Caller.SendAsync(RealtimeEvents.HubError, new HubErrorEvent(error.Code, error.Message), Context.ConnectionAborted);
}
