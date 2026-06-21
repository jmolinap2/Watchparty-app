"use client";

import { useEffect, useRef, useState } from "react";
import { Avatar, Button, Input } from "@/components/ui";
import type { ChatMessageDto } from "@/lib/api/types";
import { relativeTime } from "@/lib/utils";
import { useT } from "@/lib/i18n";

interface ChatPanelProps {
  messages: ChatMessageDto[];
  meId: string | undefined;
  canModerate: boolean;
  onSend: (content: string) => void;
  onDelete: (messageId: string) => void;
  onReport: (message: ChatMessageDto) => void;
}

export function ChatPanel({ messages, meId, canModerate, onSend, onDelete, onReport }: ChatPanelProps) {
  const t = useT();
  const [text, setText] = useState("");
  const bottomRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages.length]);

  function submit(e: React.FormEvent) {
    e.preventDefault();
    const content = text.trim();
    if (!content) return;
    onSend(content);
    setText("");
  }

  return (
    <div className="flex h-full flex-col">
      <div className="border-b border-slate-800 px-4 py-2.5 text-sm font-semibold text-white">
        {t("room.chat.title")}
      </div>
      <div className="flex-1 space-y-3 overflow-y-auto px-4 py-3">
        {messages.length === 0 ? (
          <p className="text-sm text-slate-500">{t("room.chat.empty")}</p>
        ) : (
          messages.map((m) => (
            <div key={m.id} className="group flex gap-2">
              <Avatar name={m.senderDisplayName} url={m.senderAvatarUrl} size={28} />
              <div className="min-w-0 flex-1">
                <div className="flex items-baseline gap-2">
                  <span className="truncate text-sm font-medium text-slate-200">{m.senderDisplayName}</span>
                  <span className="text-[10px] text-slate-500">{relativeTime(m.createdAtUtc, t)}</span>
                </div>
                {m.isDeleted ? (
                  <p className="text-sm italic text-slate-600">{t("room.chat.deleted")}</p>
                ) : (
                  <p className="break-words text-sm text-slate-300">{m.content}</p>
                )}
              </div>
              {!m.isDeleted ? (
                <div className="flex shrink-0 items-start gap-1 opacity-0 transition-opacity group-hover:opacity-100">
                  {(m.senderUserId === meId || canModerate) && (
                    <button
                      onClick={() => onDelete(m.id)}
                      className="text-[10px] text-slate-500 hover:text-rose-400"
                      title={t("room.chat.deleteTitle")}
                    >
                      {t("room.chat.deleteAction")}
                    </button>
                  )}
                  {m.senderUserId !== meId && (
                    <button
                      onClick={() => onReport(m)}
                      className="text-[10px] text-slate-500 hover:text-amber-400"
                      title={t("room.chat.reportTitle")}
                    >
                      {t("room.chat.reportAction")}
                    </button>
                  )}
                </div>
              ) : null}
            </div>
          ))
        )}
        <div ref={bottomRef} />
      </div>
      <form onSubmit={submit} className="flex gap-2 border-t border-slate-800 p-3">
        <Input
          value={text}
          onChange={(e) => setText(e.target.value)}
          placeholder={t("room.chat.placeholder")}
          maxLength={1000}
        />
        <Button type="submit" disabled={!text.trim()}>
          {t("room.chat.send")}
        </Button>
      </form>
    </div>
  );
}
