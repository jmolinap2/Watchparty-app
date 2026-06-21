/** Supported locales. `defaultLocale` is the fallback when nothing else matches. */
export const locales = ["es", "en"] as const;
export type Locale = (typeof locales)[number];

export const defaultLocale: Locale = "es";

/** SecureStore key used to persist an explicit user choice (overrides the device language). */
export const localeStorageKey = "wp_locale";

export function isLocale(value: string | undefined | null): value is Locale {
  return value != null && (locales as readonly string[]).includes(value);
}

/**
 * Picks the best supported locale from one or more language tags (e.g. a saved
 * choice or the device language codes). Falls back to {@link defaultLocale}.
 */
export function resolveLocale(...preferences: (string | undefined | null)[]): Locale {
  for (const pref of preferences) {
    if (!pref) continue;
    const base = pref.split("-")[0]?.trim().toLowerCase();
    if (isLocale(base)) return base;
  }
  return defaultLocale;
}
