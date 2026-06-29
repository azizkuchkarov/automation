"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import {
  Activity,
  Clock,
  FilePlus,
  GitBranch,
  Loader2,
  Map,
  Megaphone,
  FileSignature,
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
  MarketingBranchType,
  attachmentKindLabel,
  getNextPendingApprover,
  marketingSubPhaseLabel,
  contractsSubPhaseLabel,
  phaseLabel,
  topologyLabel,
} from "@/lib/procurementRequest";
import { deptLabel } from "@/lib/dcs";
import { DocumentStatusBadge } from "@/components/dcs/DocumentBadges";
import { dcsTheme } from "@/components/dcs/dcsTheme";
import { MarketingRecordPanel } from "@/components/dcs/MarketingRecordPanel";
import { MarketingDetailTabs } from "@/components/dcs/MarketingDetailTabs";
import { ProcurementPhaseOverview } from "@/components/dcs/ProcurementPhaseOverview";
import { MarketingWorkflowPanel } from "@/components/dcs/MarketingWorkflowPanel";
import { ContractsWorkflowPanel } from "@/components/dcs/ContractsWorkflowPanel";
import { TechnicalAffairsWorkflowPanel } from "@/components/dcs/TechnicalAffairsWorkflowPanel";
import { ProcurementApproversHierarchy } from "@/components/dcs/ProcurementApproversHierarchy";
import { ProcurementTopologyView } from "@/components/dcs/ProcurementTopologyView";
import { ProcurementMonitoringView } from "@/components/dcs/ProcurementMonitoringView";
import { Button } from "@/components/ui/Button";
import { fileDownloadUrl } from "@/lib/files";
import { cn } from "@/lib/utils";

interface Props {
  documentId: string;
}

type Tab = "overview" | "registration" | "approvers" | "marketing" | "contracts" | "topology" | "timeline";
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
  const [contractsWorkers, setContractsWorkers] = useState<ProcurementRequestUser[]>([]);
  const [selectedSpecialist, setSelectedSpecialist] = useState("");
  const [selectedEngineer, setSelectedEngineer] = useState("");
  const [marketingComment, setMarketingComment] = useState("");
  const [contractsComment, setContractsComment] = useState("");

  const load = useCallback(() => {
    setLoading(true);
    setActionError("");
    api
      .get(`/dcs/procurement-requests/${documentId}`)
      .then(async (r) => {
        setReq(r.data);
        if (r.data.phase === "Marketing") {
          try {
            const workers = await api.get("/dcs/procurement-requests/marketing/workers");
            setMarketingWorkers(Array.isArray(workers.data) ? workers.data : []);
          } catch (err) {
            setMarketingWorkers([]);
            setActionError(getApiErrorMessage(err, t("marketingWorkersError")));
          }
          setContractsWorkers([]);
        } else if (r.data.phase === "Contracts") {
          setMarketingWorkers([]);
          try {
            const workers = await api.get("/dcs/procurement-requests/contracts/workers");
            setContractsWorkers(Array.isArray(workers.data) ? workers.data : []);
          } catch (err) {
            setContractsWorkers([]);
            setActionError(getApiErrorMessage(err, t("contractsWorkersError")));
          }
        } else {
          setMarketingWorkers([]);
          setContractsWorkers([]);
        }
      })
      .catch((err) => setActionError(getApiErrorMessage(err, t("error"))))
      .finally(() => setLoading(false));
  }, [documentId, t]);

  useEffect(() => {
    load();
    api.get("/tasks/assignees").then((r) => setAssignable(r.data));
  }, [load]);

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

  const completeStep = (step: number, comment: string) =>
    runAction(() =>
      api
        .post(`/dcs/procurement-requests/${documentId}/steps/${step}/complete`, { comment })
    );
  const submitStep9 = () => runAction(() => api.post(`/dcs/procurement-requests/${documentId}/step9/submit`, {
    approvers: step9Approvers.filter((a) => a.userId),
    attachments: step9Attachments.filter((a) => a.fileName.trim() && a.storageKey),
  }));
  const approve = (comment: string) =>
    runAction(() => api.post(`/dcs/procurement-requests/${documentId}/approve`, { comment: comment || null }));
  const reject = (comment: string) =>
    runAction(() => api.post(`/dcs/procurement-requests/${documentId}/reject`, { comment }));

  const completeMarketingStep = (step: number, comment?: string) => runAction(() =>
    api.post(`/dcs/procurement-requests/${documentId}/marketing/steps/${step}/complete`, {
      specialistId: null,
      comment: (comment ?? marketingComment).trim() || null,
    }).then(() => setMarketingComment(""))
  );
  const assignMarketing = () => {
    const specialistId = (selectedSpecialist || req?.marketingSpecialistId || "").trim();
    const comment = marketingComment.trim();
    if (!specialistId) {
      setActionError(t("selectSpecialistRequired"));
      return Promise.resolve();
    }
    if (!comment) {
      setActionError(t("assignCommentRequired"));
      return Promise.resolve();
    }
    return runAction(() =>
      api.post(`/dcs/procurement-requests/${documentId}/marketing/assign`, {
        specialistId,
        comment,
      }).then(() => {
        setMarketingComment("");
        setSelectedSpecialist("");
      })
    );
  };
  const acceptMarketing = () => {
    const comment = marketingComment.trim();
    if (!comment) {
      setActionError(t("acceptCommentRequired"));
      return Promise.resolve();
    }
    return runAction(() =>
      api.post(`/dcs/procurement-requests/${documentId}/marketing/accept`, { comment }).then(() => {
        setMarketingComment("");
      })
    );
  };
  const assignContracts = () => {
    const specialistId = (selectedEngineer || req?.contractsSpecialistId || "").trim();
    const comment = contractsComment.trim();
    if (!specialistId) {
      setActionError(t("selectEngineerRequired"));
      return Promise.resolve();
    }
    if (!comment) {
      setActionError(t("assignCommentRequired"));
      return Promise.resolve();
    }
    return runAction(() =>
      api.post(`/dcs/procurement-requests/${documentId}/contracts/assign`, {
        specialistId,
        comment,
      }).then(() => {
        setContractsComment("");
        setSelectedEngineer("");
      })
    );
  };
  const acceptContracts = () => {
    const comment = contractsComment.trim();
    if (!comment) {
      setActionError(t("acceptCommentRequired"));
      return Promise.resolve();
    }
    return runAction(() =>
      api.post(`/dcs/procurement-requests/${documentId}/contracts/accept`, { comment }).then(() => {
        setContractsComment("");
      })
    );
  };
  const recordMarketingBranch = (branch: MarketingBranchType, resolve: boolean) => runAction(() =>
    api.post(`/dcs/procurement-requests/${documentId}/marketing/branch`, {
      branch,
      resolve,
      comment: marketingComment || null,
    })
  );

  const submitPlanApproval = (approvers: { userId: string; role: string }[]) =>
    runAction(() =>
      api.post(`/dcs/procurement-requests/${documentId}/marketing/plan/submit`, { approvers })
    );
  const approvePlan = (comment: string) =>
    runAction(() =>
      api.post(`/dcs/procurement-requests/${documentId}/marketing/plan/approve`, { comment: comment || null })
    );
  const rejectPlan = (comment: string) =>
    runAction(() =>
      api.post(`/dcs/procurement-requests/${documentId}/marketing/plan/reject`, { comment })
    );
  const confirmRegistration = () => {
    const comment = marketingComment.trim();
    if (!comment) {
      setActionError(t("step9.confirmCommentRequired"));
      return Promise.resolve();
    }
    return runAction(() =>
      api.post(`/dcs/procurement-requests/${documentId}/marketing/register`, { comment })
        .then(() => setMarketingComment(""))
    );
  };

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
  const nextPendingApprover = getNextPendingApprover(req.approvers);
  const myPendingApproval =
    user && nextPendingApprover?.userId === user.id ? nextPendingApprover : undefined;
  const marketingPerms = req.marketingPermissions;
  const contractsPerms = req.contractsPermissions;
  const showMarketingTab = req.phase === "Marketing";
  const showContractsTab = req.phase === "Contracts";

  const tabs: { id: Tab; label: string; icon: typeof Activity }[] = [
    { id: "overview", label: t("tabs.overview"), icon: Activity },
    { id: "registration", label: t("tabs.registration"), icon: ShieldCheck },
    { id: "approvers", label: t("tabs.approvers"), icon: User },
    ...(showMarketingTab ? [{ id: "marketing" as Tab, label: t("tabs.marketing"), icon: Megaphone }] : []),
    ...(showContractsTab ? [{ id: "contracts" as Tab, label: t("tabs.contracts"), icon: FileSignature }] : []),
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
              marketingPerms={marketingPerms}
              contractsPerms={contractsPerms}
              marketingWorkers={marketingWorkers}
              contractsWorkers={contractsWorkers}
              selectedSpecialist={selectedSpecialist}
              setSelectedSpecialist={setSelectedSpecialist}
              selectedEngineer={selectedEngineer}
              setSelectedEngineer={setSelectedEngineer}
              marketingComment={marketingComment}
              setMarketingComment={setMarketingComment}
              contractsComment={contractsComment}
              setContractsComment={setContractsComment}
              onCompleteMarketingStep={completeMarketingStep}
              onRecordMarketingBranch={recordMarketingBranch}
              onSubmitPlanApproval={submitPlanApproval}
              onApprovePlan={approvePlan}
              onRejectPlan={rejectPlan}
              onConfirmRegistration={confirmRegistration}
              onAssign={assignMarketing}
              onAccept={acceptMarketing}
              onAssignContracts={assignContracts}
              onAcceptContracts={acceptContracts}
              documentId={documentId}
              step9Approvers={step9Approvers}
              setStep9Approvers={setStep9Approvers}
              step9Attachments={step9Attachments}
              setStep9Attachments={setStep9Attachments}
              assignable={assignable}
              onCompleteStep={completeStep}
              onSubmitStep9={submitStep9}
            />
          )}
          {tab === "registration" && <RegistrationTab req={req} locale={locale} t={t} />}
          {tab === "approvers" && (
            <ProcurementApproversHierarchy
              req={req}
              locale={locale}
              t={t}
              acting={acting}
              myPendingApproval={myPendingApproval}
              onApprove={approve}
              onReject={reject}
            />
          )}
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
              onCompleteMarketingStep={completeMarketingStep}
              onRecordMarketingBranch={recordMarketingBranch}
              onSubmitPlanApproval={submitPlanApproval}
              onApprovePlan={approvePlan}
              onRejectPlan={rejectPlan}
              onConfirmRegistration={confirmRegistration}
              onAssign={assignMarketing}
              onAccept={acceptMarketing}
            />
          )}
          {tab === "contracts" && (
            <ContractsTab
              req={req}
              locale={locale}
              t={t}
              acting={acting}
              contractsPerms={contractsPerms}
              contractsWorkers={contractsWorkers}
              selectedEngineer={selectedEngineer}
              setSelectedEngineer={setSelectedEngineer}
              contractsComment={contractsComment}
              setContractsComment={setContractsComment}
              onAssign={assignContracts}
              onAccept={acceptContracts}
            />
          )}
          {tab === "topology" && <ProcurementTopologyView nodes={req.topology} locale={locale} hint={t("topologyHint")} />}
          {tab === "timeline" && <ProcurementMonitoringView req={req} locale={locale} t={t} />}
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
  req, locale, t, isTas, acting,
  marketingPerms, contractsPerms, marketingWorkers, contractsWorkers,
  selectedSpecialist, setSelectedSpecialist, selectedEngineer, setSelectedEngineer,
  marketingComment, setMarketingComment, contractsComment, setContractsComment,
  step9Approvers, setStep9Approvers, step9Attachments, setStep9Attachments, assignable,
  documentId,
  onCompleteStep, onSubmitStep9,
  onCompleteMarketingStep, onRecordMarketingBranch,
  onSubmitPlanApproval, onApprovePlan, onRejectPlan, onConfirmRegistration,
  onAssign, onAccept, onAssignContracts, onAcceptContracts,
}: {
  req: ProcurementRequest;
  locale: string;
  t: ReturnType<typeof useTranslations>;
  isTas: boolean;
  acting: boolean;
  marketingPerms?: ProcurementRequest["marketingPermissions"];
  contractsPerms?: ProcurementRequest["contractsPermissions"];
  marketingWorkers: ProcurementRequestUser[];
  contractsWorkers: ProcurementRequestUser[];
  selectedSpecialist: string;
  setSelectedSpecialist: (v: string) => void;
  selectedEngineer: string;
  setSelectedEngineer: (v: string) => void;
  marketingComment: string;
  setMarketingComment: (v: string) => void;
  contractsComment: string;
  setContractsComment: (v: string) => void;
  step9Approvers: ApproverRow[];
  setStep9Approvers: (v: ApproverRow[]) => void;
  step9Attachments: AttachmentRow[];
  setStep9Attachments: (v: AttachmentRow[]) => void;
  assignable: { id: string; fullName: string }[];
  documentId: string;
  onCompleteStep: (s: number, comment: string) => void;
  onSubmitStep9: () => void;
  onCompleteMarketingStep: (step: number, comment?: string) => void;
  onRecordMarketingBranch: (branch: MarketingBranchType, resolve: boolean) => void;
  onSubmitPlanApproval: (approvers: { userId: string; role: string }[]) => void;
  onApprovePlan: (comment: string) => void;
  onRejectPlan: (comment: string) => void;
  onConfirmRegistration: () => void;
  onAssign: () => void;
  onAccept: () => void;
  onAssignContracts: () => void;
  onAcceptContracts: () => void;
}) {
  return (
    <div className="space-y-6">
      <ProcurementPhaseOverview req={req} locale={locale} isTas={isTas} />

      <div className="grid lg:grid-cols-3 gap-6">
      <div className="lg:col-span-2 space-y-6">
        {isTas && req.phase === "InProgress" && (
          <TechnicalAffairsWorkflowPanel
            req={req}
            locale={locale}
            t={t}
            acting={acting}
            documentId={documentId}
            step9Approvers={step9Approvers}
            setStep9Approvers={setStep9Approvers}
            step9Attachments={step9Attachments}
            setStep9Attachments={setStep9Attachments}
            assignable={assignable}
            onCompleteStep={onCompleteStep}
            onSubmitStep9={onSubmitStep9}
          />
        )}

        {req.phase === "AwaitingApproval" && (
          <ActionCard title={t("approvalPending")} variant="amber">
            <p className="text-sm text-foreground/60">{t("approversTabHint")}</p>
          </ActionCard>
        )}

        {req.phase === "Marketing" && req.marketingSubPhase !== "Completed" && (
          <MarketingWorkflowPanel
            req={req}
            locale={locale}
            t={t}
            acting={acting}
            marketingPerms={marketingPerms}
            marketingWorkers={marketingWorkers}
            selectedSpecialist={selectedSpecialist}
            setSelectedSpecialist={setSelectedSpecialist}
            stepComment={marketingComment}
            setStepComment={setMarketingComment}
            onAssign={onAssign}
            onAccept={onAccept}
            onCompleteMarketingStep={onCompleteMarketingStep}
            onRecordMarketingBranch={onRecordMarketingBranch}
            onSubmitPlanApproval={onSubmitPlanApproval}
            onApprovePlan={onApprovePlan}
            onRejectPlan={onRejectPlan}
            onConfirmRegistration={onConfirmRegistration}
          />
        )}

        {req.phase === "Contracts" && req.contractsSubPhase !== "Completed" && (
          <ContractsWorkflowPanel
            req={req}
            locale={locale}
            t={t}
            acting={acting}
            contractsPerms={contractsPerms}
            contractsWorkers={contractsWorkers}
            selectedEngineer={selectedEngineer}
            setSelectedEngineer={setSelectedEngineer}
            stepComment={contractsComment}
            setStepComment={setContractsComment}
            onAssign={onAssignContracts}
            onAccept={onAcceptContracts}
          />
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
          {req.phase === "Contracts" && (
            <InfoRow label={t("contractsSubPhase")} value={contractsSubPhaseLabel(req.contractsSubPhase, locale)} />
          )}
          {req.contractsSpecialistName && <InfoRow label={t("contractsEngineer")} value={req.contractsSpecialistName} />}
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
    </div>
  );
}

function MarketingTab({
  req, locale, t, acting, marketingPerms, marketingWorkers,
  selectedSpecialist, setSelectedSpecialist, marketingComment, setMarketingComment,
  onCompleteMarketingStep, onRecordMarketingBranch,
  onSubmitPlanApproval, onApprovePlan, onRejectPlan, onConfirmRegistration,
  onAssign, onAccept,
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
  onCompleteMarketingStep: (step: number, comment?: string) => void;
  onRecordMarketingBranch: (branch: MarketingBranchType, resolve: boolean) => void;
  onSubmitPlanApproval: (approvers: { userId: string; role: string }[]) => void;
  onApprovePlan: (comment: string) => void;
  onRejectPlan: (comment: string) => void;
  onConfirmRegistration: () => void;
  onAssign: () => void;
  onAccept: () => void;
}) {
  return (
    <div className="space-y-6 max-w-4xl">
      <ProcurementPhaseOverview req={req} locale={locale} isTas={req.flow === "TechnicalAffairs"} />
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
        stepComment={marketingComment}
        setStepComment={setMarketingComment}
        onAssign={onAssign}
        onAccept={onAccept}
        onCompleteMarketingStep={onCompleteMarketingStep}
        onRecordMarketingBranch={onRecordMarketingBranch}
        onSubmitPlanApproval={onSubmitPlanApproval}
        onApprovePlan={onApprovePlan}
        onRejectPlan={onRejectPlan}
        onConfirmRegistration={onConfirmRegistration}
      />
    </div>
  );
}

function ContractsTab({
  req, locale, t, acting, contractsPerms, contractsWorkers,
  selectedEngineer, setSelectedEngineer, contractsComment, setContractsComment,
  onAssign, onAccept,
}: {
  req: ProcurementRequest;
  locale: string;
  t: ReturnType<typeof useTranslations>;
  acting: boolean;
  contractsPerms?: ProcurementRequest["contractsPermissions"];
  contractsWorkers: ProcurementRequestUser[];
  selectedEngineer: string;
  setSelectedEngineer: (v: string) => void;
  contractsComment: string;
  setContractsComment: (v: string) => void;
  onAssign: () => void;
  onAccept: () => void;
}) {
  return (
    <div className="space-y-6 max-w-4xl">
      <ProcurementPhaseOverview req={req} locale={locale} isTas={req.flow === "TechnicalAffairs"} />
      <ContractsWorkflowPanel
        req={req}
        locale={locale}
        t={t}
        acting={acting}
        contractsPerms={contractsPerms}
        contractsWorkers={contractsWorkers}
        selectedEngineer={selectedEngineer}
        setSelectedEngineer={setSelectedEngineer}
        stepComment={contractsComment}
        setStepComment={setContractsComment}
        onAssign={onAssign}
        onAccept={onAccept}
      />
    </div>
  );
}

function RegistrationTab({ req, locale, t }: { req: ProcurementRequest; locale: string; t: ReturnType<typeof useTranslations> }) {
  const marketingRegistered = !!req.marketingPlanRegisteredAt;
  return (
    <div className="max-w-2xl space-y-6">
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

      <div className="rounded-2xl border border-violet-500/20 bg-surface shadow-sm overflow-hidden">
        <div className="px-6 py-8 text-center border-b border-border/50 bg-gradient-to-b from-violet-500/5 to-transparent">
          <div className={cn("w-16 h-16 rounded-2xl mx-auto flex items-center justify-center mb-4", marketingRegistered ? "bg-violet-500/15" : "bg-foreground/[0.06]")}>
            <ShieldCheck size={32} className={marketingRegistered ? "text-violet-600" : "text-foreground/30"} />
          </div>
          <p className="text-[10px] font-bold uppercase tracking-[0.2em] text-foreground/40 mb-2">{t("step9.registrationTitle")}</p>
          <p className="font-mono text-2xl font-bold text-foreground">
            {marketingRegistered ? req.marketingPlanRegistrationNumber : t("step9.pendingNumber")}
          </p>
          {req.marketingPlanRegisteredAt && (
            <p className="text-sm text-foreground/50 mt-2">
              {t("regDate")}: {new Date(req.marketingPlanRegisteredAt).toLocaleString(locale)}
            </p>
          )}
        </div>
        <div className="p-6">
          <p className="text-sm text-foreground/60 leading-relaxed">
            {marketingRegistered ? t("step9.registrationDoneHint") : t("step9.registrationPendingHint")}
          </p>
        </div>
      </div>
    </div>
  );
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
