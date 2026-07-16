"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { Inbox } from "lucide-react";
import api from "@/lib/api";
import { deptLabel, HrLeaveListItem, phaseLabel } from "@/lib/hrLeave";
import {
  HrDataTable,
  HrEmptyState,
  HrLoadingState,
  HrOpenLink,
  HrPageHeader,
  HrPageShell,
  HrPhaseBadge,
  HrTableRow,
} from "@/components/hr/HrChrome";
import { formatHrDate, hrTheme } from "@/components/hr/hrTheme";
import { cn } from "@/lib/utils";

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
    <HrPageShell>
      <HrPageHeader title={t("queueTitle")} subtitle={t("queueSubtitle")} />

      <div className="flex-1 overflow-y-auto px-6 py-6">
        {loading ? (
          <HrLoadingState label={t("loading")} />
        ) : denied ? (
          <div className={cn("px-8 py-12 text-center text-sm text-amber-800", hrTheme.card, "border-amber-200 bg-amber-50/50")}>
            {t("queueDenied")}
          </div>
        ) : !items?.length ? (
          <HrEmptyState icon={Inbox} title={t("queueEmpty")} />
        ) : (
          <HrDataTable
            headers={[
              t("columns.number"),
              t("columns.author"),
              t("columns.department"),
              t("columns.date"),
              t("columns.phase"),
              "",
            ]}
          >
            {items.map((item) => (
              <HrTableRow key={item.id}>
                <td className={cn("px-4 py-3.5 font-semibold", hrTheme.accentText)}>{item.number}</td>
                <td className="px-4 py-3.5 text-slate-700">{item.authorName}</td>
                <td className="px-4 py-3.5 text-slate-600">
                  {deptLabel(item.departmentName, item.departmentNameEn, locale)}
                </td>
                <td className="px-4 py-3.5 text-slate-500 whitespace-nowrap">
                  {formatHrDate(item.requestDate, locale)}
                </td>
                <td className="px-4 py-3.5">
                  <HrPhaseBadge phase={item.phase} label={phaseLabel(item.phase, locale)} />
                </td>
                <td className="px-4 py-3.5 text-right">
                  <HrOpenLink href={`/${locale}/hr/leave/${item.id}`} label={t("open")} />
                </td>
              </HrTableRow>
            ))}
          </HrDataTable>
        )}
      </div>
    </HrPageShell>
  );
}
