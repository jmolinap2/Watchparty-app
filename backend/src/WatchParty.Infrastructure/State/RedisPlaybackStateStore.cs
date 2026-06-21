using System.Text.Json;
using StackExchange.Redis;
using WatchParty.Application.Abstractions.State;
using WatchParty.Domain.Playback;

namespace WatchParty.Infrastructure.State;

/// <summary>
/// Redis-backed live playback state (architecture §12/§13). Expires after a period
/// of inactivity; it can always be rebuilt from the persisted current media.
/// </summary>
public sealed class RedisPlaybackStateStore(IConnectionMultiplexer redis) : IPlaybackStateStore
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<PlaybackState?> GetAsync(Guid roomId, CancellationToken cancellationToken)
    {
        var value = await _db.StringGetAsync(Key(roomId));
        if (value.IsNullOrEmpty)
        {
            return null;
        }

        var payload = JsonSerializer.Deserialize<PlaybackStatePayload>(value.ToString(), JsonOptions);
        return payload?.ToDomain();
    }

    public Task SaveAsync(PlaybackState state, CancellationToken cancellationToken)
    {
        var payload = PlaybackStatePayload.FromDomain(state);
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        return _db.StringSetAsync(Key(state.RoomId), json, Ttl);
    }

    public Task RemoveAsync(Guid roomId, CancellationToken cancellationToken) =>
        _db.KeyDeleteAsync(Key(roomId));

    private static string Key(Guid roomId) => $"playback:{roomId:N}";

    private sealed record PlaybackStatePayload(
        Guid RoomId,
        Guid? MediaId,
        PlaybackStatus Status,
        double PositionSeconds,
        DateTimeOffset ServerTimestampUtc,
        long Version,
        Guid UpdatedByUserId)
    {
        public static PlaybackStatePayload FromDomain(PlaybackState state) => new(
            state.RoomId, state.MediaId, state.Status, state.PositionSeconds,
            state.ServerTimestampUtc, state.Version, state.UpdatedByUserId);

        public PlaybackState ToDomain() => PlaybackState.FromTrusted(
            RoomId, MediaId, Status, PositionSeconds, ServerTimestampUtc, Version, UpdatedByUserId);
    }
}
