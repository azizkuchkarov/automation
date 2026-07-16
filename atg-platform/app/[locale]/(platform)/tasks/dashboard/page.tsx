"use client";

import { useEffect, useState, useCallback } from "react";
import { useTranslations } from "next-intl";
import Link from "next/link";
import { useLocale } from "next-intl";
import api from "@/lib/api";
import {
  TaskAnalytics,
  TaskNavigationDto,
  TaskPriority,
  statusLabel,
  sourceLabel,
  priorityLabel,
  statusBadgeClass,
  sourceBadgeClass,
  canUseOrgNav,
  buildAnalyticsParams,
} from "@/lib/tasks";
import { useAuthStore } from "@/store/authStore";
import {
  TaskKpiRow,
  TaskInsightsPanel,
  TaskStatusDonutChart,
  TaskActivityChart,
  TaskVelocityChart,
  TaskPriorityRadarChart,
  TaskAgingChart,
  TaskSourceChart,
  EmployeeTaskBreakdown,
} from "@/components/tasks/TaskCharts";
import {
  TaskHealthGauge,
  TaskSlaPanel,
  TaskCycleTimeChart,
  TaskActivityHeatmap,
  TaskForecastChart,
  TaskBurndownChart,
  TaskWorkloadPanel,
  TaskPriorityMatrixChart,
  TaskRiskQueue,
} from "@/components/tasks/TaskAdvancedCharts";
import { TaskOrgNavigator, TaskScopeSelection } from "@/components/tasks/TaskOrgNavigator";
import { Button } from "@/components/ui/Button";
import { RefreshCw, Plus, Users, User, BarChart3, BrainCircuit } from "lucide-react";
import { cn } from "@/lib/utils";

export default function TasksDashboardPage() {
  const t = useTranslations("tasks");
  const locale = useLocale();
  const user = useAuthStore((s) => s.user);
  const showNav = user && canUseOrgNav(user.role);

  const [scope, setScope] = useState<TaskScopeSelection>({ label: "Enterprise" });
  const [navigation, setNavigation] = useState<TaskNavigationDto | null>(null);
  const [data, setData] = useState<TaskAnalytics | null>(null);
  const [loading, setLoading] = useState(true);

  const load = useCallback(() => {
    setLoading(true);
    const params = buildAnalyticsParams(scope);
    Promise.all([
      api.get(`/tasks/analytics?${params}`),
      showNav ? api.get("/tasks/navigation") : Promise.resolve({ data: null }),
    ])
      .then(([analyticsRes, navRes]) => {
        setData(analyticsRes.data);
        if (navRes.data) setNavigation(navRes.data);
      })
      .finally(() => setLoading(false));
  }, [scope, showNav]);

  useEffect(() => { load(); }, [load]);

  const sl = (s: Parameters<typeof statusLabel>[0]) => statusLabel(s, (k) => t(k as "status.New"));
  const src = (s: Parameters<typeof sourceLabel>[0]) => sourceLabel(s, (k) => t(k as "sources.Manual"));
  const pl = (p: TaskPriority) => priorityLabel(p, (k) => t(k as "priority.Low"));

  const insightText = (code: string, value?: number, context?: string) => {
    switch (code) {
      case "overdue":
        return t("insights.overdue", { count: Math.round(value ?? 0) });
      case "high_completion":
      case "low_completion":
        return t(`insights.${code}` as "insights.high_completion", { value: value ?? 0 });
      case "throughput_up":
      case "throughput_down":
        return t(`insights.${code}` as "insights.throughput_up", { value: Math.abs(value ?? 0) });
      case "dominant_source":
        return t("insights.dominant_source", {
          value: value ?? 0,
          context: context ? src(context as Parameters<typeof sourceLabel>[0]) : "",
        });
      case "stale_tasks":
        return t("insights.stale_tasks", { count: Math.round(value ?? 0) });
      case "sla_breach":
      case "sla_excellent":
        return t(`insights.${code}` as "insights.sla_breach", { value: value ?? 0 });
      case "workload_imbalance":
      case "workload_balanced":
        return t(`insights.${code}` as "insights.workload_balanced", { value: value ?? 0 });
      case "critical_risk":
        return t("insights.critical_risk", { count: Math.round(value ?? 0) });
      case "health_excellent":
      case "health_critical":
        return t(`insights.${code}` as "insights.health_excellent", { value: value ?? 0, context: context ?? "" });
      case "forecast_surge":
        return t("insights.forecast_surge", { value: Math.round(value ?? 0) });
      default:
        return code;
    }
  };

  const scopeSubtitle = () => {
    if (!data) return "";
    if (data.scope === "personal") return t("dashboard.personalScope");
    if (data.departmentId) return t("dashboard.deptScope", { name: data.scopeLabel });
    if (data.organizationId) return t("dashboard.orgScope", { name: data.scopeLabel });
    return showNav ? t("dashboard.enterpriseScope") : t("dashboard.deptScope", { name: data.scopeLabel });
  };

  const kpiLabels = {
    new: t("kpi.new"),
    progress: t("kpi.progress"),
    done: t("kpi.done"),
    completion: t("kpi.completion"),
    overdue: t("kpi.overdue"),
    avgResolution: t("kpi.avgResolution"),
    throughput: t("kpi.throughput"),
    days: t("kpi.days"),
  };

  const trendLabels = { new: t("kpi.new"), progress: t("kpi.progress"), done: t("kpi.done") };

  return (
    <>
      <header className="shrink-0 border-b border-border/80 bg-surface/80 backdrop-blur-sm px-6 py-4">
        <div className="flex items-start justify-between gap-4">
          <div>
            <div className="flex items-center gap-2 mb-1">
              <BarChart3 size={16} className="text-atg-amber" />
              <span className="text-xs text-foreground/45 uppercase tracking-wider">
                {t("dashboard.analytics")}
              </span>
            </div>
            <h1 className="text-xl font-semibold tracking-tight">{t("dashboard.title")}</h1>
            <p className="text-sm text-foreground/55 mt-1 flex items-center gap-2">
              {showNav || (user && data?.scope !== "personal") ? <Users size={14} /> : <User size={14} />}
              <span>{scopeSubtitle()}</span>
            </p>
          </div>
          <div className="flex gap-2">
            <Button variant="secondary" size="sm" onClick={load} disabled={loading}>
              <RefreshCw size={14} className={loading ? "animate-spin" : ""} />
            </Button>
            <Link href={`/${locale}/tasks/new`}>
              <Button size="sm" className="bg-atg-amber hover:bg-orange-600">
                <Plus size={14} className="mr-1.5" />
                {t("nav.create")}
              </Button>
            </Link>
          </div>
        </div>
      </header>

      <div className="flex-1 overflow-auto">
        <div className={cn("px-6 py-5 gap-5", showNav && navigation ? "flex" : "space-y-5")}>
          {showNav && navigation && (
            <div className="w-[280px] shrink-0 hidden lg:block">
              <TaskOrgNavigator navigation={navigation} selection={scope} onSelect={setScope} />
            </div>
          )}

          <div className="flex-1 min-w-0 space-y-5">
            {loading && !data ? (
              <div className="space-y-4">
                <div className="grid grid-cols-2 xl:grid-cols-4 gap-4">
                  {Array.from({ length: 4 }).map((_, i) => (
                    <div key={i} className="h-28 rounded-2xl bg-border/30 animate-pulse" />
                  ))}
                </div>
                <div className="grid lg:grid-cols-3 gap-5">
                  {Array.from({ length: 3 }).map((_, i) => (
                    <div key={i} className="h-72 rounded-2xl bg-border/30 animate-pulse" />
                  ))}
                </div>
              </div>
            ) : data ? (
              <>
                <TaskKpiRow analytics={data} labels={kpiLabels} />

                <TaskInsightsPanel
                  insights={data.insights ?? []}
                  insightLabel={insightText}
                  title={t("dashboard.insights")}
                />

                {data.healthScore && (
                  <>
                    <div className="flex items-center gap-2 pt-1">
                      <BrainCircuit size={18} className="text-atg-amber" />
                      <h2 className="text-sm font-bold uppercase tracking-wider text-foreground/60">
                        {t("dashboard.advanced")}
                      </h2>
                      <div className="flex-1 h-px bg-border/60" />
                    </div>

                    <div className="grid lg:grid-cols-3 gap-5">
                      <TaskHealthGauge
                        health={data.healthScore}
                        labels={{
                          title: t("dashboard.health"),
                          subtitle: t("dashboard.healthSubtitle"),
                          score: t("health.score"),
                          completion: t("health.completion"),
                          sla: t("health.sla"),
                          velocity: t("health.velocity"),
                          balance: t("health.balance"),
                          penalty: t("health.penalty"),
                        }}
                      />
                      {data.slaMetrics && (
                        <TaskSlaPanel
                          sla={data.slaMetrics}
                          labels={{
                            title: t("dashboard.sla"),
                            subtitle: t("dashboard.slaSubtitle"),
                            compliance: t("sla.compliance"),
                            onTime: t("sla.onTime"),
                            late: t("sla.late"),
                            atRisk: t("sla.atRisk"),
                            withDue: t("sla.withDue"),
                          }}
                        />
                      )}
                      {data.workloadBalance && (
                        <TaskWorkloadPanel
                          balance={data.workloadBalance}
                          labels={{
                            title: t("dashboard.workload"),
                            subtitle: t("dashboard.workloadSubtitle"),
                            score: t("workload.score"),
                            gini: t("workload.gini"),
                            assignees: t("workload.assignees"),
                            avg: t("workload.avg"),
                            max: t("workload.max"),
                          }}
                        />
                      )}
                    </div>

                    <div className="grid md:grid-cols-2 xl:grid-cols-3 gap-5">
                      {data.cycleTime && (
                        <TaskCycleTimeChart
                          cycle={data.cycleTime}
                          labels={{
                            title: t("dashboard.cycleTime"),
                            subtitle: t("dashboard.cycleTimeSubtitle"),
                            p50: t("cycle.p50"),
                            p75: t("cycle.p75"),
                            p90: t("cycle.p90"),
                            mean: t("cycle.mean"),
                            days: t("kpi.days"),
                          }}
                        />
                      )}
                      {data.activityHeatmap && (
                        <TaskActivityHeatmap
                          cells={data.activityHeatmap}
                          labels={{
                            title: t("dashboard.heatmap"),
                            subtitle: t("dashboard.heatmapSubtitle"),
                            created: t("heatmap.created"),
                            completed: t("heatmap.completed"),
                          }}
                        />
                      )}
                      {data.priorityMatrix && (
                        <TaskPriorityMatrixChart
                          matrix={data.priorityMatrix}
                          priorityLabel={pl}
                          statusLabel={sl}
                          labels={{
                            title: t("dashboard.matrix"),
                            subtitle: t("dashboard.matrixSubtitle"),
                          }}
                        />
                      )}
                    </div>

                    <div className="grid lg:grid-cols-2 gap-5">
                      {data.completionForecast && (
                        <TaskForecastChart
                          forecast={data.completionForecast}
                          labels={{
                            title: t("dashboard.forecast"),
                            subtitle: t("dashboard.forecastSubtitle"),
                            actual: t("forecast.actual"),
                            projected: t("forecast.projected"),
                          }}
                        />
                      )}
                      {data.burndown && (
                        <TaskBurndownChart
                          burndown={data.burndown}
                          labels={{
                            title: t("dashboard.burndown"),
                            subtitle: t("dashboard.burndownSubtitle"),
                            remaining: t("burndown.remaining"),
                            ideal: t("burndown.ideal"),
                            completed: t("burndown.completed"),
                          }}
                        />
                      )}
                    </div>

                    {data.riskQueue && data.riskQueue.length > 0 && (
                      <TaskRiskQueue
                        risks={data.riskQueue}
                        priorityLabel={pl}
                        labels={{
                          title: t("dashboard.riskQueue"),
                          subtitle: t("dashboard.riskQueueSubtitle"),
                          task: t("risk.task"),
                          assignee: t("risk.assignee"),
                          priority: t("risk.priority"),
                          score: t("risk.score"),
                          age: t("risk.age"),
                          days: t("kpi.days"),
                          overdue: t("risk.overdue"),
                        }}
                      />
                    )}
                  </>
                )}

                <div className="grid lg:grid-cols-3 gap-5">
                  <TaskActivityChart
                    trend={data.weeklyTrend ?? []}
                    labels={trendLabels}
                    title={t("dashboard.trend")}
                    subtitle={t("dashboard.trendSubtitle")}
                  />
                  <TaskStatusDonutChart
                    distribution={data.statusDistribution}
                    statusLabel={sl}
                    title={t("dashboard.distribution")}
                  />
                </div>

                <div className="grid md:grid-cols-2 xl:grid-cols-3 gap-5">
                  <TaskVelocityChart
                    velocity={data.velocityTrend ?? []}
                    labels={{
                      completed: t("velocity.completed"),
                      movingAvg: t("velocity.movingAvg"),
                    }}
                    title={t("dashboard.velocity")}
                    subtitle={t("dashboard.velocitySubtitle")}
                  />
                  <TaskPriorityRadarChart
                    byPriority={data.byPriority ?? []}
                    priorityLabel={pl}
                    title={t("dashboard.priority")}
                    subtitle={t("dashboard.prioritySubtitle")}
                  />
                  <TaskAgingChart
                    buckets={data.agingBuckets ?? []}
                    bucketLabel={(key) => t(`aging.${key}` as "aging.0_3")}
                    title={t("dashboard.aging")}
                    subtitle={t("dashboard.agingSubtitle")}
                  />
                </div>

                <div className="grid lg:grid-cols-2 gap-5">
                  <TaskSourceChart
                    bySource={data.bySource}
                    sourceLabel={src}
                    title={t("dashboard.bySource")}
                  />

                  {data.byEmployee && data.byEmployee.length > 0 ? (
                    <EmployeeTaskBreakdown
                      employees={data.byEmployee}
                      title={t("dashboard.byEmployee")}
                      labels={{
                        employee: t("fields.employee"),
                        new: t("kpi.new"),
                        progress: t("kpi.progress"),
                        done: t("kpi.done"),
                        total: t("fields.total"),
                        completion: t("kpi.completion"),
                      }}
                    />
                  ) : (
                    <div className="rounded-2xl border border-border/80 bg-surface p-5 shadow-sm flex items-center justify-center min-h-[220px]">
                      <p className="text-sm text-foreground/40">{t("dashboard.personalScope")}</p>
                    </div>
                  )}
                </div>

                <div className="rounded-2xl border border-border/80 bg-surface p-5 shadow-sm">
                  <div className="flex items-center justify-between mb-4">
                    <h2 className="text-sm font-semibold text-foreground/80">{t("dashboard.recent")}</h2>
                    <Link href={`/${locale}/tasks/list`} className="text-xs text-atg-amber hover:underline">
                      {t("dashboard.viewAll")}
                    </Link>
                  </div>
                  {data.recentTasks.length === 0 ? (
                    <p className="text-sm text-foreground/40 text-center py-8">{t("list.empty")}</p>
                  ) : (
                    <div className="space-y-2">
                      {data.recentTasks.map((task) => (
                        <Link
                          key={`${task.source}-${task.id}`}
                          href={task.source === "HelpDesk" && task.externalId
                            ? `/${locale}/helpdesk/tickets/${task.externalId}`
                            : task.source === "DCS" && task.externalId
                              ? `/${locale}/automation/documents/${task.externalId}`
                              : `/${locale}/tasks/list`}
                          className="flex items-center gap-3 p-3 rounded-xl border border-border/50 hover:border-atg-amber/30 hover:bg-atg-amber/[0.02] transition-all group"
                        >
                          <span className="font-mono text-xs font-bold text-atg-amber w-24 shrink-0">{task.number}</span>
                          <span className={cn("px-1.5 py-0.5 rounded text-[9px] font-bold uppercase border shrink-0", sourceBadgeClass(task.source))}>
                            {src(task.source)}
                          </span>
                          <span className="flex-1 text-sm font-medium truncate">{task.title}</span>
                          <span className={cn("px-2 py-0.5 rounded-md text-[10px] font-semibold uppercase border shrink-0", statusBadgeClass(task.status))}>
                            {sl(task.status)}
                          </span>
                        </Link>
                      ))}
                    </div>
                  )}
                </div>
              </>
            ) : null}
          </div>
        </div>
      </div>
    </>
  );
}
