"use client";

/* eslint-disable @typescript-eslint/no-explicit-any */
import { forwardRef, useEffect, useImperativeHandle, useRef } from "react";
import type { PlayerHandle, PlayerProps } from "@/components/player/types";
import { loadYouTubeApi, YT_STATE } from "@/components/player/youtube-api";

/**
 * YouTube player backed by the IFrame Player API, which (unlike a raw embed)
 * lets the sync engine drive play/pause/seek programmatically.
 */
export const YouTubePlayer = forwardRef<PlayerHandle, PlayerProps>(function YouTubePlayer(
  { media, onReady, onUserPlay, onUserPause, onError },
  ref,
) {
  const hostRef = useRef<HTMLDivElement | null>(null);
  const playerRef = useRef<any>(null);
  const readyRef = useRef(false);
  const suppressUntil = useRef(0);

  const suppress = () => {
    suppressUntil.current = Date.now() + 700;
  };
  const isSuppressed = () => Date.now() < suppressUntil.current;

  useImperativeHandle(ref, () => ({
    controllable: true,
    isReady: () => readyRef.current,
    getCurrentTime: () => {
      try {
        return playerRef.current?.getCurrentTime?.() ?? 0;
      } catch {
        return 0;
      }
    },
    getDuration: () => {
      try {
        return playerRef.current?.getDuration?.() ?? 0;
      } catch {
        return 0;
      }
    },
    seek: (seconds: number) => {
      suppress();
      playerRef.current?.seekTo?.(seconds, true);
    },
    play: () => {
      suppress();
      playerRef.current?.playVideo?.();
    },
    pause: () => {
      suppress();
      playerRef.current?.pauseVideo?.();
    },
  }));

  useEffect(() => {
    let destroyed = false;
    const videoId = media.providerId ?? "";

    loadYouTubeApi()
      .then((YT) => {
        if (destroyed || !hostRef.current) return;
        playerRef.current = new YT.Player(hostRef.current, {
          videoId,
          playerVars: { rel: 0, modestbranding: 1, playsinline: 1 },
          events: {
            onReady: () => {
              readyRef.current = true;
              onReady?.();
            },
            onStateChange: (event: any) => {
              if (isSuppressed()) return;
              if (event.data === YT_STATE.PLAYING) onUserPlay?.();
              else if (event.data === YT_STATE.PAUSED) onUserPause?.();
            },
            onError: () => onError?.(),
          },
        });
      })
      .catch(() => onError?.());

    return () => {
      destroyed = true;
      try {
        playerRef.current?.destroy?.();
      } catch {
        // ignore
      }
      playerRef.current = null;
      readyRef.current = false;
    };
  }, [media.providerId, onReady, onUserPlay, onUserPause, onError]);

  return (
    <div className="h-full w-full bg-black">
      <div ref={hostRef} className="h-full w-full" />
    </div>
  );
});
