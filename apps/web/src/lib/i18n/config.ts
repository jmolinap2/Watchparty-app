/** Supported locales. `defaultLocale` is the fallback when nothing else matches. */
export const locales = ["es", "en"] as const;
export type Locale = (typeof locales)[number];

export const defaultLocale: Locale = "es";

/** Cookie used to persist an explicit user choice (overrides the system language). */
export const localeCookieName = "wp_locale";

export function isLocale(value: string | undefined | null): value is Locale {
  return value != null && (locales as readonly string[]).includes(value);
}

/**
 * Picks the best supported locale from a browser/OS preference string such as a
 * cookie value or an `Accept-Language` header. Falls back to {@link defaultLocale}.
 */
export function resolveLocale(...preferences: (string | undefined | null)[]): Locale {
  for (const pref of preferences) {
    if (!pref) continue;
    for (const part of pref.split(",")) {
      const tag = part.split(";")[0]?.trim().toLowerCase();
      if (!tag) continue;
      const base = tag.split("-")[0];
      if (isLocale(base)) return base;
    }
  }
  return defaultLocale;
}
