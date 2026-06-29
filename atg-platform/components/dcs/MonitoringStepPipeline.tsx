"use client";

import type { ReactNode } from "react";
import { Check, Circle, Clock, MessageSquare } from "lucide-react";
import { deptLabel } from "@/lib/dcs";
import { cn } from "@/lib/utils";

export type MonitoringStepStatus = "completed" | "active" | "pending" | "skipped";

export interface MonitoringActivity {
  id: string;
  type: "comment" | "event";
  title: string;
  subtitle?: string;
  body?: string;
  at: string;
  variant?: "default" | "success" | "danger" | "warning";
  icon?: ReactNode;
}

export interface MonitoringStepItem {
  number: number;
  title: string;
  status: MonitoringStepStatus;
  activities: MonitoringActivity[];
}

interface Accent {
  rail: string;
  railActive: string;
  railDone: string;
  pill: string;
  pillActive: string;
  pillDone: string;
  card: string;
  cardActive: string;
}

const ACCENTS: Record<string, Accent> = {
  sky: {
    rail: "bg-foreground/10",
    railActive: "bg-sky-500",
    railDone: "bg-emerald-500",
    pill: "border-border/60 bg-background text-foreground/40",
    pillActive: "border-sky-500 bg-sky-500 text-white shadow-md shadow-sky-500/25",
    pillDone: "border-emerald-500/50 bg-emerald-500/10 text-emerald-700 dark:text-emerald-300",
    card: "border-border/50",
    cardActive: "border-sky-500/35 bg-sky-500/[0.04] shadow-sm",
  },
  violet: {
    rail: "bg-foreground/10",
    railActive: "bg-violet-500",
    railDone: "bg-emerald-500",
    pill: "border-border/60 bg-background text-foreground/40",
    pillActive: "border-violet-500 bg-violet-500 text-white shadow-md shadow-violet-500/25",
    pillDone: "border-emerald-500/50 bg-emerald-500/10 text-emerald-700 dark:text-emerald-300",
    card: "border-border/50",
    cardActive: "border-violet-500/35 bg-violet-500/[0.04] shadow-sm",
  },
  amber: {
    rail: "bg-foreground/10",
    railActive: "bg-amber-500",
    railDone: "bg-emerald-500",
    pill: "border-border/60 bg-background text-foreground/40",
    pillActive: "border-amber-500 bg-amber-500 text-white shadow-md shadow-amber-500/25",
    pillDone: "border-emerald-500/50 bg-emerald-500/10 text-emerald-700 dark:text-emerald-300",
    card: "border-border/50",
    cardActive: "border-amber-500/35 bg-amber-500/[0.04] shadow-sm",
  },
};

interface Props {
  steps: MonitoringStepItem[];
  accent?: keyof typeof ACCENTS;
  locale: string;
  stepLabel: string;
  statusCompleted: string;
  statusActive: string;
  statusPending: string;
  noActivity: string;
}

export function MonitoringStepPipeline({
  steps,
  accent = "sky",
  locale,
  stepLabel,
  statusCompleted,
  statusActive,
  statusPending,
  noActivity,
}: Props) {
  const a = ACCENTS[accent];

  return (
    <div className="space-y-6">
      {/* Horizontal flow: 1 → 2 → 3 → 4 */}
      <div className="overflow-x-auto pb-1 -mx-1 px-1">
        <div className="flex items-center min-w-max gap-0">
          {steps.map((step, i) => (
            <div key={step.number} className="flex items-center">
              <div className="flex flex-col items-center gap-1.5 min-w-[3.25rem]">
                <div
                  className={cn(
                    "w-9 h-9 rounded-full border-2 flex items-center justify-center text-xs font-bold transition-all",
                    step.status === "completed" && a.pillDone,
                    step.status === "active" && a.pillActive,
                    step.status === "pending" && a.pill,
                    step.status === "skipped" && "border-border/40 bg-foreground/[0.03] text-foreground/25"
                  )}
                >
                  {step.status === "completed" ? <Check size={16} strokeWidth={2.5} /> : step.number}
                </div>
                <span className="text-[9px] font-medium text-foreground/35 tabular-nums max-w-[4rem] text-center truncate">
                  {stepLabel} {step.number}
                </span>
              </div>
              {i < steps.length - 1 && (
                <div className="flex items-center w-8 sm:w-12 h-9 shrink-0">
                  <div
                    className={cn(
                      "h-0.5 w-full rounded-full",
                      step.status === "completed" ? a.railDone : a.rail
                    )}
                  />
                </div>
              )}
            </div>
          ))}
        </div>
      </div>

      {/* Vertical detail rail */}
      <div className="space-y-0">
        {steps.map((step, i) => (
          <div key={step.number} className="flex gap-4">
            <div className="flex flex-col items-center w-9 shrink-0">
              <div
                className={cn(
                  "w-9 h-9 rounded-full border-2 flex items-center justify-center shrink-0 z-[1]",
                  step.status === "completed" && a.pillDone,
                  step.status === "active" && a.pillActive,
                  step.status === "pending" && a.pill,
                  step.status === "skipped" && "border-border/40 bg-foreground/[0.03] text-foreground/25"
                )}
              >
                {step.status === "completed" ? (
                  <Check size={15} strokeWidth={2.5} />
                ) : step.status === "active" ? (
                  <span className="w-2.5 h-2.5 rounded-full bg-current animate-pulse" />
                ) : (
                  <span className="text-xs font-bold">{step.number}</span>
                )}
              </div>
              {i < steps.length - 1 && (
                <div
                  className={cn(
                    "w-0.5 flex-1 min-h-[1.5rem] -my-0.5",
                    step.status === "completed" ? a.railDone : a.rail
                  )}
                />
              )}
            </div>

            <div className={cn("flex-1 min-w-0", i < steps.length - 1 ? "pb-5" : "pb-1")}>
              <div
                className={cn(
                  "rounded-xl border p-4 transition-all",
                  step.status === "active" ? a.cardActive : a.card,
                  step.status === "completed" && "border-emerald-500/20 bg-emerald-500/[0.02]"
                )}
              >
                <div className="flex flex-wrap items-start justify-between gap-2 mb-2">
                  <div className="min-w-0">
                    <p className="text-[10px] font-bold uppercase tracking-wider text-foreground/40">
                      {stepLabel} {step.number}
                    </p>
                    <h4 className="text-sm font-semibold text-foreground leading-snug mt-0.5">
                      {step.title}
                    </h4>
                  </div>
                  <StatusBadge
                    status={step.status}
                    labels={{ completed: statusCompleted, active: statusActive, pending: statusPending }}
                  />
                </div>

                {step.activities.length > 0 ? (
                  <div className="mt-3 space-y-2">
                    {step.activities.map((act) => (
                      <ActivityRow key={act.id} activity={act} locale={locale} />
                    ))}
                  </div>
                ) : (
                  <p className="text-xs text-foreground/40 mt-2 flex items-center gap-1.5">
                    <Clock size={12} />
                    {noActivity}
                  </p>
                )}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function StatusBadge({
  status,
  labels,
}: {
  status: MonitoringStepStatus;
  labels: { completed: string; active: string; pending: string };
}) {
  if (status === "skipped") return null;
  const label =
    status === "completed" ? labels.completed : status === "active" ? labels.active : labels.pending;
  return (
    <span
      className={cn(
        "text-[10px] font-semibold uppercase tracking-wide px-2 py-0.5 rounded-full shrink-0",
        status === "completed" && "bg-emerald-500/12 text-emerald-700 dark:text-emerald-300",
        status === "active" && "bg-sky-500/12 text-sky-700 dark:text-sky-300",
        status === "pending" && "bg-foreground/[0.06] text-foreground/45"
      )}
    >
      {label}
    </span>
  );
}

function ActivityRow({ activity, locale }: { activity: MonitoringActivity; locale: string }) {
  return (
    <div
      className={cn(
        "rounded-lg border px-3 py-2.5",
        activity.variant === "success" && "border-emerald-500/20 bg-emerald-500/[0.04]",
        activity.variant === "danger" && "border-red-500/20 bg-red-500/[0.04]",
        activity.variant === "warning" && "border-amber-500/20 bg-amber-500/[0.04]",
        (!activity.variant || activity.variant === "default") && "border-border/40 bg-background/60"
      )}
    >
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center gap-2 min-w-0">
          {activity.icon ?? <MessageSquare size={12} className="text-foreground/35 shrink-0" />}
          <span className="text-xs font-semibold text-foreground truncate">{activity.title}</span>
        </div>
        <span className="text-[10px] text-foreground/40 shrink-0 tabular-nums">
          {new Date(activity.at).toLocaleString(locale)}
        </span>
      </div>
      {activity.subtitle && (
        <p className="text-[11px] text-foreground/45 mt-1">{activity.subtitle}</p>
      )}
      {activity.body && (
        <p className="text-sm text-foreground/70 mt-1.5 leading-relaxed whitespace-pre-wrap">
          {activity.body}
        </p>
      )}
    </div>
  );
}

export function MonitoringMilestoneList({
  items,
  locale,
}: {
  items: MonitoringActivity[];
  locale: string;
}) {
  if (items.length === 0) return null;
  return (
    <div className="space-y-2">
      {items.map((act) => (
        <ActivityRow key={act.id} activity={act} locale={locale} />
      ))}
    </div>
  );
}

export function MonitoringApproverPipeline({
  approvers,
  locale,
  roleLabel,
  statusApproved,
  statusRejected,
  statusPending,
  noDepartment,
}: {
  approvers: Array<{
    id: string;
    userName: string;
    role: string;
    status: string;
    comment?: string | null;
    decidedAt?: string | null;
    departmentName?: string | null;
    departmentNameEn?: string | null;
    organizationName?: string | null;
    organizationNameEn?: string | null;
    jobTitleRu?: string | null;
    jobTitleEn?: string | null;
    userEmail?: string | null;
    employeeId?: string | null;
  }>;
  locale: string;
  roleLabel: (role: string) => string;
  statusApproved: string;
  statusRejected: string;
  statusPending: string;
  noDepartment?: string;
}) {
  const visible = approvers.filter((a) => a.status !== "Pending" || a.userName);
  if (visible.length === 0) return null;

  return (
    <div className="space-y-6">
      <div className="overflow-x-auto pb-1">
        <div className="flex items-center min-w-max gap-0">
          {visible.map((a, i) => {
            const done = a.status === "Approved";
            const rejected = a.status === "Rejected";
            const active = a.status === "Pending" && !visible.slice(0, i).some((x) => x.status === "Pending");
            return (
              <div key={a.id} className="flex items-center">
                <div className="flex flex-col items-center gap-1.5 min-w-[4.5rem] max-w-[5.5rem]">
                  <div
                    className={cn(
                      "w-9 h-9 rounded-full border-2 flex items-center justify-center",
                      done && "border-emerald-500/50 bg-emerald-500/10 text-emerald-600",
                      rejected && "border-red-500/50 bg-red-500/10 text-red-600",
                      active && "border-amber-500 bg-amber-500 text-white shadow-md shadow-amber-500/25",
                      !done && !rejected && !active && "border-border/60 bg-background text-foreground/35"
                    )}
                  >
                    {done ? <Check size={16} /> : rejected ? <Circle size={14} /> : <span className="text-xs font-bold">{i + 1}</span>}
                  </div>
                  <span className="text-[9px] text-foreground/40 text-center leading-tight line-clamp-2">
                    {roleLabel(a.role)}
                  </span>
                </div>
                {i < visible.length - 1 && (
                  <div className={cn("h-0.5 w-8 sm:w-10 rounded-full", done ? "bg-emerald-500" : "bg-foreground/10")} />
                )}
              </div>
            );
          })}
        </div>
      </div>

      <div className="space-y-2">
        {visible.map((a) => (
          <div
            key={a.id}
            className={cn(
              "rounded-xl border px-4 py-3",
              a.status === "Approved" && "border-emerald-500/25 bg-emerald-500/[0.04]",
              a.status === "Rejected" && "border-red-500/25 bg-red-500/[0.04]",
              a.status === "Pending" && "border-border/50 bg-foreground/[0.02]"
            )}
          >
            <div className="flex flex-wrap items-center justify-between gap-2">
              <div className="min-w-0">
                <p className="text-sm font-semibold">{a.userName}</p>
                {(() => {
                  const dept = deptLabel(a.departmentName ?? "", a.departmentNameEn ?? "", locale);
                  return dept ? (
                    <p className="text-xs text-foreground/60 mt-0.5 truncate">{dept}</p>
                  ) : noDepartment ? (
                    <p className="text-xs text-foreground/40 italic mt-0.5">{noDepartment}</p>
                  ) : null;
                })()}
                <p className="text-xs text-foreground/45 mt-0.5">{roleLabel(a.role)}</p>
              </div>
              <span
                className={cn(
                  "text-[10px] font-semibold uppercase px-2 py-0.5 rounded-full",
                  a.status === "Approved" && "bg-emerald-500/12 text-emerald-700",
                  a.status === "Rejected" && "bg-red-500/12 text-red-700",
                  a.status === "Pending" && "bg-foreground/[0.06] text-foreground/45"
                )}
              >
                {a.status === "Approved" ? statusApproved : a.status === "Rejected" ? statusRejected : statusPending}
              </span>
            </div>
            {a.comment && (
              <p className="text-sm text-foreground/70 mt-2 leading-relaxed whitespace-pre-wrap">{a.comment}</p>
            )}
            {a.decidedAt && (
              <p className="text-[10px] text-foreground/40 mt-1.5">
                {new Date(a.decidedAt).toLocaleString(locale)}
              </p>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
