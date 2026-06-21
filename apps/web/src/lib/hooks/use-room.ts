"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import type { HubConnection } from "@microsoft/signalr";
import { createRoomConnection } from "@/lib/realtime/room-connection";
import {
  RealtimeEvents,
  RealtimeMethods,
  type ChatMessageDeletedEvent,
  type HostTransferredEvent,
  type HubErrorEvent,
  type MediaChangedEvent,
  type MemberJoinedEvent,
  type MemberKickedEvent,
  type MemberLeftEvent,
  type PresenceUpdatedEvent,
} from "@/lib/realtime/events";
import { roomsApi } from "@/lib/api/rooms";
import { chatApi } from "@/lib/api/chat";
import type {
  ChatMessageDto,
  MediaDto,
  PlaybackStateDto,
  RoomDto,
  RoomMemberDto,
} from "@/lib/api/types";
import { errorMessage } from "@/lib/utils";

export type ConnectionState = "connecting" | "connected" | "reconnecting" | "disconnected";
export type RoomEnded = { type: "kicked" | "closed" } | null;

const HEARTBEAT_MS = 25_000;

export function useRoom(roomId: string, meId: string | undefined) {
  const [room, setRoom] = useState<RoomDto | null>(null);
  const [members, setMembers] = useState<RoomMemberDto[]>([]);
  const [onlineIds, setOnlineIds] = useState<string[]>([]);
  const [media, setMedia] = useState<MediaDto | null>(null);
  const [playback, setPlayback] = useState<PlaybackStateDto | null>(null);
  const [messages, setMessages] = useState<ChatMessageDto[]>([]);
  const [phase, setPhase] = useState<"loading" | "ready" | "error">("loading");
  const [error, setError] = useState<string | null>(null);
  const [hubError, setHubError] = useState<string | null>(null);
  const [ended, setEnded] = useState<RoomEnded>(null);
  const [connState, setConnState] = useState<ConnectionState>("connecting");

  const connRef = useRef<HubConnection | null>(null);
  const lastVersion = useRef<number>(-1);

  const applyPlayback = useCallback((state: PlaybackStateDto) => {
    if (state.version > lastVersion.current) {
      lastVersion.current = state.version;
      setPlayback(state);
    }
  }, []);

  useEffect(() => {
    let cancelled = false;
    lastVersion.current = -1;
    const conn = createRoomConnection();
    connRef.current = conn;

    conn.on(RealtimeEvents.PlaybackStateChanged, (s: PlaybackStateDto) => applyPlayback(s));
    conn.on(RealtimeEvents.MediaChanged, (e: MediaChangedEvent) => {
      setMedia(e.media);
      applyPlayback(e.playback);
    });
    conn.on(RealtimeEvents.MemberJoined, (e: MemberJoinedEvent) => {
      setMembers((prev) => {
        const others = prev.filter((m) => m.userId !== e.member.userId);
        return [...others, e.member];
      });
      setOnlineIds((prev) => (prev.includes(e.member.userId) ? prev : [...prev, e.member.userId]));
    });
    conn.on(RealtimeEvents.MemberLeft, (e: MemberLeftEvent) => {
      setOnlineIds((prev) => prev.filter((id) => id !== e.userId));
    });
    conn.on(RealtimeEvents.PresenceUpdated, (e: PresenceUpdatedEvent) => {
      setOnlineIds(e.onlineUserIds);
    });
    conn.on(RealtimeEvents.HostTransferred, (e: HostTransferredEvent) => {
      setRoom((prev) => (prev ? { ...prev, hostUserId: e.toUserId } : prev));
      setMembers((prev) =>
        prev.map((m) =>
          m.userId === e.toUserId
            ? { ...m, role: "Host" }
            : m.userId === e.fromUserId
              ? { ...m, role: "Member" }
              : m,
        ),
      );
    });
    conn.on(RealtimeEvents.MemberKicked, (e: MemberKickedEvent) => {
      setMembers((prev) => prev.filter((m) => m.userId !== e.userId));
      setOnlineIds((prev) => prev.filter((id) => id !== e.userId));
    });
    conn.on(RealtimeEvents.YouWereKicked, () => setEnded({ type: "kicked" }));
    conn.on(RealtimeEvents.RoomClosed, () => setEnded({ type: "closed" }));
    conn.on(RealtimeEvents.ChatMessageReceived, (msg: ChatMessageDto) => {
      setMessages((prev) => (prev.some((m) => m.id === msg.id) ? prev : [...prev, msg]));
    });
    conn.on(RealtimeEvents.ChatMessageDeleted, (e: ChatMessageDeletedEvent) => {
      setMessages((prev) =>
        prev.map((m) => (m.id === e.messageId ? { ...m, isDeleted: true, content: "" } : m)),
      );
    });
    conn.on(RealtimeEvents.HubError, (e: HubErrorEvent) => setHubError(e.message));

    conn.onreconnecting(() => setConnState("reconnecting"));
    conn.onreconnected(async () => {
      setConnState("connected");
      try {
        await conn.invoke(RealtimeMethods.JoinRoom, roomId);
        const detail = await roomsApi.detail(roomId);
        if (cancelled) return;
        setMembers(detail.members);
        setMedia(detail.currentMedia);
        setOnlineIds(detail.members.filter((m) => m.isOnline).map((m) => m.userId));
        if (detail.playback) applyPlayback(detail.playback);
      } catch {
        // best-effort resync
      }
    });
    conn.onclose(() => setConnState("disconnected"));

    async function boot() {
      try {
        const detail = await roomsApi.detail(roomId);
        if (cancelled) return;
        setRoom(detail.room);
        setMembers(detail.members);
        setMedia(detail.currentMedia);
        setOnlineIds(detail.members.filter((m) => m.isOnline).map((m) => m.userId));
        if (detail.playback) {
          lastVersion.current = detail.playback.version;
          setPlayback(detail.playback);
        }

        const history = await chatApi.history(roomId, { limit: 50 });
        if (cancelled) return;
        setMessages(history);

        await conn.start();
        if (cancelled) return;
        setConnState("connected");
        await conn.invoke(RealtimeMethods.JoinRoom, roomId);
        setPhase("ready");
      } catch (err) {
        if (!cancelled) {
          setError(errorMessage(err));
          setPhase("error");
        }
      }
    }
    void boot();

    const heartbeat = setInterval(() => {
      conn.invoke(RealtimeMethods.Heartbeat).catch(() => undefined);
    }, HEARTBEAT_MS);

    return () => {
      cancelled = true;
      clearInterval(heartbeat);
      conn.invoke(RealtimeMethods.LeaveRoom).catch(() => undefined);
      conn.stop().catch(() => undefined);
      connRef.current = null;
    };
  }, [roomId, applyPlayback]);

  const invoke = useCallback(async (method: string, ...args: unknown[]) => {
    try {
      await connRef.current?.invoke(method, ...args);
    } catch (err) {
      setHubError(errorMessage(err));
    }
  }, []);

  const actions = {
    play: () => invoke(RealtimeMethods.Play, roomId),
    pause: () => invoke(RealtimeMethods.Pause, roomId),
    seek: (seconds: number) => invoke(RealtimeMethods.Seek, roomId, seconds),
    changeMedia: (url: string, title: string | null) =>
      invoke(RealtimeMethods.ChangeMedia, roomId, url, title),
    sendMessage: (content: string) => invoke(RealtimeMethods.SendMessage, roomId, content),
    deleteMessage: (messageId: string) => invoke(RealtimeMethods.DeleteMessage, messageId),
    reportPlaybackError: () => invoke(RealtimeMethods.ReportPlaybackError),
    leave: () => roomsApi.leave(roomId),
    close: () => roomsApi.close(roomId),
    transferHost: (userId: string) => roomsApi.transferHost(roomId, userId),
    kick: (userId: string) => roomsApi.kick(roomId, userId),
  };

  const isHost = Boolean(room && meId && room.hostUserId === meId);

  return {
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
  };
}
