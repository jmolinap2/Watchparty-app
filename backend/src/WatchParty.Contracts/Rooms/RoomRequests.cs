namespace WatchParty.Contracts.Rooms;

public sealed record CreateRoomRequest(string Name, bool IsPrivate, int? MaxMembers);

public sealed record JoinRoomRequest(string Code);

public sealed record TransferHostRequest(Guid ToUserId);

public sealed record KickMemberRequest(Guid UserId);
