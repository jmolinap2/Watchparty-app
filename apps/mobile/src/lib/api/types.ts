// TypeScript mirror of WatchParty.Contracts. Keep in sync with the backend DTOs.

export type MediaKind = "Direct" | "Hls" | "YouTube" | "GoogleDrive" | "Mega";
export type PlaybackStatus = "Idle" | "Playing" | "Paused" | "Ended";
export type UserRole = "User" | "Admin";
export type RoomStatus = "Active" | "Closed";
export type RoomRole = "Host" | "Member";

export interface ApiErrorResponse {
  code: string;
  message: string;
  details?: Record<string, string[]>;
  correlationId?: string;
}

export interface UserProfileDto {
  id: string;
  email: string;
  displayName: string;
  avatarUrl: string | null;
  isPrivate: boolean;
  role: UserRole;
  emailConfirmed: boolean;
  createdAtUtc: string;
}

export interface PublicUserDto {
  id: string;
  displayName: string;
  avatarUrl: string | null;
}

export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  user: UserProfileDto;
}

export interface RoomDto {
  id: string;
  code: string;
  name: string;
  hostUserId: string;
  isPrivate: boolean;
  maxMembers: number;
  status: RoomStatus;
  onlineCount: number;
  createdAtUtc: string;
}

export interface RoomMemberDto {
  userId: string;
  displayName: string;
  avatarUrl: string | null;
  role: RoomRole;
  isOnline: boolean;
  joinedAtUtc: string;
}

export interface MediaDto {
  id: string;
  kind: MediaKind;
  url: string;
  providerId: string | null;
  title: string;
  addedByUserId: string;
  createdAtUtc: string;
}

export interface PlaybackStateDto {
  roomId: string;
  mediaId: string | null;
  status: PlaybackStatus;
  positionSeconds: number;
  serverTimestampUtc: string;
  version: number;
  updatedByUserId: string;
}

export interface RoomDetailDto {
  room: RoomDto;
  members: RoomMemberDto[];
  currentMedia: MediaDto | null;
  playback: PlaybackStateDto | null;
}

export interface RoomHistoryItemDto {
  roomId: string;
  code: string;
  name: string;
  role: RoomRole;
  status: RoomStatus;
  joinedAtUtc: string;
  leftAtUtc: string | null;
}

export interface ChatMessageDto {
  id: string;
  roomId: string;
  senderUserId: string;
  senderDisplayName: string;
  senderAvatarUrl: string | null;
  content: string;
  isDeleted: boolean;
  createdAtUtc: string;
}

export interface ReportDto {
  id: string;
  type: string;
  reporterUserId: string;
  targetUserId: string | null;
  targetMessageId: string | null;
  roomId: string | null;
  reason: string;
  status: string;
  createdAtUtc: string;
  resolvedByUserId: string | null;
  resolvedAtUtc: string | null;
  resolutionNote: string | null;
}

export interface RegisterRequest {
  email: string;
  password: string;
  displayName: string;
}
export interface LoginRequest {
  email?: string;
  identifier?: string;
  username?: string;
  password: string;
}
export interface CreateRoomRequest {
  name: string;
  isPrivate: boolean;
  maxMembers: number | null;
}
export interface UpdateProfileRequest {
  displayName: string;
  isPrivate: boolean;
}
export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}
