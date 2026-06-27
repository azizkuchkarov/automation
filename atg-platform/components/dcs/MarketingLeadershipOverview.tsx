"use client";

import { useLocale, useTranslations } from "next-intl";
import Link from "next/link";
import { MarketingLeadershipRow, deadlineColorClass } from "@/lib/marketing";
import { cn } from "@/lib/utils";
import { dcsTheme } from "@/components/dcs/dcsTheme";
import { ChevronDown, ChevronRight } from "lucide-react";
import { useState } from "react";

export function MarketingLeadershipOverview({ rows }: { rows: MarketingLeadershipRow[] }) {
  const t = useTranslations("dcs.marketing.leadership");
  const locale = useLocale();

  if (rows.length === 0) {
    return (
      <div className={cn("py-16 text-center text-foreground/40 text-sm", dcsTheme.premiumCard)}>
        {t("empty")}
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {rows.map((row) => (
        <LeadershipGroup key={`${row.initiatorDepartment}-${row.initiatorFullName}`} row={row} locale={locale} t={t} />
      ))}
    </div>
  );
}

function LeadershipGroup({
  row, locale, t,
}: {
  row: MarketingLeadershipRow;
  locale: string;
  t: ReturnType<typeof useTranslations>;
}) {
  const [open, setOpen] = useState(true);
  const overdue = row.items.filter((i) => i.isOverdue).length;

  return (
    <div className={cn("overflow-hidden", dcsTheme.premiumCard)}>
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        className="w-full flex items-center gap-3 px-5 py-4 text-left hover:bg-foreground/[0.02] transition-colors"
      >
        {open ? <ChevronDown size={16} className="text-foreground/40" /> : <ChevronRight size={16} className="text-foreground/40" />}
        <div className="flex-1 min-w-0">
          <p className="font-semibold truncate">{row.initiatorFullName}</p>
          <p className="text-xs text-foreground/50 truncate">{row.initiatorDepartment}</p>
        </div>
        <div className="flex items-center gap-2 shrink-0">
          <span className="text-xs px-2 py-0.5 rounded-full bg-foreground/[0.06] tabular-nums">
            {row.items.length} {t("requests")}
          </span>
          {overdue > 0 && (
            <span className="text-xs px-2 py-0.5 rounded-full bg-red-500/10 text-red-600 font-semibold tabular-nums">
              {overdue} {t("overdue")}
            </span>
          )}
        </div>
      </button>
      {open && (
        <div className="border-t border-border/50 overflow-x-auto">
          <table className="w-full text-sm min-w-[640px]">
            <thead>
              <tr className="text-left text-[10px] uppercase tracking-wider text-foreground/45 bg-slate-50/80 dark:bg-white/[0.02]">
                <th className="px-5 py-2.5 font-bold">{t("portal")}</th>
                <th className="px-4 py-2.5 font-bold">{t("subject")}</th>
                <th className="px-4 py-2.5 font-bold w-24">{t("step")}</th>
                <th className="px-4 py-2.5 font-bold w-28">{t("deadline")}</th>
              </tr>
            </thead>
            <tbody>
              {row.items.map((item) => (
                <tr key={item.documentId} className="border-t border-border/30 hover:bg-pink-500/[0.03]">
                  <td className="px-5 py-3">
                    <Link
                      href={`/${locale}/automation/documents/${item.documentId}`}
                      className="font-mono text-[13px] font-bold text-pink-600 dark:text-pink-400 hover:underline"
                    >
                      {item.portalNumber ?? "—"}
                    </Link>
                  </td>
                  <td className="px-4 py-3">
                    <Link href={`/${locale}/automation/documents/${item.documentId}`} className="line-clamp-1 hover:text-atg-blue">
                      {item.requestTitle ?? "—"}
                    </Link>
                  </td>
                  <td className="px-4 py-3 tabular-nums text-foreground/70">{item.marketingCurrentStep}</td>
                  <td className="px-4 py-3">
                    {item.remainingWorkingDays !== undefined ? (
                      <span className={cn("text-xs font-semibold px-2 py-0.5 rounded-lg", deadlineColorClass(item.deadlineColor))}>
                        {item.remainingWorkingDays} {t("wd")}
                      </span>
                    ) : (
                      "—"
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
