"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import Link from "next/link";
import { ChevronLeft, ChevronRight, FileSignature, Search } from "lucide-react";
import api, { getApiErrorMessage } from "@/lib/api";
import type { PagedResult } from "@/lib/paged";
import {
  ContractsProcurementSectionType,
  ProcurementContractsQueueItem,
  contractsSectionLabel,
  contractsSubPhaseLabel,
} from "@/lib/procurementRequest";
import { DcsPageHeader } from "@/components/dcs/DcsPageHeader";
import { DcsEmptyState, DcsListSkeleton } from "@/components/dcs/DcsEmptyState";
import { dcsTheme } from "@/components/dcs/dcsTheme";
import { cn } from "@/lib/utils";

const PAGE_SIZE = 50;

interface Props {
  section: ContractsProcurementSectionType;
}

export function ProcurementContractsQueuePage({ section }: Props) {
  const t = useTranslations("dcs.contractsQueue");
  const locale = useLocale();
  const [items, setItems] = useState<ProcurementContractsQueueItem[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");

  const title = section === "Domestic" ? t("localTitle") : t("internationalTitle");
  const subtitle = section === "Domestic" ? t("localSubtitle") : t("internationalSubtitle");

  useEffect(() => {
    const timer = window.setTimeout(() => setDebouncedSearch(search.trim()), 300);
    return () => window.clearTimeout(timer);
  }, [search]);

  useEffect(() => {
    setPage(1);
  }, [section, debouncedSearch]);

  useEffect(() => {
    setLoading(true);
    setError("");
    const params = new URLSearchParams({
      page: String(page),
      pageSize: String(PAGE_SIZE),
      section,
    });
    if (debouncedSearch) params.set("search", debouncedSearch);

    api
      .get<PagedResult<ProcurementContractsQueueItem>>(
        `/dcs/procurement-requests/contracts/queue?${params}`,
      )
      .then((r) => {
        setItems(r.data.items);
        setTotalCount(r.data.totalCount);
      })
      .catch((err) => setError(getApiErrorMessage(err, t("loadError"))))
      .finally(() => setLoading(false));
  }, [page, section, debouncedSearch, t]);

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  return (
    <>
      <DcsPageHeader
        title={title}
        subtitle={subtitle}
        breadcrumb={title}
        icon={FileSignature}
        iconClassName="bg-indigo-500/10 text-indigo-600 dark:text-indigo-400"
      />

      <div className="flex-1 overflow-auto">
        <div className="px-6 py-6 space-y-5 max-w-[1440px]">
          {error && (
            <div className="rounded-xl border border-red-500/30 bg-red-500/5 px-4 py-3 text-sm text-red-700 dark:text-red-400">
              {error}
            </div>
          )}

          <div className={cn("p-4", dcsTheme.premiumCard)}>
            <div className="relative max-w-md">
              <Search
                size={17}
                className="absolute left-3.5 top-1/2 -translate-y-1/2 text-foreground/30 pointer-events-none"
              />
              <input
                type="search"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder={t("search")}
                className="w-full h-11 pl-10 pr-4 rounded-xl border border-slate-200/80 dark:border-white/10 bg-white/60 dark:bg-white/[0.04] text-sm shadow-inner placeholder:text-foreground/35 focus:outline-none focus:ring-2 focus:ring-indigo-500/30 focus:border-indigo-500/40 transition-all"
              />
            </div>
          </div>

          <div className={dcsTheme.tableShell}>
            <div className="overflow-x-auto">
              <table className="w-full text-sm min-w-[800px]">
                <thead>
                  <tr className="text-left text-[10px] text-foreground/45 uppercase tracking-[0.14em] bg-slate-50/90 dark:bg-white/[0.03] border-b border-slate-200/70 dark:border-white/[0.06]">
                    <th className="px-5 py-3.5 font-bold w-36">{t("columns.number")}</th>
                    <th className="px-4 py-3.5 font-bold">{t("columns.subject")}</th>
                    <th className="px-4 py-3.5 font-bold w-40">{t("columns.section")}</th>
                    <th className="px-4 py-3.5 font-bold w-36">{t("columns.subPhase")}</th>
                    <th className="px-4 py-3.5 font-bold w-40">{t("columns.assignee")}</th>
                    <th className="px-4 py-3.5 font-bold w-40">{t("columns.engineer")}</th>
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
                        <DcsEmptyState typeLabel={title} />
                      </td>
                    </tr>
                  ) : (
                    items.map((item) => (
                      <tr
                        key={item.id}
                        className="border-b border-slate-100/80 dark:border-white/[0.04] last:border-0 hover:bg-indigo-500/[0.04] transition-all"
                      >
                        <td className="px-5 py-3.5">
                          <Link
                            href={`/${locale}/automation/documents/${item.id}`}
                            className="font-mono text-[13px] font-bold text-indigo-600 dark:text-indigo-400 hover:underline"
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
                        <td className="px-4 py-3.5 text-foreground/60 text-xs">
                          {item.section
                            ? contractsSectionLabel(item.section, locale)
                            : t("unrouted")}
                        </td>
                        <td className="px-4 py-3.5">
                          <span className="text-[10px] font-bold uppercase tracking-wider px-2 py-0.5 rounded-full bg-indigo-500/10 text-indigo-700 dark:text-indigo-300">
                            {contractsSubPhaseLabel(item.contractsSubPhase, locale)}
                          </span>
                        </td>
                        <td className="px-4 py-3.5 text-foreground/60">{item.assigneeName ?? "—"}</td>
                        <td className="px-4 py-3.5 text-foreground/60">
                          {item.contractsSpecialistName ?? "—"}
                        </td>
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
