"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/hooks/use-auth";
import { useT } from "@/lib/i18n";
import { LanguageSwitcher } from "@/components/LanguageSwitcher";

export default function AuthLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const t = useT();
  const { hydrated, isAuthenticated } = useAuth();

  useEffect(() => {
    if (hydrated && isAuthenticated) router.replace("/home");
  }, [hydrated, isAuthenticated, router]);

  return (
    <div className="relative flex min-h-screen flex-col items-center justify-center px-4 py-10">
      <div className="absolute right-4 top-4">
        <LanguageSwitcher />
      </div>
      <div className="mb-8 text-center">
        <h1 className="text-3xl font-bold tracking-tight text-white">
          Watch<span className="text-indigo-400">Party</span>
        </h1>
        <p className="mt-1 text-sm text-slate-400">{t("common.tagline")}</p>
      </div>
      <div className="w-full max-w-md">{children}</div>
    </div>
  );
}
