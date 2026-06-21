"use client";

import { create } from "zustand";
import { persist } from "zustand/middleware";
import type { AuthResponse, UserProfileDto } from "@/lib/api/types";

interface AuthState {
  user: UserProfileDto | null;
  accessToken: string | null;
  refreshToken: string | null;
  accessTokenExpiresAtUtc: string | null;
  hydrated: boolean;
  setSession: (auth: AuthResponse) => void;
  setUser: (user: UserProfileDto) => void;
  clear: () => void;
  setHydrated: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      accessToken: null,
      refreshToken: null,
      accessTokenExpiresAtUtc: null,
      hydrated: false,
      setSession: (auth) =>
        set({
          user: auth.user,
          accessToken: auth.accessToken,
          refreshToken: auth.refreshToken,
          accessTokenExpiresAtUtc: auth.accessTokenExpiresAtUtc,
        }),
      setUser: (user) => set({ user }),
      clear: () =>
        set({
          user: null,
          accessToken: null,
          refreshToken: null,
          accessTokenExpiresAtUtc: null,
        }),
      setHydrated: () => set({ hydrated: true }),
    }),
    {
      name: "watchparty-auth",
      onRehydrateStorage: () => (state) => state?.setHydrated(),
      partialize: (state) => ({
        user: state.user,
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        accessTokenExpiresAtUtc: state.accessTokenExpiresAtUtc,
      }),
    },
  ),
);

export const authStore = useAuthStore;
