import { dcsTheme } from "@/components/dcs/dcsTheme";
import { cn } from "@/lib/utils";

/** HR module tokens — aligned with ATG brand (blue), not generic violet. */
export const hrTheme = {
  mesh: dcsTheme.meshBg,
  grid: dcsTheme.gridOverlay,
  card: dcsTheme.premiumCard,
  table: dcsTheme.tableShell,
  primaryBtn:
    "bg-gradient-to-r from-blue-700 to-sky-500 hover:from-blue-600 hover:to-sky-400 text-white shadow-lg shadow-blue-500/25 border-0",
  secondaryBtn: "bg-white/90 border border-slate-200 hover:bg-slate-50 text-slate-700",
  accentText: "text-blue-700",
  accentSoft: "bg-blue-50 text-blue-800 border-blue-200",
  activeNav: "bg-blue-500/10 text-blue-800 border-l-2 border-blue-600",
  link: "text-blue-700 hover:text-blue-900",
  inputFocus: "focus:outline-none focus:ring-2 focus:ring-blue-500/25 focus:border-blue-400",
  iconTile: "bg-gradient-to-br from-blue-700 to-sky-500 shadow-md shadow-blue-500/25",
  sectionLabel: "text-[10px] font-bold uppercase tracking-widest text-foreground/35",
} as const;

export function hrPhaseBadgeClass(phase: string) {
  switch (phase) {
    case "Approved":
    case "Completed":
      return "bg-emerald-50 text-emerald-700 border-emerald-200";
    case "Rejected":
      return "bg-red-50 text-red-700 border-red-200";
    case "Draft":
      return "bg-slate-100 text-slate-600 border-slate-200";
    case "OrderPending":
      return "bg-amber-50 text-amber-800 border-amber-200";
    case "AwaitingOrderEimzo":
      return "bg-sky-50 text-sky-800 border-sky-200";
    case "HrReview":
      return "bg-indigo-50 text-indigo-700 border-indigo-200";
    default:
      return "bg-blue-50 text-blue-700 border-blue-200";
  }
}

export function formatHrDate(value: string, locale: string, style: "short" | "long" = "short") {
  return new Date(value).toLocaleDateString(locale.startsWith("en") ? "en-GB" : "ru-RU", {
    day: "2-digit",
    month: style === "long" ? "long" : "short",
    year: "numeric",
  });
}

export function hrInputClass(extra?: string) {
  return cn(
    "w-full rounded-xl border border-slate-200/90 bg-white px-3.5 py-2.5 text-sm text-foreground shadow-sm",
    hrTheme.inputFocus,
    extra,
  );
}
