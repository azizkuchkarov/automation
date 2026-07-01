"use client";

import { useEffect, useState, type ReactNode } from "react";
import { CheckCircle2, Circle, XCircle } from "lucide-react";
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
  step6Approvers: ApproverRow[];
  setStep6Approvers: (v: ApproverRow[]) => void;
  step6Attachments: AttachmentRow[];
  setStep6Attachments: (v: AttachmentRow[]) => void;
  assignable: { id: string; fullName: string }[];
  onCompleteStep: (step: number, comment: string) => void;
  onSubmitStep6: () => void;
  onRejectTas: (comment: string) => void;
}

export function TechnicalAffairsWorkflowPanel({
  req,
  locale,
  t,
  acting,
  documentId,
  step6Approvers,
  setStep6Approvers,
  step6Attachments,
  setStep6Attachments,
  assignable,
  onCompleteStep,
  onSubmitStep6,
  onRejectTas,
}: Props) {
  const inputClass = cn(
    "w-full rounded-xl border border-border/70 bg-background px-3 py-2.5 text-sm",
    "focus:outline-none focus:ring-2 focus:ring-sky-500/25"
  );
  const current = req.currentStep;
  const totalSteps = req.steps.length;
  const comments: ProcurementStepComment[] = req.stepComments ?? [];
  const progress = Math.round(((current - 1) / Math.max(totalSteps - 1, 1)) * 100);
  const rejected = req.status === "Rejected";

  const [viewStep, setViewStep] = useState(current);
  const [rejectComment, setRejectComment] = useState("");
  const [showRejectForm, setShowRejectForm] = useState(false);

  useEffect(() => {
    setViewStep(current);
  }, [current]);

  const step = req.steps.find((s) => s.number === viewStep);
  if (!step) return null;

  const done = step.number < current;
  const active = step.number === current && !rejected;
  const isStep6 = step.number === 6;

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

      {rejected && (
        <div className="mx-4 mt-4 px-4 py-3 rounded-xl border border-red-500/30 bg-red-500/5 text-sm text-red-700 dark:text-red-400">
          {t("tasRejectedBanner")}
        </div>
      )}

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
              ) : rejected && step.number === current ? (
                <XCircle size={20} className="text-red-500 shrink-0 mt-0.5" />
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

                {active && !isStep6 && (
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
                    completeActionsPrefix={
                      <TasRejectBlock
                        t={t}
                        acting={acting}
                        rejectComment={rejectComment}
                        setRejectComment={setRejectComment}
                        showRejectForm={showRejectForm}
                        setShowRejectForm={setShowRejectForm}
                        onReject={onRejectTas}
                        inline
                      />
                    }
                  />
                )}

                {active && isStep6 && (
                  <>
                    <Step6Block
                      documentId={documentId}
                      approvers={step6Approvers}
                      setApprovers={setStep6Approvers}
                      attachments={step6Attachments}
                      setAttachments={setStep6Attachments}
                      assignable={assignable}
                      acting={acting}
                      onSubmit={onSubmitStep6}
                      t={t}
                      inputClass={inputClass}
                      rejectSlot={
                        <TasRejectBlock
                          t={t}
                          acting={acting}
                          rejectComment={rejectComment}
                          setRejectComment={setRejectComment}
                          showRejectForm={showRejectForm}
                          setShowRejectForm={setShowRejectForm}
                          onReject={onRejectTas}
                          inline
                        />
                      }
                    />
                  </>
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

function TasRejectBlock({
  t,
  acting,
  rejectComment,
  setRejectComment,
  showRejectForm,
  setShowRejectForm,
  onReject,
  inline = false,
}: {
  t: ReturnType<typeof useTranslations>;
  acting: boolean;
  rejectComment: string;
  setRejectComment: (v: string) => void;
  showRejectForm: boolean;
  setShowRejectForm: (v: boolean) => void;
  onReject: (comment: string) => void;
  inline?: boolean;
}) {
  if (showRejectForm) {
    return (
      <div
        className={cn(
          "space-y-2 rounded-xl border border-red-500/25 bg-red-500/5 p-3",
          inline ? "w-full" : "mt-4 pt-4 border-t border-border/50"
        )}
      >
        <p className="text-xs font-medium text-foreground/70">{t("rejectTasHint")}</p>
        <textarea
          className="w-full rounded-xl border border-border/70 bg-background px-3 py-2.5 text-sm min-h-[72px]"
          placeholder={t("rejectReasonPlaceholder")}
          value={rejectComment}
          onChange={(e) => setRejectComment(e.target.value)}
        />
        <div className="flex flex-wrap gap-2">
          <Button
            size="sm"
            variant="danger"
            disabled={acting || !rejectComment.trim()}
            onClick={() => onReject(rejectComment.trim())}
          >
            {t("confirmReject")}
          </Button>
          <Button size="sm" variant="ghost" disabled={acting} onClick={() => setShowRejectForm(false)}>
            {t("cancel")}
          </Button>
        </div>
      </div>
    );
  }

  return (
    <Button
      size="sm"
      variant="secondary"
      disabled={acting}
      onClick={() => setShowRejectForm(true)}
      className="text-red-600 border-red-500/25 hover:bg-red-500/5"
    >
      <XCircle size={14} className="mr-1.5" />
      {t("rejectTas")}
    </Button>
  );
}

function Step6Block({
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
  rejectSlot,
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
  rejectSlot?: ReactNode;
}) {
  return (
    <div className="mt-4 p-4 rounded-xl border border-sky-500/20 bg-sky-500/5 space-y-3">
      <p className="text-xs font-semibold text-foreground/70">{t("step6Hint")}</p>
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
      <div className="flex flex-wrap items-center gap-2 pt-1">
        {rejectSlot}
        <Button size="sm" disabled={acting} onClick={onSubmit}>
          {t("submitApproval")}
        </Button>
      </div>
    </div>
  );
}
