"use client";

import { forwardRef, useImperativeHandle } from "react";
import type { PlayerHandle, PlayerProps } from "@/components/player/types";

/**
 * Google Drive playback via the official preview embed. Drive's embedded player
 * exposes no JavaScript control surface, so it is not programmatically
 * synchronizable — the handle reports `controllable: false` and the room shows a
 * notice. This respects the "no download / no bypass" scope: we only stream the
 * user's own shared file through Google's player.
 */
export const DrivePlayer = forwardRef<PlayerHandle, PlayerProps>(function DrivePlayer({ media }, ref) {
  useImperativeHandle(ref, () => ({
    controllable: false,
    isReady: () => true,
    getCurrentTime: () => 0,
    getDuration: () => 0,
    seek: () => undefined,
    play: () => undefined,
    pause: () => undefined,
  }));

  const fileId = media.providerId ?? "";
  const src = `https://drive.google.com/file/d/${fileId}/preview`;

  return (
    <iframe
      title={media.title}
      src={src}
      className="h-full w-full bg-black"
      allow="autoplay; fullscreen"
      allowFullScreen
    />
  );
});
