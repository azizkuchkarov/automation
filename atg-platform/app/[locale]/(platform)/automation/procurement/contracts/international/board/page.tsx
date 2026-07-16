"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { AlertCircle, Columns3 } from "lucide-react";
import api, { getApiErrorMessage } from "@/lib/api";
import { ProcurementContractsBoardColumn } from "@/lib/procurementRequest";
import { DcsPageHeader } from "@/components/dcs/DcsPageHeader";
import {
  ContractsKanbanBoard,
  ContractsKanbanSkeleton,
} from "@/components/dcs/ContractsKanbanBoard";
import { dcsTheme } from "@/components/dcs/dcsTheme";
import { cn } from "@/lib/utils";

export default function ContractsInternationalBoardPage() {
  const t = useTranslations("dcs.contractsQueue.board");
  const [columns, setColumns] = useState<ProcurementContractsBoardColumn[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    api
      .get("/dcs/procurement-requests/contracts/board", { params: { section: "International" } })
      .then((r) => setColumns(r.data))
      .catch((err) => setError(getApiErrorMessage(err, t("loadError"))))
      .finally(() => setLoading(false));
  }, [t]);

  return (
    <div className={cn("relative flex flex-col flex-1 min-h-0", dcsTheme.meshBg)}>
      <div className={cn("absolute inset-0", dcsTheme.gridOverlay)} aria-hidden />
      <div className="relative z-[1] flex flex-col flex-1 min-h-0">
        <DcsPageHeader
          title={t("internationalTitle")}
          subtitle={t("internationalSubtitle")}
          breadcrumb={t("internationalTitle")}
          icon={Columns3}
          iconClassName="bg-gradient-to-br from-sky-500/15 to-indigo-500/15 text-sky-700 dark:text-sky-300"
        />
        <div className="flex-1 overflow-auto px-6 py-6">
          {error && (
            <div className="mb-5 flex items-start gap-3 rounded-2xl border border-red-500/25 bg-red-500/[0.06] px-4 py-3.5 text-sm text-red-700 dark:text-red-300">
              <AlertCircle size={18} className="shrink-0 mt-0.5" />
              <p>{error}</p>
            </div>
          )}
          {loading ? (
            <ContractsKanbanSkeleton />
          ) : (
            <ContractsKanbanBoard columns={columns} section="International" />
          )}
        </div>
      </div>
    </div>
  );
}
