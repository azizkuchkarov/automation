import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";

const inter = Inter({ subsets: ["latin", "cyrillic"], variable: "--font-inter" });

export const metadata: Metadata = {
  title: "ATG Unified Platform",
  description: "Asia Trans Gas JV LLC — Unified Workspace",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return children;
}

export { inter };
