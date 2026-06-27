"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import api from "@/lib/api";
import { TaskListItem, WorkTaskStatus, TaskSource, statusBadgeClass, sourceBadgeClass, priorityBadgeClass, statusLabel, sourceLabel, isDeptManager } from "@/lib/tasks";
import { useAuthStore } from "@/store/authStore";
import { Button } from "@/components/ui/Button";
import Link from "next/link";
import { Plus, Play, CheckCircle2, RotateCcw } from "lucide-react";
import { cn } from "@/lib/utils";

export default function TasksListPage() {
  const t = useTranslations("tasks");
  const locale = useLocale();
  const user = useAuthStore((s) => s.user);
  const isManager = user && isDeptManager(user.role);
  const [view, setView] = useState(isManager ? "department" : "mine");
  const [statusFilter, setStatusFilter] = useState<WorkTaskStatus | "">("");
  const [sourceFilter, setSourceFilter] = useState<TaskSource | "">("");
  const [items, setItems] = useState<TaskListItem[]>([]);
  const [loading, setLoading] = useState(true);

  const load = () => {
    setLoading(true);
    const params = new URLSearchParams({ view, pageSize: "100" });
    if (statusFilter) params.set("status", statusFilter);
    if (sourceFilter) params.set("source", sourceFilter);
    api.get(`/tasks?${params}`).then((r) => setItems(r.data.items)).finally(() => setLoading(false));
  };

  useEffect(() => { load(); }, [view, statusFilter, sourceFilter]);

  const updateStatus = async (id: string, status: WorkTaskStatus) => {
    await api.patch(`/tasks/${id}/status`, { status });
    load();
  };

  const sl = (s: WorkTaskStatus) => statusLabel(s, (k) => t(k as "status.New"));
  const src = (s: TaskSource) => sourceLabel(s, (k) => t(k as "sources.Manual"));
  const views = isManager ? (["mine", "department"] as const) : (["mine"] as const);
  const statuses: (WorkTaskStatus | "")[] = ["", "New", "InProgress", "Done"];
  const sources: (TaskSource | "")[] = ["", "Manual", "HelpDesk", "DCS", "HR"];

  return (
    <>
      <header className="shrink-0 border-b border-border/80 bg-surface/80 backdrop-blur-sm px-6 py-4">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-xl font-semibold">{t("list.title")}</h1>
            <p className="text-sm text-foreground/50 mt-0.5">{t("list.subtitle")}</p>
          </div>
          <Link href={`/${locale}/tasks/new`}>
            <Button size="sm" className="bg-atg-amber hover:bg-orange-600">
              <Plus size={14} className="mr-1.5" />
              {t("nav.create")}
            </Button>
          </Link>
        </div>
      </header>

      <div className="flex-1 overflow-auto px-6 py-5 max-w-6xl">
        <div className="flex flex-wrap gap-2 mb-5">
          {views.map((v) => (
            <button
              key={v}
              type="button"
              onClick={() => setView(v)}
              className={cn(
                "px-3.5 py-1.5 rounded-lg text-[13px] font-medium border transition-all",
                view === v
                  ? "bg-atg-amber/12 text-atg-amber border-atg-amber/30"
                  : "border-border/60 text-foreground/50 hover:text-foreground"
              )}
            >
              {t(`views.${v}`)}
            </button>
          ))}
          <div className="w-px h-6 bg-border/60 self-center mx-1" />
          {statuses.map((s) => (
            <button
              key={s || "all"}
              type="button"
              onClick={() => setStatusFilter(s)}
              className={cn(
                "px-3 py-1.5 rounded-lg text-[13px] font-medium transition-all",
                statusFilter === s
                  ? "bg-foreground/10 text-foreground"
                  : "text-foreground/45 hover:text-foreground"
              )}
            >
              {s ? sl(s) : t("list.allStatuses")}
            </button>
          ))}
          <div className="w-px h-6 bg-border/60 self-center mx-1" />
          {sources.map((s) => (
            <button
              key={s || "all-src"}
              type="button"
              onClick={() => setSourceFilter(s)}
              className={cn(
                "px-3 py-1.5 rounded-lg text-[13px] font-medium transition-all",
                sourceFilter === s ? "bg-atg-amber/12 text-atg-amber" : "text-foreground/45 hover:text-foreground"
              )}
            >
              {s ? src(s) : t("list.allSources")}
            </button>
          ))}
        </div>

        <div className="rounded-2xl border border-border/80 bg-surface shadow-sm overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-[11px] text-foreground/45 uppercase tracking-wider bg-foreground/[0.02] border-b border-border">
                <th className="px-4 py-3 font-semibold w-28">Key</th>
                <th className="px-4 py-3 font-semibold w-24">{t("fields.source")}</th>
                <th className="px-4 py-3 font-semibold">{t("fields.summary")}</th>
                <th className="px-4 py-3 font-semibold w-28">{t("fields.status")}</th>
                <th className="px-4 py-3 font-semibold w-24">{t("fields.priority")}</th>
                {isManager && <th className="px-4 py-3 font-semibold w-36">{t("fields.assignee")}</th>}
                <th className="px-4 py-3 font-semibold w-32">{t("fields.actions")}</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr><td colSpan={isManager ? 7 : 6} className="px-4 py-16 text-center text-foreground/40">{t("loading")}</td></tr>
              ) : items.length === 0 ? (
                <tr><td colSpan={isManager ? 7 : 6} className="px-4 py-16 text-center text-foreground/40">{t("list.empty")}</td></tr>
              ) : items.map((task) => (
                <tr key={`${task.source}-${task.id}`} className="border-b border-border/40 hover:bg-atg-amber/[0.02] transition-colors">
                  <td className="px-4 py-3 font-mono text-xs font-bold text-atg-amber">
                    {task.source === "HelpDesk" && task.externalId ? (
                      <Link href={`/${locale}/helpdesk/tickets/${task.externalId}`} className="hover:underline">{task.number}</Link>
                    ) : task.number}
                  </td>
                  <td className="px-4 py-3">
                    <span className={cn("px-1.5 py-0.5 rounded text-[9px] font-bold uppercase border", sourceBadgeClass(task.source))}>
                      {src(task.source)}
                    </span>
                  </td>
                  <td className="px-4 py-3 font-medium max-w-xs truncate">{task.title}</td>
                  <td className="px-4 py-3">
                    <span className={cn("px-2 py-0.5 rounded-md text-[10px] font-semibold uppercase border", statusBadgeClass(task.status))}>
                      {sl(task.status)}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <span className={cn("px-1.5 py-0.5 rounded text-[10px] font-bold uppercase", priorityBadgeClass(task.priority))}>
                      {task.priority}
                    </span>
                  </td>
                  {isManager && (
                    <td className="px-4 py-3 text-foreground/60 text-[13px]">{task.assigneeName}</td>
                  )}
                  <td className="px-4 py-3">
                    <div className="flex gap-1">
                      {task.isEditable && task.status === "New" && (
                        <button
                          type="button"
                          title={t("actions.start")}
                          onClick={() => updateStatus(task.id, "InProgress")}
                          className="p-1.5 rounded-md hover:bg-atg-blue/10 text-atg-blue"
                        >
                          <Play size={14} />
                        </button>
                      )}
                      {task.isEditable && task.status === "InProgress" && (
                        <>
                          <button
                            type="button"
                            title={t("actions.done")}
                            onClick={() => updateStatus(task.id, "Done")}
                            className="p-1.5 rounded-md hover:bg-emerald-500/10 text-emerald-600"
                          >
                            <CheckCircle2 size={14} />
                          </button>
                          <button
                            type="button"
                            title={t("actions.reopen")}
                            onClick={() => updateStatus(task.id, "New")}
                            className="p-1.5 rounded-md hover:bg-border/50 text-foreground/50"
                          >
                            <RotateCcw size={14} />
                          </button>
                        </>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </>
  );
}
