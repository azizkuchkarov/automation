"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { CalendarDays, Plus } from "lucide-react";
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
    <HrPageShell>
      <HrPageHeader
        title={t("myTitle")}
        subtitle={t("mySubtitle")}
        actionHref={`/${locale}/hr/leave/new`}
        actionLabel={t("create")}
        actionIcon={Plus}
      />

      <div className="flex-1 overflow-y-auto px-6 py-6">
        {loading ? (
          <HrLoadingState label={t("loading")} />
        ) : items.length === 0 ? (
          <HrEmptyState
            icon={CalendarDays}
            title={t("empty")}
            actionHref={`/${locale}/hr/leave/new`}
            actionLabel={t("createFirst")}
          />
        ) : (
          <HrDataTable
            headers={[
              t("columns.number"),
              t("columns.department"),
              t("columns.date"),
              t("columns.items"),
              t("columns.phase"),
              "",
            ]}
          >
            {items.map((item) => (
              <HrTableRow key={item.id}>
                <td className={cn("px-4 py-3.5 font-semibold", hrTheme.accentText)}>{item.number}</td>
                <td className="px-4 py-3.5 text-slate-600">
                  {deptLabel(item.departmentName, item.departmentNameEn, locale)}
                </td>
                <td className="px-4 py-3.5 text-slate-500 whitespace-nowrap">
                  {formatHrDate(item.requestDate, locale)}
                </td>
                <td className="px-4 py-3.5 text-slate-500">{item.itemCount}</td>
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
