using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Contracts.Users;
using WatchParty.Domain.Common;

namespace WatchParty.Application.Users;

public sealed record GetBlockedUsersQuery(Guid UserId) : IQuery<Result<IReadOnlyList<PublicUserDto>>>;

public sealed class GetBlockedUsersQueryHandler(IUserQueries userQueries)
    : IQueryHandler<GetBlockedUsersQuery, Result<IReadOnlyList<PublicUserDto>>>
{
    public async Task<Result<IReadOnlyList<PublicUserDto>>> Handle(GetBlockedUsersQuery query, CancellationToken cancellationToken)
    {
        var blocked = await userQueries.GetBlockedUsersAsync(query.UserId, cancellationToken);
        return Result.Success(blocked);
    }
}
