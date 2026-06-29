"use client";

import { useEffect, useMemo, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import Link from "next/link";
import {
  FilePlus,
  CreditCard,
  FileSignature,
  FileText,
  Inbox,
  Megaphone,
  MoreHorizontal,
  Plus,
  ScrollText,
  Search,
  Send,
  Truck,
  Users,
  Calculator,
  TrendingUp,
  type LucideIcon,
} from "lucide-react";
import api from "@/lib/api";
import {
  DocumentListItem,
  DocumentStatus,
  DOCUMENT_STATUSES,
  slugToType,
} from "@/lib/dcs";
import { phaseLabel, type ProcurementRequestPhase } from "@/lib/procurementRequest";
import { priorityDotClass } from "@/lib/procurementPriority";
import { DocumentStatusBadge } from "@/components/dcs/DocumentBadges";
import { DcsPageHeader } from "@/components/dcs/DcsPageHeader";
import { DcsEmptyState, DcsListSkeleton } from "@/components/dcs/DcsEmptyState";
import { dcsTheme } from "@/components/dcs/dcsTheme";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

const TYPE_ICONS: Record<string, LucideIcon> = {
  incoming: Inbox,
  outgoing: Send,
  memo: FileText,
  minutes: Users,
  orders: ScrollText,
  requests: FilePlus,
  marketing: Megaphone,
  contracts: FileSignature,
  payment: CreditCard,
  accounting: Calculator,
  "supply-section": Truck,
};

const TYPE_ICON_BG: Record<string, string> = {
  incoming: "bg-sky-500/10 text-sky-600 dark:text-sky-400",
  outgoing: "bg-violet-500/10 text-violet-600 dark:text-violet-400",
  memo: "bg-amber-500/10 text-amber-600 dark:text-amber-400",
  minutes: "bg-indigo-500/10 text-indigo-600 dark:text-indigo-400",
  orders: "bg-orange-500/10 text-orange-600 dark:text-orange-400",
  requests: "bg-sky-500/10 text-sky-600 dark:text-sky-400",
  marketing: "bg-pink-500/10 text-pink-600 dark:text-pink-400",
  contracts: "bg-emerald-500/10 text-emerald-600 dark:text-emerald-400",
  payment: "bg-amber-500/10 text-amber-600 dark:text-amber-400",
  accounting: "bg-slate-500/10 text-slate-600 dark:text-slate-400",
  "supply-section": "bg-purple-500/10 text-purple-600 dark:text-purple-400",
};

const PROCUREMENT_PHASES: ProcurementRequestPhase[] = [
  "InProgress",
  "AwaitingApproval",
  "Marketing",
  "Contracts",
  "Completed",
];

interface DocumentTypeListPageProps {
  typeSlug: string;
}

export function DocumentTypeListPage({ typeSlug }: DocumentTypeListPageProps) {
  const t = useTranslations("dcs");
  const locale = useLocale();
  const docType = slugToType(typeSlug);
  const isRequests = typeSlug === "requests";
  const [items, setItems] = useState<DocumentListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<DocumentStatus | "all">("all");
  const [phaseFilter, setPhaseFilter] = useState<ProcurementRequestPhase | "all">("all");
  const [canCreateRequest, setCanCreateRequest] = useState(false);

  useEffect(() => {
    if (!docType) return;
    setLoading(true);
    const view = isRequests ? "involved" : "registry";
    api
      .get(`/dcs/documents?type=${docType}&view=${view}&pageSize=200`)
      .then((r) => setItems(r.data.items))
      .finally(() => setLoading(false));
  }, [docType, isRequests]);

  useEffect(() => {
    if (!isRequests) return;
    api
      .get("/dcs/procurement-requests/create-options")
      .then((r) => setCanCreateRequest(Boolean(r.data.canCreateTas || r.data.canCreateExpress)))
      .catch(() => setCanCreateRequest(false));
  }, [isRequests]);

  const filtered = useMemo(() => {
    let list = items;
    if (isRequests) {
      if (phaseFilter !== "all") list = list.filter((d) => d.procurementPhase === phaseFilter);
    } else if (statusFilter !== "all") {
      list = list.filter((d) => d.status === statusFilter);
    }
    if (search.trim()) {
      const q = search.toLowerCase();
      list = list.filter(
        (d) =>
          d.number.toLowerCase().includes(q) ||
          d.title.toLowerCase().includes(q) ||
          d.authorName.toLowerCase().includes(q) ||
          (d.initiatorName?.toLowerCase().includes(q) ?? false)
      );
    }
    return list;
  }, [items, search, statusFilter, phaseFilter, isRequests]);

  const statusCounts = useMemo(() => {
    const counts: Record<string, number> = { all: items.length };
    for (const s of DOCUMENT_STATUSES) {
      counts[s] = items.filter((d) => d.status === s).length;
    }
    return counts;
  }, [items]);

  const phaseCounts = useMemo(() => {
    const counts: Record<string, number> = { all: items.length };
    for (const p of PROCUREMENT_PHASES) {
      counts[p] = items.filter((d) => d.procurementPhase === p).length;
    }
    return counts;
  }, [items]);

  if (!docType) return null;

  const title = t(`types.${typeSlug}`);
  const subtitle = t(`typesDesc.${typeSlug}`);
  const showNew = isRequests ? canCreateRequest : typeSlug !== "incoming";
  const newHref =
    typeSlug === "requests"
      ? `/${locale}/automation/procurement/requests/new`
      : `/${locale}/automation/documents/new?type=${typeSlug}`;
  const TypeIcon = TYPE_ICONS[typeSlug] ?? FileText;

  return (
    <>
      <DcsPageHeader
        title={title}
        subtitle={subtitle}
        breadcrumb={title}
        icon={TypeIcon}
        iconClassName={TYPE_ICON_BG[typeSlug]}
        actions={
          showNew ? (
            <Link href={newHref}>
              <Button size="sm" className={cn("h-10 px-5 font-semibold rounded-xl", dcsTheme.primaryBtn)}>
                <Plus size={15} className="mr-1.5" strokeWidth={2.5} />
                {t("nav.new")}
              </Button>
            </Link>
          ) : undefined
        }
      />

      <div className="flex-1 overflow-auto">
        <div className="px-6 py-6 space-y-5 max-w-[1440px]">
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
            <StatCard label={t("stats.total")} value={items.length} accent="from-blue-600 to-sky-500" icon={TrendingUp} />
            {isRequests ? (
              <>
                <StatCard
                  label={phaseLabel("InProgress", locale)}
                  value={phaseCounts.InProgress ?? 0}
                  accent="from-sky-500 to-cyan-500"
                />
                <StatCard
                  label={phaseLabel("AwaitingApproval", locale)}
                  value={phaseCounts.AwaitingApproval ?? 0}
                  accent="from-amber-500 to-orange-500"
                />
                <StatCard
                  label={phaseLabel("Marketing", locale)}
                  value={phaseCounts.Marketing ?? 0}
                  accent="from-pink-500 to-rose-500"
                />
              </>
            ) : (
              <>
                <StatCard label={t("status.InReview")} value={statusCounts.InReview ?? 0} accent="from-sky-500 to-cyan-500" />
                <StatCard label={t("status.Approved")} value={statusCounts.Approved ?? 0} accent="from-emerald-500 to-teal-500" />
                <StatCard label={t("status.Draft")} value={statusCounts.Draft ?? 0} accent="from-slate-500 to-slate-400" />
              </>
            )}
          </div>

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
                placeholder={t("list.search")}
                className="w-full h-11 pl-10 pr-4 rounded-xl border border-slate-200/80 dark:border-white/10 bg-white/60 dark:bg-white/[0.04] text-sm shadow-inner placeholder:text-foreground/35 focus:outline-none focus:ring-2 focus:ring-sky-500/30 focus:border-sky-500/40 transition-all"
              />
            </div>
            <div className="flex flex-wrap gap-1.5 p-1.5 rounded-xl bg-slate-100/80 dark:bg-white/[0.04] border border-slate-200/50 dark:border-white/[0.06]">
              <FilterChip
                active={(isRequests ? phaseFilter : statusFilter) === "all"}
                onClick={() => (isRequests ? setPhaseFilter("all") : setStatusFilter("all"))}
                label={t("filters.all")}
                count={isRequests ? phaseCounts.all : statusCounts.all}
              />
              {isRequests
                ? PROCUREMENT_PHASES.filter((p) => (phaseCounts[p] ?? 0) > 0 || phaseFilter === p).map((p) => (
                    <FilterChip
                      key={p}
                      active={phaseFilter === p}
                      onClick={() => setPhaseFilter(p)}
                      label={phaseLabel(p, locale)}
                      count={phaseCounts[p] ?? 0}
                    />
                  ))
                : DOCUMENT_STATUSES.filter((s) => (statusCounts[s] ?? 0) > 0 || statusFilter === s).map(
                    (s) => (
                      <FilterChip
                        key={s}
                        active={statusFilter === s}
                        onClick={() => setStatusFilter(s)}
                        label={t(`status.${s}`)}
                        count={statusCounts[s] ?? 0}
                      />
                    )
                  )}
            </div>
          </div>

          <div className={dcsTheme.tableShell}>
            <div className="overflow-x-auto">
              <table className="w-full text-sm min-w-[720px]">
                <thead>
                  <tr className="text-left text-[10px] text-foreground/45 uppercase tracking-[0.14em] bg-slate-50/90 dark:bg-white/[0.03] border-b border-slate-200/70 dark:border-white/[0.06] backdrop-blur-sm">
                    <th className="px-5 py-3.5 font-bold w-10" />
                    <th className="px-4 py-3.5 font-bold w-36">{t("fields.regNum")}</th>
                    <th className="px-4 py-3.5 font-bold">{t("fields.title")}</th>
                    <th className="px-4 py-3.5 font-bold w-40">
                      {isRequests ? t("request.phase") : t("fields.status")}
                    </th>
                    <th className="px-4 py-3.5 font-bold w-44">
                      {isRequests ? t("request.initiator") : t("fields.author")}
                    </th>
                    <th className="px-4 py-3.5 font-bold w-28">{t("fields.updated")}</th>
                  </tr>
                </thead>
                <tbody>
                  {loading ? (
                    <tr>
                      <td colSpan={6} className="p-0">
                        <DcsListSkeleton />
                      </td>
                    </tr>
                  ) : filtered.length === 0 ? (
                    <tr>
                      <td colSpan={6} className="p-0">
                        {items.length === 0 ? (
                          <DcsEmptyState newHref={showNew ? newHref : undefined} typeLabel={title} />
                        ) : (
                          <div className="py-16 text-center text-foreground/40 text-sm">
                            {t("list.noResults")}
                          </div>
                        )}
                      </td>
                    </tr>
                  ) : (
                    filtered.map((doc) => (
                      <tr
                        key={doc.id}
                        className="border-b border-slate-100/80 dark:border-white/[0.04] last:border-0 hover:bg-sky-500/[0.04] dark:hover:bg-sky-400/[0.06] transition-all duration-150 group"
                      >
                        <td className="px-5 py-3.5">
                          <button
                            type="button"
                            className="p-1.5 rounded-lg hover:bg-foreground/5 text-foreground/25 hover:text-foreground/50 opacity-0 group-hover:opacity-100 transition-all"
                            aria-label="Actions"
                          >
                            <MoreHorizontal size={16} />
                          </button>
                        </td>
                        <td className="px-4 py-3.5">
                          <Link
                            href={`/${locale}/automation/documents/${doc.id}`}
                            className="inline-flex items-center gap-2 font-mono text-[13px] font-bold text-sky-600 dark:text-sky-400 hover:text-blue-700 dark:hover:text-sky-300 transition-colors"
                          >
                            <span
                              className={cn(
                                "w-2.5 h-2.5 rounded-full shadow-sm shrink-0",
                                priorityDotClass(doc.priority ?? "Medium")
                              )}
                            />
                            {doc.number}
                          </Link>
                        </td>
                        <td className="px-4 py-3.5">
                          <Link
                            href={`/${locale}/automation/documents/${doc.id}`}
                            className="font-medium text-foreground/90 hover:text-atg-blue transition-colors line-clamp-1 max-w-lg"
                          >
                            {doc.title}
                          </Link>
                        </td>
                        <td className="px-4 py-3.5">
                          {isRequests && doc.procurementPhase ? (
                            <ProcurementPhaseBadge
                              phase={doc.procurementPhase}
                              step={doc.procurementCurrentStep}
                              locale={locale}
                            />
                          ) : (
                            <DocumentStatusBadge status={doc.status} />
                          )}
                        </td>
                        <td className="px-4 py-3.5">
                          <div className="flex items-center gap-2">
                            <div className="w-8 h-8 rounded-xl bg-gradient-to-br from-sky-500/20 to-blue-600/15 flex items-center justify-center text-[10px] font-bold text-sky-600 dark:text-sky-400 shrink-0 ring-1 ring-sky-500/10">
                              {(isRequests ? doc.initiatorName ?? doc.authorName : doc.authorName).charAt(0)}
                            </div>
                            <div className="min-w-0">
                              <span className="text-foreground/60 text-[13px] truncate block">
                                {isRequests ? doc.initiatorName ?? doc.authorName : doc.authorName}
                              </span>
                              {isRequests && doc.initiatorName && (
                                <span className="text-[11px] text-foreground/35 truncate block">
                                  {t("request.createdBy", { name: doc.authorName })}
                                </span>
                              )}
                            </div>
                          </div>
                        </td>
                        <td className="px-4 py-3.5 text-foreground/45 text-[13px] tabular-nums">
                          {new Date(doc.updatedAt).toLocaleDateString(locale, {
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

            {!loading && filtered.length > 0 && (
              <div className="px-5 py-3.5 border-t border-slate-200/60 dark:border-white/[0.06] bg-slate-50/50 dark:bg-white/[0.02] text-xs text-foreground/45 font-medium">
                {t("list.showing", { count: filtered.length, total: items.length })}
              </div>
            )}
          </div>
        </div>
      </div>
    </>
  );
}

function ProcurementPhaseBadge({
  phase,
  step,
  locale,
}: {
  phase: ProcurementRequestPhase;
  step?: number;
  locale: string;
}) {
  const label =
    step && (phase === "InProgress" || phase === "Marketing")
      ? `${phaseLabel(phase, locale)} · ${locale.startsWith("en") ? "Step" : "Шаг"} ${step}`
      : phaseLabel(phase, locale);

  const colors: Record<ProcurementRequestPhase, string> = {
    InProgress: "bg-sky-500/12 text-sky-700 dark:text-sky-300 border-sky-500/25",
    AwaitingApproval: "bg-amber-500/12 text-amber-700 dark:text-amber-300 border-amber-500/25",
    Marketing: "bg-pink-500/12 text-pink-700 dark:text-pink-300 border-pink-500/25",
    Contracts: "bg-emerald-500/12 text-emerald-700 dark:text-emerald-300 border-emerald-500/25",
    Completed: "bg-slate-500/12 text-slate-600 dark:text-slate-300 border-slate-500/25",
  };

  return (
    <span
      className={cn(
        "inline-flex items-center px-2.5 py-1 rounded-lg text-[11px] font-semibold border",
        colors[phase]
      )}
    >
      {label}
    </span>
  );
}

function StatCard({
  label,
  value,
  accent,
  icon: Icon,
}: {
  label: string;
  value: number;
  accent: string;
  icon?: LucideIcon;
}) {
  return (
    <div className={cn("relative overflow-hidden p-5 group", dcsTheme.premiumCard)}>
      <div className={cn("absolute -right-4 -top-4 w-24 h-24 rounded-full bg-gradient-to-br opacity-[0.12] blur-2xl transition-opacity group-hover:opacity-20", accent)} />
      <div className="relative flex items-start justify-between gap-2">
        <div>
          <p className="text-[10px] font-bold uppercase tracking-[0.14em] text-foreground/40">{label}</p>
          <p className="text-3xl font-bold tabular-nums mt-2 tracking-tight text-foreground">{value}</p>
        </div>
        {Icon && (
          <div className={cn("w-10 h-10 rounded-xl flex items-center justify-center bg-gradient-to-br text-white shadow-lg", accent)}>
            <Icon size={18} strokeWidth={2} />
          </div>
        )}
      </div>
    </div>
  );
}

function FilterChip({
  active,
  onClick,
  label,
  count,
}: {
  active: boolean;
  onClick: () => void;
  label: string;
  count: number;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        "inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-[12px] font-medium transition-all",
        active
          ? "bg-gradient-to-r from-sky-500/15 to-blue-500/10 text-sky-700 dark:text-sky-300 shadow-sm ring-1 ring-sky-500/20"
          : "text-foreground/50 hover:text-foreground hover:bg-white/60 dark:hover:bg-white/[0.06]"
      )}
    >
      {label}
      <span
        className={cn(
          "text-[10px] tabular-nums px-1.5 py-0.5 rounded-md",
          active ? "bg-sky-500/20 text-sky-700 dark:text-sky-300" : "bg-foreground/[0.06]"
        )}
      >
        {count}
      </span>
    </button>
  );
}
