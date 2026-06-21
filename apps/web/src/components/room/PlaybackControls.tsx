"use client";

import { useEffect, useRef, useState } from "react";
import { Button } from "@/components/ui";
import type { PlaybackStateDto } from "@/lib/api/types";
import { formatTime } from "@/lib/utils";
import { useT } from "@/lib/i18n";

interface PlaybackControlsProps {
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
}: PlaybackControlsProps) {
  const t = useT();
  const [position, setPosition] = useState(0);
  const [duration, setDuration] = useState(0);
  const [scrub, setScrub] = useState<number | null>(null);
  const scrubbing = useRef(false);

  useEffect(() => {
    const id = setInterval(() => {
      if (!scrubbing.current) setPosition(getCurrentTime());
      setDuration(getDuration());
    }, 250);
    return () => clearInterval(id);
  }, [getCurrentTime, getDuration]);

  const isPlaying = playback?.status === "Playing";

  if (!controllable) {
    return (
      <div className="rounded-lg border border-amber-900/60 bg-amber-950/30 px-4 py-3 text-sm text-amber-200">
        {t("room.media.notControllable")}
      </div>
    );
  }

  const shown = scrub ?? position;

  return (
    <div className="flex items-center gap-3">
      <Button
        variant="secondary"
        onClick={() => (isPlaying ? onPause() : onPlay())}
        className="w-24"
        disabled={!playback}
      >
        {isPlaying ? t("playback.pause") : t("playback.play")}
      </Button>
      <span className="w-12 text-right text-xs tabular-nums text-slate-400">{formatTime(shown)}</span>
      <input
        type="range"
        min={0}
        max={duration || 0}
        step={0.5}
        value={Math.min(shown, duration || 0)}
        onMouseDown={() => {
          scrubbing.current = true;
        }}
        onTouchStart={() => {
          scrubbing.current = true;
        }}
        onChange={(e) => setScrub(Number(e.target.value))}
        onMouseUp={(e) => {
          scrubbing.current = false;
          const v = Number((e.target as HTMLInputElement).value);
          setScrub(null);
          onSeek(v);
        }}
        onTouchEnd={(e) => {
          scrubbing.current = false;
          const v = Number((e.target as HTMLInputElement).value);
          setScrub(null);
          onSeek(v);
        }}
        className="h-1.5 flex-1 cursor-pointer appearance-none rounded-full bg-slate-700 accent-indigo-500"
        disabled={!playback || !duration}
      />
      <span className="w-12 text-xs tabular-nums text-slate-500">{formatTime(duration)}</span>
    </div>
  );
}
