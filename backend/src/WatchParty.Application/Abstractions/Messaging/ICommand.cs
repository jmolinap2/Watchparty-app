using WatchParty.Domain.Common;

namespace WatchParty.Application.Abstractions.Messaging;

/// <summary>A command that changes state and returns <typeparamref name="TResponse"/>.</summary>
public interface ICommand<TResponse>;

/// <summary>A command that changes state and returns a bare <see cref="Result"/>.</summary>
public interface ICommand : ICommand<Result>;
