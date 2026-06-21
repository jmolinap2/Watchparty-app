"use client";

import { useState } from "react";
import Link from "next/link";
import { authApi } from "@/lib/api/auth";
import { Alert, Button, Card, Field, Input } from "@/components/ui";
import { errorMessage } from "@/lib/utils";
import { useT } from "@/lib/i18n";

export default function ForgotPasswordPage() {
  const t = useT();
  const [email, setEmail] = useState("");
  const [done, setDone] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      await authApi.forgotPassword(email);
      setDone(true);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }

  return (
    <Card>
      <h2 className="mb-4 text-xl font-semibold text-white">{t("auth.forgotTitle")}</h2>
      {done ? (
        <Alert kind="success">{t("auth.resetSentEmail", { email })}</Alert>
      ) : (
        <form onSubmit={onSubmit} className="space-y-4">
          {error ? <Alert>{error}</Alert> : null}
          <Field label={t("auth.email")} hint={t("auth.forgotHint")}>
            <Input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
          </Field>
          <Button type="submit" disabled={loading} className="w-full">
            {loading ? t("auth.sending") : t("auth.sendResetLink")}
          </Button>
        </form>
      )}
      <div className="mt-4 text-center text-sm text-slate-400">
        <Link href="/login" className="hover:text-indigo-400">
          {t("auth.backToSignIn")}
        </Link>
      </div>
    </Card>
  );
}
