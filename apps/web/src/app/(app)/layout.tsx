"use client";

import { useEffect } from "react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useAuth } from "@/lib/hooks/use-auth";
import { Avatar, Spinner } from "@/components/ui";
import { cn } from "@/lib/utils";
import { useT, type MessageKey } from "@/lib/i18n";
import { LanguageSwitcher } from "@/components/LanguageSwitcher";

const navItems: { href: string; labelKey: MessageKey }[] = [
  { href: "/home", labelKey: "nav.home" },
  { href: "/history", labelKey: "nav.history" },
  { href: "/reports", labelKey: "nav.reports" },
  { href: "/profile", labelKey: "nav.profile" },
];

export default function AppLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const t = useT();
  const { hydrated, isAuthenticated, user, logout } = useAuth();

  useEffect(() => {
    if (hydrated && !isAuthenticated) router.replace("/login");
  }, [hydrated, isAuthenticated, router]);

  if (!hydrated || !isAuthenticated) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Spinner className="h-8 w-8" />
      </div>
    );
  }

  return (
    <div className="min-h-screen">
      <header className="sticky top-0 z-20 border-b border-slate-800 bg-slate-950/80 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-3">
          <Link href="/home" className="text-lg font-bold text-white">
            Watch<span className="text-indigo-400">Party</span>
          </Link>
          <nav className="hidden items-center gap-1 sm:flex">
            {navItems.map((item) => (
              <Link
                key={item.href}
                href={item.href}
                className={cn(
                  "rounded-md px-3 py-1.5 text-sm transition-colors",
                  pathname === item.href
                    ? "bg-slate-800 text-white"
                    : "text-slate-400 hover:text-white",
                )}
              >
                {t(item.labelKey)}
              </Link>
            ))}
          </nav>
          <div className="flex items-center gap-3">
            <LanguageSwitcher />
            {user ? <Avatar name={user.displayName} url={user.avatarUrl} size={32} /> : null}
            <button
              onClick={() => logout().then(() => router.replace("/login"))}
              className="text-sm text-slate-400 hover:text-rose-400"
            >
              {t("nav.signOut")}
            </button>
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-4 py-6">{children}</main>
    </div>
  );
}
