"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/hooks/use-auth";
import { Alert, Button, Card, Field, Input } from "@/components/ui";
import { errorMessage } from "@/lib/utils";
import { useT } from "@/lib/i18n";

export default function RegisterPage() {
  const router = useRouter();
  const t = useT();
  const { register } = useAuth();
  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      await register({ email, password, displayName });
      router.replace("/home");
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }

  return (
    <Card>
      <h2 className="mb-4 text-xl font-semibold text-white">{t("auth.createAccount")}</h2>
      <form onSubmit={onSubmit} className="space-y-4">
        {error ? <Alert>{error}</Alert> : null}
        <Field label={t("auth.displayName")}>
          <Input value={displayName} onChange={(e) => setDisplayName(e.target.value)} required maxLength={40} />
        </Field>
        <Field label={t("auth.email")}>
          <Input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required autoComplete="email" />
        </Field>
        <Field label={t("auth.password")} hint={t("auth.passwordHint")}>
          <Input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            minLength={8}
            autoComplete="new-password"
          />
        </Field>
        <Button type="submit" disabled={loading} className="w-full">
          {loading ? t("auth.creating") : t("auth.createAccount")}
        </Button>
      </form>
      <div className="mt-4 text-center text-sm text-slate-400">
        {t("auth.haveAccount")}{" "}
        <Link href="/login" className="hover:text-indigo-400">
          {t("auth.signIn")}
        </Link>
      </div>
    </Card>
  );
}
