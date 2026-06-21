"use client";

import { useQuery } from "@tanstack/react-query";
import { reportsApi } from "@/lib/api/reports";
import { Alert, Badge, Card, Spinner } from "@/components/ui";
import { errorMessage, formatDate } from "@/lib/utils";
import { useT, type MessageKey } from "@/lib/i18n";

const statusStyles: Record<string, string> = {
  Pending: "bg-amber-900/60 text-amber-300",
  Resolved: "bg-emerald-900/60 text-emerald-300",
  Rejected: "bg-slate-800 text-slate-400",
};

const statusKey: Record<string, MessageKey> = {
  Pending: "enums.reportPending",
  Resolved: "enums.reportResolved",
  Rejected: "enums.reportRejected",
};

export default function ReportsPage() {
  const t = useT();
  const reports = useQuery({ queryKey: ["my-reports"], queryFn: reportsApi.mine });

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold text-white">{t("reports.title")}</h1>
      <p className="text-sm text-slate-400">{t("reports.subtitle")}</p>
      {reports.isLoading ? (
        <Spinner />
      ) : reports.isError ? (
        <Alert>{errorMessage(reports.error)}</Alert>
      ) : reports.data && reports.data.length > 0 ? (
        <div className="space-y-2">
          {reports.data.map((r) => (
            <Card key={r.id} className="flex items-start justify-between gap-4">
              <div>
                <div className="flex items-center gap-2">
                  <Badge className="bg-slate-800 text-slate-300">
                    {t(r.type === "Message" ? "reports.typeMessage" : "reports.typeUser")}
                  </Badge>
                  <Badge className={statusStyles[r.status] ?? "bg-slate-800 text-slate-400"}>
                    {statusKey[r.status] ? t(statusKey[r.status]) : r.status}
                  </Badge>
                </div>
                <p className="mt-2 text-sm text-slate-200">{r.reason}</p>
                {r.resolutionNote ? (
                  <p className="mt-1 text-xs text-slate-500">
                    {t("reports.moderatorNote", { note: r.resolutionNote })}
                  </p>
                ) : null}
              </div>
              <div className="shrink-0 text-right text-xs text-slate-500">{formatDate(r.createdAtUtc)}</div>
            </Card>
          ))}
        </div>
      ) : (
        <p className="text-sm text-slate-500">{t("reports.empty")}</p>
      )}
    </div>
  );
}
