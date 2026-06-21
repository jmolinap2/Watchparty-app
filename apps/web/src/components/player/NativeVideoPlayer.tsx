"use client";

import { forwardRef, useEffect, useImperativeHandle, useRef } from "react";
import Hls from "hls.js";
import type { PlayerHandle, PlayerProps } from "@/components/player/types";

/**
 * HTML5 <video> player for Direct (mp4/webm) and HLS (m3u8) sources. HLS uses
 * hls.js where the browser lacks native support (everything but Safari).
 */
export const NativeVideoPlayer = forwardRef<PlayerHandle, PlayerProps>(function NativeVideoPlayer(
  { media, onReady, onUserPlay, onUserPause, onUserSeek, onError },
  ref,
) {
  const videoRef = useRef<HTMLVideoElement | null>(null);
  const readyRef = useRef(false);
  // Suppress echoing programmatic (sync-driven) play/pause/seek back to the server.
  const suppressUntil = useRef(0);

  const suppress = () => {
    suppressUntil.current = Date.now() + 500;
  };
  const isSuppressed = () => Date.now() < suppressUntil.current;

  useImperativeHandle(ref, () => ({
    controllable: true,
    isReady: () => readyRef.current,
    getCurrentTime: () => videoRef.current?.currentTime ?? 0,
    getDuration: () => videoRef.current?.duration ?? 0,
    seek: (seconds: number) => {
      if (videoRef.current) {
        suppress();
        videoRef.current.currentTime = seconds;
      }
    },
    play: () => {
      if (videoRef.current && videoRef.current.paused) {
        suppress();
        void videoRef.current.play().catch(() => undefined);
      }
    },
    pause: () => {
      if (videoRef.current && !videoRef.current.paused) {
        suppress();
        videoRef.current.pause();
      }
    },
  }));

  useEffect(() => {
    const video = videoRef.current;
    if (!video) return;
    readyRef.current = false;

    let hls: Hls | null = null;
    const nativeHls = video.canPlayType("application/vnd.apple.mpegurl") !== "";

    if (media.kind === "Hls" && !nativeHls && Hls.isSupported()) {
      hls = new Hls({ enableWorker: true });
      hls.loadSource(media.url);
      hls.attachMedia(video);
      hls.on(Hls.Events.ERROR, (_e, data) => {
        if (data.fatal) onError?.();
      });
    } else {
      video.src = media.url;
    }

    const markReady = () => {
      if (!readyRef.current) {
        readyRef.current = true;
        onReady?.();
      }
    };

    video.addEventListener("loadedmetadata", markReady);
    video.addEventListener("canplay", markReady);

    return () => {
      video.removeEventListener("loadedmetadata", markReady);
      video.removeEventListener("canplay", markReady);
      hls?.destroy();
      video.removeAttribute("src");
      video.load();
    };
  }, [media.url, media.kind, onReady, onError]);

  return (
    <video
      ref={videoRef}
      className="h-full w-full bg-black"
      controls
      playsInline
      onPlay={() => !isSuppressed() && onUserPlay?.()}
      onPause={() => !isSuppressed() && onUserPause?.()}
      onSeeked={() => !isSuppressed() && onUserSeek?.(videoRef.current?.currentTime ?? 0)}
      onError={() => onError?.()}
    />
  );
});
