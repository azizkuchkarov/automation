"use client";

import dynamic from "next/dynamic";
import { useCallback, useEffect, useMemo, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { CheckCircle2, History, Loader2 } from "lucide-react";
import { useAuthStore } from "@/store/authStore";
import api, { getApiErrorMessage } from "@/lib/api";
import {
  ProcurementAttachmentKind,
  ProcurementApproverRole,
  ProcurementRequest,
  ProcurementRequestUser,
  MarketingBranchType,
  ContractsProcurementSectionType,
  ContractsIntProcurementVariant,
  ContractsDomProcurementVariant,
  getNextPendingApprover,
  phaseLabel,
} from "@/lib/procurementRequest";
import { Button } from "@/components/ui/Button";
import { DocumentStatusBadge } from "@/components/dcs/DocumentBadges";
import { MarketingRecordPanel } from "@/components/dcs/MarketingRecordPanel";
import { ProcurementApproversHierarchy } from "@/components/dcs/ProcurementApproversHierarchy";
import { ProcurementMonitoringView } from "@/components/dcs/ProcurementMonitoringView";
import { ProcurementRequestSidebar } from "@/components/dcs/ProcurementRequestSidebar";
import { RequestSummaryCard } from "@/components/dcs/RequestSummaryCard";
import {
  PROCUREMENT_PHASE_ORDER,
  ProcurementPhaseKey,
  ProcurementPhaseStepper,
  overallProgressPercent,
  phaseKeyFromRequest,
} from "@/components/dcs/ProcurementPhaseStepper";
import {
  priorityDotClass,
  priorityLabel,
  type ProcurementPriority,
} from "@/lib/procurementPriority";
import { cn } from "@/lib/utils";

const MarketingWorkflowPanel = dynamic(
  () => import("@/components/dcs/MarketingWorkflowPanel").then((m) => m.MarketingWorkflowPanel),
  { loading: () => <PanelSkeleton /> },
);
const ContractsPhaseTabs = dynamic(
  () => import("@/components/dcs/ContractsPhaseTabs").then((m) => m.ContractsPhaseTabs),
  { loading: () => <PanelSkeleton /> },
);
const TechnicalAffairsWorkflowPanel = dynamic(
  () => import("@/components/dcs/TechnicalAffairsWorkflowPanel").then((m) => m.TechnicalAffairsWorkflowPanel),
  { loading: () => <PanelSkeleton /> },
);

function PanelSkeleton() {
  return <div className="h-24 animate-pulse rounded-xl bg-foreground/5" />;
}

interface Props {
  documentId: string;
}

type SecondaryView = "work" | "history";
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
  const [selectedPhase, setSelectedPhase] = useState<ProcurementPhaseKey>("initiation");
  const [secondaryView, setSecondaryView] = useState<SecondaryView>("work");
  const [phaseSynced, setPhaseSynced] = useState(false);
  const [assignable, setAssignable] = useState<{ id: string; fullName: string }[]>([]);
  const [step6Approvers, setStep6Approvers] = useState<ApproverRow[]>([
    { userId: "", role: "Initiator" },
    { userId: "", role: "TasManager" },
    { userId: "", role: "BmgmcTopManager" },
  ]);
  const [step6Attachments, setStep6Attachments] = useState<AttachmentRow[]>([
    { kind: "TechnicalAssignment", fileName: "" },
    { kind: "MaterialRequisition", fileName: "" },
  ]);
  const [marketingWorkers, setMarketingWorkers] = useState<ProcurementRequestUser[]>([]);
  const [contractsWorkers, setContractsWorkers] = useState<ProcurementRequestUser[]>([]);
  const [paymentWorkers, setPaymentWorkers] = useState<ProcurementRequestUser[]>([]);
  const [approverCandidates, setApproverCandidates] = useState<ProcurementRequestUser[]>([]);
  const [selectedSpecialist, setSelectedSpecialist] = useState("");
  const [selectedEngineer, setSelectedEngineer] = useState("");
  const [selectedPaymentSpecialist, setSelectedPaymentSpecialist] = useState("");
  const [selectedContractsSection, setSelectedContractsSection] = useState<ContractsProcurementSectionType | "">("");
  const [selectedIntVariant, setSelectedIntVariant] = useState<ContractsIntProcurementVariant | "">("");
  const [selectedDomVariant, setSelectedDomVariant] = useState<ContractsDomProcurementVariant | "">("");
  const [selectedApproverIds, setSelectedApproverIds] = useState<string[]>([]);
  const [marketingComment, setMarketingComment] = useState("");
  const [contractsComment, setContractsComment] = useState("");
  const [paymentComment, setPaymentComment] = useState("");

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
          } catch {
            setMarketingWorkers([]);
          }
          setContractsWorkers([]);
        } else if (r.data.phase === "Contracts") {
          setMarketingWorkers([]);
          setPaymentWorkers([]);
          if (r.data.contractsPermissions?.canAssign) {
            try {
              const workers = await api.get("/dcs/procurement-requests/contracts/workers");
              setContractsWorkers(Array.isArray(workers.data) ? workers.data : []);
            } catch {
              setContractsWorkers([]);
            }
          } else {
            setContractsWorkers([]);
          }
          if (
            r.data.contractsIntVariant
            || r.data.contractsDomVariant
            || r.data.contractsPermissions?.canSubmitIntStepApprovers
            || r.data.contractsPermissions?.canSubmitDomStepApprovers
          ) {
            try {
              const users = await api.get("/dcs/procurement-requests/marketing/plan-approver-users");
              setApproverCandidates(Array.isArray(users.data) ? users.data : []);
            } catch {
              setApproverCandidates([]);
            }
          }
        } else if (r.data.phase === "Payment") {
          setMarketingWorkers([]);
          setContractsWorkers([]);
          if (r.data.paymentPermissions?.canAssign) {
            try {
              const workers = await api.get("/dcs/procurement-requests/payment/workers");
              setPaymentWorkers(Array.isArray(workers.data) ? workers.data : []);
            } catch {
              setPaymentWorkers([]);
            }
          } else {
            setPaymentWorkers([]);
          }
        } else {
          setMarketingWorkers([]);
          setContractsWorkers([]);
          setPaymentWorkers([]);
        }
      })
      .catch((err) => setActionError(getApiErrorMessage(err, t("error"))))
      .finally(() => setLoading(false));
  }, [documentId, t]);

  useEffect(() => {
    setPhaseSynced(false);
    setSecondaryView("work");
    load();
    api.get("/tasks/assignees").then((r) => setAssignable(r.data));
  }, [load]);

  useEffect(() => {
    if (!req) return;
    const current = phaseKeyFromRequest(req.phase);
    if (!phaseSynced) {
      setSelectedPhase(current);
      setPhaseSynced(true);
      return;
    }
    setSelectedPhase((prev) => {
      const prevIdx = PROCUREMENT_PHASE_ORDER.indexOf(prev);
      const currentIdx = PROCUREMENT_PHASE_ORDER.indexOf(current);
      // Follow the live phase when it advances past the user's selection.
      if (currentIdx > prevIdx) return current;
      return prev;
    });
  }, [req, phaseSynced]);

  const progressPct = useMemo(() => (req ? overallProgressPercent(req) : 0), [req]);

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
  const submitStep6 = () => runAction(() => api.post(`/dcs/procurement-requests/${documentId}/step6/submit`, {
    approvers: step6Approvers.filter((a) => a.userId),
    attachments: step6Attachments.filter((a) => a.fileName.trim() && a.storageKey),
  }));
  const rejectTas = (comment: string) =>
    runAction(() => api.post(`/dcs/procurement-requests/${documentId}/tas/reject`, { comment }));
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
  const returnMarketingToInitiator = () => {
    const comment = marketingComment.trim();
    if (!comment) {
      setActionError(t("assignCommentRequired"));
      return Promise.resolve();
    }
    return runAction(() =>
      api.post(`/dcs/procurement-requests/${documentId}/marketing/return-to-initiator`, { comment }).then(() => {
        setMarketingComment("");
      })
    );
  };
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
  const routeContractsSection = (sectionOverride?: ContractsProcurementSectionType) => {
    const section = sectionOverride || selectedContractsSection;
    const comment = contractsComment.trim();
    if (!section) {
      setActionError(t("contractsSelectSectionRequired"));
      return Promise.resolve();
    }
    if (!comment) {
      setActionError(t("assignCommentRequired"));
      return Promise.resolve();
    }
    return runAction(() =>
      api.post(`/dcs/procurement-requests/${documentId}/contracts/route-section`, {
        section,
        comment,
      }).then(() => {
        setContractsComment("");
        setSelectedContractsSection("");
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
  const selectIntVariant = () => {
    const variant = selectedIntVariant;
    const comment = contractsComment.trim();
    if (!variant) {
      setActionError(t("intVariantRequired"));
      return Promise.resolve();
    }
    if (!comment) {
      setActionError(t("assignCommentRequired"));
      return Promise.resolve();
    }
    return runAction(() =>
      api.post(`/dcs/procurement-requests/${documentId}/contracts/int/select-variant`, {
        variant,
        comment,
      }).then(() => {
        setContractsComment("");
        setSelectedIntVariant("");
      })
    );
  };
  const completeContractsIntStep = (step: number) => {
    const comment = contractsComment.trim();
    if (!comment) {
      setActionError(t("assignCommentRequired"));
      return Promise.resolve();
    }
    return runAction(() =>
      api.post(`/dcs/procurement-requests/${documentId}/contracts/int/steps/${step}/complete`, {
        comment,
      }).then(() => {
        setContractsComment("");
        setSelectedApproverIds([]);
      })
    );
  };
  const uploadIntStepFile = (step: number, fileName: string, storageKey: string) =>
    runAction(() =>
      api.post(`/dcs/procurement-requests/${documentId}/contracts/int/steps/${step}/files`, {
        fileName,
        storageKey,
      })
    );
  const submitIntStepApprovers = (step: number) =>
    runAction(() =>
      api.post(`/dcs/procurement-requests/${documentId}/contracts/int/steps/${step}/approvers`, {
        userIds: selectedApproverIds,
      }).then(() => setSelectedApproverIds([]))
    );
  const decideIntStepApproval = (step: number, approve: boolean) => {
    const comment = contractsComment.trim();
    if (!approve && !comment) {
      setActionError(t("assignCommentRequired"));
      return Promise.resolve();
    }
    return runAction(() =>
      api.post(`/dcs/procurement-requests/${documentId}/contracts/int/steps/${step}/approvers/decide`, {
        approve,
        comment: comment || null,
      }).then(() => setContractsComment(""))
    );
  };
  const sendToSecretariat = (step: number) => {
    const comment = contractsComment.trim();
    if (!comment) {
      setActionError(t("assignCommentRequired"));
      return Promise.resolve();
    }
    return runAction(() =>
      api.post(`/dcs/procurement-requests/${documentId}/contracts/int/steps/${step}/send-secretariat`, {
        comment,
      }).then(() => setContractsComment(""))
    );
  };
  const selectDomVariant = () => {
    const variant = selectedDomVariant;
    const comment = contractsComment.trim();
    if (!variant) {
      setActionError(t("domVariantRequired"));
      return Promise.resolve();
    }
    if (!comment) {
      setActionError(t("assignCommentRequired"));
      return Promise.resolve();
    }
    return runAction(() =>
      api.post(`/dcs/procurement-requests/${documentId}/contracts/dom/select-variant`, {
        variant,
        comment,
      }).then(() => {
        setContractsComment("");
        setSelectedDomVariant("");
      })
    );
  };
  const completeContractsDomStep = (step: number) => {
    const comment = contractsComment.trim();
    if (!comment) {
      setActionError(t("assignCommentRequired"));
      return Promise.resolve();
    }
    return runAction(() =>
      api.post(`/dcs/procurement-requests/${documentId}/contracts/dom/steps/${step}/complete`, {
        comment,
      }).then(() => {
        setContractsComment("");
        setSelectedApproverIds([]);
      })
    );
  };
  const scheduleDomStep = (step: number, date: string) => {
    if (!date) {
      setActionError(t("dateRequired"));
      return Promise.resolve();
    }
    return runAction(() =>
      api.post(`/dcs/procurement-requests/${documentId}/contracts/dom/steps/${step}/schedule`, {
        date,
        comment: contractsComment.trim() || null,
      }).then(() => setContractsComment(""))
    );
  };
  const uploadDomStepFile = (step: number, fileName: string, storageKey: string) =>
    runAction(() =>
      api.post(`/dcs/procurement-requests/${documentId}/contracts/dom/steps/${step}/files`, {
        fileName,
        storageKey,
      })
    );
  const submitDomStepApprovers = (step: number) =>
    runAction(() =>
      api.post(`/dcs/procurement-requests/${documentId}/contracts/dom/steps/${step}/approvers`, {
        userIds: selectedApproverIds,
      }).then(() => setSelectedApproverIds([]))
    );
  const decideDomStepApproval = (step: number, approve: boolean) => {
    const comment = contractsComment.trim();
    if (!approve && !comment) {
      setActionError(t("assignCommentRequired"));
      return Promise.resolve();
    }
    return runAction(() =>
      api.post(`/dcs/procurement-requests/${documentId}/contracts/dom/steps/${step}/approvers/decide`, {
        approve,
        comment: comment || null,
      }).then(() => setContractsComment(""))
    );
  };
  const sendToContractsAdmin = (step: number) => {
    const comment = contractsComment.trim();
    if (!comment) {
      setActionError(t("assignCommentRequired"));
      return Promise.resolve();
    }
    return runAction(() =>
      api.post(`/dcs/procurement-requests/${documentId}/contracts/dom/steps/${step}/send-contracts-admin`, {
        comment,
      }).then(() => setContractsComment(""))
    );
  };
  const returnDomToMarketing = (step: number) => {
    const comment = contractsComment.trim();
    if (!comment) {
      setActionError(t("assignCommentRequired"));
      return Promise.resolve();
    }
    return runAction(() =>
      api.post(`/dcs/procurement-requests/${documentId}/contracts/dom/steps/${step}/return-marketing`, {
        comment,
      }).then(() => setContractsComment(""))
    );
  };
  const rollbackDomStep = (step: number) => {
    const comment = contractsComment.trim();
    if (!comment) {
      setActionError(t("assignCommentRequired"));
      return Promise.resolve();
    }
    return runAction(() =>
      api.post(`/dcs/procurement-requests/${documentId}/contracts/dom/steps/${step}/rollback`, {
        comment,
      }).then(() => {
        setContractsComment("");
        setSelectedApproverIds([]);
      })
    );
  };
  const assignPayment = () => {
    const comment = paymentComment.trim();
    if (!selectedPaymentSpecialist || !comment) {
      setActionError(t("assignCommentRequired"));
      return Promise.resolve();
    }
    return runAction(() =>
      api.post(`/dcs/procurement-requests/${documentId}/payment/assign`, {
        specialistId: selectedPaymentSpecialist,
        comment,
      }).then(() => {
        setPaymentComment("");
        setSelectedPaymentSpecialist("");
      })
    );
  };
  const acceptPayment = () => {
    const comment = paymentComment.trim();
    if (!comment) {
      setActionError(t("assignCommentRequired"));
      return Promise.resolve();
    }
    return runAction(() =>
      api.post(`/dcs/procurement-requests/${documentId}/payment/accept`, { comment })
        .then(() => setPaymentComment(""))
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
  const currentPhase = phaseKeyFromRequest(req.phase);
  const isLivePhase = selectedPhase === currentPhase;
  const nextPendingApprover = getNextPendingApprover(req.approvers);
  const myPendingApproval =
    user && nextPendingApprover?.userId === user.id ? nextPendingApprover : undefined;
  const marketingPerms = req.marketingPermissions;
  const contractsPerms = req.contractsPermissions;

  const stepperLabels = {
    initiation: {
      label: isTas ? t("phaseStepper.initiationTas") : t("phaseStepper.initiation"),
      hint: isTas ? t("phaseStepper.initiationTasHint") : t("phaseStepper.initiationHint"),
    },
    approval: {
      label: t("phaseStepper.approval"),
      hint: t("phaseStepper.approvalHint"),
    },
    marketing: {
      label: t("phaseStepper.marketing"),
      hint: t("phaseStepper.marketingHint"),
    },
    contracts: {
      label: t("phaseStepper.contracts"),
      hint: t("phaseStepper.contractsHint"),
    },
    contractsLocal: {
      label: t("phaseStepper.contractsLocal"),
      hint: t("phaseStepper.contractsLocalHint"),
    },
    contractsInternational: {
      label: t("phaseStepper.contractsInternational"),
      hint: t("phaseStepper.contractsInternationalHint"),
    },
    payment: {
      label: t("phaseStepper.payment"),
      hint: t("phaseStepper.paymentHint"),
    },
    accountingSupply: {
      label: t("phaseStepper.accountingSupply"),
      hint: t("phaseStepper.accountingSupplyHint"),
    },
    done: {
      label: t("phaseStepper.done"),
      hint: t("phaseStepper.doneHint"),
    },
  };

  const displayTitle = locale.startsWith("en") ? req.title : req.titleRu ?? req.title;
  const priority = (req.priority ?? "Medium") as ProcurementPriority;

  return (
    <div className="flex min-h-0 flex-1 flex-col bg-[#f4f6f8] dark:bg-slate-950">
      {/* Compact header */}
      <div className="shrink-0 border-b border-slate-200 bg-white dark:border-white/[0.06] dark:bg-slate-900/80">
        <div className="mx-auto max-w-[1440px] px-4 py-3 sm:px-6">
          <div className="flex flex-wrap items-center gap-x-3 gap-y-2">
            <span className="font-mono text-sm font-bold text-sky-700 dark:text-sky-400">
              {req.number}
            </span>
            <PhasePill phase={req.phase} locale={locale} />
            <DocumentStatusBadge status={req.status as "InReview"} />
            <span className="inline-flex items-center gap-1.5 rounded-full border border-slate-200 bg-slate-50 px-2 py-0.5 text-[11px] font-semibold text-slate-600 dark:border-white/10 dark:bg-white/[0.04] dark:text-slate-300">
              <span className={cn("h-2 w-2 rounded-full", priorityDotClass(priority))} />
              {priorityLabel(priority, locale)}
            </span>
            <span className="ml-auto text-xs font-semibold tabular-nums text-slate-500">
              {progressPct}%
            </span>
          </div>

          <h1 className="mt-1.5 line-clamp-2 text-base font-semibold leading-snug text-slate-900 dark:text-slate-50">
            {displayTitle}
          </h1>

          <div className="mt-2.5 flex flex-wrap items-center gap-3">
            <ProcurementPhaseStepper
              req={req}
              selected={selectedPhase}
              onSelect={(phase) => {
                setSelectedPhase(phase);
                setSecondaryView("work");
              }}
              labels={stepperLabels}
              isTas={isTas}
            />
            <button
              type="button"
              onClick={() => setSecondaryView(secondaryView === "history" ? "work" : "history")}
              className={cn(
                "ml-auto inline-flex items-center gap-2 rounded-xl border px-3.5 py-2 text-xs font-bold shadow-sm transition-all",
                secondaryView === "history"
                  ? "border-sky-500 bg-sky-600 text-white shadow-sky-500/20 hover:bg-sky-700"
                  : "border-slate-200 bg-white text-slate-600 hover:border-sky-300 hover:bg-sky-50 hover:text-sky-700 dark:border-white/10 dark:bg-white/[0.04] dark:text-slate-300 dark:hover:bg-sky-500/10",
              )}
            >
              <History size={14} />
              {t("historyTab")}
            </button>
          </div>
        </div>
      </div>

      <div className="flex-1 overflow-auto">
        <div className="mx-auto max-w-[1440px] px-4 py-4 sm:px-6">
          {actionError && (
            <div className="mb-3 rounded-lg border border-red-500/30 bg-red-500/5 px-4 py-2.5 text-sm text-red-700 dark:text-red-400">
              {actionError}
            </div>
          )}

          {secondaryView === "history" ? (
            <ProcurementMonitoringView req={req} locale={locale} t={t} />
          ) : (
            <div className="grid gap-4 lg:grid-cols-12">
              <div className="space-y-3 lg:col-span-8">
                {!isLivePhase && selectedPhase !== "done" && (
                  <div className="rounded-lg border border-slate-200 bg-white px-3 py-2 text-xs font-medium text-slate-500 dark:border-white/10 dark:bg-white/[0.03]">
                    {t("phaseReadOnly")}
                  </div>
                )}

                {selectedPhase === "initiation" && (
                  <>
                    {isTas ? (
                      <TechnicalAffairsWorkflowPanel
                        req={req}
                        locale={locale}
                        t={t}
                        acting={acting && isLivePhase}
                        documentId={documentId}
                        step6Approvers={step6Approvers}
                        setStep6Approvers={setStep6Approvers}
                        step6Attachments={step6Attachments}
                        setStep6Attachments={setStep6Attachments}
                        assignable={assignable}
                        onCompleteStep={completeStep}
                        onSubmitStep6={submitStep6}
                        onRejectTas={rejectTas}
                      />
                    ) : (
                      <div className="space-y-3">
                        <RequestSummaryCard req={req} locale={locale} t={t} />
                        <ActionCard title={t("phaseStepper.initiation")} variant="sky">
                          <p className="text-sm text-foreground/60">{t("expressRequestSummary")}</p>
                        </ActionCard>
                      </div>
                    )}
                  </>
                )}

                {selectedPhase === "approval" && (
                  <div className="space-y-3">
                    <RequestSummaryCard
                      req={req}
                      locale={locale}
                      t={t}
                      highlightAction={Boolean(isLivePhase && myPendingApproval)}
                    />
                    <ProcurementApproversHierarchy
                      req={req}
                      locale={locale}
                      t={t}
                      acting={acting && isLivePhase}
                      myPendingApproval={isLivePhase ? myPendingApproval : undefined}
                      onApprove={approve}
                      onReject={reject}
                    />
                  </div>
                )}

                {selectedPhase === "marketing" && (
                  <>
                    {(isLivePhase || req.phase === "Marketing" || ["Contracts", "Completed"].includes(req.phase)) && (
                      <>
                        <MarketingRecordPanel
                          documentId={req.id}
                          canManage={
                            isLivePhase &&
                            (!!marketingPerms?.canAssign || !!marketingPerms?.canAccept)
                          }
                        />
                        {req.marketingSubPhase !== "Completed" || isLivePhase ? (
                          <MarketingWorkflowPanel
                            req={req}
                            locale={locale}
                            t={t}
                            acting={acting && isLivePhase}
                            marketingPerms={isLivePhase ? marketingPerms : undefined}
                            marketingWorkers={marketingWorkers}
                            selectedSpecialist={selectedSpecialist}
                            setSelectedSpecialist={setSelectedSpecialist}
                            stepComment={marketingComment}
                            setStepComment={setMarketingComment}
                            onAssign={assignMarketing}
                            onReturnToInitiator={returnMarketingToInitiator}
                            onAccept={acceptMarketing}
                            onCompleteMarketingStep={completeMarketingStep}
                            onRecordMarketingBranch={recordMarketingBranch}
                            onSubmitPlanApproval={submitPlanApproval}
                            onApprovePlan={approvePlan}
                            onRejectPlan={rejectPlan}
                            onConfirmRegistration={confirmRegistration}
                          />
                        ) : (
                          <ActionCard title={t("phaseStepper.marketing")} variant="violet">
                            <p className="text-sm text-foreground/60">{t("phaseReadOnly")}</p>
                          </ActionCard>
                        )}
                      </>
                    )}
                  </>
                )}

                {selectedPhase === "contracts" && (
                  <ContractsPhaseTabs
                    req={req}
                    locale={locale}
                    t={t}
                    acting={acting && isLivePhase}
                    isLivePhase={isLivePhase}
                    contractsPerms={contractsPerms}
                    contractsWorkers={contractsWorkers}
                    selectedEngineer={selectedEngineer}
                    setSelectedEngineer={setSelectedEngineer}
                    selectedContractsSection={selectedContractsSection}
                    setSelectedContractsSection={setSelectedContractsSection}
                    contractsComment={contractsComment}
                    setContractsComment={setContractsComment}
                    onRouteSection={routeContractsSection}
                    onAssign={assignContracts}
                    onAccept={acceptContracts}
                    selectedIntVariant={selectedIntVariant}
                    setSelectedIntVariant={setSelectedIntVariant}
                    approverCandidates={approverCandidates}
                    selectedApproverIds={selectedApproverIds}
                    setSelectedApproverIds={setSelectedApproverIds}
                    onSelectIntVariant={selectIntVariant}
                    onCompleteContractsIntStep={completeContractsIntStep}
                    onUploadStepFile={uploadIntStepFile}
                    onSubmitApprovers={submitIntStepApprovers}
                    onDecideApproval={decideIntStepApproval}
                    onSendToSecretariat={sendToSecretariat}
                    selectedDomVariant={selectedDomVariant}
                    setSelectedDomVariant={setSelectedDomVariant}
                    onSelectDomVariant={selectDomVariant}
                    onCompleteContractsDomStep={completeContractsDomStep}
                    onScheduleDomStep={scheduleDomStep}
                    onUploadDomStepFile={uploadDomStepFile}
                    onSubmitDomApprovers={submitDomStepApprovers}
                    onDecideDomApproval={decideDomStepApproval}
                    onSendToContractsAdmin={sendToContractsAdmin}
                    onReturnDomToMarketing={returnDomToMarketing}
                    onRollbackDomStep={rollbackDomStep}
                  />
                )}

                {selectedPhase === "payment" && (
                  req.phase === "Payment" ? (
                    <PaymentPhasePanel
                      req={req}
                      t={t}
                      acting={acting && isLivePhase}
                      paymentWorkers={paymentWorkers}
                      selectedSpecialist={selectedPaymentSpecialist}
                      setSelectedSpecialist={setSelectedPaymentSpecialist}
                      stepComment={paymentComment}
                      setStepComment={setPaymentComment}
                      onAssign={assignPayment}
                      onAccept={acceptPayment}
                    />
                  ) : (
                    <UpcomingPhaseCard
                      title={t("phaseStepper.payment")}
                      hint={t("phaseStepper.paymentHint")}
                      lockedHint={t("phaseStepper.upcomingLocked")}
                    />
                  )
                )}

                {selectedPhase === "accountingSupply" && (
                  <UpcomingPhaseCard
                    title={t("phaseStepper.accountingSupply")}
                    hint={t("phaseStepper.accountingSupplyHint")}
                    lockedHint={t("phaseStepper.upcomingLocked")}
                  />
                )}

                {selectedPhase === "done" && (
                  <div className="rounded-2xl border border-emerald-500/25 bg-emerald-500/[0.06] p-8 text-center">
                    <div className="mx-auto mb-3 flex h-14 w-14 items-center justify-center rounded-2xl bg-emerald-500/15 text-emerald-600">
                      <CheckCircle2 size={28} />
                    </div>
                    <h2 className="text-base font-bold text-foreground">{t("phaseStepper.done")}</h2>
                    <p className="mt-1 text-sm text-foreground/55">{t("phaseStepper.doneHint")}</p>
                  </div>
                )}
              </div>

              <div className="lg:col-span-4">
                <ProcurementRequestSidebar req={req} locale={locale} isTas={isTas} t={t} />
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

function PaymentPhasePanel({
  req,
  t,
  acting,
  paymentWorkers,
  selectedSpecialist,
  setSelectedSpecialist,
  stepComment,
  setStepComment,
  onAssign,
  onAccept,
}: {
  req: ProcurementRequest;
  t: ReturnType<typeof useTranslations>;
  acting: boolean;
  paymentWorkers: ProcurementRequestUser[];
  selectedSpecialist: string;
  setSelectedSpecialist: (v: string) => void;
  stepComment: string;
  setStepComment: (v: string) => void;
  onAssign: () => void;
  onAccept: () => void;
}) {
  const inputClass = cn(
    "w-full rounded-xl border border-border/70 bg-background px-3 py-2.5 text-sm",
    "focus:outline-none focus:ring-2 focus:ring-emerald-500/25"
  );
  const perms = req.paymentPermissions;

  return (
    <section className="rounded-2xl border border-emerald-500/20 bg-surface shadow-sm overflow-hidden">
      <div className="px-5 py-4 border-b border-emerald-500/15 bg-gradient-to-r from-emerald-500/8 via-transparent to-transparent">
        <h2 className="text-sm font-bold">{t("phaseStepper.payment")}</h2>
        <p className="text-xs text-foreground/50 mt-1">{t("paymentPhaseHint")}</p>
      </div>
      <div className="p-4 space-y-3">
        {req.paymentSpecialistName && (
          <p className="text-xs text-foreground/60">
            {t("paymentSpecialist")}: <span className="font-semibold">{req.paymentSpecialistName}</span>
            {" · "}
            {req.paymentSubPhase}
          </p>
        )}
        {perms?.canAssign && (
          <div className="space-y-2 rounded-xl border border-emerald-500/20 bg-emerald-500/5 p-4">
            <p className="text-xs font-semibold">{t("assignPaymentSpecialist")}</p>
            <select
              className={inputClass}
              value={selectedSpecialist}
              onChange={(e) => setSelectedSpecialist(e.target.value)}
            >
              <option value="">{t("selectUser")}</option>
              {paymentWorkers.map((u) => (
                <option key={u.id} value={u.id}>{u.fullName}</option>
              ))}
            </select>
            <textarea
              className={cn(inputClass, "min-h-[64px]")}
              placeholder={t("assignCommentPlaceholder")}
              value={stepComment}
              onChange={(e) => setStepComment(e.target.value)}
            />
            <Button size="sm" disabled={acting || !selectedSpecialist || !stepComment.trim()} onClick={onAssign}>
              {t("assignEngineer")}
            </Button>
          </div>
        )}
        {perms?.canAccept && (
          <div className="space-y-2 rounded-xl border border-emerald-500/20 bg-emerald-500/5 p-4">
            <p className="text-xs font-semibold">{t("acceptPaymentTask")}</p>
            <textarea
              className={cn(inputClass, "min-h-[64px]")}
              placeholder={t("acceptCommentPlaceholder")}
              value={stepComment}
              onChange={(e) => setStepComment(e.target.value)}
            />
            <Button size="sm" disabled={acting || !stepComment.trim()} onClick={onAccept}>
              {t("acceptTask")}
            </Button>
          </div>
        )}
        {!perms?.canAssign && !perms?.canAccept && (
          <p className="text-sm text-foreground/55">{t("paymentWaiting")}</p>
        )}
      </div>
    </section>
  );
}

function PhasePill({ phase, locale }: { phase: ProcurementRequest["phase"]; locale: string }) {
  const colors: Record<string, string> = {
    InProgress: "bg-sky-500/15 text-sky-700 dark:text-sky-300",
    AwaitingApproval: "bg-amber-500/15 text-amber-700 dark:text-amber-300",
    Marketing: "bg-violet-500/15 text-violet-700 dark:text-violet-300",
    Contracts: "bg-indigo-500/15 text-indigo-700 dark:text-indigo-300",
    Payment: "bg-emerald-500/15 text-emerald-700 dark:text-emerald-300",
    Completed: "bg-emerald-500/15 text-emerald-700 dark:text-emerald-300",
  };
  return (
    <span className={cn("text-[10px] font-bold uppercase tracking-wider px-2 py-0.5 rounded-full", colors[phase])}>
      {phaseLabel(phase, locale)}
    </span>
  );
}

function ActionCard({ title, subtitle, variant, children }: { title: string; subtitle?: string; variant: "amber" | "violet" | "sky"; children: React.ReactNode }) {
  const styles = {
    amber: "border-amber-500/30 bg-amber-500/5",
    violet: "border-violet-500/30 bg-violet-500/5",
    sky: "border-sky-500/30 bg-sky-500/5",
  };
  return (
    <div className={cn("rounded-2xl border p-5", styles[variant])}>
      <h2 className="text-sm font-bold mb-1">{title}</h2>
      {subtitle && <p className="text-sm text-foreground/55 mb-4">{subtitle}</p>}
      {children}
    </div>
  );
}

function UpcomingPhaseCard({
  title,
  hint,
  lockedHint,
}: {
  title: string;
  hint: string;
  lockedHint: string;
}) {
  return (
    <div className="rounded-xl border border-dashed border-slate-300 bg-white p-8 text-center dark:border-white/15 dark:bg-white/[0.03]">
      <p className="text-[10px] font-bold uppercase tracking-[0.14em] text-slate-400">0%</p>
      <h2 className="mt-2 text-base font-semibold text-slate-800 dark:text-slate-100">{title}</h2>
      <p className="mx-auto mt-2 max-w-md text-sm text-slate-500">{hint}</p>
      <p className="mt-4 text-xs font-medium text-amber-700 dark:text-amber-300">{lockedHint}</p>
    </div>
  );
}
