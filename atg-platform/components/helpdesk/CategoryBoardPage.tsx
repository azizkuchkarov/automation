"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";
import { Plus, RefreshCw } from "lucide-react";
import { HelpdeskPageHeader } from "@/components/helpdesk/HelpdeskPageHeader";
import { KanbanBoard } from "@/components/helpdesk/KanbanBoard";
import { Button } from "@/components/ui/Button";
import {
  TicketBoard,
  TicketCategory,
  categoryLabel,
  categoryPath,
  type HelpDeskCategory,
} from "@/lib/helpdesk";
import { fetchHelpDeskBoard, fetchHelpDeskCategories } from "@/lib/helpdeskApi";

function BoardSkeleton() {
  return (
    <div className="flex gap-4 overflow-hidden px-6 py-6">
      {Array.from({ length: 5 }).map((_, i) => (
        <div
          key={i}
          className="h-[420px] w-[292px] shrink-0 animate-pulse rounded-xl border border-border/50 bg-slate-100/90"
        />
      ))}
    </div>
  );
}

export function CategoryBoardPage({ category }: { category: TicketCategory }) {
  const t = useTranslations("helpdesk");
  const locale = useLocale();
  const [board, setBoard] = useState<TicketBoard | null>(null);
  const [meta, setMeta] = useState<HelpDeskCategory | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchHelpDeskCategories()
      .then((items) => setMeta(items.find((c) => c.category === category) ?? null))
      .catch(() => setMeta(null));
  }, [category]);

  const load = useCallback(() => {
    setLoading(true);
    fetchHelpDeskBoard(category)
      .then(setBoard)
      .finally(() => setLoading(false));
  }, [category]);

  useEffect(() => {
    load();
  }, [load]);

  const title = meta ? categoryLabel(meta, locale) : category;

  return (
    <>
      <HelpdeskPageHeader
        title={t("board.categoryTitle", { category: title })}
        subtitle={t("board.categorySubtitle")}
        breadcrumb={title}
        actions={
          <>
            <Button variant="secondary" size="sm" onClick={load} disabled={loading} title="Refresh">
              <RefreshCw size={14} className={loading ? "animate-spin" : ""} />
            </Button>
            <Link href={categoryPath(locale, category, "new")}>
              <Button size="sm">
                <Plus size={14} className="mr-1.5" />
                {t("nav.create")}
              </Button>
            </Link>
          </>
        }
      />
      <div className="flex-1 overflow-auto px-6 py-5">
        {loading && !board ? <BoardSkeleton /> : board ? <KanbanBoard board={board} /> : null}
      </div>
    </>
  );
}
