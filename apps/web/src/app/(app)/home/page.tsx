"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { roomsApi } from "@/lib/api/rooms";
import { Alert, Badge, Button, Card, Input, Spinner } from "@/components/ui";
import { errorMessage, relativeTime } from "@/lib/utils";
import { useT } from "@/lib/i18n";

export default function HomePage() {
  const router = useRouter();
  const t = useT();
  const [code, setCode] = useState("");
  const [joinError, setJoinError] = useState<string | null>(null);
  const [joining, setJoining] = useState(false);

  const history = useQuery({ queryKey: ["room-history"], queryFn: roomsApi.history });

  async function quickJoin(e: React.FormEvent) {
    e.preventDefault();
    if (!code.trim()) return;
    setJoinError(null);
    setJoining(true);
    try {
      const detail = await roomsApi.join(code.trim());
      router.push(`/rooms/${detail.room.id}`);
    } catch (err) {
      setJoinError(errorMessage(err));
    } finally {
      setJoining(false);
    }
  }

  return (
    <div className="space-y-8">
      <div className="grid gap-4 md:grid-cols-2">
        <Card>
          <h2 className="text-lg font-semibold text-white">{t("home.createTitle")}</h2>
          <p className="mt-1 text-sm text-slate-400">{t("home.createDesc")}</p>
          <Link href="/rooms/create" className="mt-4 inline-block">
            <Button>{t("home.createRoom")}</Button>
          </Link>
        </Card>

        <Card>
          <h2 className="text-lg font-semibold text-white">{t("home.joinTitle")}</h2>
          <p className="mt-1 text-sm text-slate-400">{t("home.joinDesc")}</p>
          <form onSubmit={quickJoin} className="mt-4 flex gap-2">
            <Input
              value={code}
              onChange={(e) => setCode(e.target.value.toUpperCase())}
              placeholder={t("room.join.codePlaceholder")}
              maxLength={6}
              className="uppercase"
            />
            <Button type="submit" disabled={joining}>
              {joining ? t("room.join.joining") : t("room.join.submit")}
            </Button>
          </form>
          {joinError ? (
            <div className="mt-3">
              <Alert>{joinError}</Alert>
            </div>
          ) : null}
        </Card>
      </div>

      <section>
        <div className="mb-3 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-white">{t("home.recentRooms")}</h2>
          <Link href="/history" className="text-sm text-indigo-400 hover:text-indigo-300">
            {t("home.viewAll")}
          </Link>
        </div>
        {history.isLoading ? (
          <Spinner />
        ) : history.isError ? (
          <Alert>{errorMessage(history.error)}</Alert>
        ) : history.data && history.data.length > 0 ? (
          <div className="space-y-2">
            {history.data.slice(0, 5).map((item) => (
              <Link
                key={`${item.roomId}-${item.joinedAtUtc}`}
                href={`/rooms/${item.roomId}`}
                className="flex items-center justify-between rounded-lg border border-slate-800 bg-slate-900/50 px-4 py-3 hover:border-slate-700"
              >
                <div>
                  <div className="font-medium text-white">{item.name}</div>
                  <div className="text-xs text-slate-500">
                    {item.code} · {t("home.joinedAgo", { time: relativeTime(item.joinedAtUtc, t) })}
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  <Badge className="bg-slate-800 text-slate-300">
                    {t(item.role === "Host" ? "enums.roleHost" : "enums.roleMember")}
                  </Badge>
                  <Badge
                    className={
                      item.status === "Active"
                        ? "bg-emerald-900/60 text-emerald-300"
                        : "bg-slate-800 text-slate-400"
                    }
                  >
                    {t(item.status === "Active" ? "enums.statusActive" : "enums.statusClosed")}
                  </Badge>
                </div>
              </Link>
            ))}
          </div>
        ) : (
          <p className="text-sm text-slate-500">{t("home.noRoomsCta")}</p>
        )}
      </section>
    </div>
  );
}
