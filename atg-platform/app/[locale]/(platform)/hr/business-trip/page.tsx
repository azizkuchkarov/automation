"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { Briefcase, Plus } from "lucide-react";
import api from "@/lib/api";
import { deptLabel, HrBusinessTripListItem, phaseLabel } from "@/lib/hrBusinessTrip";
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

export default function HrBusinessTripListPage() {
  const t = useTranslations("hr.businessTrip");
  const locale = useLocale();
  const [items, setItems] = useState<HrBusinessTripListItem[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api
      .get("/hr/business-trips/mine")
      .then((r) => setItems(r.data))
      .finally(() => setLoading(false));
  }, []);

  return (
    <HrPageShell>
      <HrPageHeader
        title={t("myTitle")}
        subtitle={t("mySubtitle")}
        actionHref={`/${locale}/hr/business-trip/new`}
        actionLabel={t("create")}
        actionIcon={Plus}
      />

      <div className="flex-1 overflow-y-auto px-6 py-6">
        {loading ? (
          <HrLoadingState label={t("loading")} />
        ) : items.length === 0 ? (
          <HrEmptyState
            icon={Briefcase}
            title={t("empty")}
            actionHref={`/${locale}/hr/business-trip/new`}
            actionLabel={t("createFirst")}
          />
        ) : (
          <HrDataTable
            headers={[
              t("columns.number"),
              t("columns.department"),
              t("columns.place"),
              t("columns.dates"),
              t("columns.travelers"),
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
                <td className="px-4 py-3.5 text-slate-500">{item.placeRu}</td>
                <td className="px-4 py-3.5 text-slate-500 whitespace-nowrap">
                  {formatHrDate(item.dateFrom, locale)} — {formatHrDate(item.dateTo, locale)}
                </td>
                <td className="px-4 py-3.5 text-slate-500">{item.travelerCount}</td>
                <td className="px-4 py-3.5">
                  <div className="flex flex-wrap items-center gap-2">
                    <HrPhaseBadge phase={item.phase} label={phaseLabel(item.phase, locale)} />
                    {item.hasMyCertificate && (
                      <span className="inline-flex rounded-full bg-violet-100 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-violet-800">
                        {t("certificateReadyBadge")}
                      </span>
                    )}
                  </div>
                </td>
                <td className="px-4 py-3.5 text-right">
                  <HrOpenLink href={`/${locale}/hr/business-trip/${item.id}`} label={t("open")} />
                </td>
              </HrTableRow>
            ))}
          </HrDataTable>
        )}
      </div>
    </HrPageShell>
  );
}
