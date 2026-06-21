import { forwardRef, useEffect, useImperativeHandle, useRef, useState } from "react";
import { StyleSheet } from "react-native";
import { useVideoPlayer, VideoView } from "expo-video";
import type { PlayerHandle, PlayerProps } from "@/components/player/types";

/**
 * expo-video player for Direct (mp4/webm) and HLS (m3u8) sources. Native controls
 * are disabled; playback is driven by the room's synchronized controls so the
 * server stays authoritative.
 */
export const NativeVideoPlayer = forwardRef<PlayerHandle, PlayerProps>(function NativeVideoPlayer(
  { media, onReady, onError },
  ref,
) {
  const readyRef = useRef(false);
  const [, force] = useState(0);

  const player = useVideoPlayer(media.url, (p) => {
    p.loop = false;
    p.timeUpdateEventInterval = 1;
  });

  useEffect(() => {
    const statusSub = player.addListener("statusChange", ({ status, error }) => {
      if (status === "readyToPlay" && !readyRef.current) {
        readyRef.current = true;
        onReady?.();
        force((n) => n + 1);
      }
      if (status === "error" || error) onError?.();
    });
    return () => statusSub.remove();
  }, [player, onReady, onError]);

  useImperativeHandle(
    ref,
    () => ({
      controllable: true,
      isReady: () => readyRef.current,
      getCurrentTime: () => player.currentTime ?? 0,
      getDuration: () => player.duration ?? 0,
      seek: (seconds: number) => {
        player.currentTime = seconds;
      },
      play: () => {
        if (!player.playing) player.play();
      },
      pause: () => {
        if (player.playing) player.pause();
      },
    }),
    [player],
  );

  return <VideoView style={styles.video} player={player} nativeControls={false} contentFit="contain" />;
});

const styles = StyleSheet.create({
  video: { width: "100%", height: "100%", backgroundColor: "#000" },
});
