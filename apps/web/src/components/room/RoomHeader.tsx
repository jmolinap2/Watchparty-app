"use client";

import { useState } from "react";
import { Badge, Button } from "@/components/ui";
import type { ConnectionState } from "@/lib/hooks/use-room";
import type { RoomDto } from "@/lib/api/types";
import { cn } from "@/lib/utils";
import { useT, type MessageKey } from "@/lib/i18n";

interface RoomHeaderProps {
  room: RoomDto;
  connState: ConnectionState;
  isHost: boolean;
  onLeave: () => void;
  onClose: () => void;
}

const connMeta: Record<ConnectionState, { key: MessageKey; className: string }> = {
  connecting: { key: "room.connecting", className: "bg-amber-900/60 text-amber-300" },
  connected: { key: "room.header.live", className: "bg-emerald-900/60 text-emerald-300" },
  reconnecting: { key: "room.reconnecting", className: "bg-amber-900/60 text-amber-300" },
  disconnected: { key: "room.disconnected", className: "bg-rose-900/60 text-rose-300" },
};

export function RoomHeader({ room, connState, isHost, onLeave, onClose }: RoomHeaderProps) {
  const t = useT();
  const [copied, setCopied] = useState(false);

  async function copyInvite() {
    const link = `${window.location.origin}/rooms/join?code=${room.code}`;
    try {
      await navigator.clipboard.writeText(`${room.code} — ${link}`);
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    } catch {
      // clipboard not available
    }
  }

  const conn = connMeta[connState];

  return (
    <div className="flex flex-wrap items-center justify-between gap-3">
      <div className="flex items-center gap-3">
        <h1 className="text-xl font-bold text-white">{room.name}</h1>
        <Badge className={cn("gap-1", conn.className)}>{t(conn.key)}</Badge>
        {room.isPrivate ? (
          <Badge className="bg-slate-800 text-slate-300">{t("room.header.private")}</Badge>
        ) : null}
      </div>
      <div className="flex items-center gap-2">
        <button
          onClick={copyInvite}
          className="rounded-lg border border-slate-700 px-3 py-1.5 text-sm text-slate-300 hover:border-slate-600"
          title={t("room.header.copyInvite")}
        >
          {t("room.header.code")}: <span className="font-mono font-semibold text-white">{room.code}</span>
          {copied ? <span className="ml-2 text-emerald-400">{t("room.header.copied")}</span> : null}
        </button>
        <Button variant="ghost" onClick={onLeave}>
          {t("room.header.leave")}
        </Button>
        {isHost ? (
          <Button variant="danger" onClick={onClose}>
            {t("room.header.closeRoom")}
          </Button>
        ) : null}
      </div>
    </div>
  );
}
