"use client";

import { useEffect, useState } from "react";
import { CheckCircle2, Circle, FileUp, Globe2, Layers, Send, UserPlus } from "lucide-react";
import type { useTranslations } from "next-intl";
import {
  ContractsIntProcurementVariant,
  ProcurementContractsIntStepApprover,
  ProcurementRequest,
  ProcurementRequestUser,
  ProcurementStepComment,
  contractsIntRegistrationExample,
  contractsIntStepHint,
  contractsIntStepTitle,
  contractsIntVariantLabel,
  contractsIntWorkflowTitle,
} from "@/lib/procurementRequest";
import { DocumentFileUpload } from "@/components/dcs/DocumentFileUpload";
import { StepCommentThread } from "@/components/dcs/StepCommentThread";
import { WorkflowStepNavigator } from "@/components/dcs/WorkflowStepNavigator";
import { Button } from "@/components/ui/Button";
import { fileDownloadUrl } from "@/lib/files";
import { cn } from "@/lib/utils";

const VARIANT_OPTIONS: { id: ContractsIntProcurementVariant; enabled: boolean }[] = [
  { id: "Sbp", enabled: true },
  { id: "Tender", enabled: true },
  { id: "DirectForeignContract", enabled: true },
];

interface Props {
  req: ProcurementRequest;
  locale: string;
  t: ReturnType<typeof useTranslations>;
  acting: boolean;
  contractsPerms?: ProcurementRequest["contractsPermissions"];
  stepComment: string;
  setStepComment: (v: string) => void;
  selectedVariant: ContractsIntProcurementVariant | "";
  setSelectedVariant: (v: ContractsIntProcurementVariant | "") => void;
  approverCandidates: ProcurementRequestUser[];
  selectedApproverIds: string[];
  setSelectedApproverIds: (ids: string[]) => void;
  onSelectVariant: () => void;
  onCompleteStep: (step: number) => void;
  onUploadStepFile: (step: number, fileName: string, storageKey: string) => void;
  onSubmitApprovers: (step: number) => void;
  onDecideApproval: (step: number, approve: boolean) => void;
  onSendToSecretariat: (step: number) => void;
}

export function InternationalContractsWorkflowPanel({
  req,
  locale,
  t,
  acting,
  contractsPerms,
  stepComment,
  setStepComment,
  selectedVariant,
  setSelectedVariant,
  approverCandidates,
  selectedApproverIds,
  setSelectedApproverIds,
  onSelectVariant,
  onCompleteStep,
  onUploadStepFile,
  onSubmitApprovers,
  onDecideApproval,
  onSendToSecretariat,
}: Props) {
  const inputClass = cn(
    "w-full rounded-xl border border-border/70 bg-background px-3 py-2.5 text-sm",
    "focus:outline-none focus:ring-2 focus:ring-sky-500/25"
  );

  if (!req.contractsIntVariant && contractsPerms?.canSelectIntVariant) {
    return (
      <section className="rounded-2xl border border-sky-500/20 bg-surface shadow-sm overflow-hidden">
        <div className="px-5 py-4 border-b border-sky-500/15 bg-gradient-to-r from-sky-500/8 via-transparent to-transparent">
          <h2 className="text-sm font-bold">{t("intVariantSelection")}</h2>
          <p className="text-xs text-foreground/50 mt-1">{t("intVariantSelectionHint")}</p>
        </div>
        <div className="p-4 space-y-3">
          <div className="grid gap-3 sm:grid-cols-3">
            {VARIANT_OPTIONS.map(({ id, enabled }) => {
              const selected = selectedVariant === id;
              return (
                <button
                  key={id}
                  type="button"
                  disabled={acting || !enabled}
                  onClick={() => enabled && setSelectedVariant(id)}
                  className={cn(
                    "text-left rounded-xl border p-4 transition-all",
                    !enabled && "opacity-50 cursor-not-allowed",
                    selected
                      ? "border-sky-500/50 bg-sky-500/10 ring-2 ring-sky-500/20"
                      : "border-border/60 hover:border-sky-500/30 hover:bg-sky-500/5"
                  )}
                >
                  <div className="flex items-start gap-3">
                    <Globe2 size={18} className={cn("shrink-0 mt-0.5", selected ? "text-sky-600" : "text-foreground/40")} />
                    <div>
                      <p className="text-sm font-semibold">{contractsIntVariantLabel(id, locale)}</p>
                      {enabled ? (
                        <p className="text-xs text-foreground/50 mt-1">
                          {id === "Sbp" && t("intVariantSbpHint")}
                          {id === "Tender" && t("intVariantTenderHint")}
                          {id === "DirectForeignContract" && t("intVariantDirectForeignHint")}
                        </p>
                      ) : (
                        <p className="text-[10px] text-foreground/40 mt-1 uppercase tracking-wide">{t("comingSoon")}</p>
                      )}
                    </div>
                  </div>
                </button>
              );
            })}
          </div>
          <textarea
            className={cn(inputClass, "min-h-[64px]")}
            placeholder={t("intVariantCommentPlaceholder")}
            value={stepComment}
            onChange={(e) => setStepComment(e.target.value)}
          />
          <Button
            size="sm"
            disabled={acting || !selectedVariant || !stepComment.trim()}
            onClick={onSelectVariant}
          >
            <Layers size={14} className="mr-1.5" />
            {t("startIntWorkflow")}
          </Button>
        </div>
      </section>
    );
  }

  if (!req.contractsIntVariant || !req.contractsIntSteps?.length) return null;

  const steps = req.contractsIntSteps;
  const current = req.contractsIntCurrentStep;
  const totalSteps = steps.length;
  const completed = req.contractsSubPhase === "Completed" || req.phase === "Payment";
  const operationalStart = 3;
  const progress = completed
    ? 100
    : Math.round(((Math.max(current, operationalStart) - operationalStart) / Math.max(totalSteps - operationalStart, 1)) * 100);

  const [viewStep, setViewStep] = useState(current >= operationalStart ? current : operationalStart);
  useEffect(() => {
    if (current >= operationalStart) setViewStep(current);
  }, [current]);

  const step = steps.find((s) => s.number === viewStep);
  if (!step) return null;

  const comments: ProcurementStepComment[] = req.stepComments ?? [];
  const stepComments = comments.filter((c) => c.phase === "Contracts" && c.stepNumber === viewStep);
  const isCurrent = !completed && current === viewStep;
  const isPast = completed || viewStep < current || viewStep < operationalStart;
  const canComplete = isCurrent && (contractsPerms?.canCompleteIntStep || contractsPerms?.canCompleteAsSecretariat);
  const files = step.files ?? [];
  const approvers = step.approvers ?? [];

  const canFinish =
    canComplete &&
    stepComment.trim().length > 0 &&
    (!step.requiresUpload || files.length > 0) &&
    (!step.requiresApprovers || step.allApproversApproved) &&
    (!step.requiresSecretariat || step.secretariatPending);

  return (
    <section className="rounded-2xl border border-sky-500/20 bg-surface shadow-sm overflow-hidden">
      <div className="px-5 py-4 border-b border-sky-500/15 bg-gradient-to-r from-sky-500/8 via-transparent to-transparent">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 className="text-sm font-bold">
              {contractsIntWorkflowTitle(req.contractsIntVariant, locale, totalSteps)}
            </h2>
            <p className="text-xs text-foreground/50 mt-1">
              {contractsIntVariantLabel(req.contractsIntVariant, locale)}
              {req.contractsIntVariantSelectedAt && (
                <span className="ml-2 text-foreground/35">
                  · {new Date(req.contractsIntVariantSelectedAt).toLocaleDateString(locale)}
                </span>
              )}
            </p>
          </div>
          <div className="flex items-center gap-2 text-xs">
            <span className="px-2.5 py-1 rounded-full bg-sky-500/12 text-sky-700 dark:text-sky-300 font-semibold">
              {completed ? totalSteps : Math.max(current, operationalStart)} / {totalSteps}
            </span>
            <span className="text-foreground/40">{progress}%</span>
          </div>
        </div>
        <div className="mt-3 h-1.5 rounded-full bg-sky-500/10 overflow-hidden">
          <div className="h-full bg-sky-500/60 rounded-full transition-all" style={{ width: `${progress}%` }} />
        </div>
      </div>

      <div className="p-4 space-y-4">
        <div
          className={cn(
            "rounded-xl border p-4",
            isCurrent ? "border-sky-500/35 bg-sky-500/[0.04]" : "border-border/50",
            isPast && "border-emerald-500/20 bg-emerald-500/[0.03]"
          )}
        >
          <div className="flex gap-3">
            {isPast ? (
              <CheckCircle2 size={20} className="text-emerald-600 shrink-0 mt-0.5" />
            ) : (
              <Circle size={20} className={cn("shrink-0 mt-0.5", isCurrent ? "text-sky-500" : "text-foreground/20")} />
            )}
            <div className="flex-1 min-w-0">
              <p className="text-[10px] font-bold uppercase tracking-wider text-foreground/40">
                {t("step")} {step.number}
              </p>
              <p className="text-sm font-semibold">{contractsIntStepTitle(step, locale)}</p>
              <p className="text-xs text-foreground/55 mt-1.5 leading-relaxed">
                {contractsIntStepHint(step, locale)}
              </p>

              {step.number < operationalStart && (
                <p className="mt-3 text-xs text-emerald-700/90 dark:text-emerald-400/90 border-l-2 border-emerald-500/40 pl-2.5">
                  {t("intPrerequisiteStepDone")}
                </p>
              )}

              {step.hasBranch && step.branchHintRu && (
                <p className="mt-3 text-xs text-amber-700/90 dark:text-amber-400/90 border-l-2 border-amber-500/40 pl-2.5">
                  {locale.startsWith("en") ? step.branchHintEn : step.branchHintRu}
                </p>
              )}

              {(step.requiresUpload || files.length > 0) && (
                <div className="mt-4 space-y-2 rounded-lg border border-border/50 bg-background/60 p-3">
                  <p className="text-xs font-semibold flex items-center gap-1.5">
                    <FileUp size={13} />
                    {t("intStepUploadDocuments")}
                  </p>
                  {files.length > 0 && (
                    <ul className="space-y-1">
                      {files.map((f) => (
                        <li key={f.id} className="text-xs">
                          {f.storageKey ? (
                            <a
                              href={fileDownloadUrl(f.storageKey, f.fileName)}
                              target="_blank"
                              rel="noreferrer"
                              className="text-sky-700 hover:underline dark:text-sky-300"
                            >
                              {f.fileName}
                            </a>
                          ) : (
                            <span>{f.fileName}</span>
                          )}
                          <span className="text-foreground/40 ml-2">{f.uploadedByName}</span>
                        </li>
                      ))}
                    </ul>
                  )}
                  {isCurrent && contractsPerms?.canUploadIntStepFile && (
                    <DocumentFileUpload
                      folder="contracts-int"
                      disabled={acting}
                      onUploaded={(fileName, storageKey) => onUploadStepFile(step.number, fileName, storageKey)}
                      labels={{ uploading: t("uploading"), attached: t("attached"), pick: t("pickFile") }}
                    />
                  )}
                </div>
              )}

              {(step.requiresApprovers || approvers.length > 0) && (
                <div className="mt-4 space-y-2 rounded-lg border border-border/50 bg-background/60 p-3">
                  <p className="text-xs font-semibold flex items-center gap-1.5">
                    <UserPlus size={13} />
                    {t("intStepApprovers")}
                  </p>
                  {approvers.length > 0 ? (
                    <ApproverList approvers={approvers} locale={locale} t={t} />
                  ) : isCurrent && contractsPerms?.canSubmitIntStepApprovers ? (
                    <div className="space-y-2">
                      <select
                        className={inputClass}
                        value=""
                        onChange={(e) => {
                          const id = e.target.value;
                          if (!id || selectedApproverIds.includes(id)) return;
                          setSelectedApproverIds([...selectedApproverIds, id]);
                        }}
                      >
                        <option value="">{t("selectUser")}</option>
                        {approverCandidates.map((u) => (
                          <option key={u.id} value={u.id}>{u.fullName}</option>
                        ))}
                      </select>
                      {selectedApproverIds.length > 0 && (
                        <ul className="text-xs space-y-1">
                          {selectedApproverIds.map((id) => {
                            const u = approverCandidates.find((x) => x.id === id);
                            return (
                              <li key={id} className="flex items-center justify-between gap-2">
                                <span>{u?.fullName ?? id}</span>
                                <button
                                  type="button"
                                  className="text-rose-600 text-[11px]"
                                  onClick={() => setSelectedApproverIds(selectedApproverIds.filter((x) => x !== id))}
                                >
                                  {t("remove")}
                                </button>
                              </li>
                            );
                          })}
                        </ul>
                      )}
                      <Button
                        size="sm"
                        disabled={acting || selectedApproverIds.length === 0}
                        onClick={() => onSubmitApprovers(step.number)}
                      >
                        {t("submitApprovers")}
                      </Button>
                    </div>
                  ) : (
                    <p className="text-xs text-foreground/45">{t("intStepApproversPending")}</p>
                  )}
                  {isCurrent && contractsPerms?.canDecideIntStepApproval && (
                    <div className="flex flex-wrap gap-2 pt-1">
                      <Button size="sm" disabled={acting} onClick={() => onDecideApproval(step.number, true)}>
                        {t("approve")}
                      </Button>
                      <Button
                        size="sm"
                        variant="danger"
                        disabled={acting || !stepComment.trim()}
                        onClick={() => onDecideApproval(step.number, false)}
                      >
                        {t("reject")}
                      </Button>
                    </div>
                  )}
                </div>
              )}

              {step.requiresSecretariat && (
                <div className="mt-4 space-y-2 rounded-lg border border-violet-500/20 bg-violet-500/5 p-3">
                  <p className="text-xs font-semibold flex items-center gap-1.5 text-violet-800 dark:text-violet-300">
                    <Send size={13} />
                    {t("intStepTenderSecretariat")}
                  </p>
                  {step.secretariatPending ? (
                    <p className="text-xs text-foreground/60">
                      {t("intStepSecretariatWaiting", {
                        name: req.contractsIntSecretariatUserName ?? "—",
                      })}
                    </p>
                  ) : isCurrent && contractsPerms?.canSendToSecretariat ? (
                    <div className="space-y-2">
                      <textarea
                        className={cn(inputClass, "min-h-[72px]")}
                        placeholder={t("stepCompleteCommentPlaceholder")}
                        value={stepComment}
                        onChange={(e) => setStepComment(e.target.value)}
                      />
                      <Button
                        size="sm"
                        disabled={acting || !stepComment.trim()}
                        onClick={() => onSendToSecretariat(step.number)}
                      >
                        {t("sendToSecretariat")}
                      </Button>
                    </div>
                  ) : null}
                </div>
              )}

              {step.requiresRegistration && (
                <div className="mt-4 space-y-1.5 rounded-lg border border-emerald-500/20 bg-emerald-500/5 p-3">
                  <label className="text-xs font-semibold">{t("intStepContractRegistration")}</label>
                  {req.contractsIntContractRegistrationNumber ? (
                    <p className="text-sm font-bold tracking-wide text-emerald-800 dark:text-emerald-300">
                      {req.contractsIntContractRegistrationNumber}
                    </p>
                  ) : (
                    <p className="text-xs text-foreground/60">
                      {t("intStepRegistrationAutoHint", {
                        example: contractsIntRegistrationExample(req.contractsIntVariant),
                      })}
                    </p>
                  )}
                </div>
              )}

              {(canComplete || contractsPerms?.canDecideIntStepApproval) && (
                <div className="mt-4 space-y-3">
                  <textarea
                    className={cn(inputClass, "min-h-[72px]")}
                    placeholder={t("stepCompleteCommentPlaceholder")}
                    value={stepComment}
                    onChange={(e) => setStepComment(e.target.value)}
                  />
                  {canComplete && (
                    <Button
                      size="sm"
                      disabled={acting || !canFinish}
                      onClick={() => onCompleteStep(step.number)}
                    >
                      {contractsPerms?.canCompleteAsSecretariat
                        ? t("secretariatCompleteStep")
                        : t("completeStep")}
                    </Button>
                  )}
                </div>
              )}

              {stepComments.length > 0 && (
                <div className="mt-4">
                  <StepCommentThread
                    comments={stepComments}
                    phase="Contracts"
                    stepNumber={viewStep}
                    locale={locale}
                    acting={acting}
                    canAdd={false}
                  />
                </div>
              )}
            </div>
          </div>
        </div>

        <WorkflowStepNavigator
          viewStep={viewStep}
          totalSteps={totalSteps}
          workflowStep={completed ? totalSteps + 1 : current}
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
      </div>
    </section>
  );
}

function ApproverList({
  approvers,
  locale,
  t,
}: {
  approvers: ProcurementContractsIntStepApprover[];
  locale: string;
  t: ReturnType<typeof useTranslations>;
}) {
  return (
    <ul className="space-y-1.5">
      {approvers.map((a) => (
        <li key={a.id} className="flex items-center justify-between gap-2 text-xs">
          <span className="font-medium">{a.fullName}</span>
          <span
            className={cn(
              "rounded-full px-2 py-0.5 text-[10px] font-bold uppercase",
              a.status === "Approved" && "bg-emerald-500/15 text-emerald-700",
              a.status === "Rejected" && "bg-rose-500/15 text-rose-700",
              a.status === "Pending" && "bg-amber-500/15 text-amber-700"
            )}
          >
            {a.status === "Approved"
              ? t("statusApproved")
              : a.status === "Rejected"
                ? t("statusRejected")
                : t("statusPending")}
          </span>
        </li>
      ))}
    </ul>
  );
}
