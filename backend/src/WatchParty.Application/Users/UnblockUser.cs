using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Domain.Common;

namespace WatchParty.Application.Users;

public sealed record UnblockUserCommand(Guid UserId, Guid BlockedUserId) : ICommand<Result>;

public sealed class UnblockUserCommandHandler(IUserBlockRepository blockRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<UnblockUserCommand, Result>
{
    public async Task<Result> Handle(UnblockUserCommand command, CancellationToken cancellationToken)
    {
        var block = await blockRepository.GetAsync(command.UserId, command.BlockedUserId, cancellationToken);
        if (block is not null)
        {
            blockRepository.Remove(block);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
