"use client";

import { useTranslations } from "next-intl";
import { DocumentStatus, statusColor } from "@/lib/dcs";
import { cn } from "@/lib/utils";

const STATUS_DOT: Record<DocumentStatus, string> = {
  Draft: "bg-slate-400",
  Registered: "bg-sky-500",
  InReview: "bg-atg-blue",
  Approved: "bg-emerald-500",
  Rejected: "bg-red-500",
  Archived: "bg-foreground/30",
};

export function DocumentStatusBadge({ status }: { status: DocumentStatus }) {
  const t = useTranslations("dcs.status");

  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-[11px] font-semibold border shadow-sm",
        statusColor(status)
      )}
    >
      <span className={cn("w-1.5 h-1.5 rounded-full shrink-0 shadow-sm", STATUS_DOT[status])} />
      {t(status)}
    </span>
  );
}
