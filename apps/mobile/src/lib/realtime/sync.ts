import type { PlaybackStateDto } from "@/lib/api/types";
import type { PlayerHandle } from "@/components/player/types";

const DRIFT_THRESHOLD_SECONDS = 0.75;

export function computeTargetPosition(state: PlaybackStateDto, nowMs: number = Date.now()): number {
  if (state.status !== "Playing") {
    return state.positionSeconds;
  }
  const serverMs = new Date(state.serverTimestampUtc).getTime();
  const elapsedSeconds = Math.max(0, (nowMs - serverMs) / 1000);
  return state.positionSeconds + elapsedSeconds;
}

export function applyPlaybackState(player: PlayerHandle, state: PlaybackStateDto): void {
  if (!player.isReady() || !player.controllable) return;

  const target = computeTargetPosition(state);
  const current = player.getCurrentTime();

  if (Math.abs(current - target) > DRIFT_THRESHOLD_SECONDS) {
    player.seek(target);
  }

  if (state.status === "Playing") {
    player.play();
  } else {
    player.pause();
  }
}
