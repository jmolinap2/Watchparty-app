"use client";

import { useState } from "react";
import { Button, Input } from "@/components/ui";
import { useT } from "@/lib/i18n";

interface MediaBarProps {
  onLoad: (url: string, title: string | null) => void;
}

export function MediaBar({ onLoad }: MediaBarProps) {
  const t = useT();
  const [url, setUrl] = useState("");
  const [title, setTitle] = useState("");

  function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!url.trim()) return;
    onLoad(url.trim(), title.trim() || null);
    setUrl("");
    setTitle("");
  }

  return (
    <form onSubmit={submit} className="space-y-2">
      <div className="flex flex-col gap-2 sm:flex-row">
        <Input
          value={url}
          onChange={(e) => setUrl(e.target.value)}
          placeholder={t("room.media.placeholder")}
          className="flex-1"
        />
        <Input
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          placeholder={t("room.media.titlePlaceholder")}
          className="sm:w-48"
        />
        <Button type="submit" disabled={!url.trim()}>
          {t("room.media.load")}
        </Button>
      </div>
      <p className="text-xs text-slate-500">{t("room.media.supported")}</p>
    </form>
  );
}
