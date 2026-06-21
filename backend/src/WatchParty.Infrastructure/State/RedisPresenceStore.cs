using StackExchange.Redis;
using WatchParty.Application.Abstractions.State;

namespace WatchParty.Infrastructure.State;

/// <summary>
/// Redis-backed presence (architecture §13). Tracks per-connection liveness with a
/// TTL so dead connections can be swept, counts multiple connections per user
/// (multi-device), and maintains global counters for the admin metrics.
/// </summary>
public sealed class RedisPresenceStore(IConnectionMultiplexer redis) : IPresenceStore, IPresenceMaintenance
{
    private static readonly TimeSpan ConnectionTtl = TimeSpan.FromSeconds(90);

    private readonly IDatabase _db = redis.GetDatabase();

    private static string ConnKey(string connectionId) => $"presence:conn:{connectionId}";
    private static string RoomConnsKey(Guid roomId) => $"presence:room:{roomId:N}";
    private const string ActiveRoomsKey = "presence:active_rooms";
    private const string OnlineUsersKey = "presence:online_users";

    public async Task AddAsync(Guid roomId, Guid userId, string connectionId, CancellationToken cancellationToken)
    {
        await _db.StringSetAsync(ConnKey(connectionId), $"{roomId}|{userId}", ConnectionTtl);
        await _db.HashSetAsync(RoomConnsKey(roomId), connectionId, userId.ToString());
        await _db.SetAddAsync(ActiveRoomsKey, roomId.ToString());
        await _db.HashIncrementAsync(OnlineUsersKey, userId.ToString());
    }

    public async Task<PresenceRemoval?> RemoveAsync(string connectionId, CancellationToken cancellationToken)
    {
        var connValue = await _db.StringGetAsync(ConnKey(connectionId));
        if (connValue.IsNullOrEmpty)
        {
            return null;
        }

        var parts = connValue.ToString().Split('|');
        if (parts.Length != 2 || !Guid.TryParse(parts[0], out var roomId) || !Guid.TryParse(parts[1], out var userId))
        {
            return null;
        }

        await _db.KeyDeleteAsync(ConnKey(connectionId));
        await _db.HashDeleteAsync(RoomConnsKey(roomId), connectionId);
        await DecrementOnlineUserAsync(userId);

        var remaining = await _db.HashValuesAsync(RoomConnsKey(roomId));
        var userStillConnected = remaining.Any(v => v == userId.ToString());
        if (remaining.Length == 0)
        {
            await _db.SetRemoveAsync(ActiveRoomsKey, roomId.ToString());
        }

        return new PresenceRemoval(roomId, userId, userStillConnected);
    }

    public async Task<IReadOnlyList<Guid>> GetOnlineUserIdsAsync(Guid roomId, CancellationToken cancellationToken)
    {
        var values = await _db.HashValuesAsync(RoomConnsKey(roomId));
        return ParseDistinctUserIds(values);
    }

    public async Task<int> GetOnlineCountAsync(Guid roomId, CancellationToken cancellationToken)
    {
        var values = await _db.HashValuesAsync(RoomConnsKey(roomId));
        return ParseDistinctUserIds(values).Count;
    }

    public async Task<bool> IsUserOnlineInRoomAsync(Guid roomId, Guid userId, CancellationToken cancellationToken)
    {
        var values = await _db.HashValuesAsync(RoomConnsKey(roomId));
        return values.Any(v => v == userId.ToString());
    }

    public Task HeartbeatAsync(string connectionId, CancellationToken cancellationToken) =>
        _db.KeyExpireAsync(ConnKey(connectionId), ConnectionTtl);

    public async Task<long> GetGlobalOnlineUserCountAsync(CancellationToken cancellationToken) =>
        await _db.HashLengthAsync(OnlineUsersKey);

    public async Task<long> GetActiveRoomCountAsync(CancellationToken cancellationToken) =>
        await _db.SetLengthAsync(ActiveRoomsKey);

    // --- IPresenceMaintenance (dead-connection sweeper) ---

    public async Task<IReadOnlyList<Guid>> GetActiveRoomIdsAsync(CancellationToken cancellationToken)
    {
        var members = await _db.SetMembersAsync(ActiveRoomsKey);
        return members
            .Select(m => Guid.TryParse(m.ToString(), out var id) ? id : (Guid?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();
    }

    public async Task<PresenceSweepResult> SweepRoomAsync(Guid roomId, CancellationToken cancellationToken)
    {
        var entries = await _db.HashGetAllAsync(RoomConnsKey(roomId));
        if (entries.Length == 0)
        {
            await _db.SetRemoveAsync(ActiveRoomsKey, roomId.ToString());
            return new PresenceSweepResult([], [], Changed: false);
        }

        var deadUserIds = new List<Guid>();
        var changed = false;

        foreach (var entry in entries)
        {
            var connectionId = entry.Name.ToString();
            if (await _db.KeyExistsAsync(ConnKey(connectionId)))
            {
                continue;
            }

            // Connection's TTL expired without a clean disconnect — remove it.
            await _db.HashDeleteAsync(RoomConnsKey(roomId), connectionId);
            if (Guid.TryParse(entry.Value.ToString(), out var userId))
            {
                await DecrementOnlineUserAsync(userId);
                deadUserIds.Add(userId);
            }

            changed = true;
        }

        var remaining = ParseDistinctUserIds(await _db.HashValuesAsync(RoomConnsKey(roomId)));
        if (remaining.Count == 0)
        {
            await _db.SetRemoveAsync(ActiveRoomsKey, roomId.ToString());
        }

        var wentOffline = deadUserIds.Distinct().Where(id => !remaining.Contains(id)).ToList();
        return new PresenceSweepResult(wentOffline, remaining, changed);
    }

    private async Task DecrementOnlineUserAsync(Guid userId)
    {
        var remaining = await _db.HashDecrementAsync(OnlineUsersKey, userId.ToString());
        if (remaining <= 0)
        {
            await _db.HashDeleteAsync(OnlineUsersKey, userId.ToString());
        }
    }

    private static List<Guid> ParseDistinctUserIds(RedisValue[] values)
    {
        var set = new HashSet<Guid>();
        foreach (var value in values)
        {
            if (Guid.TryParse(value.ToString(), out var id))
            {
                set.Add(id);
            }
        }

        return set.ToList();
    }
}
