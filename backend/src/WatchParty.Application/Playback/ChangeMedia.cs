using FluentValidation;
using WatchParty.Application.Abstractions;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.Realtime;
using WatchParty.Application.Abstractions.Services;
using WatchParty.Application.Abstractions.State;
using WatchParty.Contracts.Realtime;
using WatchParty.Domain.Common;
using WatchParty.Domain.Playback;

namespace WatchParty.Application.Playback;

public sealed record ChangeMediaCommand(Guid UserId, Guid RoomId, string Url, string? Title)
    : ICommand<Result<MediaChangedEvent>>;

public sealed class ChangeMediaValidator : AbstractValidator<ChangeMediaCommand>
{
    public ChangeMediaValidator() => RuleFor(x => x.Url).NotEmpty().MaximumLength(2048);
}

public sealed class ChangeMediaCommandHandler(
    IRoomRepository roomRepository,
    IMediaItemRepository mediaItemRepository,
    IMediaSourceValidator mediaSourceValidator,
    IPlaybackStateStore playbackStateStore,
    IRoomRealtimeNotifier notifier,
    IClock clock,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ChangeMediaCommand, Result<MediaChangedEvent>>
{
    public async Task<Result<MediaChangedEvent>> Handle(ChangeMediaCommand command, CancellationToken cancellationToken)
    {
        var room = await roomRepository.GetByIdAsync(command.RoomId, cancellationToken);
        if (room is null)
        {
            return DomainErrors.Rooms.NotFound;
        }

        if (!room.IsActive)
        {
            return DomainErrors.Rooms.Closed;
        }

        if (!room.IsMember(command.UserId))
        {
            return DomainErrors.Rooms.NotMember;
        }

        var sourceResult = await mediaSourceValidator.ValidateAsync(command.Url, cancellationToken);
        if (sourceResult.IsFailure)
        {
            return sourceResult.Error;
        }

        var media = MediaItem.Create(command.RoomId, sourceResult.Value, command.Title, command.UserId);
        await mediaItemRepository.AddAsync(media, cancellationToken);

        room.SetCurrentMedia(media.Id);
        roomRepository.Update(room);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Reset the authoritative playback state to the new media (paused at the start).
        var current = await playbackStateStore.GetAsync(command.RoomId, cancellationToken) ?? PlaybackState.Empty(command.RoomId);
        var newState = current.WithMedia(media.Id, command.UserId, clock.UtcNow);
        await playbackStateStore.SaveAsync(newState, cancellationToken);

        var payload = new MediaChangedEvent(command.RoomId, media.ToDto(), newState.ToDto());
        await notifier.MediaChangedAsync(payload, cancellationToken);
        return payload;
    }
}
