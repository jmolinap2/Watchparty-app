namespace WatchParty.Api.Realtime;

/// <summary>Single source of truth for a room's SignalR group name.</summary>
public static class RoomGroups
{
    public static string Name(Guid roomId) => $"room:{roomId:N}";
}
