"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { roomsApi } from "@/lib/api/rooms";
import { Alert, Button, Card, Field, Input } from "@/components/ui";
import { errorMessage } from "@/lib/utils";
import { useT } from "@/lib/i18n";

export default function CreateRoomPage() {
  const router = useRouter();
  const t = useT();
  const [name, setName] = useState("");
  const [isPrivate, setIsPrivate] = useState(false);
  const [maxMembers, setMaxMembers] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const room = await roomsApi.create({
        name: name.trim(),
        isPrivate,
        maxMembers: maxMembers ? Number(maxMembers) : null,
      });
      router.replace(`/rooms/${room.id}`);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="mx-auto max-w-lg">
      <h1 className="mb-4 text-2xl font-bold text-white">{t("room.create.title")}</h1>
      <Card>
        <form onSubmit={onSubmit} className="space-y-4">
          {error ? <Alert>{error}</Alert> : null}
          <Field label={t("room.create.nameLabel")}>
            <Input value={name} onChange={(e) => setName(e.target.value)} required maxLength={60} />
          </Field>
          <Field label={t("room.create.maxMembers")} hint={t("room.create.maxMembersHint")}>
            <Input
              type="number"
              min={2}
              max={100}
              value={maxMembers}
              onChange={(e) => setMaxMembers(e.target.value)}
              placeholder={t("room.create.maxMembersPlaceholder")}
            />
          </Field>
          <label className="flex items-center gap-2 text-sm text-slate-300">
            <input
              type="checkbox"
              checked={isPrivate}
              onChange={(e) => setIsPrivate(e.target.checked)}
              className="h-4 w-4 rounded border-slate-600 bg-slate-900"
            />
            {t("room.create.privateCheckbox")}
          </label>
          <Button type="submit" disabled={loading} className="w-full">
            {loading ? t("room.create.creating") : t("room.create.submit")}
          </Button>
        </form>
      </Card>
    </div>
  );
}
