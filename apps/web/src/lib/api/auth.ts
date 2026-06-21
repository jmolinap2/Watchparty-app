import { apiRequest } from "@/lib/api/http";
import type { AuthResponse, LoginRequest, RegisterRequest } from "@/lib/api/types";

export const authApi = {
  register: (body: RegisterRequest) =>
    apiRequest<AuthResponse>("/api/auth/register", { method: "POST", body, auth: false }),

  login: (body: LoginRequest) =>
    apiRequest<AuthResponse>("/api/auth/login", { method: "POST", body, auth: false }),

  refresh: (refreshToken: string) =>
    apiRequest<AuthResponse>("/api/auth/refresh", { method: "POST", body: { refreshToken }, auth: false }),

  logout: (refreshToken: string) =>
    apiRequest<void>("/api/auth/logout", { method: "POST", body: { refreshToken }, auth: false }),

  confirmEmail: (token: string) =>
    apiRequest<void>("/api/auth/confirm-email", { method: "POST", body: { token }, auth: false }),

  resendConfirmation: (email: string) =>
    apiRequest<void>("/api/auth/resend-confirmation", { method: "POST", body: { email }, auth: false }),

  forgotPassword: (email: string) =>
    apiRequest<void>("/api/auth/forgot-password", { method: "POST", body: { email }, auth: false }),

  resetPassword: (token: string, newPassword: string) =>
    apiRequest<void>("/api/auth/reset-password", { method: "POST", body: { token, newPassword }, auth: false }),
};
