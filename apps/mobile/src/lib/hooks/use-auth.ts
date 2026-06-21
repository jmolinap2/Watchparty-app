import { useCallback } from "react";
import { useAuthStore } from "@/lib/store/auth-store";
import { authApi } from "@/lib/api/auth";
import type { LoginRequest, RegisterRequest } from "@/lib/api/types";

export function useAuth() {
  const user = useAuthStore((s) => s.user);
  const accessToken = useAuthStore((s) => s.accessToken);
  const refreshToken = useAuthStore((s) => s.refreshToken);
  const hydrated = useAuthStore((s) => s.hydrated);
  const setSession = useAuthStore((s) => s.setSession);
  const setUser = useAuthStore((s) => s.setUser);
  const clear = useAuthStore((s) => s.clear);

  const login = useCallback(
    async (body: LoginRequest) => {
      const auth = await authApi.login(body);
      setSession(auth);
      return auth;
    },
    [setSession],
  );

  const register = useCallback(
    async (body: RegisterRequest) => {
      const auth = await authApi.register(body);
      setSession(auth);
      return auth;
    },
    [setSession],
  );

  const logout = useCallback(async () => {
    const token = refreshToken;
    clear();
    if (token) {
      try {
        await authApi.logout(token);
      } catch {
        // best-effort
      }
    }
  }, [refreshToken, clear]);

  return {
    user,
    setUser,
    isAuthenticated: Boolean(accessToken && user),
    hydrated,
    login,
    register,
    logout,
  };
}
