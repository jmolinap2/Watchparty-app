"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { roomsApi } from "@/lib/api/rooms";
import { Alert, Button, Card, Field, Input } from "@/components/ui";
import { errorMessage } from "@/lib/utils";
import { useT } from "@/lib/i18n";

export default function JoinRoomPage() {
  const router = useRouter();
  const t = useT();
  const [code, setCode] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const detail = await roomsApi.join(code.trim());
      router.replace(`/rooms/${detail.room.id}`);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="mx-auto max-w-lg">
      <h1 className="mb-4 text-2xl font-bold text-white">{t("room.join.title")}</h1>
      <Card>
        <form onSubmit={onSubmit} className="space-y-4">
          {error ? <Alert>{error}</Alert> : null}
          <Field label={t("room.join.codeLabel")}>
            <Input
              value={code}
              onChange={(e) => setCode(e.target.value.toUpperCase())}
              placeholder={t("room.join.codePlaceholder")}
              maxLength={6}
              required
              className="uppercase tracking-widest"
            />
          </Field>
          <Button type="submit" disabled={loading} className="w-full">
            {loading ? t("room.join.joining") : t("room.join.joinRoom")}
          </Button>
        </form>
      </Card>
    </div>
  );
}
