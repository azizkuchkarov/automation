"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";
import { useSearchParams } from "next/navigation";
import { Plus } from "lucide-react";
import { HelpdeskPageHeader } from "@/components/helpdesk/HelpdeskPageHeader";
import { TicketPriorityBadge, TicketStatusBadge } from "@/components/helpdesk/TicketBadges";
import { Button } from "@/components/ui/Button";
import {
  TicketCategory,
  TicketListItem,
  categoryLabel,
  categoryPath,
  type HelpDeskCategory,
} from "@/lib/helpdesk";
import { fetchHelpDeskCategories, fetchHelpDeskTickets } from "@/lib/helpdeskApi";
import { cn } from "@/lib/utils";

export function CategoryTicketsPage({ category }: { category: TicketCategory }) {
  const searchParams = useSearchParams();
  const initialView = searchParams.get("view") ?? "mine";
  const t = useTranslations("helpdesk");
  const locale = useLocale();
  const [view, setView] = useState(initialView);
  const [items, setItems] = useState<TicketListItem[]>([]);
  const [meta, setMeta] = useState<HelpDeskCategory | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setView(initialView);
  }, [initialView]);

  useEffect(() => {
    fetchHelpDeskCategories()
      .then((items) => setMeta(items.find((c) => c.category === category) ?? null))
      .catch(() => setMeta(null));
  }, [category]);

  useEffect(() => {
    setLoading(true);
    fetchHelpDeskTickets(view, category)
      .then(setItems)
      .finally(() => setLoading(false));
  }, [view, category]);

  const views = ["mine", "assigned", "queue", "all"] as const;
  const title = meta ? categoryLabel(meta, locale) : category;

  return (
    <>
      <HelpdeskPageHeader
        title={t("tickets.categoryTitle", { category: title })}
        breadcrumb={title}
        actions={
          <Link href={categoryPath(locale, category, "new")}>
            <Button size="sm">
              <Plus size={14} className="mr-1.5" />
              {t("nav.create")}
            </Button>
          </Link>
        }
      />

      <div className="max-w-6xl flex-1 overflow-auto px-6 py-5">
        <div className="mb-5 inline-flex gap-0.5 rounded-lg border border-border/80 bg-surface p-1 shadow-sm">
          {views.map((v) => (
            <button
              key={v}
              type="button"
              onClick={() => setView(v)}
              className={cn(
                "rounded-md px-3.5 py-1.5 text-[13px] font-medium transition-all",
                view === v
                  ? "bg-atg-teal/12 text-atg-teal shadow-sm"
                  : "text-foreground/50 hover:bg-foreground/[0.03] hover:text-foreground",
              )}
            >
              {t(`views.${v}`)}
            </button>
          ))}
        </div>

        <div className="overflow-hidden rounded-xl border border-border/80 bg-surface shadow-sm">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border bg-foreground/[0.02] text-left text-[11px] uppercase tracking-wider text-foreground/45">
                <th className="w-28 px-4 py-3 font-semibold">Key</th>
                <th className="px-4 py-3 font-semibold">{t("fields.summary")}</th>
                <th className="w-32 px-4 py-3 font-semibold">{t("fields.status")}</th>
                <th className="w-24 px-4 py-3 font-semibold">{t("fields.priority")}</th>
                <th className="w-40 px-4 py-3 font-semibold">{t("fields.assignee")}</th>
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
                    className="border-b border-border/40 transition-colors last:border-0 hover:bg-atg-teal/[0.03]"
                  >
                    <td className="px-4 py-3">
                      <Link
                        href={`/${locale}/helpdesk/tickets/${ticket.id}`}
                        className="font-mono text-[13px] font-semibold text-atg-teal hover:text-atg-blue hover:underline"
                      >
                        {ticket.number}
                      </Link>
                    </td>
                    <td className="max-w-md truncate px-4 py-3 font-medium text-foreground/90">{ticket.title}</td>
                    <td className="px-4 py-3">
                      <TicketStatusBadge status={ticket.status} />
                    </td>
                    <td className="px-4 py-3">
                      <TicketPriorityBadge priority={ticket.priority} />
                    </td>
                    <td className="px-4 py-3 text-[13px] text-foreground/55">{ticket.assigneeName ?? "—"}</td>
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
