"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { Crown } from "lucide-react";
import api, { getApiErrorMessage } from "@/lib/api";
import { MarketingLeadershipRow } from "@/lib/marketing";
import { DcsPageHeader } from "@/components/dcs/DcsPageHeader";
import { MarketingLeadershipOverview } from "@/components/dcs/MarketingLeadershipOverview";

export default function MarketingLeadershipPage() {
  const t = useTranslations("dcs.marketing.leadership");
  const [rows, setRows] = useState<MarketingLeadershipRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    api
      .get("/marketing/leadership")
      .then((r) => setRows(r.data))
      .catch((err) => setError(getApiErrorMessage(err, t("loadError"))))
      .finally(() => setLoading(false));
  }, [t]);

  return (
    <>
      <DcsPageHeader
        title={t("title")}
        subtitle={t("subtitle")}
        breadcrumb={t("title")}
        icon={Crown}
        iconClassName="bg-amber-500/10 text-amber-600 dark:text-amber-400"
      />
      <div className="flex-1 overflow-auto px-6 py-6 max-w-[1200px]">
        {error && (
          <div className="mb-4 rounded-xl border border-red-500/30 bg-red-500/5 px-4 py-3 text-sm text-red-700">
            {error}
          </div>
        )}
        {loading ? (
          <p className="text-sm text-foreground/40">{t("loading")}</p>
        ) : (
          <MarketingLeadershipOverview rows={rows} />
        )}
      </div>
    </>
  );
}
