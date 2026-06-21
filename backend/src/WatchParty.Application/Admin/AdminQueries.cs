using WatchParty.Application.Abstractions.Admin;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Application.Abstractions.State;
using WatchParty.Contracts.Admin;
using WatchParty.Contracts.Common;
using WatchParty.Domain.Common;

namespace WatchParty.Application.Admin;

// Admin read models. These are admin-only; authorization is enforced at the API.

public sealed record GetUsersQuery(string? Search, int Page, int PageSize) : IQuery<Result<PagedResult<AdminUserDto>>>;

public sealed class GetUsersQueryHandler(IUserQueries userQueries)
    : IQueryHandler<GetUsersQuery, Result<PagedResult<AdminUserDto>>>
{
    public async Task<Result<PagedResult<AdminUserDto>>> Handle(GetUsersQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = Math.Clamp(query.PageSize < 1 ? 20 : query.PageSize, 1, 100);
        return Result.Success(await userQueries.SearchUsersAsync(query.Search, page, pageSize, cancellationToken));
    }
}

public sealed record GetUserDetailQuery(Guid UserId) : IQuery<Result<AdminUserDetailDto>>;

public sealed class GetUserDetailQueryHandler(IUserQueries userQueries)
    : IQueryHandler<GetUserDetailQuery, Result<AdminUserDetailDto>>
{
    public async Task<Result<AdminUserDetailDto>> Handle(GetUserDetailQuery query, CancellationToken cancellationToken)
    {
        var detail = await userQueries.GetAdminUserDetailAsync(query.UserId, cancellationToken);
        return detail is null ? DomainErrors.Users.NotFound : detail;
    }
}

public sealed record GetRoomsQuery(string? Status, int Page, int PageSize) : IQuery<Result<PagedResult<AdminRoomDto>>>;

public sealed class GetRoomsQueryHandler(IRoomQueries roomQueries, IPresenceStore presenceStore)
    : IQueryHandler<GetRoomsQuery, Result<PagedResult<AdminRoomDto>>>
{
    public async Task<Result<PagedResult<AdminRoomDto>>> Handle(GetRoomsQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = Math.Clamp(query.PageSize < 1 ? 20 : query.PageSize, 1, 100);
        var result = await roomQueries.GetRoomsAsync(query.Status, page, pageSize, cancellationToken);

        var enriched = new List<AdminRoomDto>(result.Items.Count);
        foreach (var room in result.Items)
        {
            var online = await presenceStore.GetOnlineCountAsync(room.Id, cancellationToken);
            enriched.Add(room with { OnlineCount = online });
        }

        return Result.Success(result with { Items = enriched });
    }
}

public sealed record GetAdminRoomDetailQuery(Guid RoomId) : IQuery<Result<AdminRoomDto>>;

public sealed class GetAdminRoomDetailQueryHandler(IRoomQueries roomQueries, IPresenceStore presenceStore)
    : IQueryHandler<GetAdminRoomDetailQuery, Result<AdminRoomDto>>
{
    public async Task<Result<AdminRoomDto>> Handle(GetAdminRoomDetailQuery query, CancellationToken cancellationToken)
    {
        var room = await roomQueries.GetAdminRoomAsync(query.RoomId, cancellationToken);
        if (room is null)
        {
            return DomainErrors.Rooms.NotFound;
        }

        var online = await presenceStore.GetOnlineCountAsync(room.Id, cancellationToken);
        return room with { OnlineCount = online };
    }
}

public sealed record GetAllowedDomainsQuery : IQuery<Result<IReadOnlyList<AllowedDomainDto>>>;

public sealed class GetAllowedDomainsQueryHandler(IAdminQueries adminQueries)
    : IQueryHandler<GetAllowedDomainsQuery, Result<IReadOnlyList<AllowedDomainDto>>>
{
    public async Task<Result<IReadOnlyList<AllowedDomainDto>>> Handle(GetAllowedDomainsQuery query, CancellationToken cancellationToken)
        => Result.Success(await adminQueries.GetAllowedDomainsAsync(cancellationToken));
}

public sealed record GetAuditLogsQuery(AuditLogSearchRequest Request) : IQuery<Result<PagedResult<AuditLogDto>>>;

public sealed class GetAuditLogsQueryHandler(IAuditLogReader auditLogReader)
    : IQueryHandler<GetAuditLogsQuery, Result<PagedResult<AuditLogDto>>>
{
    public async Task<Result<PagedResult<AuditLogDto>>> Handle(GetAuditLogsQuery query, CancellationToken cancellationToken)
        => Result.Success(await auditLogReader.SearchAsync(query.Request, cancellationToken));
}
