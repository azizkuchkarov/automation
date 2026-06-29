"use client";

import {
  Building2,
  CheckCircle2,
  Megaphone,
  Scale,
  Wrench,
} from "lucide-react";
import type { useTranslations } from "next-intl";
import {
  APPROVER_ROLE_ORDER,
  ProcurementRequest,
  ProcurementStepComment,
  approverRoleLabel,
  marketingStepTitle,
  stepCommentKindLabel,
  stepTitle,
  timelineActionLabel,
} from "@/lib/procurementRequest";
import {
  MonitoringActivity,
  MonitoringApproverPipeline,
  MonitoringMilestoneList,
  MonitoringStepItem,
  MonitoringStepPipeline,
  MonitoringStepStatus,
} from "@/components/dcs/MonitoringStepPipeline";
import { cn } from "@/lib/utils";

type SectionKey = "registration" | "technicalAffairs" | "approval" | "marketing" | "contracts";

const SECTION_META: Record<
  SectionKey,
  { icon: typeof Wrench; accent: string; border: string; pipelineAccent: "sky" | "violet" | "amber" }
> = {
  registration: {
    icon: CheckCircle2,
    accent: "from-slate-500/10",
    border: "border-border/60",
    pipelineAccent: "sky",
  },
  technicalAffairs: {
    icon: Wrench,
    accent: "from-sky-500/10",
    border: "border-sky-500/25",
    pipelineAccent: "sky",
  },
  approval: {
    icon: Scale,
    accent: "from-amber-500/10",
    border: "border-amber-500/25",
    pipelineAccent: "amber",
  },
  marketing: {
    icon: Megaphone,
    accent: "from-violet-500/10",
    border: "border-violet-500/25",
    pipelineAccent: "violet",
  },
  contracts: {
    icon: Building2,
    accent: "from-emerald-500/10",
    border: "border-emerald-500/25",
    pipelineAccent: "sky",
  },
};

const PROCESS_ORDER: SectionKey[] = [
  "registration",
  "technicalAffairs",
  "approval",
  "marketing",
  "contracts",
];

interface Props {
  req: ProcurementRequest;
  locale: string;
  t: ReturnType<typeof useTranslations>;
}

export function ProcurementMonitoringView({ req, locale, t }: Props) {
  const isTas = req.flow === "TechnicalAffairs";

  const registrationEvents = buildRegistrationActivities(req, locale);
  const taSteps = isTas ? buildTaSteps(req, locale) : [];
  const marketingSteps = buildMarketingSteps(req, locale);
  const contractEvents = buildContractActivities(req, locale);

  const sortedApprovers = [...req.approvers].sort(
    (a, b) => APPROVER_ROLE_ORDER.indexOf(a.role) - APPROVER_ROLE_ORDER.indexOf(b.role)
  );
  const hasApprovers =
    sortedApprovers.some((a) => a.decidedAt || a.comment) ||
    req.timeline.some((e) =>
      ["submitted_for_approval", "approved", "rejected"].includes(e.action)
    );

  const hasAny =
    registrationEvents.length > 0 ||
    taSteps.some((s) => s.activities.length > 0) ||
    hasApprovers ||
    marketingSteps.some((s) => s.activities.length > 0) ||
    contractEvents.length > 0 ||
    isTas ||
    req.phase === "Marketing";

  if (!hasAny) {
    return (
      <div className="rounded-2xl border border-dashed border-border/60 px-6 py-12 text-center text-sm text-foreground/45">
        {t("noTimeline")}
      </div>
    );
  }

  const sectionTitle = (key: SectionKey) => {
    const map: Record<SectionKey, string> = {
      technicalAffairs: t("monitoring.technicalAffairs"),
      approval: t("monitoring.approval"),
      registration: t("monitoring.registration"),
      marketing: t("monitoring.marketing"),
      contracts: t("monitoring.contracts"),
    };
    return map[key];
  };

  const sectionVisible = (key: SectionKey): boolean => {
    switch (key) {
      case "registration":
        return registrationEvents.length > 0 || Boolean(req.registeredAt);
      case "technicalAffairs":
        return isTas;
      case "approval":
        return hasApprovers || req.phase === "AwaitingApproval" || sortedApprovers.length > 0;
      case "marketing":
        return (
          req.phase === "Marketing" ||
          req.phase === "Contracts" ||
          req.phase === "Completed" ||
          marketingSteps.some((s) => s.activities.length > 0)
        );
      case "contracts":
        return contractEvents.length > 0 || req.phase === "Contracts" || req.phase === "Completed";
      default:
        return false;
    }
  };

  return (
    <div className="space-y-5 max-w-5xl">
      <p className="text-sm text-foreground/55">{t("monitoring.hint")}</p>

      {PROCESS_ORDER.filter(sectionVisible).map((key) => {
        const meta = SECTION_META[key];
        const Icon = meta.icon;

        return (
          <section
            key={key}
            className={cn("rounded-2xl border bg-surface shadow-sm overflow-hidden", meta.border)}
          >
            <div
              className={cn(
                "px-5 py-4 border-b border-border/40 bg-gradient-to-r to-transparent",
                meta.accent
              )}
            >
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-xl bg-background/80 border border-border/50 flex items-center justify-center">
                  <Icon size={18} className="text-foreground/60" />
                </div>
                <div>
                  <h3 className="text-sm font-bold">{sectionTitle(key)}</h3>
                  <p className="text-xs text-foreground/45">{t("monitoring.pipelineHint")}</p>
                </div>
              </div>
            </div>

            <div className="p-5">
              {key === "registration" && (
                <MonitoringMilestoneList items={registrationEvents} locale={locale} />
              )}

              {key === "technicalAffairs" && (
                <MonitoringStepPipeline
                  steps={taSteps}
                  accent={meta.pipelineAccent}
                  locale={locale}
                  stepLabel={t("step")}
                  statusCompleted={t("monitoring.statusCompleted")}
                  statusActive={t("monitoring.statusActive")}
                  statusPending={t("monitoring.statusPending")}
                  noActivity={t("monitoring.noStepActivity")}
                />
              )}

              {key === "approval" && (
                <div className="space-y-5">
                  <MonitoringMilestoneList
                    items={req.timeline
                      .filter((e) => e.action === "submitted_for_approval")
                      .map((e) => timelineToActivity(e, locale))}
                    locale={locale}
                  />
                  <MonitoringApproverPipeline
                    approvers={sortedApprovers}
                    locale={locale}
                    roleLabel={(role) => approverRoleLabel(role as never, locale)}
                    statusApproved={t("statusApproved")}
                    statusRejected={t("statusRejected")}
                    statusPending={t("statusPending")}
                    noDepartment={t("approverNoDepartment")}
                  />
                </div>
              )}

              {key === "marketing" && (
                <MonitoringStepPipeline
                  steps={marketingSteps}
                  accent={meta.pipelineAccent}
                  locale={locale}
                  stepLabel={t("step")}
                  statusCompleted={t("monitoring.statusCompleted")}
                  statusActive={t("monitoring.statusActive")}
                  statusPending={t("monitoring.statusPending")}
                  noActivity={t("monitoring.noStepActivity")}
                />
              )}

              {key === "contracts" && (
                <MonitoringMilestoneList items={contractEvents} locale={locale} />
              )}
            </div>
          </section>
        );
      })}
    </div>
  );
}

function timelineToActivity(
  ev: { id: string; action: string; actorName: string; details?: string; createdAt: string },
  locale: string
): MonitoringActivity {
  return {
    id: ev.id,
    type: "event",
    title: timelineActionLabel(ev.action, locale),
    subtitle: ev.actorName,
    body: ev.details,
    at: ev.createdAt,
  };
}

function commentToActivity(c: ProcurementStepComment, locale: string): MonitoringActivity {
  return {
    id: c.id,
    type: "comment",
    title: c.authorName,
    subtitle: stepCommentKindLabel(c.kind, locale),
    body: c.body,
    at: c.createdAt,
    variant:
      c.kind === "StepCompletion" || c.kind === "Acceptance"
        ? "success"
        : c.kind === "Branch"
          ? "warning"
          : "default",
  };
}

function getTaStepStatus(stepNum: number, req: ProcurementRequest): MonitoringStepStatus {
  if (req.flow !== "TechnicalAffairs") return "skipped";
  if (req.phase !== "InProgress") {
    return stepNum <= 9 ? "completed" : "pending";
  }
  if (stepNum < req.currentStep) return "completed";
  if (stepNum === req.currentStep) return "active";
  return "pending";
}

function buildTaSteps(req: ProcurementRequest, locale: string): MonitoringStepItem[] {
  const comments = (req.stepComments ?? []).filter((c) => c.phase === "TechnicalAffairs");
  const stepCompletedEvents = req.timeline
    .filter((e) => e.action === "step_completed")
    .sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());

  const createdEvent = req.timeline.find((e) => e.action === "created");
  const submittedEvent = req.timeline.find((e) => e.action === "submitted_for_approval");

  return req.steps.map((step) => {
    const activities: MonitoringActivity[] = [];

    if (step.number === 1 && createdEvent) {
      activities.push(timelineToActivity(createdEvent, locale));
    }

    for (const c of comments.filter((c) => c.stepNumber === step.number)) {
      activities.push(commentToActivity(c, locale));
    }

    const hasCompletionComment = comments.some(
      (c) => c.stepNumber === step.number && c.kind === "StepCompletion"
    );

    const eventIndex = step.number - 1;
    const stepEvent = stepCompletedEvents[eventIndex];
    if (
      stepEvent &&
      !hasCompletionComment &&
      !activities.some((a) => a.id === stepEvent.id)
    ) {
      activities.push(timelineToActivity(stepEvent, locale));
    }

    if (step.number === 9 && submittedEvent) {
      activities.push(timelineToActivity(submittedEvent, locale));
    }

    activities.sort((a, b) => new Date(a.at).getTime() - new Date(b.at).getTime());

    return {
      number: step.number,
      title: stepTitle(step, locale),
      status: getTaStepStatus(step.number, req),
      activities,
    };
  });
}

function getMarketingStepStatus(stepNum: number, req: ProcurementRequest): MonitoringStepStatus {
  const started =
    req.phase === "Marketing" || req.phase === "Contracts" || req.phase === "Completed";

  if (!started) return "pending";

  if (req.marketingSubPhase === "Completed" || req.phase === "Contracts" || req.phase === "Completed") {
    return "completed";
  }

  if (req.phase !== "Marketing") return "completed";

  if (stepNum < req.marketingCurrentStep) return "completed";
  if (stepNum === req.marketingCurrentStep) return "active";
  return "pending";
}

function parseMarketingStepFromAction(action: string): number | null {
  const m = action.match(/^marketing_step_(\d+)_completed$/);
  return m ? parseInt(m[1], 10) : null;
}

function marketingEventStep(action: string): number | null {
  if (action === "marketing_assigned" || action === "marketing_accepted") return 1;
  if (action === "marketing_completed") return 11;
  return parseMarketingStepFromAction(action);
}

function buildMarketingSteps(req: ProcurementRequest, locale: string): MonitoringStepItem[] {
  const comments = (req.stepComments ?? []).filter((c) => c.phase === "Marketing");
  const marketingEvents = req.timeline.filter((e) =>
    e.action.startsWith("marketing_") ||
    e.action === "handoff_marketing"
  );

  const handoff = marketingEvents.find((e) => e.action === "handoff_marketing");

  return req.marketingSteps.map((step) => {
    const activities: MonitoringActivity[] = [];

    if (step.number === 1 && handoff) {
      activities.push(timelineToActivity(handoff, locale));
    }

    for (const c of comments.filter((c) => c.stepNumber === step.number)) {
      activities.push(commentToActivity(c, locale));
    }

    for (const ev of marketingEvents) {
      const evStep = marketingEventStep(ev.action);
      if (evStep === step.number && ev.action !== "handoff_marketing") {
        const act = timelineToActivity(ev, locale);
        if (!activities.some((a) => a.id === act.id)) {
          activities.push(act);
        }
      }
      if (
        (ev.action === "marketing_branch_recorded" || ev.action === "marketing_branch_resolved") &&
        comments.some((c) => c.stepNumber === step.number && c.kind === "Branch")
      ) {
        const act = timelineToActivity(ev, locale);
        if (!activities.some((a) => a.id === act.id)) {
          activities.push(act);
        }
      }
    }

    activities.sort((a, b) => new Date(a.at).getTime() - new Date(b.at).getTime());

    return {
      number: step.number,
      title: marketingStepTitle(step, locale),
      status: getMarketingStepStatus(step.number, req),
      activities,
    };
  });
}

function buildRegistrationActivities(req: ProcurementRequest, locale: string): MonitoringActivity[] {
  return req.timeline
    .filter((e) => e.action === "registered")
    .map((e) => timelineToActivity(e, locale));
}

function buildContractActivities(req: ProcurementRequest, locale: string): MonitoringActivity[] {
  return req.timeline
    .filter((e) => e.action === "handoff_contracts" || e.action.startsWith("contracts_"))
    .map((e) => timelineToActivity(e, locale))
    .sort((a, b) => new Date(a.at).getTime() - new Date(b.at).getTime());
}
