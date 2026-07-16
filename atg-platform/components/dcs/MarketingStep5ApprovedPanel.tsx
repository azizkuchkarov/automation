"use client";

import { Loader2, CheckCircle2 } from "lucide-react";
import { useMarketingRecord } from "@/lib/hooks/useMarketingRecord";

interface Props {
  documentId: string;
  t: (key: string) => string;
}

export function MarketingStep5ApprovedPanel({ documentId, t }: Props) {
  const { data: record, isLoading: loading, error } = useMarketingRecord(documentId);

  if (loading) {
    return (
      <div className="flex items-center gap-2 text-sm text-foreground/50 py-4">
        <Loader2 size={16} className="animate-spin" />
        {t("loading")}
      </div>
    );
  }

  const approved = (record?.offers ?? []).filter(
    (o) => o.engineerReviewStatus === "Approved" && o.initiatorReviewStatus === "Approved",
  );

  return (
    <div className="mt-4 space-y-3 rounded-xl border border-emerald-500/25 bg-emerald-500/[0.04] p-4">
      <div>
        <h4 className="text-sm font-bold flex items-center gap-2">
          <CheckCircle2 size={16} className="text-emerald-600" />
          {t("approvedTitle")}
        </h4>
        <p className="text-xs text-foreground/50 mt-1">{t("approvedHint")}</p>
      </div>
      {error && <p className="text-sm text-red-600">{t("loadError")}</p>}
      {approved.length === 0 ? (
        <p className="text-sm text-foreground/50">{t("noApprovedOffers")}</p>
      ) : (
        <ul className="space-y-2">
          {approved.map((o) => (
            <li key={o.id} className="rounded-lg border border-border/60 bg-background/60 px-3 py-2 text-sm">
              <span className="font-semibold">{o.companyName}</span>
              <span className="text-foreground/50 ml-2">
                {o.offerAmount?.toLocaleString()} {o.currency}
              </span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
