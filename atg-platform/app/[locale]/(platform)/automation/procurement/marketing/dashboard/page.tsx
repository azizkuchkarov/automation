"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { BarChart3 } from "lucide-react";
import api, { getApiErrorMessage } from "@/lib/api";
import { MarketingStats } from "@/lib/marketing";
import { DcsPageHeader } from "@/components/dcs/DcsPageHeader";
import { MarketingDashboard } from "@/components/dcs/MarketingDashboard";

export default function MarketingDashboardPage() {
  const t = useTranslations("dcs.marketing.dashboard");
  const [stats, setStats] = useState<MarketingStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    api
      .get("/marketing/stats")
      .then((r) => setStats(r.data))
      .catch((err) => setError(getApiErrorMessage(err, t("loadError"))))
      .finally(() => setLoading(false));
  }, [t]);

  return (
    <>
      <DcsPageHeader
        title={t("title")}
        subtitle={t("subtitle")}
        breadcrumb={t("title")}
        icon={BarChart3}
        iconClassName="bg-indigo-500/10 text-indigo-600 dark:text-indigo-400"
      />
      <div className="flex-1 overflow-auto px-6 py-6 max-w-[1200px]">
        {error && (
          <div className="mb-4 rounded-xl border border-red-500/30 bg-red-500/5 px-4 py-3 text-sm text-red-700">
            {error}
          </div>
        )}
        {loading ? (
          <p className="text-sm text-foreground/40">{t("loading")}</p>
        ) : stats ? (
          <MarketingDashboard stats={stats} />
        ) : null}
      </div>
    </>
  );
}
