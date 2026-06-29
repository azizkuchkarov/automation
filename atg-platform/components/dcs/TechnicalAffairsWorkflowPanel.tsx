"use client";

import { useEffect, useState } from "react";
import { CheckCircle2, Circle } from "lucide-react";
import type { useTranslations } from "next-intl";
import {
  ProcurementAttachmentKind,
  ProcurementApproverRole,
  ProcurementRequest,
  ProcurementStepComment,
  approverRoleLabel,
  stepTitle,
} from "@/lib/procurementRequest";
import { StepCommentThread } from "@/components/dcs/StepCommentThread";
import { WorkflowStepNavigator } from "@/components/dcs/WorkflowStepNavigator";
import { Button } from "@/components/ui/Button";
import { DocumentFileUpload } from "@/components/dcs/DocumentFileUpload";
import { cn } from "@/lib/utils";

type ApproverRow = { userId: string; role: ProcurementApproverRole };
type AttachmentRow = { kind: ProcurementAttachmentKind; fileName: string; storageKey?: string };

interface Props {
  req: ProcurementRequest;
  locale: string;
  t: ReturnType<typeof useTranslations>;
  acting: boolean;
  documentId: string;
  step9Approvers: ApproverRow[];
  setStep9Approvers: (v: ApproverRow[]) => void;
  step9Attachments: AttachmentRow[];
  setStep9Attachments: (v: AttachmentRow[]) => void;
  assignable: { id: string; fullName: string }[];
  onCompleteStep: (step: number, comment: string) => void;
  onSubmitStep9: () => void;
}

export function TechnicalAffairsWorkflowPanel({
  req,
  locale,
  t,
  acting,
  documentId,
  step9Approvers,
  setStep9Approvers,
  step9Attachments,
  setStep9Attachments,
  assignable,
  onCompleteStep,
  onSubmitStep9,
}: Props) {
  const inputClass = cn(
    "w-full rounded-xl border border-border/70 bg-background px-3 py-2.5 text-sm",
    "focus:outline-none focus:ring-2 focus:ring-sky-500/25"
  );
  const current = req.currentStep;
  const totalSteps = req.steps.length;
  const comments: ProcurementStepComment[] = req.stepComments ?? [];
  const progress = Math.round(((current - 1) / Math.max(totalSteps - 1, 1)) * 100);

  const [viewStep, setViewStep] = useState(current);
  useEffect(() => {
    setViewStep(current);
  }, [current]);

  const step = req.steps.find((s) => s.number === viewStep);
  if (!step) return null;

  const done = step.number < current;
  const active = step.number === current;
  const isStep9 = step.number === 9;

  return (
    <section className="rounded-2xl border border-sky-500/20 bg-surface shadow-sm overflow-hidden">
      <div className="px-5 py-4 border-b border-sky-500/15 bg-gradient-to-r from-sky-500/8 via-transparent to-transparent">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 className="text-sm font-bold">{t("workflowSteps")}</h2>
            <p className="text-xs text-foreground/50 mt-1">
              {locale.startsWith("en")
                ? "BMGMC Technical Affairs — complete each step with a mandatory comment."
                : "BMGMC — Технический отдел: каждый этап завершается с обязательным комментарием."}
            </p>
          </div>
          <div className="flex items-center gap-2 text-xs">
            <span className="px-2.5 py-1 rounded-full bg-sky-500/12 text-sky-700 dark:text-sky-300 font-semibold">
              {current} / {totalSteps}
            </span>
            <span className="px-2.5 py-1 rounded-full bg-foreground/[0.06] text-foreground/60">
              {progress}%
            </span>
          </div>
        </div>
        <div className="mt-3 h-1.5 rounded-full bg-foreground/[0.06] overflow-hidden">
          <div
            className="h-full rounded-full bg-gradient-to-r from-sky-500 to-blue-600 transition-all duration-500"
            style={{ width: `${progress}%` }}
          />
        </div>
      </div>

      <div className="p-4">
        <div
          className={cn(
            "rounded-xl border transition-all",
            active ? "border-sky-500/35 bg-sky-500/[0.04] shadow-sm" : "border-border/50",
            done && "border-emerald-500/20 bg-emerald-500/[0.03]"
          )}
        >
          <div className="p-4">
            <div className="flex gap-3">
              {done ? (
                <CheckCircle2 size={20} className="text-emerald-600 shrink-0 mt-0.5" />
              ) : (
                <Circle
                  size={20}
                  className={cn("shrink-0 mt-0.5", active ? "text-sky-500" : "text-foreground/20")}
                />
              )}
              <div className="flex-1 min-w-0">
                <p className="text-[10px] font-bold uppercase tracking-wider text-foreground/40">
                  {t("step")} {step.number}
                </p>
                <p className="text-sm font-semibold text-foreground">{stepTitle(step, locale)}</p>

                {active && !isStep9 && (
                  <StepCommentThread
                    comments={comments}
                    phase="TechnicalAffairs"
                    stepNumber={step.number}
                    locale={locale}
                    acting={acting}
                    completePlaceholder={t("stepCommentRequired")}
                    completeAction={{
                      label: t("markComplete"),
                      disabled: acting,
                      onComplete: (body) => onCompleteStep(step.number, body),
                    }}
                  />
                )}

                {active && isStep9 && (
                  <Step9Block
                    documentId={documentId}
                    approvers={step9Approvers}
                    setApprovers={setStep9Approvers}
                    attachments={step9Attachments}
                    setAttachments={setStep9Attachments}
                    assignable={assignable}
                    acting={acting}
                    onSubmit={onSubmitStep9}
                    t={t}
                    inputClass={inputClass}
                  />
                )}

                {!active && (
                  <StepCommentThread
                    comments={comments}
                    phase="TechnicalAffairs"
                    stepNumber={step.number}
                    locale={locale}
                  />
                )}
              </div>
            </div>
          </div>
        </div>
      </div>

      <WorkflowStepNavigator
        viewStep={viewStep}
        totalSteps={totalSteps}
        workflowStep={current}
        stepLabel={t("step")}
        previousLabel={t("stepPrevious")}
        nextLabel={t("stepNext")}
        viewCompletedHint={t("stepViewCompleted")}
        viewUpcomingHint={t("stepViewUpcoming")}
        accent="sky"
        onPrevious={() => setViewStep((s) => Math.max(1, s - 1))}
        onNext={() => setViewStep((s) => Math.min(totalSteps, s + 1))}
        onSelectStep={setViewStep}
      />
    </section>
  );
}

function Step9Block({
  documentId,
  approvers,
  setApprovers,
  attachments,
  setAttachments,
  assignable,
  acting,
  onSubmit,
  t,
  inputClass,
}: {
  documentId: string;
  approvers: ApproverRow[];
  setApprovers: (v: ApproverRow[]) => void;
  attachments: AttachmentRow[];
  setAttachments: (v: AttachmentRow[]) => void;
  assignable: { id: string; fullName: string }[];
  acting: boolean;
  onSubmit: () => void;
  t: ReturnType<typeof useTranslations>;
  inputClass: string;
}) {
  return (
    <div className="mt-4 p-4 rounded-xl border border-sky-500/20 bg-sky-500/5 space-y-3">
      <p className="text-xs font-semibold text-foreground/70">{t("step9Hint")}</p>
      {attachments.map((a, i) => (
        <div key={i} className="flex gap-2 items-start flex-wrap">
          <select
            className={cn(inputClass, "w-28 shrink-0")}
            value={a.kind}
            onChange={(e) => {
              const next = [...attachments];
              next[i] = { ...next[i], kind: e.target.value as ProcurementAttachmentKind };
              setAttachments(next);
            }}
          >
            <option value="TechnicalAssignment">TA</option>
            <option value="MaterialRequisition">MR</option>
            <option value="ServiceRequisition">SR</option>
          </select>
          <DocumentFileUpload
            folder={`procurement/${documentId}`}
            fileName={a.fileName}
            storageKey={a.storageKey}
            disabled={acting}
            labels={{ uploading: t("uploading"), attached: t("fileAttached") }}
            onUploaded={(fileName, storageKey) => {
              const next = [...attachments];
              next[i] = { ...next[i], fileName, storageKey };
              setAttachments(next);
            }}
          />
        </div>
      ))}
      {approvers.map((a, i) => (
        <div key={i} className="flex gap-2 items-center">
          <span className="text-xs w-32 shrink-0 text-foreground/50">
            {approverRoleLabel(a.role, "en")}
          </span>
          <select
            className={cn(inputClass, "flex-1")}
            value={a.userId}
            onChange={(e) => {
              const next = [...approvers];
              next[i] = { ...next[i], userId: e.target.value };
              setApprovers(next);
            }}
          >
            <option value="">{t("selectUser")}</option>
            {assignable.map((u) => (
              <option key={u.id} value={u.id}>
                {u.fullName}
              </option>
            ))}
          </select>
        </div>
      ))}
      <Button size="sm" disabled={acting} onClick={onSubmit}>
        {t("submitApproval")}
      </Button>
    </div>
  );
}
