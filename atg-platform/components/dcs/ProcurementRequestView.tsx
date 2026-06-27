"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import {
  Activity,
  ArrowRight,
  CheckCircle2,
  Circle,
  Clock,
  FilePlus,
  GitBranch,
  Loader2,
  Map,
  Megaphone,
  ShieldCheck,
  User,
} from "lucide-react";
import { useAuthStore } from "@/store/authStore";
import api, { getApiErrorMessage } from "@/lib/api";
import {
  ProcurementAttachmentKind,
  ProcurementApproverRole,
  ProcurementRequest,
  ProcurementRequestUser,
  ProcurementTopologyNode,
  MarketingBranchType,
  approverRoleLabel,
  attachmentKindLabel,
  branchForMarketingStep,
  marketingStepBranchHint,
  marketingStepHint,
  marketingStepTitle,
  marketingSubPhaseLabel,
  phaseLabel,
  stepTitle,
  timelineActionLabel,
  topologyDept,
  topologyLabel,
  topologyStatusLabel,
} from "@/lib/procurementRequest";
import { deptLabel } from "@/lib/dcs";
import { DocumentStatusBadge } from "@/components/dcs/DocumentBadges";
import { dcsTheme } from "@/components/dcs/dcsTheme";
import { MarketingRecordPanel } from "@/components/dcs/MarketingRecordPanel";
import { MarketingDetailTabs } from "@/components/dcs/MarketingDetailTabs";
import { Button } from "@/components/ui/Button";
import { DocumentFileUpload } from "@/components/dcs/DocumentFileUpload";
import { fileDownloadUrl } from "@/lib/files";
import { cn } from "@/lib/utils";

interface Props {
  documentId: string;
}

type Tab = "overview" | "registration" | "approvers" | "marketing" | "topology" | "timeline";
type ApproverRow = { userId: string; role: ProcurementApproverRole };
type AttachmentRow = { kind: ProcurementAttachmentKind; fileName: string; storageKey?: string };

export function ProcurementRequestView({ documentId }: Props) {
  const t = useTranslations("dcs.request");
  const locale = useLocale();
  const user = useAuthStore((s) => s.user);
  const [req, setReq] = useState<ProcurementRequest | null>(null);
  const [loading, setLoading] = useState(true);
  const [acting, setActing] = useState(false);
  const [actionError, setActionError] = useState("");
  const [tab, setTab] = useState<Tab>("overview");
  const [assignable, setAssignable] = useState<{ id: string; fullName: string }[]>([]);
  const [step9Approvers, setStep9Approvers] = useState<ApproverRow[]>([
    { userId: "", role: "Initiator" },
    { userId: "", role: "TasManager" },
    { userId: "", role: "BmgmcTopManager" },
  ]);
  const [step9Attachments, setStep9Attachments] = useState<AttachmentRow[]>([
    { kind: "TechnicalAssignment", fileName: "" },
    { kind: "MaterialRequisition", fileName: "" },
  ]);
  const [marketingWorkers, setMarketingWorkers] = useState<ProcurementRequestUser[]>([]);
  const [selectedSpecialist, setSelectedSpecialist] = useState("");
  const [marketingComment, setMarketingComment] = useState("");

  const load = useCallback(() => {
    setLoading(true);
    setActionError("");
    api
      .get(`/dcs/procurement-requests/${documentId}`)
      .then((r) => setReq(r.data))
      .catch((err) => setActionError(getApiErrorMessage(err, t("error"))))
      .finally(() => setLoading(false));
  }, [documentId, t]);

  useEffect(() => {
    load();
    api.get("/tasks/assignees").then((r) => setAssignable(r.data));
  }, [load]);

  useEffect(() => {
    if (req?.phase === "Marketing") {
      api
        .get("/dcs/procurement-requests/marketing/workers")
        .then((r) => setMarketingWorkers(r.data))
        .catch(() => setMarketingWorkers([]));
    }
  }, [req?.phase]);

  const activeNode = useMemo(
    () => req?.topology.find((n) => n.status === "Active"),
    [req?.topology]
  );

  const progressPct = useMemo(() => {
    if (!req) return 0;
    const total = req.topology.filter((n) => n.status !== "Skipped").length;
    const done = req.topology.filter((n) => n.status === "Completed").length;
    const active = req.topology.some((n) => n.status === "Active") ? 0.5 : 0;
    return total > 0 ? Math.round(((done + active) / total) * 100) : 0;
  }, [req]);

  const runAction = async (fn: () => Promise<void>) => {
    setActing(true);
    setActionError("");
    try {
      await fn();
      load();
    } catch (err) {
      setActionError(getApiErrorMessage(err, t("error")));
    } finally {
      setActing(false);
    }
  };

  const completeStep = (step: number) => runAction(() => api.post(`/dcs/procurement-requests/${documentId}/steps/${step}/complete`));
  const submitStep9 = () => runAction(() => api.post(`/dcs/procurement-requests/${documentId}/step9/submit`, {
    approvers: step9Approvers.filter((a) => a.userId),
    attachments: step9Attachments.filter((a) => a.fileName.trim() && a.storageKey),
  }));
  const approve = () => runAction(() => api.post(`/dcs/procurement-requests/${documentId}/approve`, {}));
  const reject = () => runAction(() => api.post(`/dcs/procurement-requests/${documentId}/reject`, {}));
  const forwardContracts = () => runAction(() => api.post(`/dcs/procurement-requests/${documentId}/forward-contracts`));
  const completeMarketingStep = (step: number) => runAction(() =>
    api.post(`/dcs/procurement-requests/${documentId}/marketing/steps/${step}/complete`, {
      specialistId: step === 1 ? (selectedSpecialist || req?.marketingSpecialistId || null) : null,
      comment: marketingComment || null,
    })
  );
  const recordMarketingBranch = (branch: MarketingBranchType, resolve: boolean) => runAction(() =>
    api.post(`/dcs/procurement-requests/${documentId}/marketing/branch`, {
      branch,
      resolve,
      comment: marketingComment || null,
    })
  );

  if (loading || !req) {
    return (
      <div className="flex-1 flex flex-col items-center justify-center text-foreground/40 gap-2 px-6">
        {loading ? (
          <>
            <Loader2 className="animate-spin" size={20} />
            <span className="text-sm">{t("loading")}</span>
          </>
        ) : (
          <span className="text-sm text-red-600 text-center">{actionError || t("error")}</span>
        )}
      </div>
    );
  }

  const isTas = req.flow === "TechnicalAffairs";
  const myPendingApproval = user
    ? req.approvers.find((a) => a.userId === user.id && a.status === "Pending")
    : undefined;
  const canForward = req.marketingPermissions?.canForwardToContracts ?? false;
  const marketingPerms = req.marketingPermissions;
  const showMarketingTab = req.phase === "Marketing";

  const tabs: { id: Tab; label: string; icon: typeof Activity }[] = [
    { id: "overview", label: t("tabs.overview"), icon: Activity },
    { id: "registration", label: t("tabs.registration"), icon: ShieldCheck },
    { id: "approvers", label: t("tabs.approvers"), icon: User },
    ...(showMarketingTab ? [{ id: "marketing" as Tab, label: t("tabs.marketing"), icon: Megaphone }] : []),
    { id: "topology", label: t("tabs.topology"), icon: Map },
    { id: "timeline", label: t("tabs.timeline"), icon: GitBranch },
  ];

  return (
    <div className={cn("flex-1 flex flex-col min-h-0", dcsTheme.meshBg)}>
      {/* Enterprise header */}
      <div className="shrink-0 border-b border-slate-200/60 dark:border-white/[0.06] bg-white/75 dark:bg-slate-900/65 backdrop-blur-xl shadow-sm">
        <div className="px-6 py-5 max-w-[1400px] mx-auto">
          <div className="flex flex-col lg:flex-row lg:items-start lg:justify-between gap-4">
            <div className="flex gap-4 min-w-0">
              <div className="w-12 h-12 rounded-2xl bg-gradient-to-br from-sky-500 to-blue-700 flex items-center justify-center shadow-lg shadow-sky-900/20 shrink-0">
                <FilePlus size={22} className="text-white" />
              </div>
              <div className="min-w-0">
                <div className="flex flex-wrap items-center gap-2 mb-1">
                  <span className="font-mono text-sm font-bold text-atg-blue">{req.number}</span>
                  <PhasePill phase={req.phase} locale={locale} />
                  {req.isRegistered && (
                    <span className="text-[10px] font-bold uppercase tracking-wider px-2 py-0.5 rounded-full bg-emerald-500/15 text-emerald-700 dark:text-emerald-400">
                      {t("registeredBadge")}
                    </span>
                  )}
                </div>
                <h1 className="text-lg font-bold text-foreground leading-snug">{req.title}</h1>
                {req.titleRu && (
                  <p className="text-sm text-foreground/55 mt-0.5">{req.titleRu}</p>
                )}
                {activeNode && (
                  <p className="text-xs text-foreground/45 mt-2 flex items-center gap-1.5">
                    <span className="w-1.5 h-1.5 rounded-full bg-sky-500 animate-pulse" />
                    {t("currentStage")}: <span className="font-medium text-foreground/70">{topologyLabel(activeNode, locale)}</span>
                  </p>
                )}
              </div>
            </div>
            <div className="flex items-center gap-3 shrink-0">
              <DocumentStatusBadge status={req.status as "InReview"} />
              <div className="text-right hidden sm:block">
                <p className="text-[10px] font-bold uppercase tracking-wider text-foreground/40">{t("progress")}</p>
                <p className="text-xl font-bold tabular-nums text-atg-blue">{progressPct}%</p>
              </div>
            </div>
          </div>

          {/* Progress bar */}
          <div className="mt-4 h-1.5 rounded-full bg-foreground/[0.06] overflow-hidden">
            <div
              className="h-full rounded-full bg-gradient-to-r from-sky-500 to-blue-600 transition-all duration-700"
              style={{ width: `${progressPct}%` }}
            />
          </div>

          {/* Tabs */}
          <div className="mt-5 flex gap-1 overflow-x-auto pb-0.5 scrollbar-thin">
            {tabs.map(({ id, label, icon: Icon }) => (
              <button
                key={id}
                type="button"
                onClick={() => setTab(id)}
                className={cn(
                  "inline-flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-medium whitespace-nowrap transition-all",
                  tab === id
                    ? "bg-atg-blue text-white shadow-md shadow-atg-blue/25"
                    : "text-foreground/50 hover:text-foreground hover:bg-foreground/[0.04]"
                )}
              >
                <Icon size={15} />
                {label}
              </button>
            ))}
          </div>
        </div>
      </div>

      <div className="flex-1 overflow-auto">
        <div className="px-6 py-6 max-w-[1400px] mx-auto">
          {actionError && (
            <div className="mb-4 rounded-xl border border-red-500/30 bg-red-500/5 px-4 py-3 text-sm text-red-700 dark:text-red-400">
              {actionError}
            </div>
          )}
          {tab === "overview" && (
            <OverviewTab
              req={req}
              locale={locale}
              t={t}
              isTas={isTas}
              acting={acting}
              myPendingApproval={myPendingApproval}
              canForward={!!canForward}
              marketingPerms={marketingPerms}
              marketingWorkers={marketingWorkers}
              selectedSpecialist={selectedSpecialist}
              setSelectedSpecialist={setSelectedSpecialist}
              marketingComment={marketingComment}
              setMarketingComment={setMarketingComment}
              onCompleteMarketingStep={completeMarketingStep}
              onRecordMarketingBranch={recordMarketingBranch}
              step9Approvers={step9Approvers}
              setStep9Approvers={setStep9Approvers}
              step9Attachments={step9Attachments}
              setStep9Attachments={setStep9Attachments}
              assignable={assignable}
              onCompleteStep={completeStep}
              onSubmitStep9={submitStep9}
              onApprove={approve}
              onReject={reject}
              onForward={forwardContracts}
            />
          )}
          {tab === "registration" && <RegistrationTab req={req} locale={locale} t={t} />}
          {tab === "approvers" && <ApproversTab req={req} locale={locale} t={t} acting={acting} myPendingApproval={myPendingApproval} onApprove={approve} onReject={reject} />}
          {tab === "marketing" && (
            <MarketingTab
              req={req}
              locale={locale}
              t={t}
              acting={acting}
              marketingPerms={marketingPerms}
              marketingWorkers={marketingWorkers}
              selectedSpecialist={selectedSpecialist}
              setSelectedSpecialist={setSelectedSpecialist}
              marketingComment={marketingComment}
              setMarketingComment={setMarketingComment}
              onForward={forwardContracts}
              onCompleteMarketingStep={completeMarketingStep}
              onRecordMarketingBranch={recordMarketingBranch}
            />
          )}
          {tab === "topology" && <TopologyTab req={req} locale={locale} t={t} />}
          {tab === "timeline" && <TimelineTab req={req} locale={locale} t={t} />}
        </div>
      </div>
    </div>
  );
}

function PhasePill({ phase, locale }: { phase: ProcurementRequest["phase"]; locale: string }) {
  const colors: Record<string, string> = {
    InProgress: "bg-sky-500/15 text-sky-700 dark:text-sky-300",
    AwaitingApproval: "bg-amber-500/15 text-amber-700 dark:text-amber-300",
    Marketing: "bg-violet-500/15 text-violet-700 dark:text-violet-300",
    Contracts: "bg-indigo-500/15 text-indigo-700 dark:text-indigo-300",
    Completed: "bg-emerald-500/15 text-emerald-700 dark:text-emerald-300",
  };
  return (
    <span className={cn("text-[10px] font-bold uppercase tracking-wider px-2 py-0.5 rounded-full", colors[phase])}>
      {phaseLabel(phase, locale)}
    </span>
  );
}

function OverviewTab({
  req, locale, t, isTas, acting, myPendingApproval, canForward,
  marketingPerms, marketingWorkers, selectedSpecialist, setSelectedSpecialist,
  marketingComment, setMarketingComment,
  step9Approvers, setStep9Approvers, step9Attachments, setStep9Attachments, assignable,
  onCompleteStep, onSubmitStep9, onApprove, onReject, onForward,
  onCompleteMarketingStep, onRecordMarketingBranch,
}: {
  req: ProcurementRequest;
  locale: string;
  t: ReturnType<typeof useTranslations>;
  isTas: boolean;
  acting: boolean;
  myPendingApproval?: ProcurementRequest["approvers"][0];
  canForward: boolean;
  marketingPerms?: ProcurementRequest["marketingPermissions"];
  marketingWorkers: ProcurementRequestUser[];
  selectedSpecialist: string;
  setSelectedSpecialist: (v: string) => void;
  marketingComment: string;
  setMarketingComment: (v: string) => void;
  step9Approvers: ApproverRow[];
  setStep9Approvers: (v: ApproverRow[]) => void;
  step9Attachments: AttachmentRow[];
  setStep9Attachments: (v: AttachmentRow[]) => void;
  assignable: { id: string; fullName: string }[];
  onCompleteStep: (s: number) => void;
  onSubmitStep9: () => void;
  onApprove: () => void;
  onReject: () => void;
  onForward: () => void;
  onCompleteMarketingStep: (step: number) => void;
  onRecordMarketingBranch: (branch: MarketingBranchType, resolve: boolean) => void;
}) {
  return (
    <div className="grid lg:grid-cols-3 gap-6">
      <div className="lg:col-span-2 space-y-6">
        <MiniTopology nodes={req.topology} locale={locale} />

        {isTas && req.phase === "InProgress" && (
          <section className="rounded-2xl border border-border/70 bg-surface shadow-sm overflow-hidden">
            <div className="px-5 py-4 border-b border-border/50 bg-gradient-to-r from-sky-500/5 to-transparent">
              <h2 className="text-sm font-bold">{t("workflowSteps")}</h2>
            </div>
            <ol className="p-5 space-y-3 max-h-[480px] overflow-y-auto">
              {req.steps.map((step) => {
                const done = step.number < req.currentStep;
                const active = step.number === req.currentStep;
                const isStep9 = step.number === 9;
                return (
                  <li key={step.number} className={cn("rounded-xl border p-4", active && "border-sky-500/40 bg-sky-500/5", done && "border-emerald-500/25 bg-emerald-500/5 opacity-80")}>
                    <div className="flex gap-3">
                      {done ? <CheckCircle2 size={18} className="text-emerald-600 shrink-0" /> : <Circle size={18} className={cn("shrink-0", active ? "text-sky-500" : "text-foreground/25")} />}
                      <div className="flex-1">
                        <p className="text-[10px] font-bold text-foreground/40">{t("step")} {step.number}</p>
                        <p className="text-sm">{stepTitle(step, locale)}</p>
                        {active && !isStep9 && step.number <= 8 && (
                          <Button size="sm" className="mt-3" disabled={acting} onClick={() => onCompleteStep(step.number)}>{t("markComplete")}</Button>
                        )}
                        {active && isStep9 && (
                          <Step9Form documentId={req.id} approvers={step9Approvers} setApprovers={setStep9Approvers} attachments={step9Attachments} setAttachments={setStep9Attachments} assignable={assignable} acting={acting} onSubmit={onSubmitStep9} t={t} />
                        )}
                      </div>
                    </div>
                  </li>
                );
              })}
            </ol>
          </section>
        )}

        {req.phase === "AwaitingApproval" && myPendingApproval && (
          <ActionCard title={t("approvalPending")} variant="amber">
            <div className="flex gap-2">
              <Button onClick={onApprove} disabled={acting}>{t("approve")}</Button>
              <Button variant="secondary" onClick={onReject} disabled={acting}>{t("reject")}</Button>
            </div>
          </ActionCard>
        )}

        {req.phase === "Marketing" && req.marketingSubPhase !== "Completed" && (
          <MarketingWorkflowPanel
            req={req}
            locale={locale}
            t={t}
            acting={acting}
            compact
            marketingPerms={marketingPerms}
            marketingWorkers={marketingWorkers}
            selectedSpecialist={selectedSpecialist}
            setSelectedSpecialist={setSelectedSpecialist}
            marketingComment={marketingComment}
            setMarketingComment={setMarketingComment}
            onCompleteMarketingStep={onCompleteMarketingStep}
            onRecordMarketingBranch={onRecordMarketingBranch}
          />
        )}

        {req.phase === "Marketing" && canForward && (
          <ActionCard title={t("forwardContracts")} subtitle={t("forwardContractsHint")} variant="violet">
            <Button onClick={onForward} disabled={acting}>{t("forwardContracts")}</Button>
          </ActionCard>
        )}
      </div>

      <div className="space-y-4">
        <InfoCard title={t("tabs.registration")}>
          <InfoRow label={t("regNumber")} value={req.isRegistered ? req.number : t("pendingReg")} highlight={req.isRegistered} />
          {req.registeredAt && <InfoRow label={t("regDate")} value={new Date(req.registeredAt).toLocaleString(locale)} />}
        </InfoCard>
        <InfoCard title={t("meta")}>
          {isTas && (
            <>
              <InfoRow label={t("eamNumber")} value={req.eamNumber ?? "—"} />
              <InfoRow label={t("initiator")} value={req.initiatorName ?? "—"} />
              {req.dueDate && (
                <InfoRow label={t("deadline")} value={new Date(req.dueDate).toLocaleDateString(locale)} />
              )}
              <InfoRow label={t("responsible")} value={req.assigneeName ?? "—"} />
            </>
          )}
          <InfoRow label={t("department")} value={deptLabel(req.departmentName, req.departmentNameEn, locale)} />
          {req.marketingTaskNumber && <InfoRow label={t("marketingTask")} value={req.marketingTaskNumber} />}
          {req.phase === "Marketing" && (
            <>
              <InfoRow label={t("marketingStep")} value={`${req.marketingCurrentStep} / ${req.marketingSteps.length}`} />
              <InfoRow label={t("marketingSubPhase")} value={marketingSubPhaseLabel(req.marketingSubPhase, locale)} />
            </>
          )}
          {req.marketingSpecialistName && <InfoRow label={t("marketingSpecialist")} value={req.marketingSpecialistName} />}
          {req.contractsTaskNumber && <InfoRow label={t("contractsTask")} value={req.contractsTaskNumber} />}
        </InfoCard>
        {req.attachments.length > 0 && (
          <InfoCard title={t("attachments")}>
            <ul className="space-y-2 text-sm">
              {req.attachments.map((a) => (
                <li key={a.id} className="flex justify-between gap-2">
                  {a.storageKey ? (
                    <a href={fileDownloadUrl(a.storageKey)} className="truncate text-atg-blue hover:underline" target="_blank" rel="noreferrer">
                      {a.fileName}
                    </a>
                  ) : (
                    <span className="truncate">{a.fileName}</span>
                  )}
                  <span className="text-foreground/40 shrink-0">{attachmentKindLabel(a.kind, locale)}</span>
                </li>
              ))}
            </ul>
          </InfoCard>
        )}
      </div>
    </div>
  );
}

function MarketingTab({
  req, locale, t, acting, marketingPerms, marketingWorkers,
  selectedSpecialist, setSelectedSpecialist, marketingComment, setMarketingComment,
  onCompleteMarketingStep, onRecordMarketingBranch, onForward,
}: {
  req: ProcurementRequest;
  locale: string;
  t: ReturnType<typeof useTranslations>;
  acting: boolean;
  marketingPerms?: ProcurementRequest["marketingPermissions"];
  marketingWorkers: ProcurementRequestUser[];
  selectedSpecialist: string;
  setSelectedSpecialist: (v: string) => void;
  marketingComment: string;
  setMarketingComment: (v: string) => void;
  onCompleteMarketingStep: (step: number) => void;
  onRecordMarketingBranch: (branch: MarketingBranchType, resolve: boolean) => void;
  onForward: () => void;
}) {
  return (
    <div className="max-w-3xl space-y-6">
      <div className="rounded-2xl border border-violet-500/25 bg-violet-500/5 p-6">
        <p className="text-[10px] font-bold uppercase tracking-[0.2em] text-foreground/40 mb-2">{t("marketingDept")}</p>
        <h2 className="text-lg font-bold">{t("atMarketing")}</h2>
        <p className="text-sm text-foreground/55 mt-2 leading-relaxed">{t("atMarketingHint")}</p>
        <div className="mt-4 grid sm:grid-cols-3 gap-3 text-sm">
          <div className="rounded-xl border border-border/60 p-4">
            <p className="text-[10px] font-bold uppercase tracking-wider text-foreground/40">{t("marketingStep")}</p>
            <p className="font-semibold mt-1">{req.marketingCurrentStep} / {req.marketingSteps.length}</p>
          </div>
          <div className="rounded-xl border border-border/60 p-4">
            <p className="text-[10px] font-bold uppercase tracking-wider text-foreground/40">{t("marketingSubPhase")}</p>
            <p className="font-semibold mt-1">{marketingSubPhaseLabel(req.marketingSubPhase, locale)}</p>
          </div>
          <div className="rounded-xl border border-border/60 p-4">
            <p className="text-[10px] font-bold uppercase tracking-wider text-foreground/40">{t("marketingSpecialist")}</p>
            <p className="font-semibold mt-1">{req.marketingSpecialistName ?? "—"}</p>
          </div>
        </div>
      </div>
      <MarketingRecordPanel
        documentId={req.id}
        canManage={!!marketingPerms?.canAssign || !!marketingPerms?.canAccept}
      />
      <MarketingDetailTabs
        documentId={req.id}
        canEdit={!!marketingPerms?.canCompleteCurrentStep || !!marketingPerms?.canAssign}
      />
      <MarketingWorkflowPanel
        req={req}
        locale={locale}
        t={t}
        acting={acting}
        marketingPerms={marketingPerms}
        marketingWorkers={marketingWorkers}
        selectedSpecialist={selectedSpecialist}
        setSelectedSpecialist={setSelectedSpecialist}
        marketingComment={marketingComment}
        setMarketingComment={setMarketingComment}
        onCompleteMarketingStep={onCompleteMarketingStep}
        onRecordMarketingBranch={onRecordMarketingBranch}
      />
      {marketingPerms?.canForwardToContracts && (
        <ActionCard title={t("forwardContracts")} subtitle={t("forwardContractsHint")} variant="violet">
          <Button onClick={onForward} disabled={acting}>{t("forwardContracts")}</Button>
        </ActionCard>
      )}
    </div>
  );
}

function MarketingWorkflowPanel({
  req, locale, t, acting, compact, marketingPerms, marketingWorkers,
  selectedSpecialist, setSelectedSpecialist, marketingComment, setMarketingComment,
  onCompleteMarketingStep, onRecordMarketingBranch,
}: {
  req: ProcurementRequest;
  locale: string;
  t: ReturnType<typeof useTranslations>;
  acting: boolean;
  compact?: boolean;
  marketingPerms?: ProcurementRequest["marketingPermissions"];
  marketingWorkers: ProcurementRequestUser[];
  selectedSpecialist: string;
  setSelectedSpecialist: (v: string) => void;
  marketingComment: string;
  setMarketingComment: (v: string) => void;
  onCompleteMarketingStep: (step: number) => void;
  onRecordMarketingBranch: (branch: MarketingBranchType, resolve: boolean) => void;
}) {
  const inputClass = "w-full rounded-lg border border-border/80 bg-background px-3 py-2 text-sm";
  const current = req.marketingCurrentStep;
  const steps = compact
    ? req.marketingSteps.filter((s) => s.number === current)
    : req.marketingSteps;

  return (
    <section className="rounded-2xl border border-border/70 bg-surface shadow-sm overflow-hidden">
      <div className="px-5 py-4 border-b border-border/50 bg-gradient-to-r from-violet-500/5 to-transparent">
        <h2 className="text-sm font-bold">{t("marketingWorkflow")}</h2>
        {!compact && (
          <p className="text-xs text-foreground/50 mt-1">{t("marketingWorkflowHint")}</p>
        )}
      </div>
      <ol className="p-5 space-y-3 max-h-[720px] overflow-y-auto">
        {steps.map((step) => {
          const done = step.number < current || req.marketingSubPhase === "Completed";
          const active = step.number === current && req.marketingSubPhase !== "Completed";
          const branch = branchForMarketingStep(step.number);
          const branchActive = branch && req.marketingActiveBranch === branch;
          return (
            <li
              key={step.number}
              className={cn(
                "rounded-xl border p-4",
                active && "border-violet-500/40 bg-violet-500/5",
                done && "border-emerald-500/25 bg-emerald-500/5 opacity-90"
              )}
            >
              <div className="flex gap-3">
                {done ? (
                  <CheckCircle2 size={18} className="text-emerald-600 shrink-0 mt-0.5" />
                ) : (
                  <Circle size={18} className={cn("shrink-0 mt-0.5", active ? "text-violet-500" : "text-foreground/25")} />
                )}
                <div className="flex-1 min-w-0">
                  <p className="text-[10px] font-bold text-foreground/40">{t("step")} {step.number}</p>
                  <p className="text-sm font-semibold">{marketingStepTitle(step, locale)}</p>
                  <p className="text-xs text-foreground/55 mt-1 leading-relaxed">{marketingStepHint(step, locale)}</p>
                  {step.hasBranch && marketingStepBranchHint(step, locale) && (
                    <p className="text-xs text-amber-700/80 dark:text-amber-400/90 mt-2 leading-relaxed border-l-2 border-amber-500/40 pl-2">
                      {marketingStepBranchHint(step, locale)}
                    </p>
                  )}
                  {branchActive && (
                    <p className="text-xs font-medium text-amber-600 mt-2">{t("branchActive")}</p>
                  )}
                  {active && step.number === 1 && (
                    <div className="mt-3 space-y-2">
                      <select
                        className={inputClass}
                        value={selectedSpecialist || req.marketingSpecialistId || ""}
                        onChange={(e) => setSelectedSpecialist(e.target.value)}
                      >
                        <option value="">{t("selectUser")}</option>
                        {marketingWorkers.map((u) => (
                          <option key={u.id} value={u.id}>
                            {u.fullName} — {locale.startsWith("en") ? u.departmentNameEn : u.departmentName}
                          </option>
                        ))}
                      </select>
                    </div>
                  )}
                  {active && (
                    <div className="mt-3 space-y-2">
                      <textarea
                        className={cn(inputClass, "min-h-[60px]")}
                        placeholder={t("marketingCommentPlaceholder")}
                        value={marketingComment}
                        onChange={(e) => setMarketingComment(e.target.value)}
                      />
                      {step.hasBranch && branch && marketingPerms?.canRecordBranch && (
                        <Button
                          size="sm"
                          variant="secondary"
                          disabled={acting}
                          onClick={() => onRecordMarketingBranch(branch, false)}
                        >
                          {t(`branchRecord.${branch}`)}
                        </Button>
                      )}
                      {step.hasBranch && branch && marketingPerms?.canResolveBranch && branchActive && (
                        <Button
                          size="sm"
                          variant="secondary"
                          disabled={acting}
                          onClick={() => onRecordMarketingBranch(branch, true)}
                        >
                          {t(`branchResolve.${branch}`)}
                        </Button>
                      )}
                      {marketingPerms?.canCompleteCurrentStep && !branchActive && (
                        <Button
                          size="sm"
                          disabled={acting || (step.number === 1 && !(selectedSpecialist || req.marketingSpecialistId))}
                          onClick={() => onCompleteMarketingStep(step.number)}
                        >
                          {t("markComplete")}
                        </Button>
                      )}
                    </div>
                  )}
                </div>
              </div>
            </li>
          );
        })}
      </ol>
    </section>
  );
}

function RegistrationTab({ req, locale, t }: { req: ProcurementRequest; locale: string; t: ReturnType<typeof useTranslations> }) {
  return (
    <div className="max-w-2xl">
      <div className="rounded-2xl border border-border/70 bg-surface shadow-sm overflow-hidden">
        <div className="px-6 py-8 text-center border-b border-border/50 bg-gradient-to-b from-emerald-500/5 to-transparent">
          <div className={cn("w-16 h-16 rounded-2xl mx-auto flex items-center justify-center mb-4", req.isRegistered ? "bg-emerald-500/15" : "bg-foreground/[0.06]")}>
            <ShieldCheck size={32} className={req.isRegistered ? "text-emerald-600" : "text-foreground/30"} />
          </div>
          <p className="text-[10px] font-bold uppercase tracking-[0.2em] text-foreground/40 mb-2">{t("registrationTitle")}</p>
          <p className="font-mono text-2xl font-bold text-foreground">{req.isRegistered ? req.number : t("pendingReg")}</p>
          {req.registeredAt && (
            <p className="text-sm text-foreground/50 mt-2">{t("regDate")}: {new Date(req.registeredAt).toLocaleString(locale)}</p>
          )}
        </div>
        <div className="p-6 space-y-4 text-sm">
          <p className="text-foreground/60 leading-relaxed">{req.isRegistered ? t("registrationDoneHint") : t("registrationPendingHint")}</p>
          <div className="grid sm:grid-cols-2 gap-3">
            <div className="rounded-xl border border-border/60 p-4">
              <p className="text-[10px] font-bold uppercase tracking-wider text-foreground/40">{t("subjectEn")}</p>
              <p className="font-medium mt-1">{req.title}</p>
            </div>
            <div className="rounded-xl border border-border/60 p-4">
              <p className="text-[10px] font-bold uppercase tracking-wider text-foreground/40">{t("subjectRu")}</p>
              <p className="font-medium mt-1">{req.titleRu ?? "—"}</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

function ApproversTab({ req, locale, t, acting, myPendingApproval, onApprove, onReject }: {
  req: ProcurementRequest; locale: string; t: ReturnType<typeof useTranslations>;
  acting: boolean; myPendingApproval?: ProcurementRequest["approvers"][0];
  onApprove: () => void; onReject: () => void;
}) {
  if (req.approvers.length === 0) {
    return <EmptyPanel message={t("noApprovers")} />;
  }
  return (
    <div className="space-y-4 max-w-3xl">
      {req.approvers.map((a, i) => (
        <div key={a.id} className={cn("rounded-2xl border p-5 flex gap-4 items-start", a.status === "Approved" && "border-emerald-500/30 bg-emerald-500/5", a.status === "Pending" && "border-amber-500/30 bg-amber-500/5", a.status === "Rejected" && "border-red-500/30 bg-red-500/5")}>
          <div className="w-10 h-10 rounded-xl bg-foreground/[0.06] flex items-center justify-center font-bold text-sm shrink-0">{i + 1}</div>
          <div className="flex-1 min-w-0">
            <p className="font-semibold">{a.userName}</p>
            <p className="text-sm text-foreground/50">{approverRoleLabel(a.role, locale)}</p>
            {a.comment && <p className="text-sm text-foreground/60 mt-2 italic">&ldquo;{a.comment}&rdquo;</p>}
            {a.decidedAt && <p className="text-xs text-foreground/40 mt-1">{new Date(a.decidedAt).toLocaleString(locale)}</p>}
          </div>
          <ApproverStatusBadge status={a.status} locale={locale} t={t} />
        </div>
      ))}
      {myPendingApproval && (
        <div className="flex gap-2 pt-2">
          <Button onClick={onApprove} disabled={acting}>{t("approve")}</Button>
          <Button variant="secondary" onClick={onReject} disabled={acting}>{t("reject")}</Button>
        </div>
      )}
    </div>
  );
}

function TopologyTab({ req, locale, t }: { req: ProcurementRequest; locale: string; t: ReturnType<typeof useTranslations> }) {
  return (
    <div className="space-y-6">
      <p className="text-sm text-foreground/55 max-w-2xl">{t("topologyHint")}</p>
      <FullTopology nodes={req.topology} locale={locale} />
      <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
        {req.topology.filter((n) => n.status !== "Skipped").map((node) => (
          <TopologyCard key={node.key} node={node} locale={locale} t={t} />
        ))}
      </div>
    </div>
  );
}

function TimelineTab({ req, locale, t }: { req: ProcurementRequest; locale: string; t: ReturnType<typeof useTranslations> }) {
  if (req.timeline.length === 0) return <EmptyPanel message={t("noTimeline")} />;
  return (
    <div className="max-w-2xl">
      <ol className="relative border-l-2 border-atg-blue/20 ml-4 space-y-6">
        {[...req.timeline].reverse().map((ev) => (
          <li key={ev.id} className="ml-6 relative">
            <span className="absolute -left-[1.65rem] top-1 w-3 h-3 rounded-full bg-atg-blue ring-4 ring-background" />
            <div className="rounded-xl border border-border/60 bg-surface p-4 shadow-sm">
              <div className="flex flex-wrap items-center gap-2 mb-1">
                <span className="text-sm font-semibold">{timelineActionLabel(ev.action, locale)}</span>
                <span className="text-xs text-foreground/40">{new Date(ev.createdAt).toLocaleString(locale)}</span>
              </div>
              <p className="text-sm text-foreground/55">{ev.actorName}</p>
              {ev.details && <p className="text-xs text-foreground/45 mt-1 font-mono">{ev.details}</p>}
            </div>
          </li>
        ))}
      </ol>
    </div>
  );
}

function MiniTopology({ nodes, locale }: { nodes: ProcurementTopologyNode[]; locale: string }) {
  const visible = nodes.filter((n) => n.status !== "Skipped");
  return (
    <div className="rounded-2xl border border-border/70 bg-surface p-5 shadow-sm overflow-x-auto">
      <div className="flex items-center gap-0 min-w-max">
        {visible.map((node, i) => (
          <div key={node.key} className="flex items-center">
            <div className={cn("flex flex-col items-center w-28 px-1", node.status === "Active" && "scale-105")}>
              <div className={cn("w-10 h-10 rounded-xl flex items-center justify-center mb-2 transition-all",
                node.status === "Completed" && "bg-emerald-500 text-white shadow-lg shadow-emerald-500/30",
                node.status === "Active" && "bg-sky-500 text-white shadow-lg shadow-sky-500/30 ring-4 ring-sky-500/20",
                node.status === "Pending" && "bg-foreground/[0.08] text-foreground/35")}>
                {node.status === "Completed" ? <CheckCircle2 size={18} /> : node.status === "Active" ? <Clock size={18} /> : <Circle size={18} />}
              </div>
              <p className={cn("text-[10px] font-semibold text-center leading-tight", node.status === "Active" ? "text-sky-600" : "text-foreground/55")}>
                {topologyLabel(node, locale)}
              </p>
            </div>
            {i < visible.length - 1 && (
              <ArrowRight size={14} className={cn("mx-1 shrink-0", visible[i + 1].status !== "Pending" ? "text-emerald-500" : "text-foreground/20")} />
            )}
          </div>
        ))}
      </div>
    </div>
  );
}

function FullTopology({ nodes, locale }: { nodes: ProcurementTopologyNode[]; locale: string }) {
  const visible = nodes.filter((n) => n.status !== "Skipped");
  return (
    <div className="rounded-2xl border border-border/70 bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 p-8 shadow-xl overflow-x-auto">
      <div className="flex items-stretch gap-0 min-w-max">
        {visible.map((node, i) => (
          <div key={node.key} className="flex items-center">
            <div className={cn("relative w-44 rounded-2xl p-4 border transition-all",
              node.status === "Completed" && "bg-emerald-500/20 border-emerald-400/40",
              node.status === "Active" && "bg-sky-500/25 border-sky-400/50 shadow-lg shadow-sky-500/20 scale-105",
              node.status === "Pending" && "bg-white/5 border-white/10 opacity-60")}>
              {node.status === "Active" && <span className="absolute -top-2 left-1/2 -translate-x-1/2 text-[9px] font-bold uppercase tracking-wider px-2 py-0.5 rounded-full bg-sky-400 text-slate-900">Live</span>}
              <p className="text-white font-semibold text-sm leading-snug">{topologyLabel(node, locale)}</p>
              {topologyDept(node, locale) && <p className="text-white/45 text-[11px] mt-2 leading-tight">{topologyDept(node, locale)}</p>}
              {node.assigneeName && <p className="text-white/70 text-xs mt-2 flex items-center gap-1"><User size={11} />{node.assigneeName}</p>}
            </div>
            {i < visible.length - 1 && (
              <div className={cn("w-12 h-0.5 mx-1", visible[i + 1].status !== "Pending" ? "bg-emerald-400" : "bg-white/15")} />
            )}
          </div>
        ))}
      </div>
    </div>
  );
}

function TopologyCard({ node, locale, t }: { node: ProcurementTopologyNode; locale: string; t: ReturnType<typeof useTranslations> }) {
  return (
    <div className={cn("rounded-xl border p-4", node.status === "Active" && "border-sky-500/40 ring-2 ring-sky-500/10")}>
      <div className="flex items-center justify-between gap-2 mb-2">
        <p className="text-sm font-semibold">{topologyLabel(node, locale)}</p>
        <span className="text-[10px] font-bold uppercase text-foreground/45">{topologyStatusLabel(node.status, locale)}</span>
      </div>
      {topologyDept(node, locale) && <p className="text-xs text-foreground/50">{topologyDept(node, locale)}</p>}
      {node.assigneeName && <p className="text-xs text-foreground/60 mt-2">{node.assigneeName}</p>}
      {node.completedAt && <p className="text-[10px] text-foreground/40 mt-2">{new Date(node.completedAt).toLocaleString(locale)}</p>}
    </div>
  );
}

function ApproverStatusBadge({ status, locale, t }: { status: string; locale: string; t: ReturnType<typeof useTranslations> }) {
  const map: Record<string, string> = {
    Approved: "text-emerald-600 bg-emerald-500/10",
    Pending: "text-amber-600 bg-amber-500/10",
    Rejected: "text-red-600 bg-red-500/10",
  };
  const labels: Record<string, string> = {
    Approved: t("statusApproved"),
    Pending: t("statusPending"),
    Rejected: t("statusRejected"),
  };
  return <span className={cn("text-[10px] font-bold uppercase px-2 py-1 rounded-lg shrink-0", map[status])}>{labels[status] ?? status}</span>;
}

function InfoCard({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="rounded-2xl border border-border/70 bg-surface p-4 shadow-sm">
      <h3 className="text-[10px] font-bold uppercase tracking-wider text-foreground/40 mb-3">{title}</h3>
      <div className="space-y-3">{children}</div>
    </div>
  );
}

function InfoRow({ label, value, highlight }: { label: string; value: string; highlight?: boolean }) {
  return (
    <div>
      <p className="text-[10px] text-foreground/40">{label}</p>
      <p className={cn("text-sm font-medium", highlight && "text-emerald-600 font-mono")}>{value}</p>
    </div>
  );
}

function ActionCard({ title, subtitle, variant, children }: { title: string; subtitle?: string; variant: "amber" | "violet"; children: React.ReactNode }) {
  const styles = { amber: "border-amber-500/30 bg-amber-500/5", violet: "border-violet-500/30 bg-violet-500/5" };
  return (
    <div className={cn("rounded-2xl border p-5", styles[variant])}>
      <h2 className="text-sm font-bold mb-1">{title}</h2>
      {subtitle && <p className="text-sm text-foreground/55 mb-4">{subtitle}</p>}
      {children}
    </div>
  );
}

function EmptyPanel({ message }: { message: string }) {
  return <div className="rounded-2xl border border-dashed border-border/60 py-16 text-center text-foreground/40 text-sm">{message}</div>;
}

function Step9Form({
  documentId, approvers, setApprovers, attachments, setAttachments, assignable, acting, onSubmit, t,
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
}) {
  const inputClass = "w-full rounded-lg border border-border/80 bg-background px-2 py-1.5 text-sm";
  return (
    <div className="mt-4 space-y-3 border-t border-border/40 pt-3">
      <p className="text-xs font-semibold text-foreground/50">{t("step9Hint")}</p>
      {attachments.map((a, i) => (
        <div key={i} className="flex gap-2 items-start">
          <select className={cn(inputClass, "w-28")} value={a.kind} onChange={(e) => { const next = [...attachments]; next[i] = { ...next[i], kind: e.target.value as ProcurementAttachmentKind }; setAttachments(next); }}>
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
        <div key={i} className="flex gap-2">
          <span className="text-xs w-28 shrink-0 py-2 text-foreground/50">{approverRoleLabel(a.role, "en")}</span>
          <select className={cn(inputClass, "flex-1")} value={a.userId} onChange={(e) => { const next = [...approvers]; next[i] = { ...next[i], userId: e.target.value }; setApprovers(next); }}>
            <option value="">{t("selectUser")}</option>
            {assignable.map((u) => <option key={u.id} value={u.id}>{u.fullName}</option>)}
          </select>
        </div>
      ))}
      <Button size="sm" disabled={acting} onClick={onSubmit}>{t("submitApproval")}</Button>
    </div>
  );
}
