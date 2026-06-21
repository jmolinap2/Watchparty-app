namespace WatchParty.Application.Abstractions.Persistence;

/// <summary>
/// Commits the current changes as a single transaction and dispatches any domain
/// events raised by the affected aggregates.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
