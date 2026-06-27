"use client";

import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";
import {
  TicketListItem,
  TicketPriority,
  TicketStatus,
  priorityColor,
  statusColor,
  categoryIconColor,
} from "@/lib/helpdesk";
import { cn } from "@/lib/utils";
import { User, Monitor, Building2, Truck, Plane, Languages, Calculator } from "lucide-react";

const CATEGORY_ICONS = {
  IT: Monitor,
  Administration: Building2,
  Accountant: Calculator,
  Transport: Truck,
  TravelTickets: Plane,
  Translator: Languages,
} as const;

export function TicketStatusBadge({ status }: { status: TicketStatus }) {
  const t = useTranslations("helpdesk");
  const label =
    status === "InProgress" ? t("status.InProgress") : t(`status.${status}` as "status.Open");
  return (
    <span
      className={cn(
        "inline-flex px-2 py-0.5 rounded-md text-[10px] font-semibold uppercase tracking-wide border",
        statusColor(status)
      )}
    >
      {label}
    </span>
  );
}

export function TicketPriorityBadge({ priority }: { priority: TicketPriority }) {
  return (
    <span
      className={cn(
        "inline-flex px-1.5 py-0.5 rounded text-[10px] font-bold uppercase tracking-wide",
        priorityColor(priority)
      )}
    >
      {priority}
    </span>
  );
}

function CategoryChip({ category }: { category: TicketListItem["category"] }) {
  const Icon = CATEGORY_ICONS[category] ?? Monitor;
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-[10px] font-medium",
        categoryIconColor(category)
      )}
    >
      <Icon size={10} />
      {category === "TravelTickets" ? "Travel" : category}
    </span>
  );
}

export function TicketCard({
  ticket,
  variant = "default",
}: {
  ticket: TicketListItem;
  variant?: "default" | "kanban";
}) {
  const locale = useLocale();
  const t = useTranslations("helpdesk");

  return (
    <Link
      href={`/${locale}/helpdesk/tickets/${ticket.id}`}
      className={cn(
        "block rounded-lg border bg-surface p-3 transition-all duration-200 group",
        "border-border/60 shadow-sm",
        "hover:border-atg-teal/40 hover:shadow-md hover:-translate-y-0.5",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-atg-teal/40"
      )}
    >
      <div className="flex items-center justify-between gap-2 mb-2">
        <span className="text-[11px] font-mono font-semibold text-atg-teal group-hover:text-atg-blue transition-colors">
          {ticket.number}
        </span>
        <TicketPriorityBadge priority={ticket.priority} />
      </div>

      <p className="text-[13px] font-medium leading-snug line-clamp-2 text-foreground/90 group-hover:text-foreground">
        {ticket.title}
      </p>

      <div className="flex items-center justify-between mt-3 pt-2.5 border-t border-border/40 gap-2">
        <div className="flex items-center gap-1.5 text-[11px] text-foreground/50 min-w-0">
          {ticket.assigneeName ? (
            <>
              <span className="w-6 h-6 rounded-full bg-gradient-to-br from-atg-teal/30 to-atg-blue/30 text-atg-teal flex items-center justify-center text-[10px] font-bold shrink-0 ring-1 ring-border/50">
                {ticket.assigneeName.charAt(0)}
              </span>
              <span className="truncate max-w-[90px]">{ticket.assigneeName}</span>
            </>
          ) : (
            <>
              <span className="w-6 h-6 rounded-full bg-border/40 flex items-center justify-center shrink-0">
                <User size={11} className="text-foreground/35" />
              </span>
              <span className="text-foreground/40">{t("unassigned")}</span>
            </>
          )}
        </div>
        {variant === "kanban" ? (
          <CategoryChip category={ticket.category} />
        ) : (
          <TicketStatusBadge status={ticket.status} />
        )}
      </div>
    </Link>
  );
}
