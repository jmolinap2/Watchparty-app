namespace WatchParty.Application.Abstractions.State;

public sealed record PresenceSweepResult(IReadOnlyList<Guid> UsersWentOffline, IReadOnlyList<Guid> RemainingOnlineUserIds, bool Changed);

/// <summary>
/// Presence cleanup operations used by the background sweeper to remove dead
/// connections (architecture / scope: "Limpieza de conexiones muertas").
/// </summary>
public interface IPresenceMaintenance
{
    Task<IReadOnlyList<Guid>> GetActiveRoomIdsAsync(CancellationToken cancellationToken);
    Task<PresenceSweepResult> SweepRoomAsync(Guid roomId, CancellationToken cancellationToken);
}
