import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "ATG Unified Platform",
  description: "Asia Trans Gas JV LLC — Unified Workspace",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return children;
}
