import { forwardRef } from "react";
import type { PlayerHandle, PlayerProps } from "@/components/player/types";
import { NativeVideoPlayer } from "@/components/player/NativeVideoPlayer";
import { YouTubePlayer } from "@/components/player/YouTubePlayer";
import { DrivePlayer } from "@/components/player/DrivePlayer";
import { MegaPlayer } from "@/components/player/MegaPlayer";

/** Routes to the concrete player implementation for the media kind. */
export const VideoPlayer = forwardRef<PlayerHandle, PlayerProps>(function VideoPlayer(props, ref) {
  switch (props.media.kind) {
    case "YouTube":
      return <YouTubePlayer ref={ref} {...props} />;
    case "GoogleDrive":
      return <DrivePlayer ref={ref} {...props} />;
    case "Mega":
      return <MegaPlayer ref={ref} {...props} />;
    default:
      return <NativeVideoPlayer ref={ref} {...props} />;
  }
});
