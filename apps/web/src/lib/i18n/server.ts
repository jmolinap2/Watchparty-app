import { cookies, headers } from "next/headers";
import { localeCookieName, resolveLocale, type Locale } from "./config";

/**
 * Determines the initial locale for SSR: an explicit cookie choice wins, otherwise
 * the browser/OS `Accept-Language` header is used (falling back to the default).
 */
export async function getServerLocale(): Promise<Locale> {
  const cookieStore = await cookies();
  const fromCookie = cookieStore.get(localeCookieName)?.value;
  const acceptLanguage = (await headers()).get("accept-language");
  return resolveLocale(fromCookie, acceptLanguage);
}
