"use client";

import { useEffect, useState, useCallback } from "react";
import { useTranslations } from "next-intl";
import Link from "next/link";
import { useLocale } from "next-intl";
import api from "@/lib/api";
import {
  TaskAnalytics,
  TaskNavigationDto,
  statusLabel,
  sourceLabel,
  statusBadgeClass,
  sourceBadgeClass,
  priorityBadgeClass,
  canUseOrgNav,
  buildAnalyticsParams,
} from "@/lib/tasks";
import { useAuthStore } from "@/store/authStore";
import {
  TaskKpiRow,
  TaskDonutChart,
  TaskTrendChart,
  TaskSourceChart,
  EmployeeTaskBreakdown,
} from "@/components/tasks/TaskCharts";
import { TaskOrgNavigator, TaskScopeSelection } from "@/components/tasks/TaskOrgNavigator";
import { Button } from "@/components/ui/Button";
import { RefreshCw, Plus, Users, User, BarChart3 } from "lucide-react";
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

  const scopeSubtitle = () => {
    if (!data) return "";
    if (data.scope === "personal") return t("dashboard.personalScope");
    if (data.departmentId) return t("dashboard.deptScope", { name: data.scopeLabel });
    if (data.organizationId) return t("dashboard.orgScope", { name: data.scopeLabel });
    return showNav ? t("dashboard.enterpriseScope") : t("dashboard.deptScope", { name: data.scopeLabel });
  };

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
              <div className="grid grid-cols-2 xl:grid-cols-4 gap-4">
                {Array.from({ length: 4 }).map((_, i) => (
                  <div key={i} className="h-28 rounded-2xl bg-border/30 animate-pulse" />
                ))}
              </div>
            ) : data ? (
              <>
                <TaskKpiRow
                  analytics={data}
                  labels={{
                    new: t("kpi.new"),
                    progress: t("kpi.progress"),
                    done: t("kpi.done"),
                    completion: t("kpi.completion"),
                  }}
                />

                <div className="grid lg:grid-cols-3 gap-5">
                  <div className="rounded-2xl border border-border/80 bg-surface p-5 shadow-sm">
                    <h2 className="text-sm font-semibold mb-4 text-foreground/80">{t("dashboard.bySource")}</h2>
                    <TaskSourceChart bySource={data.bySource} sourceLabel={src} />
                  </div>
                  <div className="rounded-2xl border border-border/80 bg-surface p-5 shadow-sm">
                    <h2 className="text-sm font-semibold mb-4 text-foreground/80">{t("dashboard.distribution")}</h2>
                    <TaskDonutChart distribution={data.statusDistribution} statusLabel={sl} />
                  </div>
                  <div className="rounded-2xl border border-border/80 bg-surface p-5 shadow-sm lg:col-span-1">
                    <h2 className="text-sm font-semibold mb-4 text-foreground/80">{t("dashboard.trend")}</h2>
                    <TaskTrendChart
                      trend={data.weeklyTrend}
                      labels={{ new: t("kpi.new"), progress: t("kpi.progress"), done: t("kpi.done") }}
                    />
                  </div>
                </div>

                {data.byEmployee && data.byEmployee.length > 0 && (
                  <div className="rounded-2xl border border-border/80 bg-surface p-5 shadow-sm">
                    <h2 className="text-sm font-semibold mb-4 text-foreground/80 flex items-center gap-2">
                      <Users size={16} className="text-atg-amber" />
                      {t("dashboard.byEmployee")}
                    </h2>
                    <EmployeeTaskBreakdown
                      employees={data.byEmployee}
                      labels={{
                        employee: t("fields.employee"),
                        new: t("kpi.new"),
                        progress: t("kpi.progress"),
                        done: t("kpi.done"),
                        total: t("fields.total"),
                      }}
                    />
                  </div>
                )}

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
