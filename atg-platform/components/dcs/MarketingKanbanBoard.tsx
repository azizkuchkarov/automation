"use client";

import { useLocale, useTranslations } from "next-intl";
import Link from "next/link";
import {
  AlertTriangle,
  ArrowUpRight,
  CheckCircle2,
  FileSearch,
  FileText,
  Globe2,
  Inbox,
  Send,
  UserCheck,
} from "lucide-react";
import {
  MarketingBoardColumn,
  MarketingRecordListItem,
  MarketingRecordStatus,
  categoryLabel,
  deadlineColorClass,
  type MarketingRequestCategory,
} from "@/lib/marketing";
import { dcsTheme } from "@/components/dcs/dcsTheme";
import { cn } from "@/lib/utils";
import type { LucideIcon } from "lucide-react";

type BoardColumnStatus = Extract<
  MarketingRecordStatus,
  | "WaitingAccept"
  | "RfqSent"
  | "KpAnalysis"
  | "PlanPreparation"
  | "PlanPortalApproval"
  | "CompletedToContract"
>;

const COLUMN_CONFIG: Record<
  BoardColumnStatus,
  { dot: string; ring: string; header: string; icon: LucideIcon }
> = {
  WaitingAccept: {
    dot: "bg-amber-500",
    ring: "ring-amber-500/25",
    header: "from-amber-500/12 via-amber-500/5 to-transparent",
    icon: UserCheck,
  },
  RfqSent: {
    dot: "bg-violet-500",
    ring: "ring-violet-500/25",
    header: "from-violet-500/12 via-violet-500/5 to-transparent",
    icon: Send,
  },
  KpAnalysis: {
    dot: "bg-indigo-500",
    ring: "ring-indigo-500/25",
    header: "from-indigo-500/12 via-indigo-500/5 to-transparent",
    icon: FileSearch,
  },
  PlanPreparation: {
    dot: "bg-sky-500",
    ring: "ring-sky-500/25",
    header: "from-sky-500/12 via-sky-500/5 to-transparent",
    icon: FileText,
  },
  PlanPortalApproval: {
    dot: "bg-orange-500",
    ring: "ring-orange-500/25",
    header: "from-orange-500/12 via-orange-500/5 to-transparent",
    icon: Globe2,
  },
  CompletedToContract: {
    dot: "bg-emerald-500",
    ring: "ring-emerald-500/25",
    header: "from-emerald-500/12 via-emerald-500/5 to-transparent",
    icon: CheckCircle2,
  },
};

export function MarketingKanbanBoard({ columns }: { columns: MarketingBoardColumn[] }) {
  const t = useTranslations("dcs.marketing.board");
  const locale = useLocale();

  return (
    <div className="relative -mx-1">
        <div className="pointer-events-none absolute inset-y-0 left-0 z-10 w-8 bg-gradient-to-r from-background to-transparent" />
        <div className="pointer-events-none absolute inset-y-0 right-0 z-10 w-8 bg-gradient-to-l from-background to-transparent" />
        <div className="flex gap-4 overflow-x-auto pb-6 px-1 min-h-[calc(100vh-14rem)] scroll-smooth">
          {columns.map((col) => {
            const status = col.status as BoardColumnStatus;
            const theme = COLUMN_CONFIG[status] ?? {
              dot: "bg-slate-400",
              ring: "ring-slate-400/20",
              header: "from-slate-500/10 to-transparent",
              icon: Inbox,
            };
            const Icon = theme.icon;
            const label = t(`columns.${status}`);
            const hint = t(`columnHints.${status}`);

            return (
              <div
                key={col.status}
                className={cn(
                  "flex flex-col w-[min(100%,320px)] shrink-0 rounded-2xl overflow-hidden",
                  dcsTheme.premiumCard,
                  "ring-1 ring-inset",
                  theme.ring
                )}
              >
                <div
                  className={cn(
                    "relative px-4 py-3.5 border-b border-border/40 bg-gradient-to-br",
                    theme.header
                  )}
                >
                  <div className="flex items-start gap-3">
                    <div
                      className={cn(
                        "flex h-9 w-9 shrink-0 items-center justify-center rounded-xl",
                        "bg-surface/80 border border-border/50 shadow-sm"
                      )}
                    >
                      <Icon size={16} className="text-foreground/70" />
                    </div>
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center gap-2">
                        <span className={cn("w-2 h-2 rounded-full shrink-0", theme.dot)} />
                        <span className="text-[13px] font-bold text-foreground/90 truncate">{label}</span>
                      </div>
                      <p className="text-[11px] text-foreground/45 mt-1 leading-snug line-clamp-2">{hint}</p>
                    </div>
                    <span
                      className={cn(
                        "text-[11px] font-bold tabular-nums min-w-[26px] h-[26px] flex items-center justify-center rounded-lg",
                        "bg-surface text-foreground/55 border border-border/60 shadow-sm"
                      )}
                    >
                      {col.items.length}
                    </span>
                  </div>
                </div>

                <div className="flex-1 p-2.5 space-y-2.5 overflow-y-auto max-h-[calc(100vh-16rem)] bg-foreground/[0.015]">
                  {col.items.length === 0 ? (
                    <div className="flex flex-col items-center justify-center gap-2.5 py-12 px-4 rounded-xl border border-dashed border-border/50 bg-surface/50">
                      <div className="w-10 h-10 rounded-full bg-border/25 flex items-center justify-center">
                        <Inbox size={18} className="text-foreground/25" />
                      </div>
                      <p className="text-xs font-medium text-foreground/45 text-center">{t("empty")}</p>
                      <p className="text-[10px] text-foreground/35 text-center leading-relaxed max-w-[200px]">
                        {t("emptyHint")}
                      </p>
                    </div>
                  ) : (
                    col.items.map((item) => (
                      <KanbanCard key={item.id} item={item} locale={locale} t={t} />
                    ))
                  )}
                </div>
              </div>
            );
          })}
        </div>
    </div>
  );
}

function KanbanCard({
  item,
  locale,
  t,
}: {
  item: MarketingRecordListItem;
  locale: string;
  t: ReturnType<typeof useTranslations<"dcs.marketing.board">>;
}) {
  const deadlineKey =
    item.deadlineColor === "red"
      ? "overdue"
      : item.deadlineColor === "orange"
        ? "critical"
        : item.deadlineColor === "yellow"
          ? "warning"
          : "ok";
  const days = item.remainingWorkingDays ?? 0;
  const executorInitial = item.marketingExecutorName?.trim().charAt(0)?.toUpperCase() ?? "?";

  return (
    <Link
      href={`/${locale}/automation/documents/${item.documentId}`}
      title={t("openRequest")}
      className={cn(
        "group block rounded-xl border border-border/55 bg-surface p-3.5",
        "shadow-[0_1px_2px_rgba(15,23,42,0.04)]",
        "hover:border-pink-500/35 hover:shadow-md hover:shadow-pink-500/5",
        "transition-all duration-200"
      )}
    >
      <div className="flex items-start justify-between gap-2">
        <p className="font-mono text-[11px] font-bold text-pink-600 dark:text-pink-400 truncate">
          {item.portalNumber ?? t("noPortal")}
        </p>
        <ArrowUpRight
          size={14}
          className="shrink-0 text-foreground/20 group-hover:text-pink-500 transition-colors"
        />
      </div>

      <p className="text-sm font-semibold mt-1.5 line-clamp-2 leading-snug text-foreground/90">
        {item.requestTitle ?? t("noTitle")}
      </p>

      <div className="mt-2.5 flex flex-wrap gap-1.5">
        {item.requestCategory != null && (
          <span className="px-2 py-0.5 rounded-md text-[10px] font-semibold bg-violet-500/10 text-violet-700 dark:text-violet-300 border border-violet-500/15">
            {categoryLabel(item.requestCategory as MarketingRequestCategory, locale)}
          </span>
        )}
        {item.deadlineDate && (
          <span
            className={cn(
              "inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-[10px] font-semibold",
              deadlineColorClass(item.deadlineColor)
            )}
          >
            {item.deadlineColor === "red" && <AlertTriangle size={10} className="shrink-0" />}
            {t(`deadline.${deadlineKey}`, { count: Math.abs(days) })}
          </span>
        )}
        {item.offerCount > 0 && (
          <span className="px-2 py-0.5 rounded-md text-[10px] font-semibold bg-foreground/[0.06] text-foreground/60 border border-border/40">
            {t("offers", { count: item.offerCount })}
          </span>
        )}
      </div>

      {item.marketingExecutorName && (
        <div className="mt-3 flex items-center gap-2 pt-2.5 border-t border-border/40">
          <span className="flex h-6 w-6 items-center justify-center rounded-full bg-pink-500/12 text-[10px] font-bold text-pink-700 dark:text-pink-300">
            {executorInitial}
          </span>
          <div className="min-w-0">
            <p className="text-[10px] text-foreground/40 leading-none">{t("executor")}</p>
            <p className="text-[11px] font-medium text-foreground/65 truncate">{item.marketingExecutorName}</p>
          </div>
        </div>
      )}
    </Link>
  );
}

export function MarketingKanbanSkeleton() {
  return (
    <div className="animate-pulse">
      <div className="flex gap-4 overflow-hidden">
        {Array.from({ length: 6 }).map((_, i) => (
          <div
            key={i}
            className={cn("w-[320px] shrink-0 h-[420px]", dcsTheme.premiumCard, "bg-foreground/[0.03]")}
          />
        ))}
      </div>
    </div>
  );
}
