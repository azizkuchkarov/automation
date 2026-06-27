"use client";

import { useLocale, useTranslations } from "next-intl";
import Link from "next/link";
import { Inbox } from "lucide-react";
import { MarketingBoardColumn, MarketingRecordListItem, deadlineColorClass } from "@/lib/marketing";
import { cn } from "@/lib/utils";

const COLUMN_THEME: Record<string, { dot: string; ring: string }> = {
  WaitingAccept: { dot: "bg-amber-500", ring: "ring-amber-500/20" },
  RfqSent: { dot: "bg-violet-500", ring: "ring-violet-500/20" },
  KpAnalysis: { dot: "bg-indigo-500", ring: "ring-indigo-500/20" },
  PlanPreparation: { dot: "bg-atg-blue", ring: "ring-atg-blue/20" },
  PlanPortalApproval: { dot: "bg-orange-500", ring: "ring-orange-500/20" },
  CompletedToContract: { dot: "bg-emerald-500", ring: "ring-emerald-500/20" },
};

export function MarketingKanbanBoard({ columns }: { columns: MarketingBoardColumn[] }) {
  const t = useTranslations("dcs.marketing.board");
  const locale = useLocale();

  return (
    <div className="flex gap-4 overflow-x-auto pb-6 px-1 min-h-[calc(100vh-11rem)]">
      {columns.map((col) => {
        const theme = COLUMN_THEME[col.status] ?? { dot: "bg-slate-400", ring: "ring-slate-400/20" };
        const label = locale.startsWith("en") ? col.labelEn : col.labelRu;

        return (
          <div
            key={col.status}
            className={cn(
              "flex flex-col w-[300px] shrink-0 rounded-xl",
              "bg-slate-100/90 dark:bg-[#161b22]/90",
              "border border-border/50 shadow-sm ring-1 ring-inset",
              theme.ring
            )}
          >
            <div className="flex items-center gap-2.5 px-3.5 py-3 border-b border-border/40">
              <span className={cn("w-2.5 h-2.5 rounded-full shrink-0", theme.dot)} />
              <span className="text-[13px] font-semibold text-foreground/85 flex-1 truncate">{label}</span>
              <span className="text-[11px] font-bold tabular-nums min-w-[22px] h-[22px] flex items-center justify-center rounded-md bg-surface text-foreground/50 border border-border/60">
                {col.items.length}
              </span>
            </div>
            <div className="flex-1 p-2.5 space-y-2.5 overflow-y-auto max-h-[calc(100vh-13rem)]">
              {col.items.length === 0 ? (
                <div className="flex flex-col items-center justify-center gap-2 py-10 px-4 rounded-lg border border-dashed border-border/60 bg-surface/40">
                  <Inbox size={16} className="text-foreground/30" />
                  <p className="text-xs text-foreground/40 text-center">{t("empty")}</p>
                </div>
              ) : (
                col.items.map((item) => (
                  <KanbanCard key={item.id} item={item} locale={locale} />
                ))
              )}
            </div>
          </div>
        );
      })}
    </div>
  );
}

function KanbanCard({ item, locale }: { item: MarketingRecordListItem; locale: string }) {
  return (
    <Link
      href={`/${locale}/automation/documents/${item.documentId}`}
      className="block rounded-xl border border-border/60 bg-surface p-3.5 shadow-sm hover:border-pink-500/30 hover:shadow-md transition-all"
    >
      <p className="font-mono text-[11px] font-bold text-pink-600 dark:text-pink-400">{item.portalNumber ?? "—"}</p>
      <p className="text-sm font-medium mt-1 line-clamp-2 leading-snug">{item.requestTitle ?? "—"}</p>
      <div className="mt-2 flex flex-wrap gap-1.5 text-[10px]">
        {item.deadlineDate && (
          <span className={cn("px-1.5 py-0.5 rounded-md font-semibold", deadlineColorClass(item.deadlineColor))}>
            {item.remainingWorkingDays ?? 0} wd
          </span>
        )}
        {item.offerCount > 0 && (
          <span className="px-1.5 py-0.5 rounded-md bg-foreground/[0.06] text-foreground/60">
            KP: {item.offerCount}
          </span>
        )}
      </div>
      {item.marketingExecutorName && (
        <p className="text-[11px] text-foreground/45 mt-2 truncate">{item.marketingExecutorName}</p>
      )}
    </Link>
  );
}
