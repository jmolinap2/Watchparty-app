"use client";

import { Avatar, Badge } from "@/components/ui";
import type { RoomMemberDto } from "@/lib/api/types";
import { cn } from "@/lib/utils";
import { useT } from "@/lib/i18n";

interface MembersPanelProps {
  members: RoomMemberDto[];
  onlineIds: string[];
  meId: string | undefined;
  isHost: boolean;
  onTransferHost: (userId: string) => void;
  onKick: (userId: string) => void;
  onReport: (member: RoomMemberDto) => void;
}

export function MembersPanel({
  members,
  onlineIds,
  meId,
  isHost,
  onTransferHost,
  onKick,
  onReport,
}: MembersPanelProps) {
  const t = useT();
  const online = new Set(onlineIds);
  const sorted = [...members].sort((a, b) => {
    const ao = online.has(a.userId) ? 0 : 1;
    const bo = online.has(b.userId) ? 0 : 1;
    if (ao !== bo) return ao - bo;
    return a.displayName.localeCompare(b.displayName);
  });

  return (
    <div className="flex h-full flex-col">
      <div className="border-b border-slate-800 px-4 py-2.5 text-sm font-semibold text-white">
        {t("room.members.title")}{" "}
        <span className="text-slate-500">{t("room.members.online", { count: online.size })}</span>
      </div>
      <div className="flex-1 space-y-1 overflow-y-auto px-2 py-2">
        {sorted.map((m) => {
          const isOnline = online.has(m.userId);
          const isMe = m.userId === meId;
          return (
            <div
              key={m.userId}
              className="group flex items-center gap-2 rounded-lg px-2 py-1.5 hover:bg-slate-800/60"
            >
              <div className="relative">
                <Avatar name={m.displayName} url={m.avatarUrl} size={32} />
                <span
                  className={cn(
                    "absolute -bottom-0.5 -right-0.5 h-2.5 w-2.5 rounded-full border-2 border-slate-900",
                    isOnline ? "bg-emerald-500" : "bg-slate-600",
                  )}
                />
              </div>
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-1.5">
                  <span className="truncate text-sm text-slate-200">
                    {m.displayName}
                    {isMe ? t("room.members.youSuffix") : ""}
                  </span>
                  {m.role === "Host" ? (
                    <Badge className="bg-indigo-900/70 text-indigo-300">{t("room.members.hostBadge")}</Badge>
                  ) : null}
                </div>
              </div>
              <div className="flex shrink-0 items-center gap-1 opacity-0 transition-opacity group-hover:opacity-100">
                {isHost && !isMe ? (
                  <>
                    <button
                      onClick={() => onTransferHost(m.userId)}
                      className="text-[10px] text-slate-500 hover:text-indigo-400"
                      title={t("room.members.makeHostTitle")}
                    >
                      {t("room.members.makeHost")}
                    </button>
                    <button
                      onClick={() => onKick(m.userId)}
                      className="text-[10px] text-slate-500 hover:text-rose-400"
                      title={t("room.members.kickTitle")}
                    >
                      {t("room.members.kick")}
                    </button>
                  </>
                ) : null}
                {!isMe ? (
                  <button
                    onClick={() => onReport(m)}
                    className="text-[10px] text-slate-500 hover:text-amber-400"
                    title={t("room.members.reportTitle")}
                  >
                    {t("room.members.report")}
                  </button>
                ) : null}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
