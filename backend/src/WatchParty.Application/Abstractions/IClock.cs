namespace WatchParty.Application.Abstractions;

/// <summary>Abstracts the system clock so use cases stay deterministic and testable.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
