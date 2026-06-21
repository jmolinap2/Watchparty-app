namespace WatchParty.Contracts.Realtime;

/// <summary>
/// Canonical names of the server-to-client SignalR events. Clients subscribe to
/// these exact names (see docs/realtime-events.md).
/// </summary>
public static class RealtimeEvents
{
    public const string PlaybackStateChanged = "PlaybackStateChanged";
    public const string MediaChanged = "MediaChanged";
    public const string MemberJoined = "MemberJoined";
    public const string MemberLeft = "MemberLeft";
    public const string PresenceUpdated = "PresenceUpdated";
    public const string HostTransferred = "HostTransferred";
    public const string MemberKicked = "MemberKicked";
    public const string YouWereKicked = "YouWereKicked";
    public const string RoomClosed = "RoomClosed";
    public const string ChatMessageReceived = "ChatMessageReceived";
    public const string ChatMessageDeleted = "ChatMessageDeleted";
    public const string HubError = "HubError";
}

/// <summary>
/// Canonical names of the client-to-server hub methods (invoked by clients on
/// <c>RoomHub</c>). Kept here so client and server agree on the wire contract.
/// </summary>
public static class RealtimeMethods
{
    public const string JoinRoom = "JoinRoom";
    public const string LeaveRoom = "LeaveRoom";
    public const string Play = "Play";
    public const string Pause = "Pause";
    public const string Seek = "Seek";
    public const string ChangeMedia = "ChangeMedia";
    public const string SendMessage = "SendMessage";
    public const string DeleteMessage = "DeleteMessage";
    public const string Heartbeat = "Heartbeat";
}
