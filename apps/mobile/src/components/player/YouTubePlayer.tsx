import { forwardRef, useEffect, useImperativeHandle, useRef, useState } from "react";
import { View } from "react-native";
import YoutubePlayer, { type YoutubeIframeRef } from "react-native-youtube-iframe";
import type { PlayerHandle, PlayerProps } from "@/components/player/types";

/**
 * YouTube player via react-native-youtube-iframe. Its time getters are async, so
 * we poll them into a ref to satisfy the synchronous PlayerHandle used by the sync
 * engine. Playback is controlled through the `play` prop.
 */
export const YouTubePlayer = forwardRef<PlayerHandle, PlayerProps>(function YouTubePlayer(
  { media, onReady, onError },
  ref,
) {
  const ytRef = useRef<YoutubeIframeRef | null>(null);
  const readyRef = useRef(false);
  const timeRef = useRef(0);
  const durationRef = useRef(0);
  const [playing, setPlaying] = useState(false);
  const [height, setHeight] = useState(220);

  useEffect(() => {
    const id = setInterval(async () => {
      if (!ytRef.current || !readyRef.current) return;
      try {
        timeRef.current = await ytRef.current.getCurrentTime();
        if (!durationRef.current) durationRef.current = await ytRef.current.getDuration();
      } catch {
        // ignore transient bridge errors
      }
    }, 500);
    return () => clearInterval(id);
  }, []);

  useImperativeHandle(
    ref,
    () => ({
      controllable: true,
      isReady: () => readyRef.current,
      getCurrentTime: () => timeRef.current,
      getDuration: () => durationRef.current,
      seek: (seconds: number) => {
        timeRef.current = seconds;
        ytRef.current?.seekTo(seconds, true);
      },
      play: () => setPlaying(true),
      pause: () => setPlaying(false),
    }),
    [],
  );

  return (
    <View
      style={{ width: "100%", height: "100%", backgroundColor: "#000" }}
      onLayout={(e) => setHeight(e.nativeEvent.layout.height)}
    >
      <YoutubePlayer
        ref={ytRef}
        height={height}
        play={playing}
        videoId={media.providerId ?? ""}
        onReady={() => {
          readyRef.current = true;
          onReady?.();
        }}
        onError={() => onError?.()}
        onChangeState={(state: string) => {
          if (state === "playing") setPlaying(true);
          else if (state === "paused" || state === "ended") setPlaying(false);
        }}
        initialPlayerParams={{ controls: false, modestbranding: true, rel: false }}
      />
    </View>
  );
});
