import type { LucideIcon } from "lucide-react";
import { FileText, Inbox, ScrollText, Send } from "lucide-react";

export type OfficeDocKind = "incoming" | "outgoing" | "memo" | "orders";

export interface OfficeDocTheme {
  kind: OfficeDocKind;
  label: string;
  icon: LucideIcon;
  /** Tailwind color name base */
  accent: "sky" | "violet" | "amber" | "orange";
  meshBg: string;
  accentLine: string;
  headerGlow: string;
  iconBg: string;
  iconRing: string;
  numberText: string;
  phaseBadge: string;
  phaseBadgeActive: string;
  stepActive: string;
  stepActiveRing: string;
  stepLine: string;
  tabActive: string;
  tabIndicator: string;
  inputFocus: string;
  linkHover: string;
  rowHover: string;
  avatarBg: string;
  avatarText: string;
  primaryBtn: string;
  searchFocus: string;
  workflowCardBorder: string;
  workflowCardBg: string;
  sectionAccent: string;
}

export const OFFICE_DOC_THEMES: Record<OfficeDocKind, OfficeDocTheme> = {
  incoming: {
    kind: "incoming",
    label: "Incoming",
    icon: Inbox,
    accent: "sky",
    meshBg:
      "before:bg-[radial-gradient(ellipse_80%_60%_at_50%_-20%,rgba(56,189,248,0.14),transparent),radial-gradient(ellipse_60%_40%_at_100%_0%,rgba(14,165,233,0.08),transparent),radial-gradient(ellipse_50%_30%_at_0%_100%,rgba(2,132,199,0.06),transparent)]",
    accentLine: "from-transparent via-sky-500 to-transparent",
    headerGlow: "from-sky-500/[0.08] to-transparent",
    iconBg: "bg-gradient-to-br from-sky-500/20 to-cyan-600/10 text-sky-600 dark:text-sky-400",
    iconRing: "ring-sky-500/20",
    numberText: "text-sky-600 dark:text-sky-400",
    phaseBadge: "bg-sky-500/10 text-sky-700 dark:text-sky-300 border-sky-500/25",
    phaseBadgeActive: "bg-sky-500/15 text-sky-800 dark:text-sky-200 border-sky-500/40 shadow-sm shadow-sky-500/10",
    stepActive: "bg-sky-600 text-white",
    stepActiveRing: "ring-sky-500/25",
    stepLine: "bg-sky-500",
    tabActive: "text-sky-700 dark:text-sky-300",
    tabIndicator: "bg-sky-500",
    inputFocus: "focus:ring-sky-500/30 focus:border-sky-500/40",
    linkHover: "text-sky-600 dark:text-sky-400 hover:text-sky-700 dark:hover:text-sky-300",
    rowHover: "hover:bg-sky-500/[0.05] dark:hover:bg-sky-400/[0.07]",
    avatarBg: "from-sky-500/25 to-cyan-600/15",
    avatarText: "text-sky-600 dark:text-sky-400",
    primaryBtn: "bg-gradient-to-r from-sky-600 to-cyan-500 hover:from-sky-500 hover:to-cyan-400 shadow-sky-500/25",
    searchFocus: "focus:ring-sky-500/30 focus:border-sky-500/40",
    workflowCardBorder: "border-l-sky-500",
    workflowCardBg: "bg-gradient-to-r from-sky-500/[0.04] to-transparent",
    sectionAccent: "text-sky-600 dark:text-sky-400",
  },
  outgoing: {
    kind: "outgoing",
    label: "Outgoing",
    icon: Send,
    accent: "violet",
    meshBg:
      "before:bg-[radial-gradient(ellipse_80%_60%_at_50%_-20%,rgba(139,92,246,0.14),transparent),radial-gradient(ellipse_60%_40%_at_100%_0%,rgba(124,58,237,0.08),transparent),radial-gradient(ellipse_50%_30%_at_0%_100%,rgba(109,40,217,0.06),transparent)]",
    accentLine: "from-transparent via-violet-500 to-transparent",
    headerGlow: "from-violet-500/[0.08] to-transparent",
    iconBg: "bg-gradient-to-br from-violet-500/20 to-purple-600/10 text-violet-600 dark:text-violet-400",
    iconRing: "ring-violet-500/20",
    numberText: "text-violet-600 dark:text-violet-400",
    phaseBadge: "bg-violet-500/10 text-violet-700 dark:text-violet-300 border-violet-500/25",
    phaseBadgeActive: "bg-violet-500/15 text-violet-800 dark:text-violet-200 border-violet-500/40 shadow-sm shadow-violet-500/10",
    stepActive: "bg-violet-600 text-white",
    stepActiveRing: "ring-violet-500/25",
    stepLine: "bg-violet-500",
    tabActive: "text-violet-700 dark:text-violet-300",
    tabIndicator: "bg-violet-500",
    inputFocus: "focus:ring-violet-500/30 focus:border-violet-500/40",
    linkHover: "text-violet-600 dark:text-violet-400 hover:text-violet-700 dark:hover:text-violet-300",
    rowHover: "hover:bg-violet-500/[0.05] dark:hover:bg-violet-400/[0.07]",
    avatarBg: "from-violet-500/25 to-purple-600/15",
    avatarText: "text-violet-600 dark:text-violet-400",
    primaryBtn: "bg-gradient-to-r from-violet-600 to-purple-500 hover:from-violet-500 hover:to-purple-400 shadow-violet-500/25",
    searchFocus: "focus:ring-violet-500/30 focus:border-violet-500/40",
    workflowCardBorder: "border-l-violet-500",
    workflowCardBg: "bg-gradient-to-r from-violet-500/[0.04] to-transparent",
    sectionAccent: "text-violet-600 dark:text-violet-400",
  },
  memo: {
    kind: "memo",
    label: "Memo",
    icon: FileText,
    accent: "amber",
    meshBg:
      "before:bg-[radial-gradient(ellipse_80%_60%_at_50%_-20%,rgba(245,158,11,0.14),transparent),radial-gradient(ellipse_60%_40%_at_100%_0%,rgba(217,119,6,0.08),transparent),radial-gradient(ellipse_50%_30%_at_0%_100%,rgba(180,83,9,0.06),transparent)]",
    accentLine: "from-transparent via-amber-500 to-transparent",
    headerGlow: "from-amber-500/[0.08] to-transparent",
    iconBg: "bg-gradient-to-br from-amber-500/20 to-orange-600/10 text-amber-600 dark:text-amber-400",
    iconRing: "ring-amber-500/20",
    numberText: "text-amber-600 dark:text-amber-400",
    phaseBadge: "bg-amber-500/10 text-amber-700 dark:text-amber-300 border-amber-500/25",
    phaseBadgeActive: "bg-amber-500/15 text-amber-800 dark:text-amber-200 border-amber-500/40 shadow-sm shadow-amber-500/10",
    stepActive: "bg-amber-600 text-white",
    stepActiveRing: "ring-amber-500/25",
    stepLine: "bg-amber-500",
    tabActive: "text-amber-700 dark:text-amber-300",
    tabIndicator: "bg-amber-500",
    inputFocus: "focus:ring-amber-500/30 focus:border-amber-500/40",
    linkHover: "text-amber-600 dark:text-amber-400 hover:text-amber-700 dark:hover:text-amber-300",
    rowHover: "hover:bg-amber-500/[0.05] dark:hover:bg-amber-400/[0.07]",
    avatarBg: "from-amber-500/25 to-orange-600/15",
    avatarText: "text-amber-600 dark:text-amber-400",
    primaryBtn: "bg-gradient-to-r from-amber-600 to-orange-500 hover:from-amber-500 hover:to-orange-400 shadow-amber-500/25",
    searchFocus: "focus:ring-amber-500/30 focus:border-amber-500/40",
    workflowCardBorder: "border-l-amber-500",
    workflowCardBg: "bg-gradient-to-r from-amber-500/[0.04] to-transparent",
    sectionAccent: "text-amber-600 dark:text-amber-400",
  },
  orders: {
    kind: "orders",
    label: "Orders",
    icon: ScrollText,
    accent: "orange",
    meshBg:
      "before:bg-[radial-gradient(ellipse_80%_60%_at_50%_-20%,rgba(249,115,22,0.14),transparent),radial-gradient(ellipse_60%_40%_at_100%_0%,rgba(234,88,12,0.08),transparent),radial-gradient(ellipse_50%_30%_at_0%_100%,rgba(194,65,12,0.06),transparent)]",
    accentLine: "from-transparent via-orange-500 to-transparent",
    headerGlow: "from-orange-500/[0.08] to-transparent",
    iconBg: "bg-gradient-to-br from-orange-500/20 to-amber-600/10 text-orange-600 dark:text-orange-400",
    iconRing: "ring-orange-500/20",
    numberText: "text-orange-600 dark:text-orange-400",
    phaseBadge: "bg-orange-500/10 text-orange-700 dark:text-orange-300 border-orange-500/25",
    phaseBadgeActive: "bg-orange-500/15 text-orange-800 dark:text-orange-200 border-orange-500/40 shadow-sm shadow-orange-500/10",
    stepActive: "bg-orange-600 text-white",
    stepActiveRing: "ring-orange-500/25",
    stepLine: "bg-orange-500",
    tabActive: "text-orange-700 dark:text-orange-300",
    tabIndicator: "bg-orange-500",
    inputFocus: "focus:ring-orange-500/30 focus:border-orange-500/40",
    linkHover: "text-orange-600 dark:text-orange-400 hover:text-orange-700 dark:hover:text-orange-300",
    rowHover: "hover:bg-orange-500/[0.05] dark:hover:bg-orange-400/[0.07]",
    avatarBg: "from-orange-500/25 to-amber-600/15",
    avatarText: "text-orange-600 dark:text-orange-400",
    primaryBtn: "bg-gradient-to-r from-orange-600 to-amber-500 hover:from-orange-500 hover:to-amber-400 shadow-orange-500/25",
    searchFocus: "focus:ring-orange-500/30 focus:border-orange-500/40",
    workflowCardBorder: "border-l-orange-500",
    workflowCardBg: "bg-gradient-to-r from-orange-500/[0.04] to-transparent",
    sectionAccent: "text-orange-600 dark:text-orange-400",
  },
};

export function officeDocTheme(kind: OfficeDocKind): OfficeDocTheme {
  return OFFICE_DOC_THEMES[kind];
}
