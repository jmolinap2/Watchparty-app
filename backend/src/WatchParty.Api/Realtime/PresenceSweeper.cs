using WatchParty.Application.Abstractions.Realtime;
using WatchParty.Application.Abstractions.State;
using WatchParty.Contracts.Realtime;

namespace WatchParty.Api.Realtime;

/// <summary>
/// Periodically removes connections whose heartbeat TTL expired without a clean
/// disconnect, and broadcasts the resulting presence changes (scope: presence
/// reconnection / dead-connection cleanup).
/// </summary>
public sealed class PresenceSweeper(
    IPresenceMaintenance presenceMaintenance,
    IRoomRealtimeNotifier notifier,
    ILogger<PresenceSweeper> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Presence sweep failed.");
            }
        }
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        var roomIds = await presenceMaintenance.GetActiveRoomIdsAsync(cancellationToken);
        foreach (var roomId in roomIds)
        {
            var result = await presenceMaintenance.SweepRoomAsync(roomId, cancellationToken);
            if (!result.Changed)
            {
                continue;
            }

            await notifier.PresenceUpdatedAsync(new PresenceUpdatedEvent(roomId, result.RemainingOnlineUserIds), cancellationToken);
            foreach (var userId in result.UsersWentOffline)
            {
                await notifier.MemberLeftAsync(new MemberLeftEvent(roomId, userId, result.RemainingOnlineUserIds.Count), cancellationToken);
            }
        }
    }
}
