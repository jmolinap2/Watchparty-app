import { create } from "zustand";
import { createJSONStorage, persist, type StateStorage } from "zustand/middleware";
import * as SecureStore from "expo-secure-store";
import type { AuthResponse, UserProfileDto } from "@/lib/api/types";

// SecureStore-backed storage adapter for zustand's persist middleware.
const secureStorage: StateStorage = {
  getItem: (name) => SecureStore.getItemAsync(name),
  setItem: (name, value) => SecureStore.setItemAsync(name, value),
  removeItem: (name) => SecureStore.deleteItemAsync(name),
};

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
      storage: createJSONStorage(() => secureStorage),
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
