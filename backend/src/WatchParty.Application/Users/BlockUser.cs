using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Domain.Common;
using WatchParty.Domain.Users;

namespace WatchParty.Application.Users;

public sealed record BlockUserCommand(Guid UserId, Guid BlockedUserId) : ICommand<Result>;

public sealed class BlockUserCommandHandler(
    IUserRepository userRepository,
    IUserBlockRepository blockRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<BlockUserCommand, Result>
{
    public async Task<Result> Handle(BlockUserCommand command, CancellationToken cancellationToken)
    {
        if (command.UserId == command.BlockedUserId)
        {
            return DomainErrors.Users.CannotBlockSelf;
        }

        var target = await userRepository.GetByIdAsync(command.BlockedUserId, cancellationToken);
        if (target is null)
        {
            return DomainErrors.Users.NotFound;
        }

        if (await blockRepository.ExistsAsync(command.UserId, command.BlockedUserId, cancellationToken))
        {
            return DomainErrors.Users.AlreadyBlocked;
        }

        var blockResult = UserBlock.Create(command.UserId, command.BlockedUserId);
        if (blockResult.IsFailure)
        {
            return blockResult.Error;
        }

        await blockRepository.AddAsync(blockResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
