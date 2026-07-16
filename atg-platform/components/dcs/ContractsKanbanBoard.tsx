"use client";

import { useLocale, useTranslations } from "next-intl";
import Link from "next/link";
import {
  ArrowUpRight,
  CheckCircle2,
  ClipboardCheck,
  FileSignature,
  Inbox,
  MapPin,
  UserCheck,
  Users,
} from "lucide-react";
import {
  ContractsProcurementSectionType,
  ProcurementContractsBoardColumn,
  ProcurementContractsBoardItem,
  ProcurementContractsSubPhase,
  contractsDomVariantLabel,
  contractsIntVariantLabel,
  contractsSectionLabel,
  type ContractsDomProcurementVariant,
  type ContractsIntProcurementVariant,
} from "@/lib/procurementRequest";
import { dcsTheme } from "@/components/dcs/dcsTheme";
import { cn } from "@/lib/utils";
import type { LucideIcon } from "lucide-react";

const COLUMN_CONFIG: Record<
  ProcurementContractsSubPhase,
  { dot: string; ring: string; header: string; icon: LucideIcon }
> = {
  Pending: {
    dot: "bg-slate-500",
    ring: "ring-slate-400/20",
    header: "from-slate-500/12 via-slate-500/5 to-transparent",
    icon: Inbox,
  },
  SectionPending: {
    dot: "bg-violet-500",
    ring: "ring-violet-500/25",
    header: "from-violet-500/12 via-violet-500/5 to-transparent",
    icon: Users,
  },
  WaitingAccept: {
    dot: "bg-amber-500",
    ring: "ring-amber-500/25",
    header: "from-amber-500/12 via-amber-500/5 to-transparent",
    icon: UserCheck,
  },
  InProgress: {
    dot: "bg-sky-500",
    ring: "ring-sky-500/25",
    header: "from-sky-500/12 via-sky-500/5 to-transparent",
    icon: ClipboardCheck,
  },
  Completed: {
    dot: "bg-emerald-500",
    ring: "ring-emerald-500/25",
    header: "from-emerald-500/12 via-emerald-500/5 to-transparent",
    icon: CheckCircle2,
  },
};

export function ContractsKanbanBoard({
  columns,
  section,
}: {
  columns: ProcurementContractsBoardColumn[];
  section: ContractsProcurementSectionType;
}) {
  const t = useTranslations("dcs.contractsQueue.board");
  const locale = useLocale();
  const cardAccent =
    section === "Domestic"
      ? "hover:border-amber-500/35 hover:shadow-amber-500/5 group-hover:text-amber-600 dark:group-hover:text-amber-400"
      : "hover:border-sky-500/35 hover:shadow-sky-500/5 group-hover:text-sky-600 dark:group-hover:text-sky-400";
  const numberAccent =
    section === "Domestic" ? "text-amber-700 dark:text-amber-300" : "text-sky-700 dark:text-sky-300";

  return (
    <div className="relative -mx-1">
      <div className="pointer-events-none absolute inset-y-0 left-0 z-10 w-8 bg-gradient-to-r from-background to-transparent" />
      <div className="pointer-events-none absolute inset-y-0 right-0 z-10 w-8 bg-gradient-to-l from-background to-transparent" />
      <div className="flex gap-4 overflow-x-auto pb-6 px-1 min-h-[calc(100vh-14rem)] scroll-smooth">
        {columns.map((col) => {
          const theme = COLUMN_CONFIG[col.subPhase];
          const Icon = theme.icon;
          const label = locale.startsWith("en") ? col.labelEn : col.labelRu;
          const hint = t(`columnHints.${col.subPhase}`);

          return (
            <div
              key={col.subPhase}
              className={cn(
                "flex flex-col w-[min(100%,300px)] shrink-0 rounded-2xl overflow-hidden",
                dcsTheme.premiumCard,
                "ring-1 ring-inset",
                theme.ring,
              )}
            >
              <div
                className={cn(
                  "relative px-4 py-3.5 border-b border-border/40 bg-gradient-to-br",
                  theme.header,
                )}
              >
                <div className="flex items-start gap-3">
                  <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-surface/80 border border-border/50 shadow-sm">
                    <Icon size={16} className="text-foreground/70" />
                  </div>
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2">
                      <span className={cn("w-2 h-2 rounded-full shrink-0", theme.dot)} />
                      <span className="text-[13px] font-bold text-foreground/90 truncate">{label}</span>
                    </div>
                    <p className="text-[11px] text-foreground/45 mt-1 leading-snug line-clamp-2">{hint}</p>
                  </div>
                  <span className="text-[11px] font-bold tabular-nums min-w-[26px] h-[26px] flex items-center justify-center rounded-lg bg-surface text-foreground/55 border border-border/60 shadow-sm">
                    {col.items.length}
                  </span>
                </div>
              </div>

              <div className="flex-1 p-2.5 space-y-2.5 overflow-y-auto max-h-[calc(100vh-16rem)] bg-foreground/[0.015]">
                {col.items.length === 0 ? (
                  <div className="flex flex-col items-center justify-center gap-2.5 py-12 px-4 rounded-xl border border-dashed border-border/50 bg-surface/50">
                    <div className="w-10 h-10 rounded-full bg-border/25 flex items-center justify-center">
                      <FileSignature size={18} className="text-foreground/25" />
                    </div>
                    <p className="text-xs font-medium text-foreground/45 text-center">{t("empty")}</p>
                  </div>
                ) : (
                  col.items.map((item) => (
                    <KanbanCard
                      key={item.id}
                      item={item}
                      locale={locale}
                      section={section}
                      t={t}
                      cardAccent={cardAccent}
                      numberAccent={numberAccent}
                    />
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
  section,
  t,
  cardAccent,
  numberAccent,
}: {
  item: ProcurementContractsBoardItem;
  locale: string;
  section: ContractsProcurementSectionType;
  t: ReturnType<typeof useTranslations<"dcs.contractsQueue.board">>;
  cardAccent: string;
  numberAccent: string;
}) {
  const title = locale.startsWith("ru") && item.titleRu ? item.titleRu : item.title;
  const specialistInitial = item.contractsSpecialistName?.trim().charAt(0)?.toUpperCase() ?? "?";
  const variantLabel =
    section === "Domestic" && item.domVariant
      ? contractsDomVariantLabel(item.domVariant as ContractsDomProcurementVariant, locale)
      : section === "International" && item.intVariant
        ? contractsIntVariantLabel(item.intVariant as ContractsIntProcurementVariant, locale)
        : null;
  const currentStep = section === "Domestic" ? item.domCurrentStep : item.intCurrentStep;

  return (
    <Link
      href={`/${locale}/automation/documents/${item.id}`}
      title={t("openRequest")}
      className={cn(
        "group block rounded-xl border border-border/55 bg-surface p-3.5",
        "shadow-[0_1px_2px_rgba(15,23,42,0.04)] hover:shadow-md transition-all duration-200",
        cardAccent,
      )}
    >
      <div className="flex items-start justify-between gap-2">
        <p className={cn("font-mono text-[11px] font-bold truncate", numberAccent)}>{item.number}</p>
        <ArrowUpRight size={14} className="shrink-0 text-foreground/20 group-hover:opacity-100 transition-colors" />
      </div>

      <p className="text-sm font-semibold mt-1.5 line-clamp-2 leading-snug text-foreground/90">{title}</p>

      <div className="mt-2.5 flex flex-wrap gap-1.5">
        {!item.section && (
          <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-[10px] font-semibold bg-slate-500/10 text-slate-700 dark:text-slate-300 border border-slate-500/15">
            <MapPin size={10} />
            {t("unrouted")}
          </span>
        )}
        {item.section && item.section !== section && (
          <span className="px-2 py-0.5 rounded-md text-[10px] font-semibold bg-foreground/[0.06] text-foreground/60 border border-border/40">
            {contractsSectionLabel(item.section, locale)}
          </span>
        )}
        {variantLabel && (
          <span className="px-2 py-0.5 rounded-md text-[10px] font-semibold bg-indigo-500/10 text-indigo-700 dark:text-indigo-300 border border-indigo-500/15">
            {variantLabel}
          </span>
        )}
        {item.contractsSubPhase === "InProgress" && currentStep > 0 && (
          <span className="px-2 py-0.5 rounded-md text-[10px] font-semibold bg-sky-500/10 text-sky-700 dark:text-sky-300 border border-sky-500/15">
            {t("step", { step: currentStep })}
          </span>
        )}
      </div>

      {item.contractsSpecialistName && (
        <div className="mt-3 flex items-center gap-2 pt-2.5 border-t border-border/40">
          <span
            className={cn(
              "flex h-6 w-6 items-center justify-center rounded-full text-[10px] font-bold",
              section === "Domestic"
                ? "bg-amber-500/12 text-amber-700 dark:text-amber-300"
                : "bg-sky-500/12 text-sky-700 dark:text-sky-300",
            )}
          >
            {specialistInitial}
          </span>
          <div className="min-w-0">
            <p className="text-[10px] text-foreground/40 leading-none">{t("engineer")}</p>
            <p className="text-[11px] font-medium text-foreground/65 truncate">{item.contractsSpecialistName}</p>
          </div>
        </div>
      )}
    </Link>
  );
}

export function ContractsKanbanSkeleton() {
  return (
    <div className="animate-pulse">
      <div className="flex gap-4 overflow-hidden">
        {Array.from({ length: 5 }).map((_, i) => (
          <div
            key={i}
            className={cn("w-[300px] shrink-0 h-[420px]", dcsTheme.premiumCard, "bg-foreground/[0.03]")}
          />
        ))}
      </div>
    </div>
  );
}
