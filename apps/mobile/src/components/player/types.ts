import type { MediaDto } from "@/lib/api/types";

/**
 * Imperative handle every concrete player exposes so the sync engine can drive
 * it uniformly. Google Drive's embedded player has no JS control surface, so its
 * handle reports `controllable: false` and seek/play/pause are no-ops.
 */
export interface PlayerHandle {
  getCurrentTime: () => number;
  getDuration: () => number;
  seek: (seconds: number) => void;
  play: () => void;
  pause: () => void;
  isReady: () => boolean;
  controllable: boolean;
}

export interface PlayerProps {
  media: MediaDto;
  onReady?: () => void;
  onError?: () => void;
}
