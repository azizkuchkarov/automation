"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import api from "@/lib/api";
import { TicketBoard as TicketBoardType } from "@/lib/helpdesk";
import { KanbanBoard } from "@/components/helpdesk/KanbanBoard";
import { HelpdeskPageHeader } from "@/components/helpdesk/HelpdeskPageHeader";
import { Button } from "@/components/ui/Button";
import Link from "next/link";
import { useLocale } from "next-intl";
import { Plus, RefreshCw } from "lucide-react";

function BoardSkeleton() {
  return (
    <div className="flex gap-4 px-6 py-6 overflow-hidden">
      {Array.from({ length: 5 }).map((_, i) => (
        <div
          key={i}
          className="w-[292px] shrink-0 rounded-xl border border-border/50 bg-slate-100/90 dark:bg-surface/60 h-[420px] animate-pulse"
        />
      ))}
    </div>
  );
}

export default function HelpdeskBoardPage() {
  const t = useTranslations("helpdesk");
  const locale = useLocale();
  const [board, setBoard] = useState<TicketBoardType | null>(null);
  const [loading, setLoading] = useState(true);

  const load = () => {
    setLoading(true);
    api.get("/helpdesk/board").then((r) => setBoard(r.data)).finally(() => setLoading(false));
  };

  useEffect(() => { load(); }, []);

  return (
    <>
      <HelpdeskPageHeader
        title={t("board.title")}
        subtitle={t("board.subtitle")}
        breadcrumb={t("nav.board")}
        actions={
          <>
            <Button variant="secondary" size="sm" onClick={load} disabled={loading} title="Refresh">
              <RefreshCw size={14} className={loading ? "animate-spin" : ""} />
            </Button>
            <Link href={`/${locale}/helpdesk/tickets/new`}>
              <Button size="sm">
                <Plus size={14} className="mr-1.5" />
                {t("nav.create")}
              </Button>
            </Link>
          </>
        }
      />
      <div className="flex-1 overflow-auto px-6 py-5">
        {loading && !board ? (
          <BoardSkeleton />
        ) : board ? (
          <KanbanBoard board={board} />
        ) : null}
      </div>
    </>
  );
}
