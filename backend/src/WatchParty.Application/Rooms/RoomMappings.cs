using WatchParty.Contracts.Rooms;
using WatchParty.Domain.Rooms;

namespace WatchParty.Application.Rooms;

public static class RoomMappings
{
    public static RoomDto ToDto(this Room room, int onlineCount) => new(
        room.Id,
        room.Code.Value,
        room.Name,
        room.HostUserId,
        room.Settings.IsPrivate,
        room.Settings.MaxMembers,
        room.Status.ToString(),
        onlineCount,
        room.CreatedAtUtc);
}
