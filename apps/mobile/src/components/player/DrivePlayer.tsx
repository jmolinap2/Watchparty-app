import { forwardRef, useImperativeHandle } from "react";
import { WebView } from "react-native-webview";
import type { PlayerHandle, PlayerProps } from "@/components/player/types";

/**
 * Google Drive playback via the official preview embed in a WebView. Drive's
 * player exposes no JS control surface, so it is not programmatically
 * synchronizable (handle reports `controllable: false`).
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
  const uri = `https://drive.google.com/file/d/${fileId}/preview`;

  return (
    <WebView
      source={{ uri }}
      style={{ flex: 1, backgroundColor: "#000" }}
      allowsFullscreenVideo
      mediaPlaybackRequiresUserAction={false}
    />
  );
});
