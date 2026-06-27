"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import Link from "next/link";
import api from "@/lib/api";
import { TicketListItem } from "@/lib/helpdesk";
import { TicketPriorityBadge, TicketStatusBadge } from "@/components/helpdesk/TicketBadges";
import { HelpdeskPageHeader } from "@/components/helpdesk/HelpdeskPageHeader";
import { Button } from "@/components/ui/Button";
import { useSearchParams } from "next/navigation";
import { Plus } from "lucide-react";
import { cn } from "@/lib/utils";

export default function TicketsListPage() {
  const searchParams = useSearchParams();
  const initialView = searchParams.get("view") ?? "mine";
  const t = useTranslations("helpdesk");
  const locale = useLocale();
  const [view, setView] = useState(initialView);
  const [items, setItems] = useState<TicketListItem[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setView(initialView);
  }, [initialView]);

  useEffect(() => {
    setLoading(true);
    api.get(`/helpdesk/tickets?view=${view}&pageSize=100`).then((r) => {
      setItems(r.data.items);
    }).finally(() => setLoading(false));
  }, [view]);

  const views = ["mine", "assigned", "queue", "all"] as const;

  return (
    <>
      <HelpdeskPageHeader
        title={t("tickets.title")}
        breadcrumb={t("nav.tickets")}
        actions={
          <Link href={`/${locale}/helpdesk/tickets/new`}>
            <Button size="sm">
              <Plus size={14} className="mr-1.5" />
              {t("nav.create")}
            </Button>
          </Link>
        }
      />

      <div className="flex-1 overflow-auto px-6 py-5 max-w-6xl">
        <div className="inline-flex gap-0.5 mb-5 p-1 rounded-lg bg-surface border border-border/80 shadow-sm">
          {views.map((v) => (
            <button
              key={v}
              type="button"
              onClick={() => setView(v)}
              className={cn(
                "px-3.5 py-1.5 rounded-md text-[13px] font-medium transition-all",
                view === v
                  ? "bg-atg-teal/12 text-atg-teal shadow-sm"
                  : "text-foreground/50 hover:text-foreground hover:bg-foreground/[0.03]"
              )}
            >
              {t(`views.${v}`)}
            </button>
          ))}
        </div>

        <div className="rounded-xl border border-border/80 bg-surface shadow-sm overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-[11px] text-foreground/45 uppercase tracking-wider bg-foreground/[0.02] border-b border-border">
                <th className="px-4 py-3 font-semibold w-28">Key</th>
                <th className="px-4 py-3 font-semibold">{t("fields.summary")}</th>
                <th className="px-4 py-3 font-semibold w-32">{t("fields.status")}</th>
                <th className="px-4 py-3 font-semibold w-24">{t("fields.priority")}</th>
                <th className="px-4 py-3 font-semibold w-40">{t("fields.assignee")}</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={5} className="px-4 py-16 text-center text-foreground/40">
                    {t("loading")}
                  </td>
                </tr>
              ) : items.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-4 py-16 text-center text-foreground/40">
                    {t("tickets.empty")}
                  </td>
                </tr>
              ) : (
                items.map((ticket) => (
                  <tr
                    key={ticket.id}
                    className="border-b border-border/40 last:border-0 hover:bg-atg-teal/[0.03] transition-colors"
                  >
                    <td className="px-4 py-3">
                      <Link
                        href={`/${locale}/helpdesk/tickets/${ticket.id}`}
                        className="font-mono text-[13px] text-atg-teal font-semibold hover:text-atg-blue hover:underline"
                      >
                        {ticket.number}
                      </Link>
                    </td>
                    <td className="px-4 py-3 font-medium text-foreground/90 max-w-md truncate">
                      {ticket.title}
                    </td>
                    <td className="px-4 py-3">
                      <TicketStatusBadge status={ticket.status} />
                    </td>
                    <td className="px-4 py-3">
                      <TicketPriorityBadge priority={ticket.priority} />
                    </td>
                    <td className="px-4 py-3 text-foreground/55 text-[13px]">
                      {ticket.assigneeName ?? "—"}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </>
  );
}
