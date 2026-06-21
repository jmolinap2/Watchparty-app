namespace WatchParty.Application.Abstractions.State;

/// <summary>Result of removing a connection: tells callers whether to broadcast "left".</summary>
public sealed record PresenceRemoval(Guid RoomId, Guid UserId, bool UserStillConnected);

/// <summary>
/// Tracks live connections and room presence in Redis. A user may have several
/// connections (multi-device); "left" is only true when the last one closes.
/// Entries carry a TTL so dead connections are cleaned up automatically.
/// </summary>
public interface IPresenceStore
{
    Task AddAsync(Guid roomId, Guid userId, string connectionId, CancellationToken cancellationToken);

    /// <summary>Removes a connection. Returns the affected room/user, or null if unknown.</summary>
    Task<PresenceRemoval?> RemoveAsync(string connectionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> GetOnlineUserIdsAsync(Guid roomId, CancellationToken cancellationToken);

    Task<int> GetOnlineCountAsync(Guid roomId, CancellationToken cancellationToken);

    Task<bool> IsUserOnlineInRoomAsync(Guid roomId, Guid userId, CancellationToken cancellationToken);

    /// <summary>Refreshes the TTL for a live connection (heartbeat).</summary>
    Task HeartbeatAsync(string connectionId, CancellationToken cancellationToken);

    /// <summary>Total distinct users currently online across all rooms (metric).</summary>
    Task<long> GetGlobalOnlineUserCountAsync(CancellationToken cancellationToken);

    /// <summary>Number of rooms that currently have at least one online user (metric).</summary>
    Task<long> GetActiveRoomCountAsync(CancellationToken cancellationToken);
}
