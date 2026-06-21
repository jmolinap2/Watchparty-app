"use client";

import { useI18n, useT } from "@/lib/i18n";
import { locales, type Locale } from "@/lib/i18n/config";

/**
 * Compact language selector. The initial locale comes from the system (Accept-Language)
 * or a previously saved choice; changing it here persists the choice in a cookie.
 */
export function LanguageSwitcher({ className }: { className?: string }) {
  const { locale, setLocale } = useI18n();
  const t = useT();

  return (
    <select
      aria-label={t("common.language")}
      value={locale}
      onChange={(e) => setLocale(e.target.value as Locale)}
      className={
        className ??
        "rounded-md border border-slate-700 bg-slate-900 px-2 py-1 text-sm text-slate-300 hover:border-slate-600"
      }
    >
      {locales.map((l) => (
        <option key={l} value={l}>
          {l === "es" ? t("settings.languageEs") : t("settings.languageEn")}
        </option>
      ))}
    </select>
  );
}
