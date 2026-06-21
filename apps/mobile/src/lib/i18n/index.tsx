import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { getLocales } from "expo-localization";
import * as SecureStore from "expo-secure-store";
import { en, type Messages } from "./messages/en";
import { es } from "./messages/es";
import { defaultLocale, localeStorageKey, resolveLocale, type Locale } from "./config";

const dictionaries: Record<Locale, Messages> = { en, es };

/** Dotted key paths into the message tree, e.g. "auth.signIn". */
type Leaves<T> = {
  [K in keyof T & string]: T[K] extends string ? K : `${K}.${Leaves<T[K]>}`;
}[keyof T & string];

export type MessageKey = Leaves<Messages>;

function resolve(messages: Messages, key: string): string {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  let node: any = messages;
  for (const part of key.split(".")) {
    node = node?.[part];
    if (node == null) return key;
  }
  return typeof node === "string" ? node : key;
}

function interpolate(template: string, params?: Record<string, string | number>): string {
  if (!params) return template;
  return template.replace(/\{(\w+)\}/g, (_, name) =>
    params[name] != null ? String(params[name]) : `{${name}}`,
  );
}

export type TranslateFn = (key: MessageKey, params?: Record<string, string | number>) => string;

interface I18nContextValue {
  locale: Locale;
  t: TranslateFn;
  setLocale: (locale: Locale) => void;
}

const I18nContext = createContext<I18nContextValue | null>(null);

/** Best-effort device language read synchronously so first render is already localized. */
function deviceLocale(): Locale {
  try {
    const tag = getLocales()[0]?.languageTag ?? getLocales()[0]?.languageCode;
    return resolveLocale(tag);
  } catch {
    return defaultLocale;
  }
}

export function I18nProvider({ children }: { children: ReactNode }) {
  const [locale, setLocaleState] = useState<Locale>(deviceLocale);

  // Apply a previously saved explicit choice once on mount (overrides device language).
  useEffect(() => {
    let active = true;
    SecureStore.getItemAsync(localeStorageKey)
      .then((saved) => {
        if (active && saved && saved !== locale) setLocaleState(resolveLocale(saved));
      })
      .catch(() => undefined);
    return () => {
      active = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const setLocale = useCallback((next: Locale) => {
    setLocaleState(next);
    SecureStore.setItemAsync(localeStorageKey, next).catch(() => undefined);
  }, []);

  const value = useMemo<I18nContextValue>(() => {
    const messages = dictionaries[locale] ?? dictionaries[defaultLocale];
    return {
      locale,
      setLocale,
      t: (key, params) => interpolate(resolve(messages, key), params),
    };
  }, [locale, setLocale]);

  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>;
}

function useI18nContext(): I18nContextValue {
  const ctx = useContext(I18nContext);
  if (!ctx) throw new Error("useT/useI18n must be used within <I18nProvider>");
  return ctx;
}

/** Returns the translate function. */
export function useT(): TranslateFn {
  return useI18nContext().t;
}

/** Returns the active locale and a setter to change it. */
export function useI18n(): { locale: Locale; setLocale: (locale: Locale) => void } {
  const { locale, setLocale } = useI18nContext();
  return { locale, setLocale };
}
