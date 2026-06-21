import type { MediaDto } from "@/lib/api/types";

/**
 * Imperative handle every concrete player exposes so the sync engine can drive
 * it uniformly. Google Drive's embedded player has no JS control surface, so its
 * handle reports `controllable: false` and the seek/play/pause calls are no-ops.
 */
export interface PlayerHandle {
  getCurrentTime: () => number;
  getDuration: () => number;
  seek: (seconds: number) => void;
  play: () => void;
  pause: () => void;
  isReady: () => boolean;
  /** Whether the sync engine can programmatically control this player. */
  controllable: boolean;
}

export interface PlayerProps {
  media: MediaDto;
  /** Fired once the underlying player is ready to accept commands. */
  onReady?: () => void;
  /** Fired when a local user gesture plays the video (host-intent → server). */
  onUserPlay?: () => void;
  onUserPause?: () => void;
  onUserSeek?: (seconds: number) => void;
  /** Fired on a playback error so the room can report the metric. */
  onError?: () => void;
}
