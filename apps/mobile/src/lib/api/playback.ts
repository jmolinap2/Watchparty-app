import { apiRequest } from "@/lib/api/http";
import type { PlaybackStateDto } from "@/lib/api/types";

export const playbackApi = {
  state: (roomId: string) => apiRequest<PlaybackStateDto>(`/api/rooms/${roomId}/playback/state`),
  changeMedia: (roomId: string, url: string, title: string | null) =>
    apiRequest<PlaybackStateDto>(`/api/rooms/${roomId}/playback/media`, {
      method: "POST",
      body: { url, title },
    }),
};
