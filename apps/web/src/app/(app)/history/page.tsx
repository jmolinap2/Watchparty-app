"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { roomsApi } from "@/lib/api/rooms";
import { Alert, Badge, Card, Spinner } from "@/components/ui";
import { errorMessage, formatDate } from "@/lib/utils";
import { useT } from "@/lib/i18n";

export default function HistoryPage() {
  const t = useT();
  const history = useQuery({ queryKey: ["room-history"], queryFn: roomsApi.history });

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold text-white">{t("history.title")}</h1>
      {history.isLoading ? (
        <Spinner />
      ) : history.isError ? (
        <Alert>{errorMessage(history.error)}</Alert>
      ) : history.data && history.data.length > 0 ? (
        <Card className="p-0">
          <table className="w-full text-sm">
            <thead className="border-b border-slate-800 text-left text-slate-400">
              <tr>
                <th className="px-4 py-2 font-medium">{t("history.room")}</th>
                <th className="px-4 py-2 font-medium">{t("history.code")}</th>
                <th className="px-4 py-2 font-medium">{t("history.role")}</th>
                <th className="px-4 py-2 font-medium">{t("history.status")}</th>
                <th className="px-4 py-2 font-medium">{t("history.joined")}</th>
                <th className="px-4 py-2" />
              </tr>
            </thead>
            <tbody>
              {history.data.map((item) => (
                <tr key={`${item.roomId}-${item.joinedAtUtc}`} className="border-b border-slate-800/60">
                  <td className="px-4 py-2 font-medium text-white">{item.name}</td>
                  <td className="px-4 py-2 font-mono text-slate-400">{item.code}</td>
                  <td className="px-4 py-2">
                    <Badge className="bg-slate-800 text-slate-300">
                      {t(item.role === "Host" ? "enums.roleHost" : "enums.roleMember")}
                    </Badge>
                  </td>
                  <td className="px-4 py-2">
                    <Badge
                      className={
                        item.status === "Active"
                          ? "bg-emerald-900/60 text-emerald-300"
                          : "bg-slate-800 text-slate-400"
                      }
                    >
                      {t(item.status === "Active" ? "enums.statusActive" : "enums.statusClosed")}
                    </Badge>
                  </td>
                  <td className="px-4 py-2 text-slate-400">{formatDate(item.joinedAtUtc)}</td>
                  <td className="px-4 py-2 text-right">
                    {item.status === "Active" ? (
                      <Link href={`/rooms/${item.roomId}`} className="text-indigo-400 hover:text-indigo-300">
                        {t("history.rejoin")}
                      </Link>
                    ) : null}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      ) : (
        <p className="text-sm text-slate-500">{t("history.empty")}</p>
      )}
    </div>
  );
}
