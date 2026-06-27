"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import Link from "next/link";
import api from "@/lib/api";
import { HelpDeskAdminControl } from "@/lib/helpdesk";
import { TicketStatusBadge } from "@/components/helpdesk/TicketBadges";
import { AdminTicketAssign } from "@/components/helpdesk/AdminTicketAssign";
import { HelpDeskCategoryControl } from "@/components/helpdesk/HelpDeskCategoryControl";
import { Headset, Users, Clock, CheckCircle2, Inbox } from "lucide-react";

export default function AdminHelpdeskPage() {
  const t = useTranslations("helpdesk");
  const locale = useLocale();
  const [control, setControl] = useState<HelpDeskAdminControl | null>(null);
  const [loading, setLoading] = useState(true);

  const load = () => {
    setLoading(true);
    api
      .get("/helpdesk/admin/control")
      .then((r) => setControl(r.data))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    load();
  }, []);

  if (loading) return <div className="p-8 text-foreground/40">{t("loading")}</div>;
  if (!control) return null;

  const dash = control.dashboard;

  const stats = [
    { label: t("admin.open"), value: dash.totalOpen, icon: Inbox, color: "text-slate-400" },
    { label: t("admin.inProgress"), value: dash.totalInProgress, icon: Clock, color: "text-atg-blue" },
    { label: t("admin.done"), value: dash.totalDone, icon: CheckCircle2, color: "text-emerald-400" },
    { label: t("admin.closed"), value: dash.totalClosed, icon: Users, color: "text-foreground/40" },
  ];

  return (
    <div className="space-y-8">
      <div className="flex items-center gap-3">
        <div className="w-10 h-10 rounded-xl bg-atg-teal/20 flex items-center justify-center">
          <Headset size={20} className="text-atg-teal" />
        </div>
        <div>
          <h1 className="text-2xl font-bold">{t("admin.title")}</h1>
          <p className="text-sm text-foreground/50">{t("admin.subtitle")}</p>
        </div>
      </div>

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-3">
        {stats.map(({ label, value, icon: Icon, color }) => (
          <div key={label} className="rounded-xl border border-border bg-surface p-4">
            <div className="flex items-center gap-2 mb-2">
              <Icon size={16} className={color} />
              <span className="text-xs text-foreground/50 uppercase tracking-wider">{label}</span>
            </div>
            <div className={`text-3xl font-bold tabular-nums ${color}`}>{value}</div>
          </div>
        ))}
      </div>

      <HelpDeskCategoryControl categories={control.categories} />

      <div className="rounded-xl border border-border overflow-hidden">
        <div className="px-4 py-3 border-b border-border bg-surface">
          <h2 className="font-semibold text-sm">{t("admin.recentTickets")}</h2>
        </div>
        <table className="w-full text-sm">
          <thead className="bg-surface/50 border-b border-border">
            <tr className="text-left text-xs text-foreground/50 uppercase tracking-wider">
              <th className="px-4 py-2.5">Key</th>
              <th className="px-4 py-2.5">{t("fields.summary")}</th>
              <th className="px-4 py-2.5">{t("fields.status")}</th>
              <th className="px-4 py-2.5">{t("fields.assignee")}</th>
              <th className="px-4 py-2.5">{t("fields.reporter")}</th>
              <th className="px-4 py-2.5">{t("fields.department")}</th>
              <th className="px-4 py-2.5">{t("admin.actions")}</th>
            </tr>
          </thead>
          <tbody>
            {dash.recentTickets.map((ticket) => (
              <tr key={ticket.id} className="border-b border-border/30 hover:bg-surface/30">
                <td className="px-4 py-3">
                  <Link
                    href={`/${locale}/helpdesk/tickets/${ticket.id}`}
                    className="font-mono text-atg-teal font-semibold hover:underline"
                  >
                    {ticket.number}
                  </Link>
                </td>
                <td className="px-4 py-3 font-medium max-w-[200px] truncate">{ticket.title}</td>
                <td className="px-4 py-3">
                  <TicketStatusBadge status={ticket.status} />
                </td>
                <td className="px-4 py-3 text-foreground/60">{ticket.assigneeName ?? "—"}</td>
                <td className="px-4 py-3 text-foreground/60">{ticket.requesterName}</td>
                <td className="px-4 py-3 text-foreground/60 text-xs">{ticket.targetDepartmentName}</td>
                <td className="px-4 py-3">
                  <AdminTicketAssign ticket={ticket} onAssigned={load} />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
