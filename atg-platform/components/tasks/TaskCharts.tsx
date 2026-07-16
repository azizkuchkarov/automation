"use client";

import {
  TaskAnalytics,
  TaskSource,
  TaskPriority,
  WorkTaskStatus,
  TaskInsight,
  STATUS_COLORS,
  SOURCE_COLORS,
  PRIORITY_COLORS,
  AGING_COLORS,
} from "@/lib/tasks";
import { cn } from "@/lib/utils";
import {
  Sparkles,
  Loader2,
  CheckCircle2,
  CircleDot,
  AlertTriangle,
  Clock,
  TrendingUp,
  TrendingDown,
  Lightbulb,
  Minus,
} from "lucide-react";
import {
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  Tooltip,
  Legend,
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  ComposedChart,
  Line,
  BarChart,
  Bar,
  RadarChart,
  PolarGrid,
  PolarAngleAxis,
  PolarRadiusAxis,
  Radar,
} from "recharts";

function ChartCard({
  title,
  subtitle,
  children,
  className,
}: {
  title: string;
  subtitle?: string;
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <div className={cn("rounded-2xl border border-border/80 bg-surface p-5 shadow-sm", className)}>
      <div className="mb-4">
        <h2 className="text-sm font-semibold text-foreground/85">{title}</h2>
        {subtitle && <p className="text-[11px] text-foreground/45 mt-0.5">{subtitle}</p>}
      </div>
      {children}
    </div>
  );
}

const tooltipStyle = {
  contentStyle: {
    background: "hsl(var(--surface))",
    border: "1px solid hsl(var(--border))",
    borderRadius: 10,
    fontSize: 12,
    boxShadow: "0 8px 24px rgba(0,0,0,0.12)",
  },
  itemStyle: { padding: 0 },
};

interface KpiProps {
  analytics: TaskAnalytics;
  labels: {
    new: string;
    progress: string;
    done: string;
    completion: string;
    overdue: string;
    avgResolution: string;
    throughput: string;
    days: string;
  };
}

export function TaskKpiRow({ analytics, labels }: KpiProps) {
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

  const throughput = analytics.throughputChangePercent;
  const throughputUp = throughput > 0;
  const throughputDown = throughput < 0;

  const secondary = [
    {
      key: "overdue",
      label: labels.overdue,
      value: analytics.overdueCount,
      icon: AlertTriangle,
      tone: analytics.overdueCount > 0 ? "text-red-600 dark:text-red-400 bg-red-500/10" : "text-foreground/60 bg-border/30",
    },
    {
      key: "resolution",
      label: labels.avgResolution,
      value: analytics.avgResolutionDays > 0 ? `${analytics.avgResolutionDays} ${labels.days}` : "—",
      icon: Clock,
      tone: "text-atg-blue bg-atg-blue/10",
    },
    {
      key: "throughput",
      label: labels.throughput,
      value: `${throughput > 0 ? "+" : ""}${throughput}%`,
      icon: throughputUp ? TrendingUp : throughputDown ? TrendingDown : Minus,
      tone: throughputUp
        ? "text-emerald-600 dark:text-emerald-400 bg-emerald-500/10"
        : throughputDown
          ? "text-amber-600 dark:text-amber-400 bg-amber-500/10"
          : "text-foreground/60 bg-border/30",
    },
  ];

  return (
    <div className="space-y-4">
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

      <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
        {secondary.map(({ key, label, value, icon: Icon, tone }) => (
          <div key={key} className="flex items-center gap-3 rounded-xl border border-border/60 bg-surface/80 px-4 py-3">
            <span className={cn("flex h-9 w-9 items-center justify-center rounded-lg", tone)}>
              <Icon size={16} />
            </span>
            <div>
              <p className="text-[10px] uppercase tracking-wider text-foreground/45 font-semibold">{label}</p>
              <p className="text-lg font-bold tabular-nums">{value}</p>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

interface InsightsProps {
  insights: TaskInsight[];
  insightLabel: (code: string, value?: number, context?: string) => string;
  title: string;
}

export function TaskInsightsPanel({ insights, insightLabel, title }: InsightsProps) {
  if (!insights.length) return null;

  const severityStyles = {
    good: "border-emerald-500/30 bg-emerald-500/[0.06] text-emerald-700 dark:text-emerald-300",
    warning: "border-amber-500/30 bg-amber-500/[0.06] text-amber-800 dark:text-amber-200",
    info: "border-atg-blue/30 bg-atg-blue/[0.06] text-blue-800 dark:text-blue-200",
  };

  return (
    <div className="rounded-2xl border border-border/80 bg-gradient-to-r from-surface via-surface to-atg-amber/[0.04] p-5 shadow-sm">
      <div className="flex items-center gap-2 mb-3">
        <Lightbulb size={16} className="text-atg-amber" />
        <h2 className="text-sm font-semibold text-foreground/85">{title}</h2>
      </div>
      <div className="flex flex-wrap gap-2">
        {insights.map((insight) => (
          <span
            key={insight.code}
            className={cn(
              "inline-flex items-center rounded-full border px-3 py-1.5 text-xs font-medium",
              severityStyles[insight.severity]
            )}
          >
            {insightLabel(insight.code, insight.value, insight.context)}
          </span>
        ))}
      </div>
    </div>
  );
}

interface DonutProps {
  distribution: TaskAnalytics["statusDistribution"];
  statusLabel: (s: WorkTaskStatus) => string;
  title: string;
}

export function TaskStatusDonutChart({ distribution, statusLabel, title }: DonutProps) {
  const data = distribution
    .filter((d) => d.count > 0 && d.status !== "Cancelled")
    .map((d) => ({
      name: statusLabel(d.status),
      value: d.count,
      percent: d.percent,
      color: STATUS_COLORS[d.status],
    }));

  const total = data.reduce((s, d) => s + d.value, 0);

  return (
    <ChartCard title={title}>
      {data.length === 0 ? (
        <p className="text-sm text-foreground/40 text-center py-12">—</p>
      ) : (
        <div className="h-[260px]">
          <ResponsiveContainer width="100%" height="100%">
            <PieChart>
              <Pie
                data={data}
                cx="50%"
                cy="50%"
                innerRadius={58}
                outerRadius={88}
                paddingAngle={3}
                dataKey="value"
                stroke="none"
              >
                {data.map((entry, i) => (
                  <Cell key={i} fill={entry.color} />
                ))}
              </Pie>
              <Tooltip
                {...tooltipStyle}
                formatter={(value: number, _name, props) => [
                  `${value} (${(props.payload as { percent: number }).percent}%)`,
                  (props.payload as { name: string }).name,
                ]}
              />
              <Legend
                verticalAlign="bottom"
                iconType="circle"
                iconSize={8}
                formatter={(value) => <span className="text-xs text-foreground/70">{value}</span>}
              />
              <text x="50%" y="46%" textAnchor="middle" className="fill-foreground text-2xl font-bold">
                {total}
              </text>
              <text x="50%" y="54%" textAnchor="middle" className="fill-foreground/45 text-[10px] uppercase">
                Total
              </text>
            </PieChart>
          </ResponsiveContainer>
        </div>
      )}
    </ChartCard>
  );
}

interface ActivityProps {
  trend: TaskAnalytics["weeklyTrend"];
  labels: { new: string; progress: string; done: string };
  title: string;
  subtitle?: string;
}

export function TaskActivityChart({ trend, labels, title, subtitle }: ActivityProps) {
  const data = trend.map((p) => ({
    label: p.label,
    [labels.new]: p.new,
    [labels.progress]: p.inProgress,
    [labels.done]: p.done,
  }));

  return (
    <ChartCard title={title} subtitle={subtitle} className="lg:col-span-2">
      <div className="h-[280px]">
        <ResponsiveContainer width="100%" height="100%">
          <AreaChart data={data} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
            <defs>
              <linearGradient id="gradNew" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor="#64748b" stopOpacity={0.35} />
                <stop offset="100%" stopColor="#64748b" stopOpacity={0.02} />
              </linearGradient>
              <linearGradient id="gradProgress" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor="#2563eb" stopOpacity={0.4} />
                <stop offset="100%" stopColor="#2563eb" stopOpacity={0.02} />
              </linearGradient>
              <linearGradient id="gradDone" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor="#059669" stopOpacity={0.45} />
                <stop offset="100%" stopColor="#059669" stopOpacity={0.02} />
              </linearGradient>
            </defs>
            <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
            <XAxis dataKey="label" tick={{ fontSize: 10, fill: "hsl(var(--foreground) / 0.45)" }} axisLine={false} tickLine={false} />
            <YAxis tick={{ fontSize: 10, fill: "hsl(var(--foreground) / 0.45)" }} axisLine={false} tickLine={false} allowDecimals={false} />
            <Tooltip {...tooltipStyle} />
            <Area type="monotone" dataKey={labels.new} stackId="1" stroke="#64748b" fill="url(#gradNew)" strokeWidth={2} />
            <Area type="monotone" dataKey={labels.progress} stackId="1" stroke="#2563eb" fill="url(#gradProgress)" strokeWidth={2} />
            <Area type="monotone" dataKey={labels.done} stackId="1" stroke="#059669" fill="url(#gradDone)" strokeWidth={2} />
            <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 11 }} />
          </AreaChart>
        </ResponsiveContainer>
      </div>
    </ChartCard>
  );
}

interface VelocityProps {
  velocity: TaskAnalytics["velocityTrend"];
  labels: { completed: string; movingAvg: string };
  title: string;
  subtitle?: string;
}

export function TaskVelocityChart({ velocity, labels, title, subtitle }: VelocityProps) {
  const data = velocity.map((p) => ({
    label: p.label,
    [labels.completed]: p.completed,
    [labels.movingAvg]: p.movingAverage,
  }));

  return (
    <ChartCard title={title} subtitle={subtitle}>
      <div className="h-[240px]">
        <ResponsiveContainer width="100%" height="100%">
          <ComposedChart data={data} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
            <XAxis dataKey="label" tick={{ fontSize: 10, fill: "hsl(var(--foreground) / 0.45)" }} axisLine={false} tickLine={false} />
            <YAxis tick={{ fontSize: 10, fill: "hsl(var(--foreground) / 0.45)" }} axisLine={false} tickLine={false} allowDecimals={false} />
            <Tooltip {...tooltipStyle} />
            <Bar dataKey={labels.completed} fill="#2563eb" radius={[4, 4, 0, 0]} barSize={18} opacity={0.85} />
            <Line
              type="monotone"
              dataKey={labels.movingAvg}
              stroke="#f59e0b"
              strokeWidth={2.5}
              dot={{ r: 3, fill: "#f59e0b" }}
            />
            <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 11 }} />
          </ComposedChart>
        </ResponsiveContainer>
      </div>
    </ChartCard>
  );
}

interface PriorityProps {
  byPriority: TaskAnalytics["byPriority"];
  priorityLabel: (p: TaskPriority) => string;
  title: string;
  subtitle?: string;
}

export function TaskPriorityRadarChart({ byPriority, priorityLabel, title, subtitle }: PriorityProps) {
  const data = byPriority.map((p) => ({
    priority: priorityLabel(p.priority),
    count: p.count,
    fullMark: Math.max(...byPriority.map((x) => x.count), 1),
  }));

  return (
    <ChartCard title={title} subtitle={subtitle}>
      {byPriority.every((p) => p.count === 0) ? (
        <p className="text-sm text-foreground/40 text-center py-12">—</p>
      ) : (
        <div className="h-[240px]">
          <ResponsiveContainer width="100%" height="100%">
            <RadarChart data={data} cx="50%" cy="50%" outerRadius="72%">
              <PolarGrid stroke="hsl(var(--border))" />
              <PolarAngleAxis dataKey="priority" tick={{ fontSize: 10, fill: "hsl(var(--foreground) / 0.6)" }} />
              <PolarRadiusAxis tick={{ fontSize: 9 }} axisLine={false} />
              <Radar
                name="Tasks"
                dataKey="count"
                stroke="#7c3aed"
                fill="#7c3aed"
                fillOpacity={0.25}
                strokeWidth={2}
              />
              <Tooltip {...tooltipStyle} />
            </RadarChart>
          </ResponsiveContainer>
          <div className="flex flex-wrap justify-center gap-2 mt-1">
            {byPriority.filter((p) => p.count > 0).map((p) => (
              <span key={p.priority} className="inline-flex items-center gap-1 text-[10px] text-foreground/55">
                <span className="w-2 h-2 rounded-full" style={{ background: PRIORITY_COLORS[p.priority] }} />
                {priorityLabel(p.priority)}: {p.count}
              </span>
            ))}
          </div>
        </div>
      )}
    </ChartCard>
  );
}

interface AgingProps {
  buckets: TaskAnalytics["agingBuckets"];
  bucketLabel: (key: string) => string;
  title: string;
  subtitle?: string;
}

export function TaskAgingChart({ buckets, bucketLabel, title, subtitle }: AgingProps) {
  const data = buckets.map((b) => ({
    name: bucketLabel(b.key),
    count: b.count,
    percent: b.percent,
    key: b.key,
  }));

  return (
    <ChartCard title={title} subtitle={subtitle}>
      <div className="h-[240px]">
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={data} layout="vertical" margin={{ top: 4, right: 16, left: 4, bottom: 4 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" horizontal={false} />
            <XAxis type="number" tick={{ fontSize: 10, fill: "hsl(var(--foreground) / 0.45)" }} axisLine={false} tickLine={false} allowDecimals={false} />
            <YAxis type="category" dataKey="name" width={72} tick={{ fontSize: 10, fill: "hsl(var(--foreground) / 0.55)" }} axisLine={false} tickLine={false} />
            <Tooltip
              {...tooltipStyle}
              formatter={(value: number, _name, props) => [
                `${value} (${(props.payload as { percent: number }).percent}%)`,
                bucketLabel((props.payload as { key: string }).key),
              ]}
            />
            <Bar dataKey="count" radius={[0, 6, 6, 0]} barSize={22}>
              {data.map((entry) => (
                <Cell key={entry.key} fill={AGING_COLORS[entry.key] ?? "#64748b"} />
              ))}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      </div>
    </ChartCard>
  );
}

interface SourceChartProps {
  bySource: TaskAnalytics["bySource"];
  sourceLabel: (s: TaskSource) => string;
  title: string;
}

export function TaskSourceChart({ bySource, sourceLabel, title }: SourceChartProps) {
  const data = bySource.map((item) => ({
    name: sourceLabel(item.source),
    count: item.count,
    percent: item.percent,
    fill: SOURCE_COLORS[item.source],
  }));

  return (
    <ChartCard title={title}>
      {data.length === 0 ? (
        <p className="text-sm text-foreground/40 text-center py-12">—</p>
      ) : (
        <div className="h-[220px]">
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={data} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
              <XAxis dataKey="name" tick={{ fontSize: 10, fill: "hsl(var(--foreground) / 0.45)" }} axisLine={false} tickLine={false} />
              <YAxis tick={{ fontSize: 10, fill: "hsl(var(--foreground) / 0.45)" }} axisLine={false} tickLine={false} allowDecimals={false} />
              <Tooltip
                {...tooltipStyle}
                formatter={(value: number, _name, props) => [
                  `${value} (${(props.payload as { percent: number }).percent}%)`,
                  (props.payload as { name: string }).name,
                ]}
              />
              <Bar dataKey="count" radius={[6, 6, 0, 0]} barSize={28}>
                {data.map((entry, i) => (
                  <Cell key={i} fill={entry.fill} />
                ))}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        </div>
      )}
    </ChartCard>
  );
}

interface EmployeeProps {
  employees: NonNullable<TaskAnalytics["byEmployee"]>;
  labels: {
    employee: string;
    new: string;
    progress: string;
    done: string;
    total: string;
    completion: string;
  };
  title: string;
}

export function EmployeeTaskBreakdown({ employees, labels, title }: EmployeeProps) {
  const chartData = employees.slice(0, 8).map((e) => ({
    name: e.fullName.split(" ")[0],
    fullName: e.fullName,
    new: e.newCount,
    progress: e.inProgressCount,
    done: e.doneCount,
    completion: e.completionRate,
  }));

  return (
    <ChartCard title={title}>
      <div className="h-[280px] mb-4">
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={chartData} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" vertical={false} />
            <XAxis dataKey="name" tick={{ fontSize: 10, fill: "hsl(var(--foreground) / 0.45)" }} axisLine={false} tickLine={false} />
            <YAxis tick={{ fontSize: 10, fill: "hsl(var(--foreground) / 0.45)" }} axisLine={false} tickLine={false} allowDecimals={false} />
            <Tooltip
              {...tooltipStyle}
              labelFormatter={(_, payload) => (payload?.[0]?.payload as { fullName: string })?.fullName ?? ""}
            />
            <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 11 }} />
            <Bar dataKey="new" name={labels.new} stackId="a" fill="#64748b" radius={[0, 0, 0, 0]} />
            <Bar dataKey="progress" name={labels.progress} stackId="a" fill="#2563eb" />
            <Bar dataKey="done" name={labels.done} stackId="a" fill="#059669" radius={[4, 4, 0, 0]} />
          </BarChart>
        </ResponsiveContainer>
      </div>

      <div className="overflow-x-auto border-t border-border/50 pt-4">
        <table className="w-full text-sm">
          <thead>
            <tr className="text-left text-[11px] text-foreground/45 uppercase tracking-wider">
              <th className="pb-2 font-semibold">{labels.employee}</th>
              <th className="pb-2 font-semibold w-14 text-center">{labels.new}</th>
              <th className="pb-2 font-semibold w-14 text-center">{labels.progress}</th>
              <th className="pb-2 font-semibold w-14 text-center">{labels.done}</th>
              <th className="pb-2 font-semibold w-16 text-center">{labels.total}</th>
              <th className="pb-2 font-semibold w-20 text-center">{labels.completion}</th>
            </tr>
          </thead>
          <tbody>
            {employees.map((e) => (
              <tr key={e.userId} className="border-t border-border/40 hover:bg-atg-amber/[0.03] transition-colors">
                <td className="py-3">
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
                <td className="py-3 text-center font-semibold text-slate-500 tabular-nums">{e.newCount}</td>
                <td className="py-3 text-center font-semibold text-atg-blue tabular-nums">{e.inProgressCount}</td>
                <td className="py-3 text-center font-semibold text-emerald-600 dark:text-emerald-400 tabular-nums">{e.doneCount}</td>
                <td className="py-3 text-center font-bold tabular-nums">{e.total}</td>
                <td className="py-3 text-center">
                  <span
                    className={cn(
                      "inline-flex rounded-full px-2 py-0.5 text-xs font-bold tabular-nums",
                      e.completionRate >= 70
                        ? "bg-emerald-500/12 text-emerald-700 dark:text-emerald-300"
                        : e.completionRate >= 40
                          ? "bg-amber-500/12 text-amber-700 dark:text-amber-300"
                          : "bg-red-500/12 text-red-700 dark:text-red-300"
                    )}
                  >
                    {e.completionRate}%
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </ChartCard>
  );
}

// Legacy exports kept for compatibility
export { TaskStatusDonutChart as TaskDonutChart };
export { TaskActivityChart as TaskTrendChart };
