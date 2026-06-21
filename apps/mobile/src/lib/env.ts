import Constants from "expo-constants";

// API base URL comes from app.json `expo.extra.apiUrl`, overridable via the
// EXPO_PUBLIC_API_URL environment variable for builds.
const fromExtra = (Constants.expoConfig?.extra as { apiUrl?: string } | undefined)?.apiUrl;

export const API_URL = (
  process.env.EXPO_PUBLIC_API_URL ??
  fromExtra ??
  "http://localhost:5210"
).replace(/\/$/, "");

export const HUB_URL = `${API_URL}/hubs/room`;
