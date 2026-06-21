// Single source of truth for the API base URL. Falls back to the local dev port.
export const API_URL = (
  process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5210"
).replace(/\/$/, "");

export const HUB_URL = `${API_URL}/hubs/room`;
