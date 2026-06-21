import { apiRequest } from "@/lib/api/http";
import type {
  ChangePasswordRequest,
  PublicUserDto,
  UpdateProfileRequest,
  UserProfileDto,
} from "@/lib/api/types";

export const usersApi = {
  me: () => apiRequest<UserProfileDto>("/api/users/me"),
  updateProfile: (body: UpdateProfileRequest) =>
    apiRequest<UserProfileDto>("/api/users/me", { method: "PUT", body }),
  setAvatar: (avatarUrl: string | null) =>
    apiRequest<UserProfileDto>("/api/users/me/avatar", { method: "PUT", body: { avatarUrl } }),
  changePassword: (body: ChangePasswordRequest) =>
    apiRequest<void>("/api/users/me/password", { method: "PUT", body }),
  blocked: () => apiRequest<PublicUserDto[]>("/api/users/me/blocked"),
  block: (userId: string) =>
    apiRequest<void>("/api/users/blocks", { method: "POST", body: { userId } }),
  unblock: (userId: string) =>
    apiRequest<void>(`/api/users/blocks/${userId}`, { method: "DELETE" }),
};
