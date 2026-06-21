import type { PlaybackStateDto } from "@/lib/api/types";
import type { PlayerHandle } from "@/components/player/types";

// If the player drifts more than this from the authoritative position, re-seek.
const DRIFT_THRESHOLD_SECONDS = 0.75;

/**
 * The authoritative position right now: the stored position plus the time that
 * has elapsed on the server clock since the state was stamped (only while playing).
 */
export function computeTargetPosition(state: PlaybackStateDto, nowMs: number = Date.now()): number {
  if (state.status !== "Playing") {
    return state.positionSeconds;
  }
  const serverMs = new Date(state.serverTimestampUtc).getTime();
  const elapsedSeconds = Math.max(0, (nowMs - serverMs) / 1000);
  return state.positionSeconds + elapsedSeconds;
}

/**
 * Reconcile a player with the server's authoritative state. Returns the applied
 * target position. Safe to call repeatedly; only seeks when drift is significant.
 */
export function applyPlaybackState(player: PlayerHandle, state: PlaybackStateDto): void {
  if (!player.isReady() || !player.controllable) {
    return;
  }

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
