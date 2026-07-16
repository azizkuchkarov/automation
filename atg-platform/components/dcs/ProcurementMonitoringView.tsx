"use client";

import { useMemo, useState } from "react";
import {
  Building2,
  CheckCircle2,
  ChevronDown,
  Circle,
  CreditCard,
  FileCheck2,
  Megaphone,
  Scale,
  Sparkles,
  Wrench,
} from "lucide-react";
import type { useTranslations } from "next-intl";
import {
  APPROVER_ROLE_ORDER,
  ProcurementRequest,
  ProcurementStepComment,
  approverRoleLabel,
  marketingStepTitle,
  phaseLabel,
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

type JourneyKey =
  | "initiation"
  | "approval"
  | "marketing"
  | "contracts"
  | "payment"
  | "done";

type SectionKey =
  | "registration"
  | "technicalAffairs"
  | "approval"
  | "marketing"
  | "contracts"
  | "payment";

type JourneyStatus = "completed" | "active" | "upcoming";

interface Props {
  req: ProcurementRequest;
  locale: string;
  t: ReturnType<typeof useTranslations>;
}

const JOURNEY_ORDER: JourneyKey[] = [
  "initiation",
  "approval",
  "marketing",
  "contracts",
  "payment",
  "done",
];

const JOURNEY_META: Record<
  JourneyKey,
  { icon: typeof Wrench; color: string; ring: string; bg: string }
> = {
  initiation: {
    icon: Sparkles,
    color: "text-sky-600 dark:text-sky-400",
    ring: "ring-sky-500/25",
    bg: "bg-sky-500/10",
  },
  approval: {
    icon: Scale,
    color: "text-amber-600 dark:text-amber-400",
    ring: "ring-amber-500/25",
    bg: "bg-amber-500/10",
  },
  marketing: {
    icon: Megaphone,
    color: "text-violet-600 dark:text-violet-400",
    ring: "ring-violet-500/25",
    bg: "bg-violet-500/10",
  },
  contracts: {
    icon: Building2,
    color: "text-indigo-600 dark:text-indigo-400",
    ring: "ring-indigo-500/25",
    bg: "bg-indigo-500/10",
  },
  payment: {
    icon: CreditCard,
    color: "text-emerald-600 dark:text-emerald-400",
    ring: "ring-emerald-500/25",
    bg: "bg-emerald-500/10",
  },
  done: {
    icon: CheckCircle2,
    color: "text-slate-600 dark:text-slate-300",
    ring: "ring-slate-400/25",
    bg: "bg-slate-500/10",
  },
};

const SECTION_META: Record<
  SectionKey,
  { icon: typeof Wrench; accent: string; border: string; pipelineAccent: "sky" | "violet" | "amber" }
> = {
  registration: {
    icon: FileCheck2,
    accent: "from-slate-500/10",
    border: "border-slate-200 dark:border-white/10",
    pipelineAccent: "sky",
  },
  technicalAffairs: {
    icon: Wrench,
    accent: "from-sky-500/10",
    border: "border-sky-200 dark:border-sky-500/20",
    pipelineAccent: "sky",
  },
  approval: {
    icon: Scale,
    accent: "from-amber-500/10",
    border: "border-amber-200 dark:border-amber-500/20",
    pipelineAccent: "amber",
  },
  marketing: {
    icon: Megaphone,
    accent: "from-violet-500/10",
    border: "border-violet-200 dark:border-violet-500/20",
    pipelineAccent: "violet",
  },
  contracts: {
    icon: Building2,
    accent: "from-indigo-500/10",
    border: "border-indigo-200 dark:border-indigo-500/20",
    pipelineAccent: "sky",
  },
  payment: {
    icon: CreditCard,
    accent: "from-emerald-500/10",
    border: "border-emerald-200 dark:border-emerald-500/20",
    pipelineAccent: "sky",
  },
};

function journeyKeyFromPhase(phase: ProcurementRequest["phase"]): JourneyKey {
  switch (phase) {
    case "InProgress":
      return "initiation";
    case "AwaitingApproval":
      return "approval";
    case "Marketing":
      return "marketing";
    case "Contracts":
      return "contracts";
    case "Payment":
      return "payment";
    case "Completed":
      return "done";
    default:
      return "initiation";
  }
}

function journeyStatus(key: JourneyKey, current: JourneyKey): JourneyStatus {
  const a = JOURNEY_ORDER.indexOf(key);
  const b = JOURNEY_ORDER.indexOf(current);
  if (a < b) return "completed";
  if (a === b) return "active";
  return "upcoming";
}

function journeyLabel(key: JourneyKey, locale: string, t: ReturnType<typeof useTranslations>) {
  const en = locale.startsWith("en");
  const map: Record<JourneyKey, string> = {
    initiation: en ? "Initiation" : "Инициация",
    approval: en ? "Approval" : "Согласование",
    marketing: t("monitoring.marketing"),
    contracts: t("monitoring.contracts"),
    payment: en ? "Payment" : "Payment",
    done: en ? "Done" : "Завершено",
  };
  return map[key];
}

function journeyMoveTitle(action: string, locale: string): string | null {
  const en = locale.startsWith("en");
  const map: Record<string, { en: string; ru: string }> = {
    created: {
      en: "Request created",
      ru: "Заявка создана",
    },
    submitted_for_approval: {
      en: "Successfully submitted for approval",
      ru: "Успешно отправлено на согласование",
    },
    handoff_marketing: {
      en: "Successfully moved to Marketing Department",
      ru: "Успешно передано в Департамент маркетинга",
    },
    handoff_contracts: {
      en: "Successfully moved to Contracts & Procurement",
      ru: "Успешно передано в Департамент контрактов и закупок",
    },
    contracts_section_routed: {
      en: "Routed to procurement section",
      ru: "Направлено в отдел закупок",
    },
    handed_off_to_payment: {
      en: "Successfully moved to Payment Department",
      ru: "Успешно передано в отдел Payment",
    },
    contracts_int_completed: {
      en: "International contracts workflow completed",
      ru: "Международный контрактный процесс завершён",
    },
    registered: {
      en: "Request registered",
      ru: "Заявка зарегистрирована",
    },
  };
  const row = map[action];
  if (!row) return null;
  return en ? row.en : row.ru;
}

export function ProcurementMonitoringView({ req, locale, t }: Props) {
  const isTas = req.flow === "TechnicalAffairs";
  const currentJourney = journeyKeyFromPhase(req.phase);

  const registrationEvents = buildRegistrationActivities(req, locale);
  const taSteps = isTas ? buildTaSteps(req, locale) : [];
  const marketingSteps = buildMarketingSteps(req, locale);
  const contractEvents = buildContractActivities(req, locale);
  const paymentEvents = buildPaymentActivities(req, locale);

  const sortedApprovers = [...req.approvers].sort(
    (a, b) => APPROVER_ROLE_ORDER.indexOf(a.role) - APPROVER_ROLE_ORDER.indexOf(b.role)
  );
  const hasApprovers =
    sortedApprovers.some((a) => a.decidedAt || a.comment) ||
    req.timeline.some((e) =>
      ["submitted_for_approval", "approved", "rejected"].includes(e.action)
    );

  const journeyMoves = useMemo(() => {
    const actions = new Set([
      "created",
      "submitted_for_approval",
      "registered",
      "handoff_marketing",
      "handoff_contracts",
      "contracts_section_routed",
      "contracts_int_completed",
      "handed_off_to_payment",
    ]);
    return [...req.timeline]
      .filter((e) => actions.has(e.action))
      .sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime())
      .map((e) => ({
        id: e.id,
        action: e.action,
        title: journeyMoveTitle(e.action, locale) ?? timelineActionLabel(e.action, locale),
        actor: e.actorName,
        details: e.details,
        at: e.createdAt,
      }));
  }, [req.timeline, locale]);

  const PROCESS_ORDER: SectionKey[] = [
    "registration",
    "technicalAffairs",
    "approval",
    "marketing",
    "contracts",
    "payment",
  ];

  const sectionCtx = {
    registrationEvents,
    isTas,
    hasApprovers,
    req,
    sortedApprovers,
    marketingSteps,
    contractEvents,
    paymentEvents,
  };

  const [expanded, setExpanded] = useState<Record<SectionKey, boolean>>(() => {
    const initial = {} as Record<SectionKey, boolean>;
    for (const key of PROCESS_ORDER) initial[key] = false;
    return initial;
  });

  const toggleSection = (key: SectionKey) =>
    setExpanded((prev) => ({ ...prev, [key]: !prev[key] }));

  const sectionTitle = (key: SectionKey) => {
    const map: Record<SectionKey, string> = {
      technicalAffairs: t("monitoring.technicalAffairs"),
      approval: t("monitoring.approval"),
      registration: t("monitoring.registration"),
      marketing: t("monitoring.marketing"),
      contracts: t("monitoring.contracts"),
      payment: t("monitoring.payment"),
    };
    return map[key];
  };

  const completedCount = JOURNEY_ORDER.filter(
    (k) => journeyStatus(k, currentJourney) === "completed"
  ).length;

  return (
    <div className="mx-auto max-w-5xl space-y-5">
      {/* Journey overview */}
      <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-white/10 dark:bg-slate-900/70">
        <div className="border-b border-slate-100 bg-gradient-to-r from-sky-500/[0.07] via-transparent to-emerald-500/[0.06] px-5 py-4 dark:border-white/[0.06]">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <p className="text-[10px] font-bold uppercase tracking-[0.16em] text-sky-700 dark:text-sky-300">
                {t("monitoring.journeyTitle")}
              </p>
              <h2 className="mt-1 text-base font-bold text-slate-900 dark:text-slate-50">
                {t("monitoring.journeySubtitle")}
              </h2>
              <p className="mt-1 text-xs text-slate-500">{t("monitoring.hint")}</p>
            </div>
            <div className="rounded-xl border border-slate-200 bg-white px-3 py-2 text-right dark:border-white/10 dark:bg-white/[0.04]">
              <p className="text-[10px] font-semibold uppercase tracking-wide text-slate-400">
                {t("monitoring.stagesDone")}
              </p>
              <p className="text-lg font-bold tabular-nums text-slate-800 dark:text-slate-100">
                {completedCount}
                <span className="text-sm font-semibold text-slate-400"> / {JOURNEY_ORDER.length}</span>
              </p>
            </div>
          </div>
        </div>

        <div className="px-4 py-5 sm:px-6">
          <div className="flex flex-col gap-0">
            {JOURNEY_ORDER.map((key, index) => {
              const status = journeyStatus(key, currentJourney);
              const meta = JOURNEY_META[key];
              const Icon = meta.icon;
              const isLast = index === JOURNEY_ORDER.length - 1;
              return (
                <div key={key} className="flex gap-4">
                  <div className="flex w-10 flex-col items-center">
                    <div
                      className={cn(
                        "flex h-10 w-10 items-center justify-center rounded-2xl ring-4 ring-white dark:ring-slate-900",
                        status === "completed" && "bg-emerald-500 text-white",
                        status === "active" && cn(meta.bg, meta.color, "ring-2", meta.ring),
                        status === "upcoming" && "bg-slate-100 text-slate-400 dark:bg-white/[0.06]"
                      )}
                    >
                      {status === "completed" ? <CheckCircle2 size={18} /> : <Icon size={18} />}
                    </div>
                    {!isLast && (
                      <div
                        className={cn(
                          "my-1 w-0.5 flex-1 min-h-[28px] rounded-full",
                          status === "completed" ? "bg-emerald-400/70" : "bg-slate-200 dark:bg-white/10"
                        )}
                      />
                    )}
                  </div>
                  <div className={cn("min-w-0 flex-1 pb-5", isLast && "pb-0")}>
                    <div
                      className={cn(
                        "rounded-xl border px-4 py-3 transition-colors",
                        status === "active" &&
                          "border-sky-300 bg-sky-50/80 shadow-sm dark:border-sky-500/30 dark:bg-sky-500/10",
                        status === "completed" &&
                          "border-emerald-200/80 bg-emerald-50/40 dark:border-emerald-500/20 dark:bg-emerald-500/[0.06]",
                        status === "upcoming" &&
                          "border-slate-200 bg-slate-50/50 dark:border-white/[0.06] dark:bg-white/[0.02]"
                      )}
                    >
                      <div className="flex flex-wrap items-center gap-2">
                        <p
                          className={cn(
                            "text-sm font-bold",
                            status === "upcoming" ? "text-slate-400" : "text-slate-900 dark:text-slate-50"
                          )}
                        >
                          {journeyLabel(key, locale, t)}
                        </p>
                        <span
                          className={cn(
                            "rounded-full px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide",
                            status === "completed" && "bg-emerald-500/15 text-emerald-700 dark:text-emerald-300",
                            status === "active" && "bg-sky-500/15 text-sky-700 dark:text-sky-300",
                            status === "upcoming" && "bg-slate-200/80 text-slate-500 dark:bg-white/10 dark:text-slate-400"
                          )}
                        >
                          {status === "completed"
                            ? t("monitoring.statusCompleted")
                            : status === "active"
                              ? t("monitoring.statusActive")
                              : t("monitoring.statusPending")}
                        </span>
                      </div>
                      {status === "active" && (
                        <p className="mt-1 text-xs text-slate-500">
                          {phaseLabel(req.phase, locale)} · {req.number}
                        </p>
                      )}
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </section>

      {/* Department move events */}
      <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-white/10 dark:bg-slate-900/70">
        <div className="border-b border-slate-100 px-5 py-4 dark:border-white/[0.06]">
          <p className="text-[10px] font-bold uppercase tracking-[0.16em] text-emerald-700 dark:text-emerald-300">
            {t("monitoring.movesTitle")}
          </p>
          <h3 className="mt-1 text-sm font-bold text-slate-900 dark:text-slate-50">
            {t("monitoring.movesSubtitle")}
          </h3>
        </div>
        <div className="p-4 sm:p-5">
          {journeyMoves.length === 0 ? (
            <div className="rounded-xl border border-dashed border-slate-200 px-4 py-8 text-center text-sm text-slate-400 dark:border-white/10">
              {t("noTimeline")}
            </div>
          ) : (
            <ol className="relative space-y-3 before:absolute before:left-[19px] before:top-3 before:bottom-3 before:w-px before:bg-gradient-to-b before:from-emerald-400/50 before:via-sky-300/40 before:to-slate-200 dark:before:to-white/10">
              {journeyMoves.map((move, index) => (
                <li key={move.id} className="relative flex gap-3 pl-0">
                  <div className="relative z-[1] flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-white ring-4 ring-white dark:bg-slate-900 dark:ring-slate-900">
                    <span className="flex h-8 w-8 items-center justify-center rounded-full bg-emerald-500 text-white shadow-sm">
                      {index === journeyMoves.length - 1 ? (
                        <Circle size={14} className="fill-white" />
                      ) : (
                        <CheckCircle2 size={16} />
                      )}
                    </span>
                  </div>
                  <div className="min-w-0 flex-1 rounded-xl border border-emerald-200/70 bg-gradient-to-r from-emerald-50/80 to-white px-4 py-3 shadow-sm dark:border-emerald-500/20 dark:from-emerald-500/[0.08] dark:to-transparent">
                    <p className="text-sm font-bold text-slate-900 dark:text-slate-50">{move.title}</p>
                    {move.details && (
                      <p className="mt-1 text-xs leading-relaxed text-slate-600 dark:text-slate-300">
                        {move.details}
                      </p>
                    )}
                    <div className="mt-2 flex flex-wrap items-center gap-2 text-[11px] text-slate-400">
                      <span className="font-semibold text-slate-500 dark:text-slate-300">{move.actor}</span>
                      <span>·</span>
                      <span className="tabular-nums">
                        {new Date(move.at).toLocaleString(locale, {
                          day: "2-digit",
                          month: "short",
                          year: "numeric",
                          hour: "2-digit",
                          minute: "2-digit",
                        })}
                      </span>
                    </div>
                  </div>
                </li>
              ))}
            </ol>
          )}
        </div>
      </section>

      {/* Department detail sections */}
      <div className="space-y-3">
        <p className="px-1 text-[10px] font-bold uppercase tracking-[0.14em] text-slate-400">
          {t("monitoring.detailsTitle")}
        </p>
        {PROCESS_ORDER.filter((key) => isSectionVisible(key, sectionCtx)).map((key) => {
          const meta = SECTION_META[key];
          const Icon = meta.icon;
          const isOpen = expanded[key];
          const summary = sectionSummary(
            key,
            { taSteps, marketingSteps, registrationEvents, contractEvents, paymentEvents, sortedApprovers, req },
            t
          );

          return (
            <section
              key={key}
              className={cn(
                "overflow-hidden rounded-2xl border bg-white shadow-sm dark:bg-slate-900/70",
                meta.border
              )}
            >
              <button
                type="button"
                onClick={() => toggleSection(key)}
                className={cn(
                  "w-full px-5 py-4 text-left bg-gradient-to-r to-transparent transition-colors",
                  "hover:bg-slate-50/80 focus:outline-none focus-visible:ring-2 focus-visible:ring-sky-500/30 dark:hover:bg-white/[0.03]",
                  isOpen && "border-b border-slate-100 dark:border-white/[0.06]",
                  meta.accent
                )}
                aria-expanded={isOpen}
              >
                <div className="flex items-center gap-3">
                  <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl border border-slate-200 bg-white dark:border-white/10 dark:bg-white/[0.04]">
                    <Icon size={18} className="text-slate-600 dark:text-slate-300" />
                  </div>
                  <div className="min-w-0 flex-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <h3 className="text-sm font-bold text-slate-900 dark:text-slate-50">
                        {sectionTitle(key)}
                      </h3>
                      {summary && (
                        <span className="rounded-full bg-slate-100 px-2 py-0.5 text-[10px] font-semibold text-slate-500 dark:bg-white/10 dark:text-slate-300">
                          {summary}
                        </span>
                      )}
                    </div>
                    <p className="text-xs text-slate-400">
                      {isOpen ? t("monitoring.pipelineHint") : t("monitoring.clickToExpand")}
                    </p>
                  </div>
                  <ChevronDown
                    size={18}
                    className={cn(
                      "shrink-0 text-slate-400 transition-transform duration-200",
                      isOpen && "rotate-180"
                    )}
                  />
                </div>
              </button>

              {isOpen && (
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
                  {key === "payment" && (
                    <MonitoringMilestoneList items={paymentEvents} locale={locale} />
                  )}
                </div>
              )}
            </section>
          );
        })}
      </div>
    </div>
  );
}

function isSectionVisible(
  key: SectionKey,
  ctx: {
    registrationEvents: MonitoringActivity[];
    isTas: boolean;
    hasApprovers: boolean;
    req: ProcurementRequest;
    sortedApprovers: ProcurementRequest["approvers"];
    marketingSteps: MonitoringStepItem[];
    contractEvents: MonitoringActivity[];
    paymentEvents: MonitoringActivity[];
  }
): boolean {
  const { registrationEvents, isTas, hasApprovers, req, sortedApprovers, marketingSteps, contractEvents, paymentEvents } =
    ctx;
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
        req.phase === "Payment" ||
        req.phase === "Completed" ||
        marketingSteps.some((s) => s.activities.length > 0)
      );
    case "contracts":
      return (
        contractEvents.length > 0 ||
        req.phase === "Contracts" ||
        req.phase === "Payment" ||
        req.phase === "Completed"
      );
    case "payment":
      return paymentEvents.length > 0 || req.phase === "Payment" || req.phase === "Completed";
    default:
      return false;
  }
}

function sectionSummary(
  key: SectionKey,
  ctx: {
    taSteps: MonitoringStepItem[];
    marketingSteps: MonitoringStepItem[];
    registrationEvents: MonitoringActivity[];
    contractEvents: MonitoringActivity[];
    paymentEvents: MonitoringActivity[];
    sortedApprovers: ProcurementRequest["approvers"];
    req: ProcurementRequest;
  },
  t: ReturnType<typeof useTranslations>
): string | null {
  const { taSteps, marketingSteps, registrationEvents, contractEvents, paymentEvents, sortedApprovers, req } = ctx;
  switch (key) {
    case "technicalAffairs": {
      const total = taSteps.length;
      const done = taSteps.filter((s) => s.status === "completed").length;
      const active = taSteps.find((s) => s.status === "active");
      if (active) return `${t("monitoring.statusActive")}: ${t("step")} ${active.number}`;
      return `${done} / ${total}`;
    }
    case "marketing": {
      const total = marketingSteps.length;
      const done = marketingSteps.filter((s) => s.status === "completed").length;
      const active = marketingSteps.find((s) => s.status === "active");
      if (active) return `${t("monitoring.statusActive")}: ${t("step")} ${active.number}`;
      if (done === 0 && req.phase === "Marketing") return t("monitoring.statusActive");
      return `${done} / ${total}`;
    }
    case "approval": {
      const decided = sortedApprovers.filter((a) => a.status !== "Pending").length;
      const total = sortedApprovers.length;
      if (total === 0) return null;
      return `${decided} / ${total}`;
    }
    case "registration":
      return registrationEvents.length > 0 || req.registeredAt ? t("monitoring.statusCompleted") : null;
    case "contracts":
      return contractEvents.length > 0 ? `${contractEvents.length}` : null;
    case "payment":
      return paymentEvents.length > 0
        ? `${paymentEvents.length}`
        : req.phase === "Payment"
          ? t("monitoring.statusActive")
          : null;
    default:
      return null;
  }
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
      c.kind === "StepCompletion" || c.kind === "Acceptance" || c.kind === "Assignment"
        ? "success"
        : c.kind === "Branch"
          ? "warning"
          : "default",
  };
}

function hasStepComment(
  comments: ProcurementStepComment[],
  stepNumber: number,
  kind: ProcurementStepComment["kind"]
): boolean {
  return comments.some((c) => c.stepNumber === stepNumber && c.kind === kind);
}

function shouldSkipMarketingTimelineEvent(
  action: string,
  comments: ProcurementStepComment[]
): boolean {
  if (action === "marketing_assigned") return hasStepComment(comments, 1, "Assignment");
  if (action === "marketing_accepted") return hasStepComment(comments, 1, "Acceptance");
  const stepMatch = action.match(/^marketing_step_(\d+)_completed$/);
  if (stepMatch) {
    return hasStepComment(comments, parseInt(stepMatch[1], 10), "StepCompletion");
  }
  return false;
}

function getTaStepStatus(stepNum: number, req: ProcurementRequest): MonitoringStepStatus {
  if (req.flow !== "TechnicalAffairs") return "skipped";
  const totalSteps = req.steps.length;
  const tasFinished =
    req.phase === "Marketing" ||
    req.phase === "Contracts" ||
    req.phase === "Payment" ||
    req.phase === "Completed";
  if (tasFinished) return "completed";
  if (req.phase === "AwaitingApproval") return stepNum <= 6 ? "completed" : "pending";
  if (req.phase !== "InProgress") return stepNum <= totalSteps ? "completed" : "pending";
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
  const handoffEvent = req.timeline.find((e) => e.action === "handoff_marketing");

  return req.steps.map((step) => {
    const activities: MonitoringActivity[] = [];
    if (step.number === 1 && createdEvent) activities.push(timelineToActivity(createdEvent, locale));
    for (const c of comments.filter((c) => c.stepNumber === step.number)) {
      activities.push(commentToActivity(c, locale));
    }
    const hasCompletionComment = comments.some(
      (c) => c.stepNumber === step.number && c.kind === "StepCompletion"
    );
    const stepEvent = stepCompletedEvents[step.number - 1];
    if (stepEvent && !hasCompletionComment && !activities.some((a) => a.id === stepEvent.id)) {
      activities.push(timelineToActivity(stepEvent, locale));
    }
    if (step.number === 6 && submittedEvent) activities.push(timelineToActivity(submittedEvent, locale));
    if (step.number === 7 && handoffEvent) activities.push(timelineToActivity(handoffEvent, locale));
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
    req.phase === "Marketing" ||
    req.phase === "Contracts" ||
    req.phase === "Payment" ||
    req.phase === "Completed";
  if (!started) return "pending";
  if (
    req.marketingSubPhase === "Completed" ||
    req.phase === "Contracts" ||
    req.phase === "Payment" ||
    req.phase === "Completed"
  ) {
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
  const marketingEvents = req.timeline.filter(
    (e) => e.action.startsWith("marketing_") || e.action === "handoff_marketing"
  );
  const handoff = marketingEvents.find((e) => e.action === "handoff_marketing");

  return req.marketingSteps.map((step) => {
    const activities: MonitoringActivity[] = [];
    if (step.number === 1 && handoff) activities.push(timelineToActivity(handoff, locale));
    for (const c of comments.filter((c) => c.stepNumber === step.number)) {
      activities.push(commentToActivity(c, locale));
    }
    for (const ev of marketingEvents) {
      const evStep = marketingEventStep(ev.action);
      if (evStep === step.number && ev.action !== "handoff_marketing") {
        if (shouldSkipMarketingTimelineEvent(ev.action, comments)) continue;
        const act = timelineToActivity(ev, locale);
        if (!activities.some((a) => a.id === act.id)) activities.push(act);
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
    .filter(
      (e) =>
        e.action === "handoff_contracts" ||
        (e.action.startsWith("contracts_") && e.action !== "contracts_int_completed")
    )
    .map((e) => timelineToActivity(e, locale))
    .sort((a, b) => new Date(a.at).getTime() - new Date(b.at).getTime());
}

function buildPaymentActivities(req: ProcurementRequest, locale: string): MonitoringActivity[] {
  return req.timeline
    .filter(
      (e) =>
        e.action === "handed_off_to_payment" ||
        e.action.startsWith("payment_") ||
        e.action === "contracts_int_completed"
    )
    .map((e) => timelineToActivity(e, locale))
    .sort((a, b) => new Date(a.at).getTime() - new Date(b.at).getTime());
}
