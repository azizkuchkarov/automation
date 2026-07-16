"use client";

import {
  Building2,
  FileText,
  Flag,
  User,
} from "lucide-react";
import { ProcurementRequest } from "@/lib/procurementRequest";
import { deptLabel } from "@/lib/dcs";
import {
  priorityDotClass,
  priorityLabel,
  type ProcurementPriority,
} from "@/lib/procurementPriority";
import { cn } from "@/lib/utils";

interface Props {
  req: ProcurementRequest;
  locale: string;
  t: (key: string) => string;
  highlightAction?: boolean;
}

export function RequestSummaryCard({ req, locale, t, highlightAction }: Props) {
  const title = locale.startsWith("en") ? req.title : req.titleRu ?? req.title;
  const titleAlt =
    locale.startsWith("en") && req.titleRu
      ? req.titleRu
      : !locale.startsWith("en") && req.title !== req.titleRu
        ? req.title
        : null;

  const priority = (req.priority ?? "Medium") as ProcurementPriority;
  const department = deptLabel(
    req.initiatorDepartmentName ?? req.departmentName,
    req.initiatorDepartmentNameEn ?? req.departmentNameEn,
    locale,
  );

  return (
    <section
      className={cn(
        "rounded-xl border bg-white shadow-sm dark:bg-slate-900/40",
        highlightAction
          ? "border-amber-300 ring-1 ring-amber-200/80 dark:border-amber-500/40 dark:ring-amber-500/20"
          : "border-slate-200 dark:border-white/10",
      )}
    >
      {highlightAction && (
        <div className="border-b border-amber-200/80 bg-amber-50 px-4 py-2 text-xs font-semibold text-amber-900 dark:border-amber-500/20 dark:bg-amber-500/10 dark:text-amber-100">
          {t("approvalYourTurnBanner")}
        </div>
      )}

      <div className="space-y-4 p-4">
        <div>
          <p className="text-[10px] font-bold uppercase tracking-[0.14em] text-slate-400">
            {t("requestSummaryTitle")}
          </p>
          <h2 className="mt-1 text-base font-semibold leading-snug text-slate-900 dark:text-slate-50">
            {title}
          </h2>
          {titleAlt && (
            <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">{titleAlt}</p>
          )}
        </div>

        <div className="grid gap-2 sm:grid-cols-2">
          <MetaRow icon={User} label={t("initiator")} value={req.initiatorName ?? "—"} />
          <MetaRow icon={Building2} label={t("department")} value={department} />
          <MetaRow
            icon={Flag}
            label={t("priority")}
            value={
              <span className="inline-flex items-center gap-1.5">
                <span className={cn("h-2 w-2 rounded-full", priorityDotClass(priority))} />
                {priorityLabel(priority, locale)}
              </span>
            }
          />
          <MetaRow
            icon={FileText}
            label={t("flow")}
            value={req.flow === "TechnicalAffairs" ? t("flowTas") : t("flowExpress")}
          />
        </div>

      </div>
    </section>
  );
}

function MetaRow({
  icon: Icon,
  label,
  value,
}: {
  icon: typeof User;
  label: string;
  value: React.ReactNode;
}) {
  return (
    <div className="rounded-lg border border-slate-100 bg-slate-50/80 px-3 py-2 dark:border-white/[0.06] dark:bg-white/[0.02]">
      <p className="mb-0.5 flex items-center gap-1 text-[10px] font-bold uppercase tracking-wider text-slate-400">
        <Icon size={10} /> {label}
      </p>
      <div className="text-sm font-semibold text-slate-800 dark:text-slate-100">{value}</div>
    </div>
  );
}
