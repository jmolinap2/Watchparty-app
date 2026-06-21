using WatchParty.Domain.Common;

namespace WatchParty.Application.Abstractions.Messaging;

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> Handle(TCommand command, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand>
    where TCommand : ICommand<Result>
{
    Task<Result> Handle(TCommand command, CancellationToken cancellationToken);
}
