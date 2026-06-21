import { useEffect, useRef, useState } from "react";
import {
  KeyboardAvoidingView,
  Platform,
  Pressable,
  Text,
  View,
} from "react-native";
import { Stack, useLocalSearchParams, useRouter } from "expo-router";
import * as Clipboard from "expo-clipboard";
import { useAuth } from "@/lib/hooks/use-auth";
import { useRoom } from "@/lib/hooks/use-room";
import { applyPlaybackState } from "@/lib/realtime/sync";
import { reportsApi } from "@/lib/api/reports";
import { VideoPlayer } from "@/components/player/VideoPlayer";
import type { PlayerHandle } from "@/components/player/types";
import { PlaybackControls } from "@/components/room/PlaybackControls";
import { ChatList } from "@/components/room/ChatList";
import { MembersList } from "@/components/room/MembersList";
import { ReportModal } from "@/components/room/ReportModal";
import { Alert, Badge, Button, Spinner, TextField } from "@/components/ui";
import { colors, spacing } from "@/lib/theme";
import type { ChatMessageDto, RoomMemberDto } from "@/lib/api/types";
import { useT, type MessageKey } from "@/lib/i18n";

type ReportTarget =
  | { kind: "user"; member: RoomMemberDto }
  | { kind: "message"; message: ChatMessageDto }
  | null;

const connMeta: Record<string, { key: MessageKey; color: string }> = {
  connecting: { key: "room.header.connecting", color: colors.warning },
  connected: { key: "room.header.live", color: colors.success },
  reconnecting: { key: "room.header.reconnecting", color: colors.warning },
  disconnected: { key: "room.header.offline", color: colors.danger },
};

export default function RoomScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const roomId = id!;
  const router = useRouter();
  const t = useT();
  const { user } = useAuth();
  const meId = user?.id;

  const room = useRoom(roomId, meId);
  const playerRef = useRef<PlayerHandle | null>(null);
  const playbackRef = useRef(room.playback);
  const [tab, setTab] = useState<"chat" | "members">("chat");
  const [mediaUrl, setMediaUrl] = useState("");
  const [reportTarget, setReportTarget] = useState<ReportTarget>(null);

  useEffect(() => {
    playbackRef.current = room.playback;
    if (room.playback && playerRef.current) applyPlaybackState(playerRef.current, room.playback);
  }, [room.playback]);

  useEffect(() => {
    if (room.playback?.status !== "Playing") return;
    const timer = setInterval(() => {
      if (playbackRef.current && playerRef.current) {
        applyPlaybackState(playerRef.current, playbackRef.current);
      }
    }, 3000);
    return () => clearInterval(timer);
  }, [room.playback?.status]);

  useEffect(() => {
    if (!room.hubError) return;
    const timer = setTimeout(() => room.setHubError(null), 4000);
    return () => clearTimeout(timer);
  }, [room.hubError, room.setHubError]);

  useEffect(() => {
    if (!room.ended) return;
    const timer = setTimeout(() => router.replace("/home"), 3000);
    return () => clearTimeout(timer);
  }, [room.ended, router]);

  if (room.phase === "loading") {
    return (
      <View style={{ flex: 1, backgroundColor: colors.bg, alignItems: "center", justifyContent: "center" }}>
        <Spinner />
      </View>
    );
  }

  if (room.phase === "error" || !room.room) {
    return (
      <View style={{ flex: 1, backgroundColor: colors.bg, padding: spacing.lg }}>
        <Alert>{room.error ?? t("room.errorLoad")}</Alert>
      </View>
    );
  }

  if (room.ended) {
    return (
      <View style={{ flex: 1, backgroundColor: colors.bg, alignItems: "center", justifyContent: "center", padding: spacing.lg }}>
        <Text style={{ color: colors.white, fontSize: 18, fontWeight: "700", textAlign: "center" }}>
          {room.ended.type === "kicked" ? t("room.kicked") : t("room.closedRoom")}
        </Text>
        <Text style={{ color: colors.textFaint, marginTop: 8 }}>{t("room.leaving")}</Text>
      </View>
    );
  }

  const controllable = room.media
    ? room.media.kind !== "GoogleDrive" && room.media.kind !== "Mega"
    : false;
  const conn = connMeta[room.connState];

  async function loadMedia() {
    if (!mediaUrl.trim()) return;
    await room.actions.changeMedia(mediaUrl.trim(), null);
    setMediaUrl("");
  }

  async function copyCode() {
    if (room.room) await Clipboard.setStringAsync(room.room.code);
  }

  async function submitReport(reason: string) {
    if (!reportTarget) return;
    if (reportTarget.kind === "user") {
      await reportsApi.reportUser(reportTarget.member.userId, reason, roomId);
    } else {
      await reportsApi.reportMessage(reportTarget.message.id, reason);
    }
  }

  return (
    <KeyboardAvoidingView
      style={{ flex: 1, backgroundColor: colors.bg }}
      behavior={Platform.OS === "ios" ? "padding" : undefined}
    >
      <Stack.Screen
        options={{
          title: room.room.name,
          headerRight: () => (
            <Pressable onPress={() => room.actions.leave().finally(() => router.replace("/home"))}>
              <Text style={{ color: colors.danger, fontSize: 14 }}>{t("room.header.leave")}</Text>
            </Pressable>
          ),
        }}
      />

      {/* Video */}
      <View style={{ width: "100%", aspectRatio: 16 / 9, backgroundColor: "#000" }}>
        {room.media ? (
          <VideoPlayer
            key={room.media.id}
            ref={playerRef}
            media={room.media}
            onReady={() => {
              if (playbackRef.current && playerRef.current) {
                applyPlaybackState(playerRef.current, playbackRef.current);
              }
            }}
            onError={room.actions.reportPlaybackError}
          />
        ) : (
          <View style={{ flex: 1, alignItems: "center", justifyContent: "center", padding: spacing.lg }}>
            <Text style={{ color: colors.textFaint, textAlign: "center" }}>{t("room.media.noMedia")}</Text>
          </View>
        )}
      </View>

      <View style={{ padding: spacing.md, gap: spacing.sm }}>
        <View style={{ flexDirection: "row", alignItems: "center", gap: spacing.sm }}>
          <Badge color={colors.bgElevated} textColor={conn.color}>
            {t(conn.key)}
          </Badge>
          <Pressable onPress={copyCode}>
            <Text style={{ color: colors.textMuted, fontSize: 12 }}>
              {t("room.header.code")} <Text style={{ color: colors.white, fontWeight: "700" }}>{room.room.code}</Text>{" "}
              {t("room.header.tapToCopy")}
            </Text>
          </Pressable>
          <View style={{ flex: 1 }} />
          {room.isHost ? (
            <Pressable onPress={() => room.actions.close().finally(() => router.replace("/home"))}>
              <Text style={{ color: colors.danger, fontSize: 12 }}>{t("room.header.closeRoom")}</Text>
            </Pressable>
          ) : null}
        </View>

        {room.hubError ? <Alert>{room.hubError}</Alert> : null}

        {room.media ? (
          <PlaybackControls
            playback={room.playback}
            controllable={controllable}
            getCurrentTime={() => playerRef.current?.getCurrentTime() ?? 0}
            getDuration={() => playerRef.current?.getDuration() ?? 0}
            onPlay={room.actions.play}
            onPause={room.actions.pause}
            onSeek={room.actions.seek}
          />
        ) : null}

        <View style={{ flexDirection: "row", gap: spacing.sm }}>
          <TextField
            style={{ flex: 1 }}
            value={mediaUrl}
            onChangeText={setMediaUrl}
            placeholder={t("room.media.placeholder")}
            autoCapitalize="none"
          />
          <Button title={t("common.load")} onPress={loadMedia} />
        </View>
      </View>

      {/* Chat / Members toggle */}
      <View style={{ flexDirection: "row", borderBottomWidth: 1, borderBottomColor: colors.border }}>
        {(["chat", "members"] as const).map((tabKey) => (
          <Pressable
            key={tabKey}
            onPress={() => setTab(tabKey)}
            style={{
              flex: 1,
              paddingVertical: spacing.sm,
              alignItems: "center",
              borderBottomWidth: 2,
              borderBottomColor: tab === tabKey ? colors.primary : "transparent",
            }}
          >
            <Text style={{ color: tab === tabKey ? colors.white : colors.textFaint, fontWeight: "600" }}>
              {tabKey === "chat"
                ? t("room.tabsChat")
                : t("room.tabsMembers", { count: room.onlineIds.length })}
            </Text>
          </Pressable>
        ))}
      </View>

      <View style={{ flex: 1 }}>
        {tab === "chat" ? (
          <ChatList
            messages={room.messages}
            meId={meId}
            canModerate={room.isHost}
            onSend={room.actions.sendMessage}
            onDelete={room.actions.deleteMessage}
            onReport={(message) => setReportTarget({ kind: "message", message })}
          />
        ) : (
          <MembersList
            members={room.members}
            onlineIds={room.onlineIds}
            meId={meId}
            isHost={room.isHost}
            onTransferHost={room.actions.transferHost}
            onKick={room.actions.kick}
            onReport={(member) => setReportTarget({ kind: "user", member })}
          />
        )}
      </View>

      <ReportModal
        visible={reportTarget !== null}
        title={reportTarget?.kind === "user" ? t("reports.reportUser") : t("reports.reportMessage")}
        subtitle={
          reportTarget?.kind === "user"
            ? reportTarget.member.displayName
            : reportTarget?.kind === "message"
              ? reportTarget.message.content
              : undefined
        }
        onClose={() => setReportTarget(null)}
        onSubmit={submitReport}
      />
    </KeyboardAvoidingView>
  );
}
