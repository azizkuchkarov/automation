"use client";

import { useTranslations } from "next-intl";
import { TicketBoard, BOARD_COLUMNS } from "@/lib/helpdesk";
import { TicketCard } from "./TicketBadges";
import { cn } from "@/lib/utils";
import { Inbox } from "lucide-react";

const COLUMN_THEME: Record<string, { dot: string; ring: string }> = {
  open: { dot: "bg-slate-400", ring: "ring-slate-400/20" },
  assigned: { dot: "bg-violet-500", ring: "ring-violet-500/20" },
  accepted: { dot: "bg-indigo-500", ring: "ring-indigo-500/20" },
  inProgress: { dot: "bg-atg-blue", ring: "ring-atg-blue/20" },
  done: { dot: "bg-emerald-500", ring: "ring-emerald-500/20" },
};

export function KanbanBoard({ board }: { board: TicketBoard }) {
  const t = useTranslations("helpdesk");

  return (
    <div className="flex gap-4 overflow-x-auto pb-6 px-1 min-h-[calc(100vh-11rem)]">
      {BOARD_COLUMNS.map(({ key, status }) => {
        const items = board[key];
        const theme = COLUMN_THEME[key];

        return (
          <div
            key={key}
            className={cn(
              "flex flex-col w-[292px] shrink-0 rounded-xl",
              "bg-slate-100/90 dark:bg-[#161b22]/90",
              "border border-border/50 shadow-sm",
              "ring-1 ring-inset",
              theme.ring
            )}
          >
            <div className="flex items-center gap-2.5 px-3.5 py-3 border-b border-border/40">
              <span className={cn("w-2.5 h-2.5 rounded-full shrink-0", theme.dot)} />
              <span className="text-[13px] font-semibold text-foreground/85 flex-1 truncate">
                {t(`status.${status}`)}
              </span>
              <span
                className={cn(
                  "text-[11px] font-bold tabular-nums min-w-[22px] h-[22px] flex items-center justify-center rounded-md",
                  "bg-surface text-foreground/50 border border-border/60 shadow-sm"
                )}
              >
                {items.length}
              </span>
            </div>

            <div className="flex-1 p-2.5 space-y-2.5 overflow-y-auto max-h-[calc(100vh-13rem)]">
              {items.length === 0 ? (
                <div className="flex flex-col items-center justify-center gap-2 py-10 px-4 rounded-lg border border-dashed border-border/60 bg-surface/40">
                  <div className="w-9 h-9 rounded-full bg-border/30 flex items-center justify-center">
                    <Inbox size={16} className="text-foreground/30" />
                  </div>
                  <p className="text-xs text-foreground/40 text-center">{t("board.empty")}</p>
                </div>
              ) : (
                items.map((ticket) => (
                  <TicketCard key={ticket.id} ticket={ticket} variant="kanban" />
                ))
              )}
            </div>
          </div>
        );
      })}
    </div>
  );
}
