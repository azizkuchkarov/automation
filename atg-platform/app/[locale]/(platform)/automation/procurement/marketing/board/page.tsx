"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { AlertCircle, Columns3 } from "lucide-react";
import api, { getApiErrorMessage } from "@/lib/api";
import { MarketingBoardColumn } from "@/lib/marketing";
import { DcsPageHeader } from "@/components/dcs/DcsPageHeader";
import {
  MarketingKanbanBoard,
  MarketingKanbanSkeleton,
} from "@/components/dcs/MarketingKanbanBoard";
import { dcsTheme } from "@/components/dcs/dcsTheme";
import { cn } from "@/lib/utils";

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
    <div className={cn("relative flex flex-col flex-1 min-h-0", dcsTheme.meshBg)}>
      <div className={cn("absolute inset-0", dcsTheme.gridOverlay)} aria-hidden />
      <div className="relative z-[1] flex flex-col flex-1 min-h-0">
        <DcsPageHeader
          title={t("title")}
          subtitle={t("subtitle")}
          breadcrumb={t("title")}
          icon={Columns3}
          iconClassName="bg-gradient-to-br from-pink-500/15 to-violet-500/15 text-pink-600 dark:text-pink-400"
        />
        <div className="flex-1 overflow-auto px-6 py-6">
          {error && (
            <div
              className={cn(
                "mb-5 flex items-start gap-3 rounded-2xl border border-red-500/25",
                "bg-red-500/[0.06] px-4 py-3.5 text-sm text-red-700 dark:text-red-300"
              )}
            >
              <AlertCircle size={18} className="shrink-0 mt-0.5" />
              <p>{error}</p>
            </div>
          )}
          {loading ? <MarketingKanbanSkeleton /> : <MarketingKanbanBoard columns={columns} />}
        </div>
      </div>
    </div>
  );
}
