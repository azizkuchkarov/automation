import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function formatRelativeTime(date: string | null, locale: string) {
  if (!date) return "—";
  const d = new Date(date);
  const diff = Date.now() - d.getTime();
  const minutes = Math.floor(diff / 60000);
  if (minutes < 1) return locale === "ru" ? "только что" : "just now";
  if (minutes < 60) return `${minutes}m`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h`;
  const days = Math.floor(hours / 24);
  return `${days}d`;
}

const ROLE_NAMES = [
  "SuperAdmin",
  "HOTopManager",
  "HONachalnik",
  "HOEngineer",
  "BMGMCManager",
  "BMGMCNachalnikiOtdeli",
  "BMGMCEngineer",
  "StationEngineer",
] as const;

export const adminRoles = ["SuperAdmin", "HOTopManager"];

export function normalizeRole(role: string | number | undefined): string {
  if (role === undefined || role === null) return "";
  if (typeof role === "number") return ROLE_NAMES[role] ?? String(role);
  const n = Number(role);
  if (!Number.isNaN(n) && ROLE_NAMES[n] && role === String(n)) return ROLE_NAMES[n];
  return role;
}

export function isAdminRole(role?: string | number) {
  return adminRoles.includes(normalizeRole(role));
}
