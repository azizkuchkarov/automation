"use client";

import { useEffect, useMemo, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import Link from "next/link";
import { Megaphone, Search, ChevronLeft, ChevronRight } from "lucide-react";
import api, { getApiErrorMessage } from "@/lib/api";
import type { PagedResult } from "@/lib/paged";
import {
  ProcurementMarketingQueueItem,
  marketingSubPhaseLabel,
} from "@/lib/procurementRequest";
import { DcsPageHeader } from "@/components/dcs/DcsPageHeader";
import { DcsEmptyState, DcsListSkeleton } from "@/components/dcs/DcsEmptyState";
import { dcsTheme } from "@/components/dcs/dcsTheme";
import { cn } from "@/lib/utils";

type SubPhaseFilter = "all" | "Pending" | "InProgress" | "Completed";

interface QueueSummary {
  total: number;
  pending: number;
  inProgress: number;
  completed: number;
}

const PAGE_SIZE = 50;

export function ProcurementMarketingQueuePage() {
  const t = useTranslations("dcs.marketing");
  const locale = useLocale();
  const [items, setItems] = useState<ProcurementMarketingQueueItem[]>([]);
  const [summary, setSummary] = useState<QueueSummary>({ total: 0, pending: 0, inProgress: 0, completed: 0 });
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [phaseFilter, setPhaseFilter] = useState<SubPhaseFilter>("all");

  useEffect(() => {
    const timer = window.setTimeout(() => setDebouncedSearch(search.trim()), 300);
    return () => window.clearTimeout(timer);
  }, [search]);

  useEffect(() => {
    setPage(1);
  }, [phaseFilter, debouncedSearch]);

  useEffect(() => {
    api.get<QueueSummary>("/dcs/procurement-requests/marketing/queue/summary")
      .then((r) => setSummary(r.data))
      .catch(() => {});
  }, []);

  useEffect(() => {
    setLoading(true);
    setError("");
    const params = new URLSearchParams({
      page: String(page),
      pageSize: String(PAGE_SIZE),
    });
    if (phaseFilter !== "all") params.set("subPhase", phaseFilter);
    if (debouncedSearch) params.set("search", debouncedSearch);

    api
      .get<PagedResult<ProcurementMarketingQueueItem>>(
        `/dcs/procurement-requests/marketing/queue?${params}`,
      )
      .then((r) => {
        setItems(r.data.items);
        setTotalCount(r.data.totalCount);
      })
      .catch((err) => setError(getApiErrorMessage(err, t("loadError"))))
      .finally(() => setLoading(false));
  }, [page, phaseFilter, debouncedSearch, t]);

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  const counts = useMemo(
    () => ({
      all: summary.total,
      Pending: summary.pending,
      InProgress: summary.inProgress,
      Completed: summary.completed,
    }),
    [summary],
  );

  const filters: { id: SubPhaseFilter; label: string }[] = [
    { id: "all", label: t("filters.all") },
    { id: "Pending", label: t("filters.pending") },
    { id: "InProgress", label: t("filters.inProgress") },
    { id: "Completed", label: t("filters.completed") },
  ];

  return (
    <>
      <DcsPageHeader
        title={t("title")}
        subtitle={t("subtitle")}
        breadcrumb={t("title")}
        icon={Megaphone}
        iconClassName="bg-pink-500/10 text-pink-600 dark:text-pink-400"
      />

      <div className="flex-1 overflow-auto">
        <div className="px-6 py-6 space-y-5 max-w-[1440px]">
          {error && (
            <div className="rounded-xl border border-red-500/30 bg-red-500/5 px-4 py-3 text-sm text-red-700 dark:text-red-400">
              {error}
            </div>
          )}

          <div className={cn("p-4 flex flex-col sm:flex-row sm:items-center gap-3", dcsTheme.premiumCard)}>
            <div className="relative flex-1 max-w-md">
              <Search
                size={17}
                className="absolute left-3.5 top-1/2 -translate-y-1/2 text-foreground/30 pointer-events-none"
              />
              <input
                type="search"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder={t("search")}
                className="w-full h-11 pl-10 pr-4 rounded-xl border border-slate-200/80 dark:border-white/10 bg-white/60 dark:bg-white/[0.04] text-sm shadow-inner placeholder:text-foreground/35 focus:outline-none focus:ring-2 focus:ring-pink-500/30 focus:border-pink-500/40 transition-all"
              />
            </div>
            <div className="flex flex-wrap gap-1.5 p-1.5 rounded-xl bg-slate-100/80 dark:bg-white/[0.04] border border-slate-200/50 dark:border-white/[0.06]">
              {filters.map((f) => (
                <button
                  key={f.id}
                  type="button"
                  onClick={() => setPhaseFilter(f.id)}
                  className={cn(
                    "inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-[12px] font-medium transition-all",
                    phaseFilter === f.id
                      ? "bg-gradient-to-r from-pink-500/15 to-rose-500/10 text-pink-700 dark:text-pink-300 shadow-sm ring-1 ring-pink-500/20"
                      : "text-foreground/50 hover:text-foreground hover:bg-white/60 dark:hover:bg-white/[0.06]"
                  )}
                >
                  {f.label}
                  <span className="text-[10px] tabular-nums px-1.5 py-0.5 rounded-md bg-foreground/[0.06]">
                    {counts[f.id]}
                  </span>
                </button>
              ))}
            </div>
          </div>

          <div className={dcsTheme.tableShell}>
            <div className="overflow-x-auto">
              <table className="w-full text-sm min-w-[800px]">
                <thead>
                  <tr className="text-left text-[10px] text-foreground/45 uppercase tracking-[0.14em] bg-slate-50/90 dark:bg-white/[0.03] border-b border-slate-200/70 dark:border-white/[0.06]">
                    <th className="px-5 py-3.5 font-bold w-36">{t("columns.number")}</th>
                    <th className="px-4 py-3.5 font-bold">{t("columns.subject")}</th>
                    <th className="px-4 py-3.5 font-bold w-48">{t("columns.step")}</th>
                    <th className="px-4 py-3.5 font-bold w-36">{t("columns.subPhase")}</th>
                    <th className="px-4 py-3.5 font-bold w-40">{t("columns.assignee")}</th>
                    <th className="px-4 py-3.5 font-bold w-40">{t("columns.specialist")}</th>
                    <th className="px-4 py-3.5 font-bold w-28">{t("columns.updated")}</th>
                  </tr>
                </thead>
                <tbody>
                  {loading ? (
                    <tr>
                      <td colSpan={7} className="p-0">
                        <DcsListSkeleton />
                      </td>
                    </tr>
                  ) : items.length === 0 ? (
                    <tr>
                      <td colSpan={7} className="p-0">
                        <DcsEmptyState typeLabel={t("title")} />
                      </td>
                    </tr>
                  ) : (
                    items.map((item) => (
                      <tr
                        key={item.id}
                        className="border-b border-slate-100/80 dark:border-white/[0.04] last:border-0 hover:bg-pink-500/[0.04] transition-all group"
                      >
                        <td className="px-5 py-3.5">
                          <Link
                            href={`/${locale}/automation/documents/${item.id}`}
                            className="font-mono text-[13px] font-bold text-pink-600 dark:text-pink-400 hover:text-rose-700"
                          >
                            {item.number}
                          </Link>
                        </td>
                        <td className="px-4 py-3.5">
                          <Link
                            href={`/${locale}/automation/documents/${item.id}`}
                            className="font-medium text-foreground/90 hover:text-atg-blue line-clamp-1 max-w-lg"
                          >
                            {locale.startsWith("en") ? item.title : item.titleRu ?? item.title}
                          </Link>
                        </td>
                        <td className="px-4 py-3.5">
                          <span className="text-xs font-medium text-foreground/70">
                            {item.marketingCurrentStep}. {locale.startsWith("en") ? item.marketingStepTitleEn : item.marketingStepTitleRu}
                          </span>
                        </td>
                        <td className="px-4 py-3.5">
                          <SubPhaseBadge phase={item.marketingSubPhase} locale={locale} />
                        </td>
                        <td className="px-4 py-3.5 text-foreground/60">{item.assigneeName ?? "—"}</td>
                        <td className="px-4 py-3.5 text-foreground/60">{item.marketingSpecialistName ?? "—"}</td>
                        <td className="px-4 py-3.5 text-foreground/45 text-[13px] tabular-nums">
                          {new Date(item.updatedAt).toLocaleDateString(locale, {
                            day: "2-digit",
                            month: "short",
                            year: "numeric",
                          })}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>

          {totalPages > 1 && (
            <div className="flex items-center justify-between gap-3 pt-2">
              <p className="text-sm text-foreground/50">
                {totalCount} · {page} / {totalPages}
              </p>
              <div className="flex gap-2">
                <button
                  type="button"
                  disabled={page <= 1 || loading}
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  className="inline-flex items-center gap-1 px-3 py-1.5 rounded-lg border border-border/60 text-sm disabled:opacity-40"
                >
                  <ChevronLeft size={16} />
                </button>
                <button
                  type="button"
                  disabled={page >= totalPages || loading}
                  onClick={() => setPage((p) => p + 1)}
                  className="inline-flex items-center gap-1 px-3 py-1.5 rounded-lg border border-border/60 text-sm disabled:opacity-40"
                >
                  <ChevronRight size={16} />
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </>
  );
}

function SubPhaseBadge({ phase, locale }: { phase: string; locale: string }) {
  const colors: Record<string, string> = {
    Pending: "bg-amber-500/15 text-amber-700 dark:text-amber-300",
    InProgress: "bg-violet-500/15 text-violet-700 dark:text-violet-300",
    Completed: "bg-emerald-500/15 text-emerald-700 dark:text-emerald-300",
  };
  return (
    <span className={cn("text-[10px] font-bold uppercase tracking-wider px-2 py-0.5 rounded-full", colors[phase])}>
      {marketingSubPhaseLabel(phase as "Pending" | "InProgress" | "Completed", locale)}
    </span>
  );
}
