"use client";

import { useState } from "react";
import { Check, ChevronDown, Clock } from "lucide-react";
import type { MarketingPlanRegistrationMethod } from "@/lib/marketing";
import { planRegistrationMethodLabel } from "@/lib/marketing";
import {
  ProcurementRequest,
  contractsSubPhaseLabel,
  marketingSubPhaseLabel,
} from "@/lib/procurementRequest";
import { deptLabel } from "@/lib/dcs";
import { ProcessDocumentsPanel } from "@/components/dcs/ProcessDocumentsPanel";
import { cn } from "@/lib/utils";

interface Props {
  req: ProcurementRequest;
  locale: string;
  isTas: boolean;
  t: (key: string) => string;
}

interface RegItem {
  id: string;
  title: string;
  number?: string | null;
  at?: string | null;
  pending: string;
  done: boolean;
  extra?: string;
}

type SidebarSection = "details" | "registration" | "attachments";

export function ProcurementRequestSidebar({ req, locale, isTas, t }: Props) {
  const [open, setOpen] = useState<Record<SidebarSection, boolean>>({
    details: false,
    registration: false,
    attachments: false,
  });

  const toggle = (key: SidebarSection) =>
    setOpen((prev) => ({ ...prev, [key]: !prev[key] }));

  const planMethod = req.marketingProcurementPlanRegistrationMethod as
    | MarketingPlanRegistrationMethod
    | undefined;

  const registrations: RegItem[] = [
    {
      id: "req",
      title: t("registrationTitle"),
      number: req.isRegistered ? req.number : null,
      at: req.registeredAt,
      pending: t("pendingReg"),
      done: req.isRegistered,
    },
    {
      id: "rfq",
      title: t("marketingRfqRegistrationTitle"),
      number: req.marketingRfqRegistrationNumber,
      at: req.marketingRfqRegisteredAt,
      pending: t("marketingRfqPendingNumber"),
      done: !!req.marketingRfqRegistrationNumber,
    },
    {
      id: "plan",
      title: t("marketingPlanRegistrationTitle"),
      number: req.marketingProcurementPlanRegistrationNumber,
      at: req.marketingProcurementPlanRegisteredAt,
      pending: t("marketingPlanPendingNumber"),
      done: !!req.marketingProcurementPlanRegistrationNumber,
      extra:
        planMethod && req.marketingProcurementPlanRegistrationNumber
          ? planRegistrationMethodLabel(planMethod, locale)
          : undefined,
    },
    {
      id: "mkt-close",
      title: t("step9.registrationTitle"),
      number: req.marketingPlanRegistrationNumber,
      at: req.marketingPlanRegisteredAt,
      pending: t("step9.pendingNumber"),
      done: !!req.marketingPlanRegisteredAt,
    },
  ];

  const docsCount =
    req.processDocuments?.length ?? req.attachments?.length ?? 0;
  const regDoneCount = registrations.filter((r) => r.done).length;

  return (
    <aside className="space-y-3">
      <CollapsibleCard
        title={t("meta")}
        open={open.details}
        onToggle={() => toggle("details")}
        badge={undefined}
      >
        <div className="space-y-3">
          {isTas && (
            <>
              <MetaRow label={t("eamNumber")} value={req.eamNumber ?? "—"} />
              <MetaRow label={t("initiator")} value={req.initiatorName ?? "—"} />
              {req.dueDate && (
                <MetaRow
                  label={t("deadline")}
                  value={new Date(req.dueDate).toLocaleDateString(locale)}
                />
              )}
              <MetaRow
                label={t("responsible")}
                value={req.tasResponsibleName ?? req.assigneeName ?? "—"}
              />
            </>
          )}
          <MetaRow
            label={t("department")}
            value={deptLabel(req.departmentName, req.departmentNameEn, locale)}
          />
          {req.assigneeName && (
            <MetaRow label={t("assignee")} value={req.assigneeName} />
          )}
          {req.marketingSpecialistName && (
            <MetaRow label={t("marketingSpecialist")} value={req.marketingSpecialistName} />
          )}
          {req.contractsSpecialistName && (
            <MetaRow label={t("contractsEngineer")} value={req.contractsSpecialistName} />
          )}
          {req.phase === "Marketing" && (
            <>
              <MetaRow
                label={t("marketingStep")}
                value={`${req.marketingCurrentStep} / ${req.marketingSteps.length}`}
              />
              <MetaRow
                label={t("marketingSubPhase")}
                value={marketingSubPhaseLabel(req.marketingSubPhase, locale)}
              />
            </>
          )}
          {req.phase === "Contracts" && (
            <MetaRow
              label={t("contractsSubPhase")}
              value={contractsSubPhaseLabel(req.contractsSubPhase, locale)}
            />
          )}
          {req.marketingTaskNumber && (
            <MetaRow label={t("marketingTask")} value={req.marketingTaskNumber} mono />
          )}
          {req.contractsTaskNumber && (
            <MetaRow label={t("contractsTask")} value={req.contractsTaskNumber} mono />
          )}
        </div>
      </CollapsibleCard>

      <CollapsibleCard
        title={t("registrationTimeline")}
        open={open.registration}
        onToggle={() => toggle("registration")}
        badge={`${regDoneCount}/${registrations.length}`}
      >
        <ol className="relative space-y-0">
          {registrations.map((item, index) => (
            <li key={item.id} className="relative flex gap-3 pb-4 last:pb-0">
              {index < registrations.length - 1 && (
                <span
                  className={cn(
                    "absolute left-[11px] top-6 bottom-0 w-px",
                    item.done ? "bg-emerald-300" : "bg-foreground/10",
                  )}
                  aria-hidden
                />
              )}
              <span
                className={cn(
                  "relative z-[1] mt-0.5 flex h-6 w-6 shrink-0 items-center justify-center rounded-full",
                  item.done
                    ? "bg-emerald-500 text-white"
                    : "bg-foreground/[0.06] text-foreground/35",
                )}
              >
                {item.done ? <Check size={12} strokeWidth={3} /> : <Clock size={12} />}
              </span>
              <div className="min-w-0 pt-0.5">
                <p className="text-[11px] font-semibold text-foreground/70">{item.title}</p>
                <p
                  className={cn(
                    "mt-0.5 font-mono text-sm font-bold",
                    item.done ? "text-foreground" : "text-foreground/35",
                  )}
                >
                  {item.done ? item.number : item.pending}
                </p>
                {item.extra && (
                  <p className="mt-0.5 text-[11px] text-foreground/45">{item.extra}</p>
                )}
                {item.at && (
                  <p className="mt-0.5 text-[11px] text-foreground/40">
                    {new Date(item.at).toLocaleString(locale)}
                  </p>
                )}
              </div>
            </li>
          ))}
        </ol>
      </CollapsibleCard>

      <CollapsibleCard
        title={t("attachments")}
        open={open.attachments}
        onToggle={() => toggle("attachments")}
        badge={docsCount > 0 ? String(docsCount) : undefined}
      >
        <ProcessDocumentsPanel req={req} locale={locale} t={t} embedded />
      </CollapsibleCard>
    </aside>
  );
}

function CollapsibleCard({
  title,
  open,
  onToggle,
  badge,
  children,
}: {
  title: string;
  open: boolean;
  onToggle: () => void;
  badge?: string;
  children: React.ReactNode;
}) {
  return (
    <section className="overflow-hidden rounded-xl border border-slate-200/80 bg-white/85 shadow-sm dark:border-white/10 dark:bg-white/[0.03]">
      <button
        type="button"
        onClick={onToggle}
        aria-expanded={open}
        className={cn(
          "flex w-full items-center gap-2 px-4 py-3 text-left transition-colors",
          "hover:bg-foreground/[0.02] focus:outline-none focus-visible:ring-2 focus-visible:ring-sky-500/30",
          open && "border-b border-slate-100 dark:border-white/[0.06]",
        )}
      >
        <h3 className="flex-1 text-[10px] font-bold uppercase tracking-[0.14em] text-foreground/40">
          {title}
        </h3>
        {badge && !open && (
          <span className="rounded-full bg-foreground/[0.06] px-2 py-0.5 text-[10px] font-semibold tabular-nums text-foreground/50">
            {badge}
          </span>
        )}
        <ChevronDown
          size={16}
          className={cn(
            "shrink-0 text-foreground/35 transition-transform duration-200",
            open && "rotate-180",
          )}
        />
      </button>
      {open && <div className="px-4 py-3">{children}</div>}
    </section>
  );
}

function MetaRow({
  label,
  value,
  mono,
}: {
  label: string;
  value: string;
  mono?: boolean;
}) {
  return (
    <div>
      <p className="text-[10px] text-foreground/40">{label}</p>
      <p className={cn("text-sm font-medium text-foreground/90", mono && "font-mono")}>{value}</p>
    </div>
  );
}
