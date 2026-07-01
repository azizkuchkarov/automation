"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";
import { CalendarDays, ChevronRight, Loader2, Plus } from "lucide-react";
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
  if (phase === "Draft") return "bg-slate-100 text-slate-600 border-slate-200";
  return "bg-violet-50 text-violet-700 border-violet-200";
}

export default function HrLeaveListPage() {
  const t = useTranslations("hr.leave");
  const locale = useLocale();
  const [items, setItems] = useState<HrLeaveListItem[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api
      .get("/hr/leave-requests/mine")
      .then((r) => setItems(r.data))
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="flex flex-col flex-1 min-h-0">
      <header className="shrink-0 border-b border-border/80 bg-surface px-6 py-5">
        <div className="flex items-start justify-between gap-4">
          <div>
            <h1 className="text-xl font-semibold text-foreground">{t("myTitle")}</h1>
            <p className="text-sm text-foreground/50 mt-1">{t("mySubtitle")}</p>
          </div>
          <Link
            href={`/${locale}/hr/leave/new`}
            className="inline-flex items-center justify-center h-9 px-4 rounded-md text-sm font-medium bg-violet-600 text-white hover:bg-violet-700 transition-colors"
          >
            <Plus size={16} className="mr-1.5" />
            {t("create")}
          </Link>
        </div>
      </header>

      <div className="flex-1 overflow-y-auto px-6 py-5">
        {loading ? (
          <div className="flex items-center justify-center gap-2 py-16 text-foreground/40">
            <Loader2 className="animate-spin" size={20} />
            <span className="text-sm">{t("loading")}</span>
          </div>
        ) : items.length === 0 ? (
          <div className="rounded-xl border border-dashed border-border/80 bg-surface p-10 text-center">
            <CalendarDays className="mx-auto mb-3 text-foreground/25" size={36} />
            <p className="text-sm text-foreground/50 mb-4">{t("empty")}</p>
            <Link
              href={`/${locale}/hr/leave/new`}
              className="inline-flex items-center justify-center h-9 px-4 rounded-md text-sm font-medium bg-surface border border-border hover:bg-border/30"
            >
              {t("createFirst")}
            </Link>
          </div>
        ) : (
          <div className="rounded-xl border border-border/80 bg-surface overflow-hidden shadow-sm">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border/60 bg-foreground/[0.02] text-left">
                  <th className="px-4 py-3 font-medium text-foreground/50">{t("columns.number")}</th>
                  <th className="px-4 py-3 font-medium text-foreground/50">{t("columns.department")}</th>
                  <th className="px-4 py-3 font-medium text-foreground/50">{t("columns.date")}</th>
                  <th className="px-4 py-3 font-medium text-foreground/50">{t("columns.items")}</th>
                  <th className="px-4 py-3 font-medium text-foreground/50">{t("columns.phase")}</th>
                  <th className="px-4 py-3" />
                </tr>
              </thead>
              <tbody>
                {items.map((item) => (
                  <tr key={item.id} className="border-b border-border/40 last:border-0 hover:bg-foreground/[0.02]">
                    <td className="px-4 py-3 font-medium text-violet-700">{item.number}</td>
                    <td className="px-4 py-3 text-foreground/70">
                      {deptLabel(item.departmentName, item.departmentNameEn, locale)}
                    </td>
                    <td className="px-4 py-3 text-foreground/60">{formatDate(item.requestDate, locale)}</td>
                    <td className="px-4 py-3 text-foreground/60">{item.itemCount}</td>
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
