"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { Columns3 } from "lucide-react";
import api, { getApiErrorMessage } from "@/lib/api";
import { MarketingBoardColumn } from "@/lib/marketing";
import { DcsPageHeader } from "@/components/dcs/DcsPageHeader";
import { MarketingKanbanBoard } from "@/components/dcs/MarketingKanbanBoard";

export default function MarketingBoardPage() {
  const t = useTranslations("dcs.marketing.board");
  const [columns, setColumns] = useState<MarketingBoardColumn[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    api
      .get("/marketing/board")
      .then((r) => setColumns(r.data))
      .catch((err) => setError(getApiErrorMessage(err, t("loadError"))))
      .finally(() => setLoading(false));
  }, [t]);

  return (
    <>
      <DcsPageHeader
        title={t("title")}
        subtitle={t("subtitle")}
        breadcrumb={t("title")}
        icon={Columns3}
        iconClassName="bg-violet-500/10 text-violet-600 dark:text-violet-400"
      />
      <div className="flex-1 overflow-auto px-6 py-6">
        {error && (
          <div className="mb-4 rounded-xl border border-red-500/30 bg-red-500/5 px-4 py-3 text-sm text-red-700">
            {error}
          </div>
        )}
        {loading ? (
          <p className="text-sm text-foreground/40">{t("loading")}</p>
        ) : (
          <MarketingKanbanBoard columns={columns} />
        )}
      </div>
    </>
  );
}
