import type { Metadata } from "next";
import "./globals.css";
import Header from "@/components/Header";

export const metadata: Metadata = {
  title: "Polite RSS - Premium Dashboard",
  description: "A premium dashboard for your RSS feeds, powered by Next.js and GitHub Actions.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="ja">
      <body>
        <Header />
        <main className="main-container">
          {children}
        </main>
      </body>
    </html>
  );
}
