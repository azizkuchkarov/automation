"use client";

import {
  TaskHealthScore,
  TaskSlaMetrics,
  TaskCycleTime,
  TaskHeatmapCell,
  TaskForecastPoint,
  TaskBurndownPoint,
  TaskRiskItem,
  TaskWorkloadBalance,
  TaskPriorityStatusCell,
  TaskPriority,
  WorkTaskStatus,
  PRIORITY_COLORS,
  STATUS_COLORS,
  RISK_COLORS,
  HEATMAP_SCALE,
} from "@/lib/tasks";
import { cn } from "@/lib/utils";
import {
  ResponsiveContainer,
  RadialBarChart,
  RadialBar,
  PolarAngleAxis,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ComposedChart,
  Line,
  Area,
  Cell,
} from "recharts";
import { Shield, Target, Activity, Scale, AlertOctagon } from "lucide-react";

const tooltipStyle = {
  contentStyle: {
    background: "hsl(var(--surface))",
    border: "1px solid hsl(var(--border))",
    borderRadius: 10,
    fontSize: 12,
    boxShadow: "0 8px 24px rgba(0,0,0,0.12)",
  },
};

function ChartCard({
  title,
  subtitle,
  children,
  className,
  icon: Icon,
}: {
  title: string;
  subtitle?: string;
  children: React.ReactNode;
  className?: string;
  icon?: React.ComponentType<{ size?: number; className?: string }>;
}) {
  return (
    <div className={cn("rounded-2xl border border-border/80 bg-surface p-5 shadow-sm", className)}>
      <div className="mb-4 flex items-start gap-2">
        {Icon && <Icon size={16} className="text-atg-amber mt-0.5 shrink-0" />}
        <div>
          <h2 className="text-sm font-semibold text-foreground/85">{title}</h2>
          {subtitle && <p className="text-[11px] text-foreground/45 mt-0.5">{subtitle}</p>}
        </div>
      </div>
      {children}
    </div>
  );
}

function gradeColor(grade: string) {
  switch (grade) {
    case "A": return "text-emerald-500";
    case "B": return "text-blue-500";
    case "C": return "text-amber-500";
    case "D": return "text-orange-500";
    default: return "text-red-500";
  }
}

interface HealthProps {
  health: TaskHealthScore;
  labels: {
    title: string;
    subtitle: string;
    completion: string;
    sla: string;
    velocity: string;
    balance: string;
    penalty: string;
    score: string;
  };
}

export function TaskHealthGauge({ health, labels }: HealthProps) {
  const data = [{ name: "Health", value: health.score, fill: health.score >= 70 ? "#059669" : health.score >= 50 ? "#f59e0b" : "#dc2626" }];

  return (
    <ChartCard title={labels.title} subtitle={labels.subtitle} icon={Shield}>
      <div className="flex flex-col sm:flex-row items-center gap-6">
        <div className="relative h-[200px] w-[200px] shrink-0">
          <ResponsiveContainer width="100%" height="100%">
            <RadialBarChart cx="50%" cy="50%" innerRadius="70%" outerRadius="100%" barSize={14} data={data} startAngle={210} endAngle={-30}>
              <PolarAngleAxis type="number" domain={[0, 100]} angleAxisId={0} tick={false} />
              <RadialBar background={{ fill: "hsl(var(--border) / 0.4)" }} dataKey="value" cornerRadius={8} />
            </RadialBarChart>
          </ResponsiveContainer>
          <div className="absolute inset-0 flex flex-col items-center justify-center pt-4">
            <span className="text-4xl font-black tabular-nums">{health.score}</span>
            <span className={cn("text-2xl font-bold", gradeColor(health.grade))}>{health.grade}</span>
            <span className="text-[10px] text-foreground/40 uppercase tracking-wider mt-0.5">{labels.score}</span>
          </div>
        </div>

        <div className="flex-1 space-y-2.5 w-full">
          {[
            { label: labels.completion, value: health.completionComponent, color: "#64748b" },
            { label: labels.sla, value: health.slaComponent, color: "#2563eb" },
            { label: labels.velocity, value: health.velocityComponent, color: "#059669" },
            { label: labels.balance, value: health.balanceComponent, color: "#7c3aed" },
          ].map((item) => (
            <div key={item.label}>
              <div className="flex justify-between text-xs mb-1">
                <span className="text-foreground/55">{item.label}</span>
                <span className="font-semibold tabular-nums">+{item.value}</span>
              </div>
              <div className="h-1.5 rounded-full bg-border/40 overflow-hidden">
                <div className="h-full rounded-full transition-all" style={{ width: `${Math.min(100, item.value * 3)}%`, background: item.color }} />
              </div>
            </div>
          ))}
          {health.riskPenalty > 0 && (
            <div className="flex justify-between text-xs text-red-600 dark:text-red-400 pt-1 border-t border-border/40">
              <span>{labels.penalty}</span>
              <span className="font-bold">−{health.riskPenalty}</span>
            </div>
          )}
        </div>
      </div>
    </ChartCard>
  );
}

interface SlaProps {
  sla: TaskSlaMetrics;
  labels: {
    title: string;
    subtitle: string;
    compliance: string;
    onTime: string;
    late: string;
    atRisk: string;
    withDue: string;
  };
}

export function TaskSlaPanel({ sla, labels }: SlaProps) {
  const ringColor = sla.compliancePercent >= 90 ? "#059669" : sla.compliancePercent >= 70 ? "#f59e0b" : "#dc2626";

  return (
    <ChartCard title={labels.title} subtitle={labels.subtitle} icon={Target}>
      <div className="flex items-center gap-6">
        <div className="relative h-[120px] w-[120px] shrink-0">
          <svg viewBox="0 0 120 120" className="w-full h-full -rotate-90">
            <circle cx="60" cy="60" r="50" fill="none" stroke="hsl(var(--border) / 0.4)" strokeWidth="10" />
            <circle
              cx="60" cy="60" r="50" fill="none"
              stroke={ringColor} strokeWidth="10" strokeLinecap="round"
              strokeDasharray={`${sla.compliancePercent * 3.14} 314`}
            />
          </svg>
          <div className="absolute inset-0 flex flex-col items-center justify-center">
            <span className="text-2xl font-bold tabular-nums">{sla.compliancePercent}%</span>
            <span className="text-[9px] text-foreground/40 uppercase">{labels.compliance}</span>
          </div>
        </div>
        <div className="grid grid-cols-2 gap-3 flex-1">
          {[
            { label: labels.withDue, value: sla.withDueDate, tone: "text-foreground/70" },
            { label: labels.onTime, value: sla.onTime, tone: "text-emerald-600 dark:text-emerald-400" },
            { label: labels.late, value: sla.late, tone: "text-red-600 dark:text-red-400" },
            { label: labels.atRisk, value: sla.atRisk, tone: "text-amber-600 dark:text-amber-400" },
          ].map((m) => (
            <div key={m.label} className="rounded-lg bg-border/20 px-3 py-2">
              <p className="text-[10px] text-foreground/45 uppercase tracking-wider">{m.label}</p>
              <p className={cn("text-xl font-bold tabular-nums", m.tone)}>{m.value}</p>
            </div>
          ))}
        </div>
      </div>
    </ChartCard>
  );
}

interface CycleProps {
  cycle: TaskCycleTime;
  labels: { title: string; subtitle: string; p50: string; p75: string; p90: string; mean: string; days: string };
}

export function TaskCycleTimeChart({ cycle, labels }: CycleProps) {
  const data = [
    { name: labels.p50, days: cycle.p50Days, fill: "#2563eb" },
    { name: labels.p75, days: cycle.p75Days, fill: "#7c3aed" },
    { name: labels.p90, days: cycle.p90Days, fill: "#f59e0b" },
    { name: labels.mean, days: cycle.meanDays, fill: "#059669" },
  ];

  return (
    <ChartCard title={labels.title} subtitle={labels.subtitle} icon={Activity}>
      {cycle.meanDays === 0 ? (
        <p className="text-sm text-foreground/40 text-center py-10">—</p>
      ) : (
        <div className="h-[200px]">
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={data} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
              <XAxis dataKey="name" tick={{ fontSize: 10, fill: "hsl(var(--foreground) / 0.45)" }} axisLine={false} tickLine={false} />
              <YAxis tick={{ fontSize: 10, fill: "hsl(var(--foreground) / 0.45)" }} axisLine={false} tickLine={false} />
              <Tooltip {...tooltipStyle} formatter={(v: number) => [`${v} ${labels.days}`, ""]} />
              <Bar dataKey="days" radius={[6, 6, 0, 0]} barSize={36}>
                {data.map((d, i) => <Cell key={i} fill={d.fill} />)}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        </div>
      )}
    </ChartCard>
  );
}

interface HeatmapProps {
  cells: TaskHeatmapCell[];
  labels: { title: string; subtitle: string; created: string; completed: string };
}

export function TaskActivityHeatmap({ cells, labels }: HeatmapProps) {
  const maxIntensity = Math.max(...cells.map((c) => c.intensity), 1);

  const colorFor = (intensity: number) => {
    if (intensity === 0) return HEATMAP_SCALE[0];
    const idx = Math.min(HEATMAP_SCALE.length - 1, Math.ceil((intensity / maxIntensity) * (HEATMAP_SCALE.length - 1)));
    return HEATMAP_SCALE[idx];
  };

  return (
    <ChartCard title={labels.title} subtitle={labels.subtitle} icon={Activity}>
      <div className="grid grid-cols-7 gap-2">
        {cells.map((cell) => (
          <div key={cell.dayOfWeek} className="text-center">
            <div
              className="aspect-square rounded-xl flex flex-col items-center justify-center transition-transform hover:scale-105 cursor-default border border-border/30"
              style={{ background: colorFor(cell.intensity) }}
              title={`${cell.label}: +${cell.created} / ✓${cell.completed}`}
            >
              <span className="text-lg font-bold tabular-nums text-white drop-shadow-sm">{cell.intensity}</span>
            </div>
            <span className="text-[10px] text-foreground/45 mt-1 block">{cell.label}</span>
          </div>
        ))}
      </div>
      <div className="flex justify-center gap-4 mt-4 text-[10px] text-foreground/45">
        <span className="flex items-center gap-1"><span className="w-2 h-2 rounded-sm" style={{ background: HEATMAP_SCALE[0] }} /> Low</span>
        <span className="flex items-center gap-1"><span className="w-2 h-2 rounded-sm" style={{ background: HEATMAP_SCALE[2] }} /> Med</span>
        <span className="flex items-center gap-1"><span className="w-2 h-2 rounded-sm" style={{ background: HEATMAP_SCALE[4] }} /> High</span>
        <span>+{labels.created} / ✓{labels.completed}</span>
      </div>
    </ChartCard>
  );
}

interface ForecastProps {
  forecast: TaskForecastPoint[];
  labels: { title: string; subtitle: string; actual: string; projected: string };
}

export function TaskForecastChart({ forecast, labels }: ForecastProps) {
  const data = forecast.map((p) => ({
    label: p.label,
    [labels.actual]: p.isProjected ? null : p.actual,
    [labels.projected]: p.isProjected ? p.forecast : p.forecast,
  }));

  return (
    <ChartCard title={labels.title} subtitle={labels.subtitle} icon={Activity}>
      <div className="h-[220px]">
        <ResponsiveContainer width="100%" height="100%">
          <ComposedChart data={data} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
            <XAxis dataKey="label" tick={{ fontSize: 9, fill: "hsl(var(--foreground) / 0.45)" }} axisLine={false} tickLine={false} />
            <YAxis tick={{ fontSize: 10, fill: "hsl(var(--foreground) / 0.45)" }} axisLine={false} tickLine={false} allowDecimals={false} />
            <Tooltip {...tooltipStyle} />
            <Area type="monotone" dataKey={labels.actual} fill="#2563eb" fillOpacity={0.15} stroke="#2563eb" strokeWidth={2} connectNulls={false} />
            <Line type="monotone" dataKey={labels.projected} stroke="#f59e0b" strokeWidth={2.5} strokeDasharray="6 4" dot={{ r: 3, fill: "#f59e0b" }} connectNulls />
            <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 11 }} />
          </ComposedChart>
        </ResponsiveContainer>
      </div>
    </ChartCard>
  );
}

interface BurndownProps {
  burndown: TaskBurndownPoint[];
  labels: { title: string; subtitle: string; remaining: string; ideal: string; completed: string };
}

export function TaskBurndownChart({ burndown, labels }: BurndownProps) {
  const data = burndown.map((p) => ({
    label: p.label,
    [labels.remaining]: p.remaining,
    [labels.ideal]: p.ideal,
    [labels.completed]: p.completed,
  }));

  return (
    <ChartCard title={labels.title} subtitle={labels.subtitle} icon={Target}>
      <div className="h-[220px]">
        <ResponsiveContainer width="100%" height="100%">
          <ComposedChart data={data} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
            <XAxis dataKey="label" tick={{ fontSize: 9, fill: "hsl(var(--foreground) / 0.45)" }} axisLine={false} tickLine={false} />
            <YAxis tick={{ fontSize: 10, fill: "hsl(var(--foreground) / 0.45)" }} axisLine={false} tickLine={false} allowDecimals={false} />
            <Tooltip {...tooltipStyle} />
            <Area type="monotone" dataKey={labels.remaining} fill="#dc2626" fillOpacity={0.1} stroke="#dc2626" strokeWidth={2.5} />
            <Line type="monotone" dataKey={labels.ideal} stroke="#94a3b8" strokeWidth={2} strokeDasharray="5 5" dot={false} />
            <Bar dataKey={labels.completed} fill="#059669" opacity={0.7} barSize={12} radius={[3, 3, 0, 0]} />
            <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 11 }} />
          </ComposedChart>
        </ResponsiveContainer>
      </div>
    </ChartCard>
  );
}

interface WorkloadProps {
  balance: TaskWorkloadBalance;
  labels: { title: string; subtitle: string; score: string; gini: string; assignees: string; avg: string; max: string };
}

export function TaskWorkloadPanel({ balance, labels }: WorkloadProps) {
  const tone = balance.balanceScore >= 75 ? "text-emerald-600" : balance.balanceScore >= 50 ? "text-amber-600" : "text-red-600";

  return (
    <ChartCard title={labels.title} subtitle={labels.subtitle} icon={Scale}>
      <div className="text-center mb-4">
        <span className={cn("text-5xl font-black tabular-nums", tone)}>{balance.balanceScore}</span>
        <p className="text-[10px] text-foreground/45 uppercase tracking-wider mt-1">{labels.score}</p>
      </div>
      <div className="h-2 rounded-full bg-border/40 overflow-hidden mb-4">
        <div
          className="h-full rounded-full transition-all"
          style={{
            width: `${balance.balanceScore}%`,
            background: balance.balanceScore >= 75 ? "#059669" : balance.balanceScore >= 50 ? "#f59e0b" : "#dc2626",
          }}
        />
      </div>
      <div className="grid grid-cols-2 gap-2 text-xs">
        <div className="rounded-lg bg-border/20 px-3 py-2">
          <p className="text-foreground/45">{labels.gini}</p>
          <p className="font-bold tabular-nums">{balance.giniCoefficient}</p>
        </div>
        <div className="rounded-lg bg-border/20 px-3 py-2">
          <p className="text-foreground/45">{labels.assignees}</p>
          <p className="font-bold tabular-nums">{balance.assigneeCount}</p>
        </div>
        <div className="rounded-lg bg-border/20 px-3 py-2">
          <p className="text-foreground/45">{labels.avg}</p>
          <p className="font-bold tabular-nums">{balance.avgLoad}</p>
        </div>
        <div className="rounded-lg bg-border/20 px-3 py-2">
          <p className="text-foreground/45">{labels.max}</p>
          <p className="font-bold tabular-nums">{balance.maxLoad}</p>
        </div>
      </div>
    </ChartCard>
  );
}

interface MatrixProps {
  matrix: TaskPriorityStatusCell[];
  priorityLabel: (p: TaskPriority) => string;
  statusLabel: (s: WorkTaskStatus) => string;
  labels: { title: string; subtitle: string };
}

export function TaskPriorityMatrixChart({ matrix, priorityLabel, statusLabel, labels }: MatrixProps) {
  const priorities: TaskPriority[] = ["Critical", "High", "Medium", "Low"];
  const statuses: WorkTaskStatus[] = ["New", "InProgress", "Done"];

  const data = priorities.map((p) => {
    const row: Record<string, string | number> = { priority: priorityLabel(p) };
    statuses.forEach((s) => {
      row[statusLabel(s)] = matrix.find((c) => c.priority === p && c.status === s)?.count ?? 0;
    });
    return row;
  });

  return (
    <ChartCard title={labels.title} subtitle={labels.subtitle} icon={Target}>
      <div className="h-[240px]">
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={data} margin={{ top: 8, right: 8, left: -8, bottom: 0 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
            <XAxis dataKey="priority" tick={{ fontSize: 10, fill: "hsl(var(--foreground) / 0.45)" }} axisLine={false} tickLine={false} />
            <YAxis tick={{ fontSize: 10, fill: "hsl(var(--foreground) / 0.45)" }} axisLine={false} tickLine={false} allowDecimals={false} />
            <Tooltip {...tooltipStyle} />
            <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 11 }} />
            {statuses.map((s) => (
              <Bar key={s} dataKey={statusLabel(s)} stackId="a" fill={STATUS_COLORS[s]} />
            ))}
          </BarChart>
        </ResponsiveContainer>
      </div>
    </ChartCard>
  );
}

interface RiskProps {
  risks: TaskRiskItem[];
  priorityLabel: (p: TaskPriority) => string;
  labels: {
    title: string;
    subtitle: string;
    task: string;
    assignee: string;
    priority: string;
    score: string;
    age: string;
    days: string;
    overdue: string;
  };
}

export function TaskRiskQueue({ risks, priorityLabel, labels }: RiskProps) {
  if (!risks.length) {
    return (
      <ChartCard title={labels.title} subtitle={labels.subtitle} icon={AlertOctagon}>
        <p className="text-sm text-foreground/40 text-center py-10">—</p>
      </ChartCard>
    );
  }

  const maxScore = Math.max(...risks.map((r) => r.riskScore), 1);

  return (
    <ChartCard title={labels.title} subtitle={labels.subtitle} icon={AlertOctagon} className="lg:col-span-2">
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="text-left text-[11px] text-foreground/45 uppercase tracking-wider border-b border-border">
              <th className="pb-2 font-semibold">{labels.task}</th>
              <th className="pb-2 font-semibold">{labels.assignee}</th>
              <th className="pb-2 font-semibold w-20">{labels.priority}</th>
              <th className="pb-2 font-semibold w-16 text-center">{labels.age}</th>
              <th className="pb-2 font-semibold min-w-[140px]">{labels.score}</th>
            </tr>
          </thead>
          <tbody>
            {risks.map((r) => (
              <tr key={r.id} className="border-b border-border/40 hover:bg-red-500/[0.02]">
                <td className="py-3">
                  <div className="flex items-center gap-2">
                    <span className="font-mono text-xs font-bold text-atg-amber">{r.number}</span>
                    <span className="truncate max-w-[200px]">{r.title}</span>
                    {r.isOverdue && (
                      <span className="text-[9px] font-bold uppercase text-red-600 bg-red-500/10 px-1.5 py-0.5 rounded">{labels.overdue}</span>
                    )}
                  </div>
                </td>
                <td className="py-3 text-foreground/70">{r.assigneeName}</td>
                <td className="py-3">
                  <span className="text-xs font-semibold" style={{ color: PRIORITY_COLORS[r.priority] }}>
                    {priorityLabel(r.priority)}
                  </span>
                </td>
                <td className="py-3 text-center tabular-nums text-foreground/60">{r.ageDays}{labels.days}</td>
                <td className="py-3">
                  <div className="flex items-center gap-2">
                    <div className="flex-1 h-2 rounded-full bg-border/40 overflow-hidden">
                      <div
                        className="h-full rounded-full"
                        style={{ width: `${(r.riskScore / maxScore) * 100}%`, background: RISK_COLORS[r.riskLevel] }}
                      />
                    </div>
                    <span className="text-xs font-bold tabular-nums w-8" style={{ color: RISK_COLORS[r.riskLevel] }}>
                      {r.riskScore}
                    </span>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </ChartCard>
  );
}
