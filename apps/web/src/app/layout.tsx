import type { Metadata, Viewport } from "next";
import "./globals.css";
import { Providers } from "./providers";
import { getServerLocale } from "@/lib/i18n/server";

export const metadata: Metadata = {
  title: "WatchParty",
  description: "Watch videos in sync with friends.",
};

export const viewport: Viewport = {
  themeColor: "#0f172a",
};

export default async function RootLayout({ children }: { children: React.ReactNode }) {
  const locale = await getServerLocale();
  return (
    <html lang={locale}>
      <body>
        <Providers locale={locale}>{children}</Providers>
      </body>
    </html>
  );
}
