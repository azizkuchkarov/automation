"use client";

import { Check, ChevronRight } from "lucide-react";
import type { ProcurementRequest } from "@/lib/procurementRequest";
import { cn } from "@/lib/utils";

export type ProcurementPhaseKey =
  | "initiation"
  | "approval"
  | "marketing"
  | "contracts"
  | "payment"
  | "accountingSupply"
  | "done";

export type PhaseStepStatus = "completed" | "active" | "upcoming" | "locked";

export const PROCUREMENT_PHASE_ORDER: ProcurementPhaseKey[] = [
  "initiation",
  "approval",
  "marketing",
  "contracts",
  "payment",
  "accountingSupply",
  "done",
];

export function phaseKeyFromRequest(phase: ProcurementRequest["phase"]): ProcurementPhaseKey {
  switch (phase) {
    case "InProgress":
      return "initiation";
    case "AwaitingApproval":
      return "approval";
    case "Marketing":
      return "marketing";
    case "Contracts":
      return "contracts";
    case "Payment":
      return "payment";
    case "Completed":
      return "done";
    default:
      return "initiation";
  }
}

function statusFor(step: ProcurementPhaseKey, current: ProcurementPhaseKey): PhaseStepStatus {
  const stepIdx = PROCUREMENT_PHASE_ORDER.indexOf(step);
  const currentIdx = PROCUREMENT_PHASE_ORDER.indexOf(current);
  if (stepIdx < currentIdx) return "completed";
  if (stepIdx === currentIdx) return "active";
  if (stepIdx === currentIdx + 1) return "upcoming";
  return "locked";
}

/** Per-step progress 0–100. */
export function phaseProgressPercent(
  req: ProcurementRequest,
  step: ProcurementPhaseKey,
): number {
  const current = phaseKeyFromRequest(req.phase);
  const stepIdx = PROCUREMENT_PHASE_ORDER.indexOf(step);
  const currentIdx = PROCUREMENT_PHASE_ORDER.indexOf(current);

  if (stepIdx < currentIdx) return 100;
  if (stepIdx > currentIdx) return 0;

  switch (step) {
    case "initiation": {
      if (req.flow !== "TechnicalAffairs") return 100;
      const total = Math.max(req.steps?.length ?? 6, 1);
      return Math.min(100, Math.round((req.currentStep / total) * 100));
    }
    case "approval": {
      const total = Math.max(req.approvers?.length ?? 1, 1);
      const done = req.approvers.filter((a) => a.status === "Approved").length;
      return Math.min(100, Math.round((done / total) * 100));
    }
    case "marketing": {
      const total = Math.max(req.marketingSteps?.length ?? 8, 1);
      return Math.min(100, Math.round((req.marketingCurrentStep / total) * 100));
    }
    case "contracts": {
      if (req.phase === "Payment" || req.phase === "Completed") return 100;
      if (req.contractsSubPhase === "Completed") return 100;
      if (req.contractsSubPhase === "Pending") return 10;
      if (
        req.contractsSubPhase === "SectionPending" ||
        req.contractsSubPhase === "WaitingAccept"
      ) {
        return 25;
      }
      if (req.contractsIntVariant && req.contractsIntSteps?.length) {
        const total = req.contractsIntSteps.length;
        const stepNo = Math.max(req.contractsIntCurrentStep - 1, 0);
        return Math.min(95, Math.round(30 + (stepNo / total) * 65));
      }
      return 50;
    }
    case "payment": {
      if (req.paymentSubPhase === "Completed") return 100;
      if (req.paymentSubPhase === "InProgress") return 60;
      if (req.paymentSubPhase === "WaitingAccept") return 30;
      if (req.paymentSubPhase === "Pending") return 10;
      return 0;
    }
    case "accountingSupply":
      return 0;
    case "done":
      return 100;
    default:
      return 0;
  }
}

export function overallProgressPercent(req: ProcurementRequest): number {
  const current = phaseKeyFromRequest(req.phase);
  const currentIdx = PROCUREMENT_PHASE_ORDER.indexOf(current);
  const n = PROCUREMENT_PHASE_ORDER.length - 1;
  const base = (currentIdx / n) * 100;
  const activePct = phaseProgressPercent(req, current);
  const slice = 100 / n;
  return Math.min(100, Math.round(base + (activePct / 100) * slice * 0.9));
}

export type PhaseStepperLabels = Record<
  ProcurementPhaseKey,
  { label: string; hint: string }
> & {
  contractsLocal: { label: string; hint: string };
  contractsInternational: { label: string; hint: string };
};

function contractsDisplayLabel(req: ProcurementRequest, labels: PhaseStepperLabels): string {
  if (req.contractsProcurementSection === "Domestic") return labels.contractsLocal.label;
  if (req.contractsProcurementSection === "International")
    return labels.contractsInternational.label;
  return labels.contracts.label;
}

interface Props {
  req: ProcurementRequest;
  selected: ProcurementPhaseKey;
  onSelect: (phase: ProcurementPhaseKey) => void;
  labels: PhaseStepperLabels;
  isTas: boolean;
}

export function ProcurementPhaseStepper({
  req,
  selected,
  onSelect,
  labels,
}: Props) {
  const current = phaseKeyFromRequest(req.phase);

  return (
    <nav aria-label="Procurement process" className="w-full overflow-x-auto">
      <ol className="flex min-w-max items-center gap-0.5">
        {PROCUREMENT_PHASE_ORDER.map((id, index) => {
          const status = statusFor(id, current);
          const isSelected = selected === id;
          const clickable = status === "completed" || status === "active";
          const isLast = index === PROCUREMENT_PHASE_ORDER.length - 1;
          const pct = phaseProgressPercent(req, id);
          const displayLabel =
            id === "contracts" ? contractsDisplayLabel(req, labels) : labels[id].label;

          return (
            <li key={id} className="flex items-center">
              <button
                type="button"
                disabled={!clickable}
                onClick={() => clickable && onSelect(id)}
                title={`${displayLabel}: ${pct}%`}
                className={cn(
                  "inline-flex max-w-[9.5rem] flex-col items-start gap-0.5 rounded-lg px-2.5 py-1.5 text-left transition-all",
                  isSelected && status === "active" && "bg-sky-600 text-white shadow-sm",
                  isSelected && status === "completed" && "bg-emerald-600 text-white shadow-sm",
                  !isSelected &&
                    status === "completed" &&
                    "text-emerald-700 hover:bg-emerald-50 dark:text-emerald-400 dark:hover:bg-emerald-500/10",
                  !isSelected &&
                    status === "active" &&
                    "text-sky-700 hover:bg-sky-50 dark:text-sky-300 dark:hover:bg-sky-500/10",
                  !isSelected &&
                    (status === "upcoming" || status === "locked") &&
                    "text-slate-400",
                  !clickable && "cursor-default",
                )}
              >
                <span className="inline-flex max-w-full items-center gap-1.5 text-xs font-semibold">
                  <span
                    className={cn(
                      "flex h-4 w-4 shrink-0 items-center justify-center rounded-full text-[10px]",
                      isSelected && "bg-white/20",
                      !isSelected && status === "completed" && "bg-emerald-500/15 text-emerald-600",
                      !isSelected && status === "active" && "bg-sky-500/15 text-sky-600",
                      !isSelected &&
                        status !== "completed" &&
                        status !== "active" &&
                        "bg-slate-100 text-slate-400 dark:bg-white/10",
                    )}
                  >
                    {status === "completed" ? (
                      <Check size={10} strokeWidth={3} />
                    ) : (
                      index + 1
                    )}
                  </span>
                  <span className="truncate" title={displayLabel}>
                    {displayLabel}
                  </span>
                </span>
                <span
                  className={cn(
                    "pl-5 text-[10px] font-bold tabular-nums",
                    isSelected
                      ? "text-white/85"
                      : status === "completed"
                        ? "text-emerald-600/80"
                        : status === "active"
                          ? "text-sky-600"
                          : "text-slate-400",
                  )}
                >
                  {pct}%
                </span>
              </button>
              {!isLast && (
                <ChevronRight size={12} className="mx-0.5 shrink-0 text-slate-300" aria-hidden />
              )}
            </li>
          );
        })}
      </ol>
    </nav>
  );
}
