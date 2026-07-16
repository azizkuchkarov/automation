"use client";

import { Briefcase, Check, CheckCircle2, Clock, User, XCircle } from "lucide-react";
import {
  DcsDocumentHero,
  DcsMetaPill,
  DcsWorkflowCard,
  DcsWorkflowShell,
  DcsWorkflowStepper,
  type DcsStepItem,
} from "@/components/dcs/DcsWorkflowUI";
import {
  approverRoleLabel,
  approverStatusLabel,
  HrBusinessTripApprovalRole,
  HrBusinessTripRequest,
  workflowActiveIndex,
  workflowCurrentHint,
  workflowStepItems,
  workflowStepStatus,
  type HrBusinessTripWorkflowStepKey,
} from "@/lib/hrBusinessTrip";
import { cn } from "@/lib/utils";

export function HrBusinessTripHero({
  request,
  locale,
  backLabel,
  printLabel,
  onBack,
  onDownload,
}: {
  request: HrBusinessTripRequest;
  locale: string;
  backLabel: string;
  printLabel: string;
  onBack: () => void;
  onDownload: () => void;
}) {
  const place = locale.startsWith("en") && request.placeEn ? request.placeEn : request.placeRu;
  const hint = workflowCurrentHint(request, locale);

  return (
    <DcsDocumentHero
      kind="memo"
      number={request.number}
      title={place}
      titleRu={request.placeRu !== place ? request.placeRu : undefined}
      phaseLabel={hint}
      backLabel={backLabel}
      printLabel={printLabel}
      onBack={onBack}
      onPrint={onDownload}
      icon={Briefcase}
      meta={
        <>
          <DcsMetaPill>{request.authorName}</DcsMetaPill>
          <DcsMetaPill>
            {locale.startsWith("en") && request.departmentNameEn
              ? request.departmentNameEn
              : request.departmentName}
          </DcsMetaPill>
          <DcsMetaPill>
            {request.daysCount} {locale.startsWith("en") ? "day(s)" : "дн."}
          </DcsMetaPill>
        </>
      }
    />
  );
}

export function HrBusinessTripWorkflowStepper({
  request,
  locale,
  title,
}: {
  request: HrBusinessTripRequest;
  locale: string;
  title: string;
}) {
  const steps: DcsStepItem[] = workflowStepItems(locale);
  const activeIndex = workflowActiveIndex(request);

  return (
    <DcsWorkflowStepper
      kind="memo"
      title={title}
      steps={steps}
      activeIndex={activeIndex}
      hint={workflowCurrentHint(request, locale)}
    />
  );
}

const STEP_ROLE: Partial<Record<HrBusinessTripWorkflowStepKey, HrBusinessTripApprovalRole>> = {
  departmentHead: "DepartmentHead",
  hrManager: "HrManager",
  firstDeputyGd: "FirstDeputyGeneralDirector",
};

export function HrBusinessTripApprovalChain({
  request,
  locale,
  title,
}: {
  request: HrBusinessTripRequest;
  locale: string;
  title: string;
}) {
  const steps = workflowStepItems(locale).filter(
    (s) =>
      s.key !== "draft"
      && s.key !== "order"
      && s.key !== "gdOrderEimzo"
      && s.key !== "certificate"
      && s.key !== "completed",
  );

  return (
    <DcsWorkflowCard kind="memo" title={title}>
      <div className="space-y-3">
        {steps.map((step, index) => {
          const role = STEP_ROLE[step.key as HrBusinessTripWorkflowStepKey];
          const approver = role
            ? request.approvers.find((a) => a.role === role)
            : request.approvers.find((a) => a.role === "DeputyDepartmentHead");
          const status = workflowStepStatus(request, step.key as HrBusinessTripWorkflowStepKey);
          const isActive = status === "active";
          const isDone = status === "completed";
          const isRejected = status === "rejected";

          return (
            <div
              key={step.key}
              className={cn(
                "flex items-start gap-3 rounded-xl border px-4 py-3 transition-colors",
                isActive && "border-blue-300 bg-blue-50/60 dark:bg-blue-950/20",
                isDone && "border-emerald-200 bg-emerald-50/40 dark:bg-emerald-950/15",
                isRejected && "border-red-200 bg-red-50/40",
                !isActive && !isDone && !isRejected && "border-border/60 bg-foreground/[0.02]"
              )}
            >
              <div
                className={cn(
                  "mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-xs font-bold",
                  isDone && "bg-emerald-600 text-white",
                  isActive && "bg-blue-700 text-white ring-4 ring-blue-500/20",
                  isRejected && "bg-red-600 text-white",
                  !isDone && !isActive && !isRejected && "bg-foreground/10 text-foreground/40"
                )}
              >
                {isDone ? (
                  <Check size={14} strokeWidth={2.5} />
                ) : isRejected ? (
                  <XCircle size={14} />
                ) : isActive ? (
                  <Clock size={14} />
                ) : (
                  index + 1
                )}
              </div>

              <div className="min-w-0 flex-1">
                <p className="text-sm font-semibold text-foreground">{step.label}</p>
                {approver ? (
                  <div className="mt-1 flex flex-wrap items-center gap-2 text-xs text-foreground/55">
                    <span className="inline-flex items-center gap-1">
                      <User size={12} />
                      {approver.userName}
                    </span>
                    {approver.positionRu && (
                      <span className="text-foreground/40">· {approver.positionRu}</span>
                    )}
                    <span
                      className={cn(
                        "inline-flex rounded-full border px-2 py-0.5 font-medium",
                        isDone
                          ? "border-emerald-200 bg-emerald-50 text-emerald-700"
                          : isRejected
                            ? "border-red-200 bg-red-50 text-red-700"
                            : isActive
                              ? "border-blue-200 bg-blue-50 text-blue-800"
                              : "border-amber-200 bg-amber-50 text-amber-700"
                      )}
                    >
                      {approverStatusLabel(approver.status, locale)}
                    </span>
                  </div>
                ) : (
                  <p className="mt-1 text-xs text-foreground/40">
                    {locale.startsWith("en") ? "Will be assigned on submit" : "Назначается при отправке"}
                  </p>
                )}
                {approver?.decidedAt && (
                  <p className="mt-1 text-[11px] text-foreground/35">
                    {new Date(approver.decidedAt).toLocaleString(locale.startsWith("en") ? "en-GB" : "ru-RU")}
                  </p>
                )}
              </div>

              {role && (
                <span className="hidden sm:inline text-[10px] font-semibold uppercase tracking-wider text-foreground/30 shrink-0">
                  {approverRoleLabel(role, locale)}
                </span>
              )}
            </div>
          );
        })}

        {request.phase === "Approved" && (
          <div className="flex items-center gap-2 rounded-xl border border-emerald-200 bg-emerald-50/50 px-4 py-3 text-sm text-emerald-800">
            <CheckCircle2 size={18} />
            {locale.startsWith("en")
              ? "Memorandum fully approved and signed."
              : "Служебная записка полностью утверждена и подписана."}
          </div>
        )}
      </div>
    </DcsWorkflowCard>
  );
}

export function HrBusinessTripWorkflowShell({ children }: { children: React.ReactNode }) {
  return <DcsWorkflowShell kind="memo">{children}</DcsWorkflowShell>;
}
