"use client";

import { useState } from "react";
import { Alert, Button, Textarea } from "@/components/ui";
import { errorMessage } from "@/lib/utils";
import { useT } from "@/lib/i18n";

interface ReportModalProps {
  open: boolean;
  title: string;
  subtitle?: string;
  onClose: () => void;
  onSubmit: (reason: string) => Promise<void>;
}

export function ReportModal({ open, title, subtitle, onClose, onSubmit }: ReportModalProps) {
  const t = useT();
  const [reason, setReason] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  if (!open) return null;

  async function submit() {
    if (!reason.trim()) return;
    setError(null);
    setLoading(true);
    try {
      await onSubmit(reason.trim());
      setReason("");
      onClose();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4" onClick={onClose}>
      <div
        className="w-full max-w-md rounded-xl border border-slate-700 bg-slate-900 p-5"
        onClick={(e) => e.stopPropagation()}
      >
        <h3 className="text-lg font-semibold text-white">{title}</h3>
        {subtitle ? <p className="mt-1 text-sm text-slate-400">{subtitle}</p> : null}
        <div className="mt-4 space-y-3">
          {error ? <Alert>{error}</Alert> : null}
          <Textarea
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder={t("reports.reasonPlaceholder")}
            rows={4}
            maxLength={1000}
          />
          <div className="flex justify-end gap-2">
            <Button variant="ghost" onClick={onClose}>
              {t("common.cancel")}
            </Button>
            <Button variant="danger" onClick={submit} disabled={loading || !reason.trim()}>
              {loading ? t("reports.submitting") : t("reports.submit")}
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
