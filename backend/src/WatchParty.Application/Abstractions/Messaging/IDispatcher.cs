namespace WatchParty.Application.Abstractions.Messaging;

/// <summary>
/// Entry point used by controllers and hubs to execute use cases. Runs the
/// contract-level validation pipeline before invoking the handler.
/// </summary>
public interface IDispatcher
{
    Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);

    Task<TResponse> Query<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
}
