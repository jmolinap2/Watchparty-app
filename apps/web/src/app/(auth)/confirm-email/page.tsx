"use client";

import { Suspense, useEffect, useState } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { authApi } from "@/lib/api/auth";
import { Alert, Card, Spinner } from "@/components/ui";
import { errorMessage } from "@/lib/utils";
import { useT } from "@/lib/i18n";

function ConfirmEmailInner() {
  const t = useT();
  const params = useSearchParams();
  const token = params.get("token");
  const [status, setStatus] = useState<"loading" | "ok" | "error">("loading");
  const [message, setMessage] = useState("");

  useEffect(() => {
    if (!token) {
      setStatus("error");
      setMessage(t("auth.confirmMissingToken"));
      return;
    }
    authApi
      .confirmEmail(token)
      .then(() => setStatus("ok"))
      .catch((err) => {
        setStatus("error");
        setMessage(errorMessage(err));
      });
  }, [token, t]);

  return (
    <Card>
      <h2 className="mb-4 text-xl font-semibold text-white">{t("auth.emailConfirmation")}</h2>
      {status === "loading" ? (
        <div className="flex items-center gap-3 text-slate-300">
          <Spinner /> {t("auth.confirming")}
        </div>
      ) : status === "ok" ? (
        <Alert kind="success">{t("auth.confirmDone")}</Alert>
      ) : (
        <Alert>{message}</Alert>
      )}
      <div className="mt-4 text-center text-sm text-slate-400">
        <Link href="/login" className="hover:text-indigo-400">
          {t("auth.backToSignIn")}
        </Link>
      </div>
    </Card>
  );
}

export default function ConfirmEmailPage() {
  return (
    <Suspense fallback={<Spinner />}>
      <ConfirmEmailInner />
    </Suspense>
  );
}
