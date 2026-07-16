"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { useQueryClient } from "@tanstack/react-query";
import api, { getApiErrorMessage } from "@/lib/api";
import {
  MarketingRequestCategory,
  categoryLabel,
  deadlineColorClass,
} from "@/lib/marketing";
import {
  marketingRecordKey,
  useMarketingRecord,
} from "@/lib/hooks/useMarketingRecord";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

interface Props {
  documentId: string;
  canManage: boolean;
}

export function MarketingRecordPanel({ documentId, canManage }: Props) {
  const t = useTranslations("dcs.request");
  const locale = useLocale();
  const queryClient = useQueryClient();
  const { data: record, isLoading: loading } = useMarketingRecord(documentId);
  const [category, setCategory] = useState<MarketingRequestCategory>(2);
  const [acting, setActing] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (record?.requestCategory) setCategory(record.requestCategory);
  }, [record?.requestCategory]);

  const saveCategory = async () => {
    setActing(true);
    setError("");
    try {
      const r = await api.put(`/marketing/records/by-document/${documentId}/category`, { category });
      queryClient.setQueryData(marketingRecordKey(documentId), r.data);
    } catch (err) {
      setError(getApiErrorMessage(err, t("error")));
    } finally {
      setActing(false);
    }
  };

  if (loading) return <p className="text-sm text-foreground/40">{t("loading")}</p>;
  if (!record) return null;

  return (
    <div className="rounded-2xl border border-border/70 bg-surface p-5 space-y-4">
      <h3 className="text-sm font-bold">{t("marketingRecord")}</h3>
      {error && <p className="text-sm text-red-600">{error}</p>}
      <div className="grid sm:grid-cols-2 gap-3 text-sm">
        <div>
          <p className="text-[10px] font-bold uppercase tracking-wider text-foreground/40">{t("portalNumber")}</p>
          <p className="font-mono font-semibold">{record.portalNumber ?? "—"}</p>
        </div>
        <div>
          <p className="text-[10px] font-bold uppercase tracking-wider text-foreground/40">{t("marketingDeadline")}</p>
          {record.deadlineDate ? (
            <p className={cn("inline-flex items-center gap-2 font-semibold px-2 py-0.5 rounded-lg text-xs", deadlineColorClass(record.deadlineColor))}>
              {record.deadlineDate}
              {record.remainingWorkingDays !== undefined && (
                <span>({record.remainingWorkingDays} {t("workingDaysLeft")})</span>
              )}
            </p>
          ) : (
            <p className="text-foreground/50">{t("categoryNotSet")}</p>
          )}
        </div>
      </div>
      {canManage && (
        <div className="flex flex-wrap gap-2 items-end">
          <div className="flex-1 min-w-[200px]">
            <label className="text-[10px] font-bold uppercase tracking-wider text-foreground/40 block mb-1">
              {t("requestCategory")}
            </label>
            <select
              className="w-full rounded-lg border border-border/80 bg-background px-3 py-2 text-sm"
              value={category}
              onChange={(e) => setCategory(Number(e.target.value) as MarketingRequestCategory)}
            >
              {([1, 2, 3, 4] as MarketingRequestCategory[]).map((c) => (
                <option key={c} value={c}>{categoryLabel(c, locale)}</option>
              ))}
            </select>
          </div>
          <Button size="sm" disabled={acting} onClick={saveCategory}>{t("setCategory")}</Button>
        </div>
      )}
      {record.requestCategory && (
        <p className="text-xs text-foreground/50">{t("categoryDeadlineHint")}</p>
      )}
    </div>
  );
}
