"use client";

import { useEffect, useState, useCallback } from "react";
import { useParams } from "next/navigation";
import { useTranslations, useLocale } from "next-intl";
import api from "@/lib/api";
import {
  Ticket,
  HelpDeskAssignee,
  canAssignTicket,
  canManageWorkflow,
  isPlatformAdmin,
  deptLabel,
} from "@/lib/helpdesk";
import { useAuthStore } from "@/store/authStore";
import { TicketPriorityBadge, TicketStatusBadge } from "@/components/helpdesk/TicketBadges";
import { HelpdeskPageHeader } from "@/components/helpdesk/HelpdeskPageHeader";
import { Button } from "@/components/ui/Button";
import {
  CheckCircle2, Play, ThumbsUp, UserPlus, MessageSquare, Clock, Circle,
} from "lucide-react";
import { cn } from "@/lib/utils";

export default function TicketDetailPage() {
  const { id } = useParams<{ id: string }>();
  const t = useTranslations("helpdesk");
  const locale = useLocale();
  const user = useAuthStore((s) => s.user);
  const [ticket, setTicket] = useState<Ticket | null>(null);
  const [assignees, setAssignees] = useState<HelpDeskAssignee[]>([]);
  const [selectedAssignee, setSelectedAssignee] = useState("");
  const [comment, setComment] = useState("");
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState(false);

  const load = useCallback(() => {
    api.get(`/helpdesk/tickets/${id}`).then((r) => setTicket(r.data)).finally(() => setLoading(false));
  }, [id]);

  useEffect(() => { load(); }, [load]);

  useEffect(() => {
    if (ticket?.assigneeId) setSelectedAssignee(ticket.assigneeId);
  }, [ticket?.assigneeId]);

  const canAssign = user && ticket && canAssignTicket(user, ticket);
  const canWorkflow = user && ticket && canManageWorkflow(user, ticket);
  const isAdmin = user && isPlatformAdmin(user.role);

  useEffect(() => {
    if (canAssign && ticket) {
      api.get(`/helpdesk/tickets/${id}/assignees`).then((r) => setAssignees(r.data));
    }
  }, [canAssign, ticket?.status, id, ticket]);

  const action = async (endpoint: string, body?: object) => {
    setActionLoading(true);
    try {
      await api.post(`/helpdesk/tickets/${id}/${endpoint}`, body ?? {});
      load();
    } finally {
      setActionLoading(false);
    }
  };

  const addComment = async () => {
    if (!comment.trim()) return;
    await api.post(`/helpdesk/tickets/${id}/comments`, { body: comment });
    setComment("");
    load();
  };

  if (loading) {
    return (
      <div className="flex-1 flex items-center justify-center text-foreground/40">{t("loading")}</div>
    );
  }
  if (!ticket) {
    return (
      <div className="flex-1 flex items-center justify-center text-red-500">{t("notFound")}</div>
    );
  }

  const isAssignee = user?.id === ticket.assigneeId;
  const canClose = (user?.id === ticket.requesterId || isAdmin) && ticket.status === "Done";

  const inputClass =
    "rounded-lg border border-border/80 bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-atg-teal/30 focus:border-atg-teal/50";

  return (
    <div className="flex flex-col flex-1 min-h-0">
      <HelpdeskPageHeader
        title={ticket.title}
        breadcrumb={ticket.number}
        actions={
          <div className="flex items-center gap-2">
            <TicketStatusBadge status={ticket.status} />
            <TicketPriorityBadge priority={ticket.priority} />
          </div>
        }
      />

      <div className="flex flex-1 min-h-0 overflow-hidden">
        {/* Main */}
        <div className="flex-1 overflow-y-auto px-6 py-5 max-w-3xl">
          <section className="mb-6">
            <h2 className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-3">
              {t("fields.description")}
            </h2>
            <div className="rounded-xl border border-border/80 bg-surface p-4 text-sm leading-relaxed whitespace-pre-wrap shadow-sm">
              {ticket.description || "—"}
            </div>
          </section>

          {(canAssign || canWorkflow || canClose) && (
            <section className="mb-6 rounded-xl border border-border/80 bg-surface p-4 shadow-sm">
              <h2 className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-3">
                {t("actions.workflow")}
              </h2>
              <div className="flex flex-wrap gap-2">
                {canAssign && (
                  <div className="flex items-center gap-2 w-full p-3 rounded-lg border border-atg-teal/25 bg-atg-teal/5">
                    {isAdmin && (
                      <span className="text-[10px] font-bold uppercase tracking-wider text-atg-teal shrink-0 px-2 py-0.5 rounded bg-atg-teal/10">
                        {t("actions.adminAssign")}
                      </span>
                    )}
                    <select
                      value={selectedAssignee}
                      onChange={(e) => setSelectedAssignee(e.target.value)}
                      className={cn(inputClass, "flex-1 h-9")}
                    >
                      <option value="">{t("actions.selectAssignee")}</option>
                      {assignees.map((a) => (
                        <option key={a.id} value={a.id}>{a.fullName} ({a.role})</option>
                      ))}
                    </select>
                    <Button
                      size="sm"
                      disabled={!selectedAssignee || actionLoading}
                      onClick={() => action("assign", { assigneeId: selectedAssignee })}
                    >
                      <UserPlus size={14} className="mr-1" />
                      {t("actions.assign")}
                    </Button>
                  </div>
                )}
                {canWorkflow && ticket.status === "Assigned" && (
                  <Button size="sm" disabled={actionLoading} onClick={() => action("accept")}>
                    <ThumbsUp size={14} className="mr-1" />
                    {isAdmin && !isAssignee ? t("actions.approve") : t("actions.accept")}
                  </Button>
                )}
                {canWorkflow && (ticket.status === "Accepted" || ticket.status === "Assigned") && (
                  <Button size="sm" disabled={actionLoading} onClick={() => action("start")}>
                    <Play size={14} className="mr-1" />
                    {t("actions.start")}
                  </Button>
                )}
                {canWorkflow && ticket.status === "InProgress" && (
                  <Button size="sm" disabled={actionLoading} onClick={() => action("complete")}>
                    <CheckCircle2 size={14} className="mr-1" />
                    {t("actions.done")}
                  </Button>
                )}
                {canClose && (
                  <Button size="sm" disabled={actionLoading} onClick={() => action("close")}>
                    <CheckCircle2 size={14} className="mr-1" />
                    {t("actions.close")}
                  </Button>
                )}
              </div>
            </section>
          )}

          <section>
            <h2 className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-3 flex items-center gap-2">
              <MessageSquare size={14} />
              {t("activity.comments")}
            </h2>
            <div className="space-y-3 mb-4">
              {ticket.comments.length === 0 && (
                <p className="text-sm text-foreground/35 py-4 text-center border border-dashed border-border/60 rounded-lg">
                  {t("activity.noComments")}
                </p>
              )}
              {ticket.comments.map((c) => (
                <div
                  key={c.id}
                  className={cn(
                    "rounded-xl border p-3.5 shadow-sm",
                    c.isInternal
                      ? "border-amber-500/30 bg-amber-500/5"
                      : "border-border/80 bg-surface"
                  )}
                >
                  <div className="flex items-center gap-2 mb-2">
                    <span className="w-7 h-7 rounded-full bg-atg-teal/15 text-atg-teal flex items-center justify-center text-xs font-bold">
                      {c.authorName.charAt(0)}
                    </span>
                    <span className="text-sm font-semibold">{c.authorName}</span>
                    {c.isInternal && (
                      <span className="text-[10px] text-amber-600 dark:text-amber-400 uppercase font-bold px-1.5 py-0.5 rounded bg-amber-500/10">
                        Internal
                      </span>
                    )}
                    <span className="text-[11px] text-foreground/40 ml-auto">
                      {new Date(c.createdAt).toLocaleString()}
                    </span>
                  </div>
                  <p className="text-sm whitespace-pre-wrap text-foreground/85 pl-9">{c.body}</p>
                </div>
              ))}
            </div>
            <div className="flex gap-2 rounded-xl border border-border/80 bg-surface p-3 shadow-sm">
              <textarea
                value={comment}
                onChange={(e) => setComment(e.target.value)}
                rows={2}
                placeholder={t("activity.commentPlaceholder")}
                className={cn(inputClass, "flex-1 py-2 resize-none bg-background")}
              />
              <Button size="sm" onClick={addComment} className="self-end">
                {t("activity.addComment")}
              </Button>
            </div>
          </section>
        </div>

        {/* Right panel */}
        <aside className="w-[300px] shrink-0 border-l border-border/80 bg-surface/50 overflow-y-auto p-5 space-y-5">
          <PanelSection title={t("fields.status")}>
            <TicketStatusBadge status={ticket.status} />
          </PanelSection>

          <PanelSection title={t("fields.details")}>
            <DetailRow label={t("fields.reporter")} value={ticket.requesterName} />
            <DetailRow label={t("fields.assignee")} value={ticket.assigneeName ?? t("unassigned")} />
            <DetailRow
              label={t("fields.department")}
              value={deptLabel(ticket.targetDepartmentName, ticket.targetDepartmentNameEn, locale)}
            />
            <DetailRow label={t("fields.organization")} value={ticket.organizationName} />
            <DetailRow label={t("fields.category")} value={ticket.category} />
            <DetailRow label={t("fields.created")} value={new Date(ticket.createdAt).toLocaleString()} />
          </PanelSection>

          <PanelSection title={t("activity.timeline")} icon={<Clock size={12} />}>
            <div className="relative pl-4 space-y-4 before:absolute before:left-[5px] before:top-2 before:bottom-2 before:w-px before:bg-border">
              {ticket.activities.map((a) => (
                <div key={a.id} className="relative text-xs">
                  <Circle
                    size={10}
                    className="absolute -left-4 top-0.5 text-atg-teal fill-atg-teal/20"
                  />
                  <p className="text-foreground/80">
                    <span className="font-semibold">{a.actorName}</span>
                    {" "}
                    <span className="text-foreground/55">{a.action}</span>
                    {a.toStatus && (
                      <span className="text-atg-teal font-medium"> → {a.toStatus}</span>
                    )}
                  </p>
                  <p className="text-[10px] text-foreground/35 mt-0.5">
                    {new Date(a.createdAt).toLocaleString()}
                  </p>
                </div>
              ))}
            </div>
          </PanelSection>
        </aside>
      </div>
    </div>
  );
}

function PanelSection({
  title,
  icon,
  children,
}: {
  title: string;
  icon?: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <div>
      <p className="text-[10px] font-bold uppercase tracking-widest text-foreground/40 mb-2.5 flex items-center gap-1">
        {icon}
        {title}
      </p>
      {children}
    </div>
  );
}

function DetailRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="py-2 border-b border-border/40 last:border-0">
      <p className="text-[10px] text-foreground/40 mb-0.5">{label}</p>
      <p className="text-[13px] font-medium text-foreground/85">{value}</p>
    </div>
  );
}
