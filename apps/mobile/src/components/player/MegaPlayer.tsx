import { forwardRef, useImperativeHandle } from "react";
import { WebView } from "react-native-webview";
import type { PlayerHandle, PlayerProps } from "@/components/player/types";

/**
 * MEGA playback via the official embed player in a WebView. Like Google Drive,
 * MEGA's embed exposes no JS control surface, so it is not programmatically
 * synchronizable (handle reports `controllable: false`). The embed URL (carrying
 * its decryption key) is built and validated server-side and arrives as `media.url`.
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
    <WebView
      source={{ uri: media.url }}
      style={{ flex: 1, backgroundColor: "#000" }}
      allowsFullscreenVideo
      mediaPlaybackRequiresUserAction={false}
    />
  );
});
