using FluentValidation;
using WatchParty.Application.Abstractions;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.Realtime;
using WatchParty.Application.Abstractions.State;
using WatchParty.Contracts.Playback;
using WatchParty.Domain.Common;
using WatchParty.Domain.Playback;

namespace WatchParty.Application.Playback;

// Server-authoritative play / pause / seek (architecture §12). Each produces a new
// versioned, timestamped state which is stored in Redis and broadcast to the room.

public sealed record PlaybackPlayCommand(Guid UserId, Guid RoomId) : ICommand<Result<PlaybackStateDto>>;

public sealed record PlaybackPauseCommand(Guid UserId, Guid RoomId) : ICommand<Result<PlaybackStateDto>>;

public sealed record PlaybackSeekCommand(Guid UserId, Guid RoomId, double PositionSeconds) : ICommand<Result<PlaybackStateDto>>;

public sealed class PlaybackSeekValidator : AbstractValidator<PlaybackSeekCommand>
{
    public PlaybackSeekValidator() => RuleFor(x => x.PositionSeconds).GreaterThanOrEqualTo(0);
}

/// <summary>Shared logic: authorise the member, load the live state, apply a transition, persist and broadcast.</summary>
internal static class PlaybackControl
{
    public static async Task<Result<PlaybackStateDto>> ApplyAsync(
        Guid userId,
        Guid roomId,
        Func<PlaybackState, PlaybackState> transition,
        IRoomQueries roomQueries,
        IPlaybackStateStore store,
        IRoomRealtimeNotifier notifier,
        CancellationToken cancellationToken)
    {
        if (!await roomQueries.IsActiveMemberAsync(roomId, userId, cancellationToken))
        {
            return DomainErrors.Rooms.NotMember;
        }

        var state = await store.GetAsync(roomId, cancellationToken);
        if (state is null || state.MediaId is null)
        {
            return DomainErrors.Playback.NoMediaLoaded;
        }

        var updated = transition(state);
        await store.SaveAsync(updated, cancellationToken);

        var dto = updated.ToDto();
        await notifier.PlaybackStateChangedAsync(roomId, dto, cancellationToken);
        return dto;
    }
}

public sealed class PlaybackPlayCommandHandler(
    IRoomQueries roomQueries,
    IPlaybackStateStore store,
    IRoomRealtimeNotifier notifier,
    IClock clock)
    : ICommandHandler<PlaybackPlayCommand, Result<PlaybackStateDto>>
{
    public Task<Result<PlaybackStateDto>> Handle(PlaybackPlayCommand command, CancellationToken cancellationToken) =>
        PlaybackControl.ApplyAsync(command.UserId, command.RoomId,
            state => state.Play(command.UserId, clock.UtcNow),
            roomQueries, store, notifier, cancellationToken);
}

public sealed class PlaybackPauseCommandHandler(
    IRoomQueries roomQueries,
    IPlaybackStateStore store,
    IRoomRealtimeNotifier notifier,
    IClock clock)
    : ICommandHandler<PlaybackPauseCommand, Result<PlaybackStateDto>>
{
    public Task<Result<PlaybackStateDto>> Handle(PlaybackPauseCommand command, CancellationToken cancellationToken) =>
        PlaybackControl.ApplyAsync(command.UserId, command.RoomId,
            state => state.Pause(command.UserId, clock.UtcNow),
            roomQueries, store, notifier, cancellationToken);
}

public sealed class PlaybackSeekCommandHandler(
    IRoomQueries roomQueries,
    IPlaybackStateStore store,
    IRoomRealtimeNotifier notifier,
    IClock clock)
    : ICommandHandler<PlaybackSeekCommand, Result<PlaybackStateDto>>
{
    public Task<Result<PlaybackStateDto>> Handle(PlaybackSeekCommand command, CancellationToken cancellationToken) =>
        PlaybackControl.ApplyAsync(command.UserId, command.RoomId,
            state => state.Seek(command.UserId, command.PositionSeconds, clock.UtcNow),
            roomQueries, store, notifier, cancellationToken);
}
