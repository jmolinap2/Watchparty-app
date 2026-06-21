import { apiRequest } from "@/lib/api/http";
import type { PlaybackStateDto } from "@/lib/api/types";

// REST mirror of the playback use cases. Realtime clients normally drive these
// over the SignalR hub; ChangeMedia is exposed here for the "load video" form.
export const playbackApi = {
  state: (roomId: string) =>
    apiRequest<PlaybackStateDto>(`/api/rooms/${roomId}/playback/state`),

  changeMedia: (roomId: string, url: string, title: string | null) =>
    apiRequest<PlaybackStateDto>(`/api/rooms/${roomId}/playback/media`, {
      method: "POST",
      body: { url, title },
    }),

  play: (roomId: string) =>
    apiRequest<PlaybackStateDto>(`/api/rooms/${roomId}/playback/play`, { method: "POST" }),

  pause: (roomId: string) =>
    apiRequest<PlaybackStateDto>(`/api/rooms/${roomId}/playback/pause`, { method: "POST" }),

  seek: (roomId: string, positionSeconds: number) =>
    apiRequest<PlaybackStateDto>(`/api/rooms/${roomId}/playback/seek`, {
      method: "POST",
      body: { positionSeconds },
    }),
};
