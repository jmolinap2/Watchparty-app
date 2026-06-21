using WatchParty.Contracts.Admin;
using WatchParty.Contracts.Common;
using WatchParty.Contracts.Playback;
using WatchParty.Contracts.Rooms;

namespace WatchParty.Application.Abstractions.Persistence;

/// <summary>Read-side projections for rooms. Online status is layered on by handlers (from presence).</summary>
public interface IRoomQueries
{
    Task<bool> IsActiveMemberAsync(Guid roomId, Guid userId, CancellationToken cancellationToken);
    Task<RoomDto?> GetRoomAsync(Guid roomId, CancellationToken cancellationToken);
    Task<RoomDto?> GetRoomByCodeAsync(string code, CancellationToken cancellationToken);
    Task<IReadOnlyList<RoomMemberDto>> GetActiveMembersAsync(Guid roomId, CancellationToken cancellationToken);
    Task<MediaDto?> GetMediaAsync(Guid mediaId, CancellationToken cancellationToken);
    Task<MediaDto?> GetCurrentMediaAsync(Guid roomId, CancellationToken cancellationToken);
    Task<IReadOnlyList<RoomHistoryItemDto>> GetUserRoomHistoryAsync(Guid userId, int limit, CancellationToken cancellationToken);

    // Admin
    Task<PagedResult<AdminRoomDto>> GetRoomsAsync(string? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<AdminRoomDto?> GetAdminRoomAsync(Guid roomId, CancellationToken cancellationToken);
    Task<long> CountRoomsAsync(CancellationToken cancellationToken);
}
