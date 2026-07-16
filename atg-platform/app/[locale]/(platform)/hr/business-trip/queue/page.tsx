"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { FileText, Inbox, Loader2 } from "lucide-react";
import api, { getApiErrorMessage } from "@/lib/api";
import { deptLabel, HrBusinessTripListItem, phaseLabel } from "@/lib/hrBusinessTrip";
import {
  HrDataTable,
  HrEmptyState,
  HrLoadingState,
  HrOpenLink,
  HrPageHeader,
  HrPageShell,
  HrPhaseBadge,
  HrPrimaryButton,
  HrSectionTitle,
  HrTableRow,
} from "@/components/hr/HrChrome";
import { formatHrDate, hrTheme } from "@/components/hr/hrTheme";
import { cn } from "@/lib/utils";

export default function HrBusinessTripQueuePage() {
  const t = useTranslations("hr.businessTrip");
  const locale = useLocale();
  const [items, setItems] = useState<HrBusinessTripListItem[] | null>(null);
  const [orderItems, setOrderItems] = useState<HrBusinessTripListItem[] | null>(null);
  const [certificateItems, setCertificateItems] = useState<HrBusinessTripListItem[] | null>(null);
  const [selectedOrderIds, setSelectedOrderIds] = useState<string[]>([]);
  const [issuing, setIssuing] = useState(false);
  const [error, setError] = useState("");

  const reload = useCallback(() => {
    setError("");
    Promise.all([
      api.get("/hr/business-trips/queue"),
      api.get("/hr/business-trips/order-queue"),
      api.get("/hr/business-trips/certificate-queue"),
    ])
      .then(([queueRes, orderRes, certificateRes]) => {
        setItems(queueRes.data);
        setOrderItems(orderRes.data);
        setCertificateItems(certificateRes.data);
        setSelectedOrderIds((prev) =>
          prev.filter((id) => orderRes.data.some((o: HrBusinessTripListItem) => o.id === id)),
        );
      })
      .catch(() => {
        setItems([]);
        setOrderItems([]);
        setCertificateItems([]);
      });
  }, []);

  useEffect(() => {
    reload();
  }, [reload]);

  const approvalItems = useMemo(() => {
    if (!items) return [];
    const orderIds = new Set((orderItems ?? []).map((o) => o.id));
    const certificateIds = new Set((certificateItems ?? []).map((o) => o.id));
    return items.filter((i) => !orderIds.has(i.id) && !certificateIds.has(i.id));
  }, [items, orderItems, certificateItems]);

  const toggleOrderSelection = (id: string) => {
    setSelectedOrderIds((prev) => (prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]));
  };

  const issueSelectedOrders = async () => {
    if (selectedOrderIds.length === 0) return;
    setIssuing(true);
    setError("");
    try {
      await api.post("/hr/business-trips/issue-order", { requestIds: selectedOrderIds });
      setSelectedOrderIds([]);
      reload();
    } catch (err: unknown) {
      setError(getApiErrorMessage(err) ?? t("actionError"));
    } finally {
      setIssuing(false);
    }
  };

  const loading = items === null || orderItems === null || certificateItems === null;

  return (
    <HrPageShell>
      <HrPageHeader title={t("queueTitle")} subtitle={t("queueSubtitle")} />

      <div className="flex-1 overflow-y-auto px-6 py-6 space-y-8">
        {loading ? (
          <HrLoadingState label={t("loading")} />
        ) : (
          <>
            {(orderItems?.length ?? 0) > 0 && (
              <section className="space-y-3">
                <HrSectionTitle
                  title={t("orderQueueTitle")}
                  subtitle={t("orderQueueSubtitle")}
                  action={
                    <HrPrimaryButton
                      disabled={issuing || selectedOrderIds.length === 0}
                      onClick={issueSelectedOrders}
                    >
                      {issuing ? (
                        <Loader2 size={14} className="animate-spin mr-1.5" />
                      ) : (
                        <FileText size={14} className="mr-1.5" />
                      )}
                      {t("issueOrderBatch", { count: selectedOrderIds.length })}
                    </HrPrimaryButton>
                  }
                />
                {error && (
                  <p className="text-sm text-red-700 bg-red-50 border border-red-200 rounded-xl px-4 py-2.5">
                    {error}
                  </p>
                )}
                <HrDataTable
                  accent="amber"
                  headers={[
                    "",
                    t("columns.number"),
                    t("columns.department"),
                    t("columns.place"),
                    t("columns.dates"),
                    "",
                  ]}
                >
                  {orderItems!.map((item) => (
                    <HrTableRow key={item.id} accent="amber">
                      <td className="px-4 py-3.5 w-10">
                        <input
                          type="checkbox"
                          checked={selectedOrderIds.includes(item.id)}
                          onChange={() => toggleOrderSelection(item.id)}
                          className="rounded border-slate-300 text-blue-600 focus:ring-blue-500/30"
                        />
                      </td>
                      <td className={cn("px-4 py-3.5 font-semibold", hrTheme.accentText)}>{item.number}</td>
                      <td className="px-4 py-3.5 text-slate-600">
                        {deptLabel(item.departmentName, item.departmentNameEn, locale)}
                      </td>
                      <td className="px-4 py-3.5 text-slate-500">{item.placeRu}</td>
                      <td className="px-4 py-3.5 text-slate-500 whitespace-nowrap">
                        {formatHrDate(item.dateFrom, locale)} — {formatHrDate(item.dateTo, locale)}
                      </td>
                      <td className="px-4 py-3.5 text-right">
                        <HrOpenLink href={`/${locale}/hr/business-trip/${item.id}`} label={t("open")} />
                      </td>
                    </HrTableRow>
                  ))}
                </HrDataTable>
              </section>
            )}

            {(certificateItems?.length ?? 0) > 0 && (
              <section className="space-y-3">
                <HrSectionTitle
                  title={t("certificateQueueTitle")}
                  subtitle={t("certificateQueueSubtitle")}
                />
                <HrDataTable
                  headers={[
                    t("columns.number"),
                    t("columns.department"),
                    t("columns.place"),
                    t("columns.dates"),
                    "",
                  ]}
                >
                  {certificateItems!.map((item) => (
                    <HrTableRow key={item.id}>
                      <td className={cn("px-4 py-3.5 font-semibold", hrTheme.accentText)}>{item.number}</td>
                      <td className="px-4 py-3.5 text-slate-600">
                        {deptLabel(item.departmentName, item.departmentNameEn, locale)}
                      </td>
                      <td className="px-4 py-3.5 text-slate-500">{item.placeRu}</td>
                      <td className="px-4 py-3.5 text-slate-500 whitespace-nowrap">
                        {formatHrDate(item.dateFrom, locale)} — {formatHrDate(item.dateTo, locale)}
                      </td>
                      <td className="px-4 py-3.5 text-right">
                        <HrOpenLink href={`/${locale}/hr/business-trip/${item.id}`} label={t("open")} />
                      </td>
                    </HrTableRow>
                  ))}
                </HrDataTable>
              </section>
            )}

            <section className="space-y-3">
              <HrSectionTitle title={t("approvalQueueTitle")} />
              {approvalItems.length === 0 ? (
                <HrEmptyState icon={Inbox} title={t("queueEmpty")} />
              ) : (
                <HrDataTable
                  headers={[
                    t("columns.number"),
                    t("columns.department"),
                    t("columns.place"),
                    t("columns.dates"),
                    t("columns.phase"),
                    "",
                  ]}
                >
                  {approvalItems.map((item) => (
                    <HrTableRow key={item.id}>
                      <td className={cn("px-4 py-3.5 font-semibold", hrTheme.accentText)}>{item.number}</td>
                      <td className="px-4 py-3.5 text-slate-600">
                        {deptLabel(item.departmentName, item.departmentNameEn, locale)}
                      </td>
                      <td className="px-4 py-3.5 text-slate-500">{item.placeRu}</td>
                      <td className="px-4 py-3.5 text-slate-500 whitespace-nowrap">
                        {formatHrDate(item.dateFrom, locale)} — {formatHrDate(item.dateTo, locale)}
                      </td>
                      <td className="px-4 py-3.5">
                        <HrPhaseBadge phase={item.phase} label={phaseLabel(item.phase, locale)} />
                      </td>
                      <td className="px-4 py-3.5 text-right">
                        <HrOpenLink href={`/${locale}/hr/business-trip/${item.id}`} label={t("open")} />
                      </td>
                    </HrTableRow>
                  ))}
                </HrDataTable>
              )}
            </section>
          </>
        )}
      </div>
    </HrPageShell>
  );
}
