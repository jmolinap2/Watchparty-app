import { useEffect, useRef, useState } from "react";
import { Text, View } from "react-native";
import Slider from "@react-native-community/slider";
import { Button } from "@/components/ui";
import { colors, spacing } from "@/lib/theme";
import type { PlaybackStateDto } from "@/lib/api/types";
import { formatTime } from "@/lib/utils";
import { useT } from "@/lib/i18n";

interface Props {
  playback: PlaybackStateDto | null;
  controllable: boolean;
  getCurrentTime: () => number;
  getDuration: () => number;
  onPlay: () => void;
  onPause: () => void;
  onSeek: (seconds: number) => void;
}

export function PlaybackControls({
  playback,
  controllable,
  getCurrentTime,
  getDuration,
  onPlay,
  onPause,
  onSeek,
}: Props) {
  const t = useT();
  const [position, setPosition] = useState(0);
  const [duration, setDuration] = useState(0);
  const [scrub, setScrub] = useState<number | null>(null);
  const scrubbing = useRef(false);

  useEffect(() => {
    const id = setInterval(() => {
      if (!scrubbing.current) setPosition(getCurrentTime());
      setDuration(getDuration());
    }, 300);
    return () => clearInterval(id);
  }, [getCurrentTime, getDuration]);

  if (!controllable) {
    return (
      <View style={{ backgroundColor: "#3b2f0a", borderRadius: 10, padding: spacing.md }}>
        <Text style={{ color: colors.warning, fontSize: 13 }}>{t("room.media.notControllable")}</Text>
      </View>
    );
  }

  const isPlaying = playback?.status === "Playing";
  const shown = scrub ?? position;

  return (
    <View style={{ gap: spacing.xs }}>
      <View style={{ flexDirection: "row", alignItems: "center", gap: spacing.sm }}>
        <Button
          title={isPlaying ? t("playback.pause") : t("playback.play")}
          variant="secondary"
          onPress={() => (isPlaying ? onPause() : onPlay())}
          style={{ width: 96 }}
        />
        <Slider
          style={{ flex: 1 }}
          minimumValue={0}
          maximumValue={duration || 1}
          value={Math.min(shown, duration || 0)}
          minimumTrackTintColor={colors.primary}
          maximumTrackTintColor={colors.border}
          thumbTintColor={colors.primary}
          onSlidingStart={() => {
            scrubbing.current = true;
          }}
          onValueChange={(v) => setScrub(v)}
          onSlidingComplete={(v) => {
            scrubbing.current = false;
            setScrub(null);
            onSeek(v);
          }}
        />
      </View>
      <View style={{ flexDirection: "row", justifyContent: "space-between" }}>
        <Text style={{ color: colors.textFaint, fontSize: 11 }}>{formatTime(shown)}</Text>
        <Text style={{ color: colors.textFaint, fontSize: 11 }}>{formatTime(duration)}</Text>
      </View>
    </View>
  );
}
