"use client";

import { useLocale, useTranslations } from "next-intl";
import {
  MarketingStats,
  categoryLabel,
  procurementMethodLabel,
  type MarketingRequestCategory,
  type ProcurementMethodType,
} from "@/lib/marketing";
import { cn } from "@/lib/utils";
import { dcsTheme } from "@/components/dcs/dcsTheme";
import { AlertTriangle, CheckCircle2, Layers, TrendingUp } from "lucide-react";

const CATEGORY_COLORS = ["#ec4899", "#8b5cf6", "#3b82f6", "#10b981"];

export function MarketingDashboard({ stats }: { stats: MarketingStats }) {
  const t = useTranslations("dcs.marketing.dashboard");
  const locale = useLocale();

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <KpiCard label={t("total")} value={stats.total} accent="from-pink-600 to-rose-500" icon={Layers} />
        <KpiCard label={t("inProgress")} value={stats.inProgress} accent="from-violet-500 to-purple-500" icon={TrendingUp} />
        <KpiCard label={t("overdue")} value={stats.overdue} accent="from-orange-500 to-red-500" icon={AlertTriangle} />
        <KpiCard label={t("completed")} value={stats.completed} accent="from-emerald-500 to-teal-500" icon={CheckCircle2} />
      </div>

      <div className="grid lg:grid-cols-2 gap-6">
        <ChartCard title={t("byCategory")}>
          <CategoryDonut
            data={stats.byCategory.map((c) => ({
              label: categoryLabel(c.category as MarketingRequestCategory, locale),
              count: c.count,
            }))}
          />
        </ChartCard>
        <ChartCard title={t("byMethod")}>
          <BarChart
            items={stats.byMethod.map((m) => ({
              label: procurementMethodLabel(m.method as ProcurementMethodType, locale),
              count: m.count,
            }))}
          />
        </ChartCard>
      </div>

      {stats.byExecutor.length > 0 && (
        <ChartCard title={t("byExecutor")}>
          <ExecutorTable
            rows={stats.byExecutor}
            labels={{ executor: t("executor"), total: t("count"), overdue: t("overdueCol") }}
          />
        </ChartCard>
      )}
    </div>
  );
}

function KpiCard({
  label, value, accent, icon: Icon,
}: {
  label: string;
  value: number;
  accent: string;
  icon: React.ComponentType<{ size?: number; strokeWidth?: number }>;
}) {
  return (
    <div className={cn("relative overflow-hidden p-5", dcsTheme.premiumCard)}>
      <div className={cn("absolute -right-4 -top-4 w-24 h-24 rounded-full bg-gradient-to-br opacity-[0.12] blur-2xl", accent)} />
      <p className="text-[10px] font-bold uppercase tracking-[0.14em] text-foreground/40">{label}</p>
      <div className="flex items-end justify-between mt-2">
        <p className="text-3xl font-bold tabular-nums">{value}</p>
        <div className={cn("w-10 h-10 rounded-xl flex items-center justify-center bg-gradient-to-br text-white shadow-lg", accent)}>
          <Icon size={18} strokeWidth={2} />
        </div>
      </div>
    </div>
  );
}

function ChartCard({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className={cn("p-5", dcsTheme.premiumCard)}>
      <h3 className="text-sm font-bold mb-4">{title}</h3>
      {children}
    </div>
  );
}

function CategoryDonut({ data }: { data: { label: string; count: number }[] }) {
  const total = data.reduce((s, d) => s + d.count, 0) || 1;
  const radius = 54;
  const circumference = 2 * Math.PI * radius;
  let offset = 0;

  const segments = data.map((d, i) => {
    const dash = (d.count / total) * circumference;
    const seg = { ...d, dash, offset, color: CATEGORY_COLORS[i % CATEGORY_COLORS.length] };
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
              key={s.label}
              cx="70" cy="70" r={radius}
              fill="none" stroke={s.color} strokeWidth="14"
              strokeDasharray={`${s.dash} ${circumference - s.dash}`}
              strokeDashoffset={-s.offset}
              strokeLinecap="round"
            />
          ))}
        </svg>
        <div className="absolute inset-0 flex flex-col items-center justify-center">
          <span className="text-2xl font-bold tabular-nums">{total}</span>
        </div>
      </div>
      <div className="flex-1 space-y-2 w-full">
        {segments.map((s) => (
          <div key={s.label} className="flex items-center gap-2 text-sm">
            <span className="w-2.5 h-2.5 rounded-full shrink-0" style={{ background: s.color }} />
            <span className="flex-1 text-foreground/70 truncate">{s.label}</span>
            <span className="font-bold tabular-nums">{s.count}</span>
          </div>
        ))}
      </div>
    </div>
  );
}

function BarChart({ items }: { items: { label: string; count: number }[] }) {
  const max = Math.max(...items.map((i) => i.count), 1);
  if (items.length === 0) return <p className="text-sm text-foreground/40">—</p>;

  return (
    <div className="space-y-3">
      {items.map((item) => (
        <div key={item.label}>
          <div className="flex justify-between text-xs mb-1">
            <span className="text-foreground/65 truncate pr-2">{item.label}</span>
            <span className="font-bold tabular-nums">{item.count}</span>
          </div>
          <div className="h-2 rounded-full bg-foreground/[0.06] overflow-hidden">
            <div
              className="h-full rounded-full bg-gradient-to-r from-pink-500 to-violet-500 transition-all"
              style={{ width: `${(item.count / max) * 100}%` }}
            />
          </div>
        </div>
      ))}
    </div>
  );
}

function ExecutorTable({
  rows,
  labels,
}: {
  rows: { executorName: string; count: number; overdue: number }[];
  labels: { executor: string; total: string; overdue: string };
}) {
  return (
    <table className="w-full text-sm">
      <thead>
        <tr className="text-left text-[10px] uppercase tracking-wider text-foreground/45 border-b border-border/60">
          <th className="pb-2 font-bold">{labels.executor}</th>
          <th className="pb-2 font-bold w-24 text-right">{labels.total}</th>
          <th className="pb-2 font-bold w-24 text-right">{labels.overdue}</th>
        </tr>
      </thead>
      <tbody>
        {rows.map((r) => (
          <tr key={r.executorName} className="border-b border-border/30 last:border-0">
            <td className="py-2.5 font-medium">{r.executorName}</td>
            <td className="py-2.5 text-right tabular-nums font-semibold">{r.count}</td>
            <td className={cn("py-2.5 text-right tabular-nums font-semibold", r.overdue > 0 && "text-red-600")}>
              {r.overdue}
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
