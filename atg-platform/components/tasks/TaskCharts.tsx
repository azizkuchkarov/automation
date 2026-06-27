"use client";

import { TaskAnalytics, TaskSource, STATUS_COLORS, SOURCE_COLORS, WorkTaskStatus } from "@/lib/tasks";
import { cn } from "@/lib/utils";
import { Sparkles, Loader2, CheckCircle2, CircleDot } from "lucide-react";

interface Props {
  analytics: TaskAnalytics;
  labels: { new: string; progress: string; done: string; completion: string };
}

export function TaskKpiRow({ analytics, labels }: Props) {
  const cards = [
    {
      key: "new",
      label: labels.new,
      value: analytics.totalNew,
      icon: CircleDot,
      gradient: "from-slate-600 to-slate-800",
      accent: "text-slate-300",
      glow: "shadow-slate-500/20",
    },
    {
      key: "progress",
      label: labels.progress,
      value: analytics.totalInProgress,
      icon: Loader2,
      gradient: "from-blue-600 to-indigo-800",
      accent: "text-blue-200",
      glow: "shadow-blue-500/25",
    },
    {
      key: "done",
      label: labels.done,
      value: analytics.totalDone,
      icon: CheckCircle2,
      gradient: "from-emerald-600 to-teal-800",
      accent: "text-emerald-200",
      glow: "shadow-emerald-500/25",
    },
    {
      key: "rate",
      label: labels.completion,
      value: `${analytics.completionRate}%`,
      icon: Sparkles,
      gradient: "from-amber-500 to-orange-700",
      accent: "text-amber-100",
      glow: "shadow-amber-500/25",
    },
  ];

  return (
    <div className="grid grid-cols-2 xl:grid-cols-4 gap-4">
      {cards.map(({ key, label, value, icon: Icon, gradient, accent, glow }) => (
        <div
          key={key}
          className={cn(
            "relative overflow-hidden rounded-2xl p-5 text-white shadow-lg",
            `bg-gradient-to-br ${gradient}`,
            glow
          )}
        >
          <div className="absolute -right-4 -top-4 w-24 h-24 rounded-full bg-white/5" />
          <div className="absolute -right-2 -bottom-6 w-32 h-32 rounded-full bg-white/[0.03]" />
          <div className="relative">
            <div className="flex items-center justify-between mb-3">
              <span className={cn("text-xs font-semibold uppercase tracking-wider opacity-80", accent)}>
                {label}
              </span>
              <Icon size={18} className="opacity-60" />
            </div>
            <div className="text-4xl font-bold tabular-nums tracking-tight">{value}</div>
          </div>
        </div>
      ))}
    </div>
  );
}

interface DonutProps {
  distribution: TaskAnalytics["statusDistribution"];
  statusLabel: (s: WorkTaskStatus) => string;
}

export function TaskDonutChart({ distribution, statusLabel }: DonutProps) {
  const total = distribution.reduce((s, d) => s + d.count, 0) || 1;
  let offset = 0;
  const radius = 54;
  const circumference = 2 * Math.PI * radius;

  const segments = distribution
    .filter((d) => d.status !== "Cancelled" || d.count > 0)
    .map((d) => {
      const pct = d.count / total;
      const dash = pct * circumference;
      const seg = { ...d, dash, offset, color: STATUS_COLORS[d.status] };
      offset += dash;
      return seg;
    });

  return (
    <div className="flex flex-col sm:flex-row items-center gap-6">
      <div className="relative shrink-0">
        <svg width="140" height="140" viewBox="0 0 140 140" className="-rotate-90">
          <circle cx="70" cy="70" r={radius} fill="none" stroke="currentColor" strokeWidth="14" className="text-border/40" />
          {segments.map((s) => (
            <circle
              key={s.status}
              cx="70"
              cy="70"
              r={radius}
              fill="none"
              stroke={s.color}
              strokeWidth="14"
              strokeDasharray={`${s.dash} ${circumference - s.dash}`}
              strokeDashoffset={-s.offset}
              strokeLinecap="round"
              className="transition-all duration-700"
            />
          ))}
        </svg>
        <div className="absolute inset-0 flex flex-col items-center justify-center">
          <span className="text-2xl font-bold tabular-nums">{total}</span>
          <span className="text-[10px] text-foreground/45 uppercase tracking-wider">Total</span>
        </div>
      </div>
      <div className="flex-1 space-y-2.5 w-full">
        {segments.map((s) => (
          <div key={s.status} className="flex items-center gap-3">
            <span className="w-2.5 h-2.5 rounded-full shrink-0" style={{ background: s.color }} />
            <span className="text-sm text-foreground/70 flex-1">{statusLabel(s.status)}</span>
            <span className="text-sm font-bold tabular-nums">{s.count}</span>
            <span className="text-xs text-foreground/40 w-10 text-right tabular-nums">{s.percent}%</span>
          </div>
        ))}
      </div>
    </div>
  );
}

interface TrendProps {
  trend: TaskAnalytics["weeklyTrend"];
  labels: { new: string; progress: string; done: string };
}

export function TaskTrendChart({ trend, labels }: TrendProps) {
  const max = Math.max(...trend.flatMap((p) => [p.new, p.inProgress, p.done]), 1);

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap gap-4 text-xs">
        <span className="flex items-center gap-1.5"><span className="w-2.5 h-2.5 rounded-sm bg-slate-500" />{labels.new}</span>
        <span className="flex items-center gap-1.5"><span className="w-2.5 h-2.5 rounded-sm bg-atg-blue" />{labels.progress}</span>
        <span className="flex items-center gap-1.5"><span className="w-2.5 h-2.5 rounded-sm bg-emerald-500" />{labels.done}</span>
      </div>
      <div className="flex items-end gap-2 h-40">
        {trend.map((p) => (
          <div key={p.label} className="flex-1 flex flex-col items-center gap-1 min-w-0">
            <div className="flex items-end gap-0.5 h-32 w-full justify-center">
              <div
                className="w-2.5 rounded-t-sm bg-slate-500/80 transition-all"
                style={{ height: `${(p.new / max) * 100}%`, minHeight: p.new ? 4 : 0 }}
                title={`${labels.new}: ${p.new}`}
              />
              <div
                className="w-2.5 rounded-t-sm bg-atg-blue/80 transition-all"
                style={{ height: `${(p.inProgress / max) * 100}%`, minHeight: p.inProgress ? 4 : 0 }}
                title={`${labels.progress}: ${p.inProgress}`}
              />
              <div
                className="w-2.5 rounded-t-sm bg-emerald-500/80 transition-all"
                style={{ height: `${(p.done / max) * 100}%`, minHeight: p.done ? 4 : 0 }}
                title={`${labels.done}: ${p.done}`}
              />
            </div>
            <span className="text-[10px] text-foreground/40 truncate w-full text-center">{p.label}</span>
          </div>
        ))}
      </div>
    </div>
  );
}

interface EmployeeProps {
  employees: NonNullable<TaskAnalytics["byEmployee"]>;
  labels: { employee: string; new: string; progress: string; done: string; total: string };
}

export function EmployeeTaskBreakdown({ employees, labels }: EmployeeProps) {
  const maxTotal = Math.max(...employees.map((e) => e.total), 1);

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="text-left text-[11px] text-foreground/45 uppercase tracking-wider border-b border-border">
            <th className="pb-3 font-semibold">{labels.employee}</th>
            <th className="pb-3 font-semibold w-16 text-center">{labels.new}</th>
            <th className="pb-3 font-semibold w-16 text-center">{labels.progress}</th>
            <th className="pb-3 font-semibold w-16 text-center">{labels.done}</th>
            <th className="pb-3 font-semibold w-20 text-center">{labels.total}</th>
            <th className="pb-3 font-semibold min-w-[180px]">Distribution</th>
          </tr>
        </thead>
        <tbody>
          {employees.map((e) => (
            <tr key={e.userId} className="border-b border-border/40 hover:bg-atg-amber/[0.03] transition-colors">
              <td className="py-3.5">
                <div className="flex items-center gap-2.5">
                  <span className="w-8 h-8 rounded-full bg-gradient-to-br from-atg-amber/30 to-orange-500/20 text-atg-amber flex items-center justify-center text-xs font-bold ring-1 ring-border/50">
                    {e.fullName.charAt(0)}
                  </span>
                  <div>
                    <p className="font-medium text-foreground/90">{e.fullName}</p>
                    {e.employeeId && (
                      <p className="text-[11px] text-foreground/40 font-mono">{e.employeeId}</p>
                    )}
                  </div>
                </div>
              </td>
              <td className="py-3.5 text-center font-semibold text-slate-500 tabular-nums">{e.newCount}</td>
              <td className="py-3.5 text-center font-semibold text-atg-blue tabular-nums">{e.inProgressCount}</td>
              <td className="py-3.5 text-center font-semibold text-emerald-600 dark:text-emerald-400 tabular-nums">{e.doneCount}</td>
              <td className="py-3.5 text-center font-bold tabular-nums">{e.total}</td>
              <td className="py-3.5">
                <div className="h-2.5 rounded-full bg-border/40 overflow-hidden flex" style={{ width: `${(e.total / maxTotal) * 100}%`, minWidth: e.total ? 48 : 0 }}>
                  {e.newCount > 0 && (
                    <div className="bg-slate-500 h-full" style={{ width: `${(e.newCount / e.total) * 100}%` }} />
                  )}
                  {e.inProgressCount > 0 && (
                    <div className="bg-atg-blue h-full" style={{ width: `${(e.inProgressCount / e.total) * 100}%` }} />
                  )}
                  {e.doneCount > 0 && (
                    <div className="bg-emerald-500 h-full" style={{ width: `${(e.doneCount / e.total) * 100}%` }} />
                  )}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

interface SourceChartProps {
  bySource: TaskAnalytics["bySource"];
  sourceLabel: (s: TaskSource) => string;
}

export function TaskSourceChart({ bySource, sourceLabel }: SourceChartProps) {
  const total = bySource.reduce((s, d) => s + d.count, 0) || 1;
  const max = Math.max(...bySource.map((d) => d.count), 1);

  return (
    <div className="space-y-3">
      {bySource.map((item) => (
        <div key={item.source}>
          <div className="flex items-center justify-between text-sm mb-1">
            <span className="flex items-center gap-2 font-medium">
              <span className="w-2.5 h-2.5 rounded-full" style={{ background: SOURCE_COLORS[item.source] }} />
              {sourceLabel(item.source)}
            </span>
            <span className="tabular-nums text-foreground/60">
              <strong className="text-foreground">{item.count}</strong>
              <span className="text-xs ml-1">({item.percent}%)</span>
            </span>
          </div>
          <div className="h-2 rounded-full bg-border/40 overflow-hidden">
            <div
              className="h-full rounded-full transition-all duration-700"
              style={{
                width: `${(item.count / max) * 100}%`,
                background: SOURCE_COLORS[item.source],
                minWidth: item.count ? 4 : 0,
              }}
            />
          </div>
        </div>
      ))}
      {bySource.length === 0 && (
        <p className="text-sm text-foreground/40 text-center py-6">—</p>
      )}
      <p className="text-[10px] text-foreground/35 pt-1 border-t border-border/40">
        Total: {total}
      </p>
    </div>
  );
}
