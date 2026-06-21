namespace WatchParty.Application.Abstractions.State;

/// <summary>
/// High-frequency operational counters kept in Redis (architecture §13): playback
/// errors and SignalR reconnections for the admin metrics view.
/// </summary>
public interface IMetricsCounter
{
    Task IncrementAsync(string key, CancellationToken cancellationToken);
    Task<long> GetAsync(string key, CancellationToken cancellationToken);
}

public static class MetricKeys
{
    public const string PlaybackErrors = "metrics:playback_errors";
    public const string SignalrReconnections = "metrics:signalr_reconnections";
}
