import type {
  ChatMessageDto,
  MediaDto,
  PlaybackStateDto,
  RoomMemberDto,
} from "@/lib/api/types";

// Server-to-client event names (must match WatchParty.Contracts.RealtimeEvents).
export const RealtimeEvents = {
  PlaybackStateChanged: "PlaybackStateChanged",
  MediaChanged: "MediaChanged",
  MemberJoined: "MemberJoined",
  MemberLeft: "MemberLeft",
  PresenceUpdated: "PresenceUpdated",
  HostTransferred: "HostTransferred",
  MemberKicked: "MemberKicked",
  YouWereKicked: "YouWereKicked",
  RoomClosed: "RoomClosed",
  ChatMessageReceived: "ChatMessageReceived",
  ChatMessageDeleted: "ChatMessageDeleted",
  HubError: "HubError",
} as const;

// Client-to-server hub methods (must match WatchParty.Contracts.RealtimeMethods).
export const RealtimeMethods = {
  JoinRoom: "JoinRoom",
  LeaveRoom: "LeaveRoom",
  Play: "Play",
  Pause: "Pause",
  Seek: "Seek",
  ChangeMedia: "ChangeMedia",
  SendMessage: "SendMessage",
  DeleteMessage: "DeleteMessage",
  Heartbeat: "Heartbeat",
  ReportPlaybackError: "ReportPlaybackError",
} as const;

export interface MemberJoinedEvent {
  roomId: string;
  member: RoomMemberDto;
  onlineCount: number;
}
export interface MemberLeftEvent {
  roomId: string;
  userId: string;
  onlineCount: number;
}
export interface PresenceUpdatedEvent {
  roomId: string;
  onlineUserIds: string[];
}
export interface HostTransferredEvent {
  roomId: string;
  fromUserId: string;
  toUserId: string;
}
export interface MemberKickedEvent {
  roomId: string;
  userId: string;
}
export interface RoomClosedEvent {
  roomId: string;
}
export interface MediaChangedEvent {
  roomId: string;
  media: MediaDto;
  playback: PlaybackStateDto;
}
export interface ChatMessageDeletedEvent {
  roomId: string;
  messageId: string;
  deletedByUserId: string;
}
export interface HubErrorEvent {
  code: string;
  message: string;
}

export type { ChatMessageDto, PlaybackStateDto };
