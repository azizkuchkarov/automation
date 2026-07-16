"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { useQueryClient } from "@tanstack/react-query";
import { MarketingRfqTab } from "@/components/dcs/MarketingRfqTab";
import { MarketingKpTab } from "@/components/dcs/MarketingKpTab";
import { marketingRecordKey, useMarketingRecord } from "@/lib/hooks/useMarketingRecord";
import type { MarketingRecord } from "@/lib/marketing";
import { cn } from "@/lib/utils";

type SubTab = "rfq" | "kp";

interface Props {
  documentId: string;
  canEdit: boolean;
}

export function MarketingDetailTabs({ documentId, canEdit }: Props) {
  const t = useTranslations("dcs.marketing");
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<SubTab>("rfq");
  const { data: record, isLoading: loading } = useMarketingRecord(documentId);

  const onUpdated = (next: MarketingRecord) => {
    queryClient.setQueryData(marketingRecordKey(documentId), next);
  };

  if (loading) return <p className="text-sm text-foreground/40">{t("loading")}</p>;
  if (!record) return null;

  const tabs: { id: SubTab; label: string }[] = [
    { id: "rfq", label: t("tabs.rfq") },
    { id: "kp", label: t("tabs.kp") },
  ];

  return (
    <div className="rounded-2xl border border-border/70 bg-surface shadow-sm overflow-hidden">
      <div className="flex gap-1 p-2 border-b border-border/50 bg-foreground/[0.02]">
        {tabs.map((item) => (
          <button
            key={item.id}
            type="button"
            onClick={() => setTab(item.id)}
            className={cn(
              "px-4 py-2 rounded-lg text-sm font-medium transition-all",
              tab === item.id
                ? "bg-pink-500/10 text-pink-700 dark:text-pink-300 ring-1 ring-pink-500/20"
                : "text-foreground/50 hover:text-foreground hover:bg-foreground/[0.04]",
            )}
          >
            {item.label}
          </button>
        ))}
      </div>
      <div className="p-5">
        {tab === "rfq" ? (
          <MarketingRfqTab documentId={documentId} record={record} canEdit={canEdit} onUpdated={onUpdated} />
        ) : (
          <MarketingKpTab documentId={documentId} record={record} canEdit={canEdit} onUpdated={onUpdated} />
        )}
      </div>
    </div>
  );
}
