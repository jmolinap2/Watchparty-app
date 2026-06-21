"use client";

import { useEffect, useRef, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { useAuth } from "@/lib/hooks/use-auth";
import { useRoom } from "@/lib/hooks/use-room";
import { applyPlaybackState } from "@/lib/realtime/sync";
import { reportsApi } from "@/lib/api/reports";
import { VideoPlayer } from "@/components/player/VideoPlayer";
import type { PlayerHandle } from "@/components/player/types";
import { RoomHeader } from "@/components/room/RoomHeader";
import { MediaBar } from "@/components/room/MediaBar";
import { PlaybackControls } from "@/components/room/PlaybackControls";
import { MembersPanel } from "@/components/room/MembersPanel";
import { ChatPanel } from "@/components/chat/ChatPanel";
import { ReportModal } from "@/components/room/ReportModal";
import { Alert, Card, Spinner } from "@/components/ui";
import type { ChatMessageDto, RoomMemberDto } from "@/lib/api/types";
import { useT } from "@/lib/i18n";

type ReportTarget =
  | { kind: "user"; member: RoomMemberDto }
  | { kind: "message"; message: ChatMessageDto }
  | null;

export default function RoomPage() {
  const params = useParams<{ id: string }>();
  const roomId = params.id;
  const router = useRouter();
  const t = useT();
  const { user } = useAuth();
  const meId = user?.id;

  const {
    room,
    members,
    onlineIds,
    media,
    playback,
    messages,
    phase,
    error,
    hubError,
    setHubError,
    ended,
    connState,
    isHost,
    actions,
  } = useRoom(roomId, meId);

  const playerRef = useRef<PlayerHandle | null>(null);
  const playbackRef = useRef(playback);
  const [reportTarget, setReportTarget] = useState<ReportTarget>(null);

  useEffect(() => {
    playbackRef.current = playback;
    if (playback && playerRef.current) applyPlaybackState(playerRef.current, playback);
  }, [playback]);

  // Periodic drift correction while playing.
  useEffect(() => {
    if (playback?.status !== "Playing") return;
    const id = setInterval(() => {
      if (playbackRef.current && playerRef.current) {
        applyPlaybackState(playerRef.current, playbackRef.current);
      }
    }, 3000);
    return () => clearInterval(id);
  }, [playback?.status]);

  // Auto-dismiss hub errors.
  useEffect(() => {
    if (!hubError) return;
    const id = setTimeout(() => setHubError(null), 4000);
    return () => clearTimeout(id);
  }, [hubError, setHubError]);

  // Redirect away once the room ends for us.
  useEffect(() => {
    if (!ended) return;
    const id = setTimeout(() => router.replace("/home"), 3500);
    return () => clearTimeout(id);
  }, [ended, router]);

  if (phase === "loading") {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <Spinner className="h-8 w-8" />
      </div>
    );
  }

  if (phase === "error" || !room) {
    return (
      <div className="mx-auto max-w-lg pt-10">
        <Alert>{error ?? t("room.errorLoad")}</Alert>
      </div>
    );
  }

  if (ended) {
    return (
      <div className="mx-auto max-w-lg pt-16 text-center">
        <Card>
          <h2 className="text-xl font-semibold text-white">
            {ended.type === "kicked" ? t("room.kicked") : t("room.closedRoom")}
          </h2>
          <p className="mt-2 text-sm text-slate-400">{t("room.leaving")}</p>
        </Card>
      </div>
    );
  }

  const controllable = media ? media.kind !== "GoogleDrive" && media.kind !== "Mega" : false;

  async function submitReport(reason: string) {
    if (!reportTarget) return;
    if (reportTarget.kind === "user") {
      await reportsApi.reportUser(reportTarget.member.userId, reason, roomId);
    } else {
      await reportsApi.reportMessage(reportTarget.message.id, reason);
    }
  }

  return (
    <div className="space-y-4">
      <RoomHeader
        room={room}
        connState={connState}
        isHost={isHost}
        onLeave={() => actions.leave().finally(() => router.replace("/home"))}
        onClose={() => actions.close().finally(() => router.replace("/home"))}
      />

      {hubError ? <Alert>{hubError}</Alert> : null}

      <div className="grid gap-4 lg:grid-cols-[1fr_340px]">
        {/* Player column */}
        <div className="space-y-3">
          <div className="aspect-video w-full overflow-hidden rounded-xl border border-slate-800 bg-black">
            {media ? (
              <VideoPlayer
                key={media.id}
                ref={playerRef}
                media={media}
                onReady={() => {
                  if (playbackRef.current && playerRef.current) {
                    applyPlaybackState(playerRef.current, playbackRef.current);
                  }
                }}
                onUserPlay={actions.play}
                onUserPause={actions.pause}
                onUserSeek={actions.seek}
                onError={actions.reportPlaybackError}
              />
            ) : (
              <div className="flex h-full items-center justify-center text-slate-500">
                {t("room.media.noMedia")}
              </div>
            )}
          </div>

          {media ? (
            <PlaybackControls
              playback={playback}
              controllable={controllable}
              getCurrentTime={() => playerRef.current?.getCurrentTime() ?? 0}
              getDuration={() => playerRef.current?.getDuration() ?? 0}
              onPlay={actions.play}
              onPause={actions.pause}
              onSeek={actions.seek}
            />
          ) : null}

          {media ? (
            <p className="text-sm text-slate-400">{t("room.nowPlaying", { title: media.title })}</p>
          ) : null}

          <Card className="p-3">
            <MediaBar onLoad={actions.changeMedia} />
          </Card>
        </div>

        {/* Side column: members + chat */}
        <div className="flex flex-col gap-4">
          <Card className="h-64 p-0">
            <MembersPanel
              members={members}
              onlineIds={onlineIds}
              meId={meId}
              isHost={isHost}
              onTransferHost={actions.transferHost}
              onKick={actions.kick}
              onReport={(member) => setReportTarget({ kind: "user", member })}
            />
          </Card>
          <Card className="flex h-[28rem] flex-col p-0">
            <ChatPanel
              messages={messages}
              meId={meId}
              canModerate={isHost}
              onSend={actions.sendMessage}
              onDelete={actions.deleteMessage}
              onReport={(message) => setReportTarget({ kind: "message", message })}
            />
          </Card>
        </div>
      </div>

      <ReportModal
        open={reportTarget !== null}
        title={reportTarget?.kind === "user" ? t("reports.reportUser") : t("reports.reportMessage")}
        subtitle={
          reportTarget?.kind === "user"
            ? reportTarget.member.displayName
            : reportTarget?.kind === "message"
              ? `"${reportTarget.message.content}"`
              : undefined
        }
        onClose={() => setReportTarget(null)}
        onSubmit={submitReport}
      />
    </div>
  );
}
