import { apiRequest } from "@/lib/api/http";
import type {
  CreateRoomRequest,
  RoomDetailDto,
  RoomDto,
  RoomHistoryItemDto,
} from "@/lib/api/types";

export const roomsApi = {
  create: (body: CreateRoomRequest) =>
    apiRequest<RoomDto>("/api/rooms", { method: "POST", body }),
  history: () => apiRequest<RoomHistoryItemDto[]>("/api/rooms/history"),
  getByCode: (code: string) => apiRequest<RoomDto>(`/api/rooms/by-code/${encodeURIComponent(code)}`),
  join: (code: string) =>
    apiRequest<RoomDetailDto>("/api/rooms/join", { method: "POST", body: { code } }),
  detail: (id: string) => apiRequest<RoomDetailDto>(`/api/rooms/${id}`),
  leave: (id: string) => apiRequest<void>(`/api/rooms/${id}/leave`, { method: "POST" }),
  close: (id: string) => apiRequest<void>(`/api/rooms/${id}/close`, { method: "POST" }),
  transferHost: (id: string, toUserId: string) =>
    apiRequest<void>(`/api/rooms/${id}/transfer-host`, { method: "POST", body: { toUserId } }),
  kick: (id: string, userId: string) =>
    apiRequest<void>(`/api/rooms/${id}/kick`, { method: "POST", body: { userId } }),
};
