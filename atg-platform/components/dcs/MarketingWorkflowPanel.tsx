"use client";

import { useEffect, useState } from "react";
import { CheckCircle2, Circle, UserCheck, UserPlus, RotateCcw } from "lucide-react";
import type { useTranslations } from "next-intl";
import {
  MarketingBranchType,
  ProcurementMarketingStep,
  ProcurementRequest,
  ProcurementRequestUser,
  ProcurementStepComment,
  branchForMarketingStep,
  marketingStepBranchHint,
  marketingStepHint,
  marketingStepTitle,
  marketingSubPhaseLabel,
} from "@/lib/procurementRequest";
import { StepCommentThread } from "@/components/dcs/StepCommentThread";
import { MarketingStep3RfqPanel } from "@/components/dcs/MarketingStep4RfqPanel";
import { MarketingStep4ProposalsPanel } from "@/components/dcs/MarketingStep4ProposalsPanel";
import { MarketingStep5ApprovedPanel } from "@/components/dcs/MarketingStep5ApprovedPanel";
import { MarketingStep6PlanPanel } from "@/components/dcs/MarketingStep6PlanPanel";
import { MarketingStep8PlanApprovalPanel } from "@/components/dcs/MarketingStep8PlanApprovalPanel";
import { MarketingStep9RegistrationPanel } from "@/components/dcs/MarketingStep9RegistrationPanel";
import { WorkflowStepNavigator } from "@/components/dcs/WorkflowStepNavigator";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

interface Props {
  req: ProcurementRequest;
  locale: string;
  t: ReturnType<typeof useTranslations>;
  acting: boolean;
  marketingPerms?: ProcurementRequest["marketingPermissions"];
  marketingWorkers: ProcurementRequestUser[];
  selectedSpecialist: string;
  setSelectedSpecialist: (v: string) => void;
  stepComment: string;
  setStepComment: (v: string) => void;
  onAssign: () => void;
  onReturnToInitiator?: () => void;
  onAccept: () => void;
  onCompleteMarketingStep: (step: number, comment?: string) => void;
  onRecordMarketingBranch: (branch: MarketingBranchType, resolve: boolean) => void;
  onSubmitPlanApproval?: (approvers: { userId: string; role: string }[]) => void;
  onApprovePlan?: (comment: string) => void;
  onRejectPlan?: (comment: string) => void;
  onConfirmRegistration?: () => void;
}

export function MarketingWorkflowPanel({
  req,
  locale,
  t,
  acting,
  marketingPerms,
  marketingWorkers,
  selectedSpecialist,
  setSelectedSpecialist,
  stepComment,
  setStepComment,
  onAssign,
  onReturnToInitiator,
  onAccept,
  onCompleteMarketingStep,
  onRecordMarketingBranch,
  onSubmitPlanApproval,
  onApprovePlan,
  onRejectPlan,
  onConfirmRegistration,
}: Props) {
  const inputClass = cn(
    "w-full rounded-xl border border-border/70 bg-background px-3 py-2.5 text-sm",
    "focus:outline-none focus:ring-2 focus:ring-violet-500/25"
  );
  const current = req.marketingCurrentStep;
  const totalSteps = req.marketingSteps.length;
  const comments: ProcurementStepComment[] = req.stepComments ?? [];
  const progress = Math.round(((current - 1) / Math.max(totalSteps - 1, 1)) * 100);

  const [viewStep, setViewStep] = useState(current);
  useEffect(() => {
    setViewStep(current);
  }, [current]);

  const step = req.marketingSteps.find((s) => s.number === viewStep);
  if (!step) return null;

  return (
    <section className="rounded-2xl border border-violet-500/20 bg-surface shadow-sm overflow-hidden">
      <div className="px-5 py-4 border-b border-violet-500/15 bg-gradient-to-r from-violet-500/8 via-transparent to-transparent">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 className="text-sm font-bold">{t("marketingWorkflow")}</h2>
            <p className="text-xs text-foreground/50 mt-1">{t("marketingWorkflowHint")}</p>
          </div>
          <div className="flex items-center gap-2 text-xs">
            <span className="px-2.5 py-1 rounded-full bg-violet-500/12 text-violet-700 dark:text-violet-300 font-semibold">
              {current} / {totalSteps}
            </span>
            <span className="px-2.5 py-1 rounded-full bg-foreground/[0.06] text-foreground/60">
              {marketingSubPhaseLabel(req.marketingSubPhase, locale)}
            </span>
          </div>
        </div>
        <div className="mt-3 h-1.5 rounded-full bg-foreground/[0.06] overflow-hidden">
          <div
            className="h-full rounded-full bg-gradient-to-r from-violet-500 to-purple-600 transition-all duration-500"
            style={{ width: `${progress}%` }}
          />
        </div>
      </div>

      <div className="p-4">
        <MarketingStepCard
          step={step}
          req={req}
          locale={locale}
          t={t}
          acting={acting}
          current={current}
          inputClass={inputClass}
          marketingPerms={marketingPerms}
          marketingWorkers={marketingWorkers}
          selectedSpecialist={selectedSpecialist}
          setSelectedSpecialist={setSelectedSpecialist}
          stepComment={stepComment}
          setStepComment={setStepComment}
          comments={comments}
          onAssign={onAssign}
          onReturnToInitiator={onReturnToInitiator}
          onAccept={onAccept}
          onCompleteMarketingStep={onCompleteMarketingStep}
          onRecordMarketingBranch={onRecordMarketingBranch}
          onSubmitPlanApproval={onSubmitPlanApproval}
          onApprovePlan={onApprovePlan}
          onRejectPlan={onRejectPlan}
          onConfirmRegistration={onConfirmRegistration}
        />
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
        accent="violet"
        onPrevious={() => setViewStep((s) => Math.max(1, s - 1))}
        onNext={() => setViewStep((s) => Math.min(totalSteps, s + 1))}
        onSelectStep={setViewStep}
      />
    </section>
  );
}

function MarketingStepCard({
  step,
  req,
  locale,
  t,
  acting,
  current,
  inputClass,
  marketingPerms,
  marketingWorkers,
  selectedSpecialist,
  setSelectedSpecialist,
  stepComment,
  setStepComment,
  comments,
  onAssign,
  onReturnToInitiator,
  onAccept,
  onCompleteMarketingStep,
  onRecordMarketingBranch,
  onSubmitPlanApproval,
  onApprovePlan,
  onRejectPlan,
  onConfirmRegistration,
}: {
  step: ProcurementMarketingStep;
  req: ProcurementRequest;
  locale: string;
  t: ReturnType<typeof useTranslations>;
  acting: boolean;
  current: number;
  inputClass: string;
  marketingPerms?: ProcurementRequest["marketingPermissions"];
  marketingWorkers: ProcurementRequestUser[];
  selectedSpecialist: string;
  setSelectedSpecialist: (v: string) => void;
  stepComment: string;
  setStepComment: (v: string) => void;
  comments: ProcurementStepComment[];
  onAssign: () => void;
  onReturnToInitiator?: () => void;
  onAccept: () => void;
  onCompleteMarketingStep: (step: number, comment?: string) => void;
  onRecordMarketingBranch: (branch: MarketingBranchType, resolve: boolean) => void;
  onSubmitPlanApproval?: (approvers: { userId: string; role: string }[]) => void;
  onApprovePlan?: (comment: string) => void;
  onRejectPlan?: (comment: string) => void;
  onConfirmRegistration?: () => void;
}) {
  const done = step.number < current || req.marketingSubPhase === "Completed";
  const active = step.number === current && req.marketingSubPhase !== "Completed";
  const branch = branchForMarketingStep(step.number);
  const branchActive = branch && req.marketingActiveBranch === branch;
  const isStep1 = step.number === 1;
  const planApprovers = req.marketingPlanApprovers ?? [];
  const allPlanApproved = planApprovers.length > 0 && planApprovers.every((a) => a.status === "Approved");
  const canCompleteStep7 = step.number === 7 ? allPlanApproved : true;
  const hideGenericComplete = step.number === 8;

  return (
    <div
      className={cn(
        "rounded-xl border transition-all",
        active ? "border-violet-500/35 bg-violet-500/[0.04] shadow-sm" : "border-border/50",
        done && !active && "border-emerald-500/20 bg-emerald-500/[0.03]"
      )}
    >
      <div className="p-4">
        <div className="flex gap-3">
          {done ? (
            <CheckCircle2 size={20} className="text-emerald-600 shrink-0 mt-0.5" />
          ) : (
            <Circle size={20} className={cn("shrink-0 mt-0.5", active ? "text-violet-500" : "text-foreground/20")} />
          )}
          <div className="flex-1 min-w-0">
            <p className="text-[10px] font-bold uppercase tracking-wider text-foreground/40">
              {t("step")} {step.number}
            </p>
            <p className="text-sm font-semibold text-foreground">{marketingStepTitle(step, locale)}</p>
            <p className="text-xs text-foreground/55 mt-1.5 leading-relaxed">{marketingStepHint(step, locale)}</p>

            {step.hasBranch && marketingStepBranchHint(step, locale) && (
              <p className="text-xs text-amber-700/90 dark:text-amber-400/90 mt-2 leading-relaxed border-l-2 border-amber-500/40 pl-2.5">
                {marketingStepBranchHint(step, locale)}
              </p>
            )}

            {branchActive && (
              <p className="text-xs font-medium text-amber-600 mt-2">{t("branchActive")}</p>
            )}

            {active && isStep1 && marketingPerms?.canAssign && (
              <AssignSpecialistForm
                t={t}
                inputClass={inputClass}
                marketingWorkers={marketingWorkers}
                selectedSpecialist={selectedSpecialist}
                setSelectedSpecialist={setSelectedSpecialist}
                stepComment={stepComment}
                setStepComment={setStepComment}
                acting={acting}
                onAssign={onAssign}
                onReturnToInitiator={marketingPerms?.canReturnToInitiator ? onReturnToInitiator : undefined}
              />
            )}

            {active && isStep1 && marketingPerms?.canAccept && (
              <div className="mt-4 p-4 rounded-xl border-2 border-emerald-500/40 bg-emerald-500/8 space-y-3">
                <p className="text-xs font-semibold text-foreground/80 flex items-center gap-2">
                  <UserCheck size={14} className="text-emerald-600" />
                  {t("acceptMarketing")}
                </p>
                <p className="text-xs text-foreground/55">{t("acceptMarketingHintEngineer")}</p>
                <textarea
                  className={cn(inputClass, "min-h-[64px]")}
                  placeholder={t("acceptCommentPlaceholder")}
                  value={stepComment}
                  onChange={(e) => setStepComment(e.target.value)}
                />
                <Button
                  size="sm"
                  disabled={acting || !stepComment.trim()}
                  onClick={onAccept}
                >
                  <UserCheck size={14} className="mr-1.5" />
                  {t("acceptMarketing")}
                </Button>
              </div>
            )}

            {active && isStep1 && !marketingPerms?.canAssign && !marketingPerms?.canAccept
              && req.marketingSubPhase === "WaitingAccept" && (
              <p className="mt-4 text-xs text-amber-700/90 dark:text-amber-400/90 border-l-2 border-amber-500/40 pl-2.5">
                {t("waitingSpecialistAccept")}
              </p>
            )}

            {active && !isStep1 && step.number === 3 && (
              <MarketingStep3RfqPanel
                documentId={req.id}
                canEdit={!!marketingPerms?.canCompleteCurrentStep}
                acting={acting}
                t={(key) => t(`step3.${key}`)}
              />
            )}

            {active && step.number === 4 && (
              <MarketingStep4ProposalsPanel
                documentId={req.id}
                canEdit={!!marketingPerms?.canCompleteCurrentStep}
                canReview={!!marketingPerms?.canReviewProposals}
                canReviewEngineer={!!marketingPerms?.canReviewProposalsAsEngineer}
                acting={acting}
                t={(key) => t(`step4Proposals.${key}`)}
              />
            )}

            {active && step.number === 5 && (
              <MarketingStep5ApprovedPanel
                documentId={req.id}
                t={(key) => t(`step5Approved.${key}`)}
              />
            )}

            {active && step.number === 6 && (
              <MarketingStep6PlanPanel
                documentId={req.id}
                locale={locale}
                canEdit={!!marketingPerms?.canCompleteCurrentStep}
                acting={acting}
                t={(key) => t(`step6Plan.${key}`)}
              />
            )}

            {active && step.number === 7 && onSubmitPlanApproval && onApprovePlan && onRejectPlan && (
              <MarketingStep8PlanApprovalPanel
                req={req}
                locale={locale}
                t={t}
                acting={acting}
                onSubmit={onSubmitPlanApproval}
                onApprove={onApprovePlan}
                onReject={onRejectPlan}
              />
            )}

            {active && step.number === 8 && onConfirmRegistration && (
              <MarketingStep9RegistrationPanel
                req={req}
                locale={locale}
                t={t}
                acting={acting}
                comment={stepComment}
                setComment={setStepComment}
                onConfirm={onConfirmRegistration}
              />
            )}

            {active && !isStep1 && !hideGenericComplete ? (
              <div className="mt-4 space-y-3">
                {step.hasBranch && branch && marketingPerms?.canRecordBranch && (
                  <Button size="sm" variant="secondary" disabled={acting} onClick={() => onRecordMarketingBranch(branch, false)}>
                    {t(`branchRecord.${branch}`)}
                  </Button>
                )}
                {step.hasBranch && branch && marketingPerms?.canResolveBranch && branchActive && (
                  <Button size="sm" variant="secondary" disabled={acting} onClick={() => onRecordMarketingBranch(branch, true)}>
                    {t(`branchResolve.${branch}`)}
                  </Button>
                )}
                <StepCommentThread
                  comments={comments}
                  phase="Marketing"
                  stepNumber={step.number}
                  locale={locale}
                  acting={acting}
                  completePlaceholder={t("stepCommentRequired")}
                  completeAction={
                    marketingPerms?.canCompleteCurrentStep && !branchActive && canCompleteStep7
                      ? {
                          label: t("markComplete"),
                          disabled: acting,
                          onComplete: (body) => onCompleteMarketingStep(step.number, body),
                        }
                      : undefined
                  }
                />
              </div>
            ) : (
              <StepCommentThread
                comments={comments}
                phase="Marketing"
                stepNumber={step.number}
                locale={locale}
              />
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

function AssignSpecialistForm({
  t,
  inputClass,
  marketingWorkers,
  selectedSpecialist,
  setSelectedSpecialist,
  stepComment,
  setStepComment,
  acting,
  onAssign,
  onReturnToInitiator,
}: {
  t: ReturnType<typeof useTranslations>;
  inputClass: string;
  marketingWorkers: ProcurementRequestUser[];
  selectedSpecialist: string;
  setSelectedSpecialist: (v: string) => void;
  stepComment: string;
  setStepComment: (v: string) => void;
  acting: boolean;
  onAssign: () => void;
  onReturnToInitiator?: () => void;
}) {
  const specialistId = selectedSpecialist.trim();
  const commentOk = stepComment.trim().length > 0;
  const canSubmit = Boolean(specialistId && commentOk);

  return (
    <div className="mt-4 p-4 rounded-xl border border-violet-500/20 bg-violet-500/5 space-y-3">
      <p className="text-xs font-semibold text-foreground/70 flex items-center gap-2">
        <UserPlus size={14} className="text-violet-600" />
        {t("assignSpecialist")}
      </p>
      <p className="text-xs text-foreground/50">{t("assignSpecialistHintMkt")}</p>
      <select
        className={inputClass}
        value={selectedSpecialist}
        onChange={(e) => setSelectedSpecialist(e.target.value)}
      >
        <option value="">{t("selectUser")}</option>
        {marketingWorkers.length === 0 ? (
          <option value="" disabled>
            {t("noMarketingWorkers")}
          </option>
        ) : (
          marketingWorkers.map((u) => (
            <option key={u.id} value={u.id}>{u.fullName}</option>
          ))
        )}
      </select>
      {!specialistId && (
        <p className="text-[11px] text-amber-600/90">{t("selectSpecialistRequired")}</p>
      )}
      <textarea
        className={cn(inputClass, "min-h-[64px]")}
        placeholder={t("assignCommentPlaceholder")}
        value={stepComment}
        onChange={(e) => setStepComment(e.target.value)}
      />
      {!commentOk && (
        <p className="text-[11px] text-amber-600/90">{t("assignCommentRequired")}</p>
      )}
      <Button
        size="sm"
        disabled={acting || !canSubmit}
        onClick={onAssign}
      >
        <UserPlus size={14} className="mr-1.5" />
        {t("assignSpecialist")}
      </Button>
      {onReturnToInitiator && (
        <Button
          size="sm"
          variant="secondary"
          disabled={acting || !commentOk}
          onClick={onReturnToInitiator}
          className="border-amber-500/40 text-amber-800 dark:text-amber-200"
        >
          <RotateCcw size={14} className="mr-1.5" />
          {t("returnToInitiator")}
        </Button>
      )}
    </div>
  );
}
