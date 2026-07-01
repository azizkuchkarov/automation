"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";
import { ChevronRight, Inbox, Loader2 } from "lucide-react";
import api from "@/lib/api";
import { deptLabel, HrLeaveListItem, phaseLabel } from "@/lib/hrLeave";
import { cn } from "@/lib/utils";

function formatDate(value: string, locale: string) {
  return new Date(value).toLocaleDateString(locale.startsWith("en") ? "en-GB" : "ru-RU", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  });
}

function phaseBadgeClass(phase: string) {
  if (phase === "Approved") return "bg-emerald-50 text-emerald-700 border-emerald-200";
  if (phase === "Rejected") return "bg-red-50 text-red-700 border-red-200";
  return "bg-violet-50 text-violet-700 border-violet-200";
}

export default function HrLeaveQueuePage() {
  const t = useTranslations("hr.leave");
  const locale = useLocale();
  const [items, setItems] = useState<HrLeaveListItem[] | null>(null);
  const [denied, setDenied] = useState(false);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api
      .get("/hr/leave-requests/hr-queue")
      .then((r) => setItems(r.data))
      .catch(() => setDenied(true))
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="flex flex-col flex-1 min-h-0">
      <header className="shrink-0 border-b border-border/80 bg-surface px-6 py-5">
        <h1 className="text-xl font-semibold text-foreground">{t("queueTitle")}</h1>
        <p className="text-sm text-foreground/50 mt-1">{t("queueSubtitle")}</p>
      </header>

      <div className="flex-1 overflow-y-auto px-6 py-5">
        {loading ? (
          <div className="flex items-center justify-center gap-2 py-16 text-foreground/40">
            <Loader2 className="animate-spin" size={20} />
            <span className="text-sm">{t("loading")}</span>
          </div>
        ) : denied ? (
          <div className="rounded-xl border border-border/80 bg-surface p-10 text-center text-sm text-foreground/50">
            {t("queueDenied")}
          </div>
        ) : !items?.length ? (
          <div className="rounded-xl border border-dashed border-border/80 bg-surface p-10 text-center">
            <Inbox className="mx-auto mb-3 text-foreground/25" size={36} />
            <p className="text-sm text-foreground/50">{t("queueEmpty")}</p>
          </div>
        ) : (
          <div className="rounded-xl border border-border/80 bg-surface overflow-hidden shadow-sm">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border/60 bg-foreground/[0.02] text-left">
                  <th className="px-4 py-3 font-medium text-foreground/50">{t("columns.number")}</th>
                  <th className="px-4 py-3 font-medium text-foreground/50">{t("columns.author")}</th>
                  <th className="px-4 py-3 font-medium text-foreground/50">{t("columns.department")}</th>
                  <th className="px-4 py-3 font-medium text-foreground/50">{t("columns.date")}</th>
                  <th className="px-4 py-3 font-medium text-foreground/50">{t("columns.phase")}</th>
                  <th className="px-4 py-3" />
                </tr>
              </thead>
              <tbody>
                {items.map((item) => (
                  <tr key={item.id} className="border-b border-border/40 last:border-0 hover:bg-foreground/[0.02]">
                    <td className="px-4 py-3 font-medium text-violet-700">{item.number}</td>
                    <td className="px-4 py-3">{item.authorName}</td>
                    <td className="px-4 py-3 text-foreground/70">
                      {deptLabel(item.departmentName, item.departmentNameEn, locale)}
                    </td>
                    <td className="px-4 py-3 text-foreground/60">{formatDate(item.requestDate, locale)}</td>
                    <td className="px-4 py-3">
                      <span className={cn("inline-flex px-2 py-0.5 rounded-full text-xs border", phaseBadgeClass(item.phase))}>
                        {phaseLabel(item.phase, locale)}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-right">
                      <Link
                        href={`/${locale}/hr/leave/${item.id}`}
                        className="inline-flex items-center gap-1 text-violet-600 hover:text-violet-800 text-sm font-medium"
                      >
                        {t("open")}
                        <ChevronRight size={14} />
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
