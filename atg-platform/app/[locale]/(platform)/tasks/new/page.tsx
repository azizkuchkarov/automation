"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import api from "@/lib/api";
import { TaskPriority } from "@/lib/tasks";
import { useAuthStore } from "@/store/authStore";
import { isDeptManager } from "@/lib/tasks";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

interface Assignee {
  id: string;
  fullName: string;
  employeeId?: string;
}

export default function CreateTaskPage() {
  const t = useTranslations("tasks");
  const locale = useLocale();
  const router = useRouter();
  const user = useAuthStore((s) => s.user);
  const canAssignOthers = user && isDeptManager(user.role);

  const [assignees, setAssignees] = useState<Assignee[]>([]);
  const [assigneeId, setAssigneeId] = useState("");
  const [priority, setPriority] = useState<TaskPriority>("Medium");
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [dueDate, setDueDate] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    api.get("/tasks/assignees").then((r) => {
      setAssignees(r.data);
      if (user && !canAssignOthers) setAssigneeId(user.id);
      else if (r.data.length) setAssigneeId(r.data[0].id);
    });
  }, [user, canAssignOthers]);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setSubmitting(true);
    try {
      await api.post("/tasks", {
        title,
        description,
        assigneeId,
        priority,
        dueDate: dueDate || null,
      });
      router.push(`/${locale}/tasks/dashboard`);
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error;
      setError(msg ?? t("create.error"));
    } finally {
      setSubmitting(false);
    }
  };

  const inputClass =
    "w-full rounded-lg border border-border/80 bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-atg-amber/30 focus:border-atg-amber/50";

  return (
    <>
      <header className="shrink-0 border-b border-border/80 bg-surface/80 px-6 py-4">
        <h1 className="text-xl font-semibold">{t("create.title")}</h1>
        <p className="text-sm text-foreground/50 mt-0.5">{t("create.subtitle")}</p>
      </header>

      <div className="flex-1 overflow-auto px-6 py-5">
        <form onSubmit={submit} className="max-w-2xl rounded-2xl border border-border/80 bg-surface p-6 shadow-sm space-y-5">
          {canAssignOthers && (
            <div>
              <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
                {t("fields.assignee")}
              </label>
              <select
                value={assigneeId}
                onChange={(e) => setAssigneeId(e.target.value)}
                className={cn(inputClass, "h-10")}
                required
              >
                {assignees.map((a) => (
                  <option key={a.id} value={a.id}>
                    {a.fullName} {a.employeeId ? `(${a.employeeId})` : ""}
                  </option>
                ))}
              </select>
            </div>
          )}

          <div>
            <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
              {t("fields.priority")}
            </label>
            <select
              value={priority}
              onChange={(e) => setPriority(e.target.value as TaskPriority)}
              className={cn(inputClass, "h-10")}
            >
              {(["Low", "Medium", "High", "Critical"] as TaskPriority[]).map((p) => (
                <option key={p} value={p}>{p}</option>
              ))}
            </select>
          </div>

          <div>
            <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
              {t("fields.summary")}
            </label>
            <input
              required
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              maxLength={200}
              placeholder={t("create.summaryPlaceholder")}
              className={cn(inputClass, "h-10")}
            />
          </div>

          <div>
            <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
              {t("fields.description")}
            </label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              rows={5}
              placeholder={t("create.descPlaceholder")}
              className={cn(inputClass, "py-2.5 resize-y")}
            />
          </div>

          <div>
            <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
              {t("fields.dueDate")}
            </label>
            <input
              type="date"
              value={dueDate}
              onChange={(e) => setDueDate(e.target.value)}
              className={cn(inputClass, "h-10")}
            />
          </div>

          {error && (
            <p className="text-sm text-red-500 bg-red-500/8 border border-red-500/20 rounded-lg px-3 py-2">{error}</p>
          )}

          <div className="flex gap-3 pt-2 border-t border-border/60">
            <Button type="submit" disabled={submitting} className="bg-atg-amber hover:bg-orange-600">
              {t("create.submit")}
            </Button>
            <Button type="button" variant="secondary" onClick={() => router.back()}>
              {t("create.cancel")}
            </Button>
          </div>
        </form>
      </div>
    </>
  );
}
