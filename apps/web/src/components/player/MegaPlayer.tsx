"use client";

import { forwardRef, useImperativeHandle } from "react";
import type { PlayerHandle, PlayerProps } from "@/components/player/types";

/**
 * MEGA playback via the official embed player. Like Google Drive, MEGA's embed
 * exposes no JavaScript control surface, so it is not programmatically
 * synchronizable — the handle reports `controllable: false` and the room shows a
 * notice. The embed URL (with its decryption key in the fragment) is built and
 * validated server-side and arrives as `media.url`; we only stream the user's own
 * file through MEGA's player (no download, no bypass).
 */
export const MegaPlayer = forwardRef<PlayerHandle, PlayerProps>(function MegaPlayer({ media }, ref) {
  useImperativeHandle(ref, () => ({
    controllable: false,
    isReady: () => true,
    getCurrentTime: () => 0,
    getDuration: () => 0,
    seek: () => undefined,
    play: () => undefined,
    pause: () => undefined,
  }));

  return (
    <iframe
      title={media.title}
      src={media.url}
      className="h-full w-full bg-black"
      allow="autoplay; fullscreen"
      allowFullScreen
    />
  );
});
