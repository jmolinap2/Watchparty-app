using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Contracts.Users;
using WatchParty.Domain.Common;

namespace WatchParty.Application.Users;

public sealed record GetMyProfileQuery(Guid UserId) : IQuery<Result<UserProfileDto>>;

public sealed class GetMyProfileQueryHandler(IUserQueries userQueries)
    : IQueryHandler<GetMyProfileQuery, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(GetMyProfileQuery query, CancellationToken cancellationToken)
    {
        var profile = await userQueries.GetProfileAsync(query.UserId, cancellationToken);
        return profile is null ? DomainErrors.Users.NotFound : profile;
    }
}
