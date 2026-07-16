"use client";

import { useEffect, useMemo, useState } from "react";
import { CalendarDays, CheckCircle2, Circle, FileUp, Layers, MapPin, RotateCcw, Send, Undo2, UserPlus } from "lucide-react";
import type { useTranslations } from "next-intl";
import {
  ContractsDomProcurementVariant,
  ProcurementContractsDomStepApprover,
  ProcurementRequest,
  ProcurementRequestUser,
  ProcurementStepComment,
  contractsDomRegistrationExample,
  contractsDomStepHint,
  contractsDomStepTitle,
  contractsDomVariantLabel,
  contractsDomWorkflowTitle,
} from "@/lib/procurementRequest";
import { DocumentFileUpload } from "@/components/dcs/DocumentFileUpload";
import { StepCommentThread } from "@/components/dcs/StepCommentThread";
import { WorkflowStepNavigator } from "@/components/dcs/WorkflowStepNavigator";
import { Button } from "@/components/ui/Button";
import { fileDownloadUrl } from "@/lib/files";
import { cn } from "@/lib/utils";

const VARIANT_OPTIONS: { id: ContractsDomProcurementVariant; hintKey: string }[] = [
  { id: "EShop", hintKey: "domVariantEshopHint" },
  { id: "ElectronicAuction", hintKey: "domVariantAuctionHint" },
  { id: "QuotationRequest", hintKey: "domVariantQuotationHint" },
  { id: "DirectContract", hintKey: "domVariantDirectHint" },
  { id: "SmallValue", hintKey: "domVariantSmallValueHint" },
];

interface Props {
  req: ProcurementRequest;
  locale: string;
  t: ReturnType<typeof useTranslations>;
  acting: boolean;
  contractsPerms?: ProcurementRequest["contractsPermissions"];
  stepComment: string;
  setStepComment: (v: string) => void;
  selectedVariant: ContractsDomProcurementVariant | "";
  setSelectedVariant: (v: ContractsDomProcurementVariant | "") => void;
  approverCandidates: ProcurementRequestUser[];
  selectedApproverIds: string[];
  setSelectedApproverIds: (ids: string[]) => void;
  onSelectVariant: () => void;
  onCompleteStep: (step: number) => void;
  onScheduleStep: (step: number, date: string) => void;
  onUploadStepFile: (step: number, fileName: string, storageKey: string) => void;
  onSubmitApprovers: (step: number) => void;
  onDecideApproval: (step: number, approve: boolean) => void;
  onSendToContractsAdmin: (step: number) => void;
  onReturnToMarketing: (step: number) => void;
  onRollbackStep: (step: number) => void;
}

export function DomesticContractsWorkflowPanel({
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
  onScheduleStep,
  onUploadStepFile,
  onSubmitApprovers,
  onDecideApproval,
  onSendToContractsAdmin,
  onReturnToMarketing,
  onRollbackStep,
}: Props) {
  const inputClass = cn(
    "w-full rounded-xl border border-border/70 bg-background px-3 py-2.5 text-sm",
    "focus:outline-none focus:ring-2 focus:ring-amber-500/25"
  );

  if (!req.contractsDomVariant && contractsPerms?.canSelectDomVariant) {
    return (
      <section className="rounded-2xl border border-amber-500/20 bg-surface shadow-sm overflow-hidden">
        <div className="px-5 py-4 border-b border-amber-500/15 bg-gradient-to-r from-amber-500/8 via-transparent to-transparent">
          <h2 className="text-sm font-bold">{t("domVariantSelection")}</h2>
          <p className="text-xs text-foreground/50 mt-1">{t("domVariantSelectionHint")}</p>
        </div>
        <div className="p-4 space-y-3">
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {VARIANT_OPTIONS.map(({ id, hintKey }) => {
              const selected = selectedVariant === id;
              return (
                <button
                  key={id}
                  type="button"
                  disabled={acting}
                  onClick={() => setSelectedVariant(id)}
                  className={cn(
                    "text-left rounded-xl border p-4 transition-all",
                    selected
                      ? "border-amber-500/50 bg-amber-500/10 ring-2 ring-amber-500/20"
                      : "border-border/60 hover:border-amber-500/30 hover:bg-amber-500/5"
                  )}
                >
                  <div className="flex items-start gap-3">
                    <MapPin size={18} className={cn("shrink-0 mt-0.5", selected ? "text-amber-600" : "text-foreground/40")} />
                    <div>
                      <p className="text-sm font-semibold">{contractsDomVariantLabel(id, locale)}</p>
                      <p className="text-xs text-foreground/50 mt-1">{t(hintKey)}</p>
                    </div>
                  </div>
                </button>
              );
            })}
          </div>
          <textarea
            className={cn(inputClass, "min-h-[64px]")}
            placeholder={t("domVariantCommentPlaceholder")}
            value={stepComment}
            onChange={(e) => setStepComment(e.target.value)}
          />
          <Button
            size="sm"
            disabled={acting || !selectedVariant || !stepComment.trim()}
            onClick={onSelectVariant}
          >
            <Layers size={14} className="mr-1.5" />
            {t("startDomWorkflow")}
          </Button>
        </div>
      </section>
    );
  }

  if (!req.contractsDomVariant || !req.contractsDomSteps?.length) return null;

  const steps = req.contractsDomSteps;
  const current = req.contractsDomCurrentStep;
  const totalSteps = steps.length;
  const completed = req.contractsSubPhase === "Completed" || req.phase === "Payment" || req.phase === "Completed";
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
  const canComplete = isCurrent && (contractsPerms?.canCompleteDomStep || contractsPerms?.canCompleteAsContractsAdmin);
  const files = step.files ?? [];
  const approvers = step.approvers ?? [];
  const currentScheduleValue = useMemo(() => {
    if (req.contractsDomVariant === "EShop" && step.number === 5) return req.contractsDomPriceRequestDate?.slice(0, 10) ?? "";
    if (req.contractsDomVariant === "EShop" && step.number === 7) return req.contractsDomDeliveryDueDate?.slice(0, 10) ?? "";
    if (req.contractsDomVariant === "SmallValue" && step.number === 6) return req.contractsDomDeliveryDueDate?.slice(0, 10) ?? "";
    return "";
  }, [req.contractsDomVariant, req.contractsDomPriceRequestDate, req.contractsDomDeliveryDueDate, step.number]);
  const [scheduleDate, setScheduleDate] = useState(currentScheduleValue);

  useEffect(() => {
    setScheduleDate(currentScheduleValue);
  }, [currentScheduleValue, step.number]);

  const canFinish =
    canComplete &&
    stepComment.trim().length > 0 &&
    (!step.requiresUpload || files.length > 0) &&
    (!step.requiresApprovers || step.allApproversApproved) &&
    (!step.requiresContractsAdmin || step.contractsAdminPending) &&
    (!step.requiresScheduleDate || Boolean(currentScheduleValue));

  return (
    <section className="rounded-2xl border border-amber-500/20 bg-surface shadow-sm overflow-hidden">
      <div className="px-5 py-4 border-b border-amber-500/15 bg-gradient-to-r from-amber-500/8 via-transparent to-transparent">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 className="text-sm font-bold">
              {contractsDomWorkflowTitle(req.contractsDomVariant, locale, totalSteps)}
            </h2>
            <p className="text-xs text-foreground/50 mt-1">
              {contractsDomVariantLabel(req.contractsDomVariant, locale)}
              {req.contractsDomVariantSelectedAt && (
                <span className="ml-2 text-foreground/35">
                  · {new Date(req.contractsDomVariantSelectedAt).toLocaleDateString(locale)}
                </span>
              )}
            </p>
          </div>
          <div className="flex items-center gap-2 text-xs">
            <span className="px-2.5 py-1 rounded-full bg-amber-500/12 text-amber-700 dark:text-amber-300 font-semibold">
              {completed ? totalSteps : Math.max(current, operationalStart)} / {totalSteps}
            </span>
            <span className="text-foreground/40">{progress}%</span>
          </div>
        </div>
        <div className="mt-3 h-1.5 rounded-full bg-amber-500/10 overflow-hidden">
          <div className="h-full bg-amber-500/60 rounded-full transition-all" style={{ width: `${progress}%` }} />
        </div>
      </div>

      <div className="p-4 space-y-4">
        <div
          className={cn(
            "rounded-xl border p-4",
            isCurrent ? "border-amber-500/35 bg-amber-500/[0.04]" : "border-border/50",
            isPast && "border-emerald-500/20 bg-emerald-500/[0.03]"
          )}
        >
          <div className="flex gap-3">
            {isPast ? (
              <CheckCircle2 size={20} className="text-emerald-600 shrink-0 mt-0.5" />
            ) : (
              <Circle size={20} className={cn("shrink-0 mt-0.5", isCurrent ? "text-amber-500" : "text-foreground/20")} />
            )}
            <div className="flex-1 min-w-0">
              <p className="text-[10px] font-bold uppercase tracking-wider text-foreground/40">
                {t("step")} {step.number}
              </p>
              <p className="text-sm font-semibold">{contractsDomStepTitle(step, locale)}</p>
              <p className="text-xs text-foreground/55 mt-1.5 leading-relaxed">
                {contractsDomStepHint(step, locale)}
              </p>

              {step.number < operationalStart && (
                <p className="mt-3 text-xs text-emerald-700/90 dark:text-emerald-400/90 border-l-2 border-emerald-500/40 pl-2.5">
                  {t("domPrerequisiteStepDone")}
                </p>
              )}

              {step.requiresScheduleDate && (
                <div className="mt-4 space-y-2 rounded-lg border border-sky-500/20 bg-sky-500/5 p-3">
                  <p className="text-xs font-semibold flex items-center gap-1.5 text-sky-800 dark:text-sky-300">
                    <CalendarDays size={13} />
                    {locale.startsWith("en") ? (step.scheduleLabelEn ?? t("scheduleDate")) : (step.scheduleLabelRu ?? t("scheduleDate"))}
                  </p>
                  {(locale.startsWith("en") ? step.scheduleHintEn : step.scheduleHintRu) && (
                    <p className="text-xs text-foreground/60">
                      {locale.startsWith("en") ? step.scheduleHintEn : step.scheduleHintRu}
                    </p>
                  )}
                  <div className="grid gap-3 sm:grid-cols-[minmax(0,220px)_auto] sm:items-end">
                    <input
                      type="date"
                      className={inputClass}
                      value={scheduleDate}
                      onChange={(e) => setScheduleDate(e.target.value)}
                      disabled={acting || !(isCurrent && contractsPerms?.canScheduleDomStep)}
                    />
                    {isCurrent && contractsPerms?.canScheduleDomStep && (
                      <Button
                        size="sm"
                        disabled={acting || !scheduleDate}
                        onClick={() => onScheduleStep(step.number, scheduleDate)}
                      >
                        {t("saveScheduleDate")}
                      </Button>
                    )}
                  </div>
                  {req.contractsDomVariant === "EShop" && step.number === 5 && req.contractsDomPriceResponseDueDate && (
                    <p className="text-xs text-foreground/60">
                      {t("domPriceDueDate", {
                        date: new Date(req.contractsDomPriceResponseDueDate).toLocaleDateString(locale),
                      })}
                    </p>
                  )}
                </div>
              )}

              {(step.requiresUpload || files.length > 0) && (
                <div className="mt-4 space-y-2 rounded-lg border border-border/50 bg-background/60 p-3">
                  <p className="text-xs font-semibold flex items-center gap-1.5">
                    <FileUp size={13} />
                    {t("domStepUploadDocuments")}
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
                              className="text-amber-700 hover:underline dark:text-amber-300"
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
                  {isCurrent && contractsPerms?.canUploadDomStepFile && (
                    <DocumentFileUpload
                      folder="contracts-dom"
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
                    {t("domStepApprovers")}
                  </p>
                  {approvers.length > 0 ? (
                    <ApproverList approvers={approvers} locale={locale} t={t} />
                  ) : isCurrent && contractsPerms?.canSubmitDomStepApprovers ? (
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
                    <p className="text-xs text-foreground/45">{t("domStepApproversPending")}</p>
                  )}
                  {isCurrent && contractsPerms?.canDecideDomStepApproval && (
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

              {step.requiresContractsAdmin && (
                <div className="mt-4 space-y-2 rounded-lg border border-violet-500/20 bg-violet-500/5 p-3">
                  <p className="text-xs font-semibold flex items-center gap-1.5 text-violet-800 dark:text-violet-300">
                    <Send size={13} />
                    {t("domStepContractsAdmin")}
                  </p>
                  {step.contractsAdminPending ? (
                    <p className="text-xs text-foreground/60">
                      {t("domStepContractsAdminWaiting", {
                        name: req.contractsDomContractsAdminUserName ?? "—",
                      })}
                    </p>
                  ) : isCurrent && contractsPerms?.canSendToContractsAdmin ? (
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
                        onClick={() => onSendToContractsAdmin(step.number)}
                      >
                        {t("sendToContractsAdmin")}
                      </Button>
                    </div>
                  ) : null}
                </div>
              )}

              {step.requiresRegistration && (
                <div className="mt-4 space-y-1.5 rounded-lg border border-emerald-500/20 bg-emerald-500/5 p-3">
                  <label className="text-xs font-semibold">{t("domStepContractRegistration")}</label>
                  {req.contractsDomContractRegistrationNumber ? (
                    <p className="text-sm font-bold tracking-wide text-emerald-800 dark:text-emerald-300">
                      {req.contractsDomContractRegistrationNumber}
                    </p>
                  ) : (
                    <p className="text-xs text-foreground/60">
                      {t("domStepRegistrationAutoHint", {
                        example: contractsDomRegistrationExample(req.contractsDomVariant),
                      })}
                    </p>
                  )}
                </div>
              )}

              {isCurrent && step.allowsReturnToMarketing && contractsPerms?.canReturnDomStepToMarketing && (
                <div className="mt-4 space-y-2 rounded-lg border border-rose-500/20 bg-rose-500/5 p-3">
                  <p className="text-xs font-semibold flex items-center gap-1.5 text-rose-800 dark:text-rose-300">
                    <Undo2 size={13} />
                    {t("returnToMarketing")}
                  </p>
                  <p className="text-xs text-foreground/60">{t("returnToMarketingHint")}</p>
                  <Button
                    size="sm"
                    variant="danger"
                    disabled={acting || !stepComment.trim()}
                    onClick={() => onReturnToMarketing(step.number)}
                  >
                    {t("returnToMarketing")}
                  </Button>
                </div>
              )}

              {isCurrent && step.allowsTerminationRollback && contractsPerms?.canRollbackDomStep && (
                <div className="mt-4 space-y-2 rounded-lg border border-rose-500/20 bg-rose-500/5 p-3">
                  <p className="text-xs font-semibold flex items-center gap-1.5 text-rose-800 dark:text-rose-300">
                    <RotateCcw size={13} />
                    {t("rollbackContract")}
                  </p>
                  <p className="text-xs text-foreground/60">
                    {t("rollbackContractHint", { step: step.rollbackStepNumber ?? "—" })}
                  </p>
                  <Button
                    size="sm"
                    variant="danger"
                    disabled={acting || !stepComment.trim()}
                    onClick={() => onRollbackStep(step.number)}
                  >
                    {t("rollbackContract")}
                  </Button>
                </div>
              )}

              {(canComplete || contractsPerms?.canDecideDomStepApproval) && (
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
                      {contractsPerms?.canCompleteAsContractsAdmin
                        ? t("contractsAdminCompleteStep")
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
          accent="amber"
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
  approvers: ProcurementContractsDomStepApprover[];
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
