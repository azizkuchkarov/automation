"use client";

import { useState, type ReactNode } from "react";
import { CheckCircle2, MessageSquare, Send } from "lucide-react";
import { ProcurementStepComment, ProcurementWorkflowPhase } from "@/lib/procurementRequest";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

interface CompleteAction {
  label: string;
  disabled?: boolean;
  onComplete: (body: string) => void | Promise<void>;
}

interface Props {
  comments: ProcurementStepComment[];
  phase: ProcurementWorkflowPhase;
  stepNumber: number;
  locale: string;
  canAdd?: boolean;
  acting?: boolean;
  placeholder?: string;
  onAdd?: (body: string) => Promise<void>;
  completeAction?: CompleteAction;
  completePlaceholder?: string;
  completeActionsPrefix?: ReactNode;
}

export function StepCommentThread({
  comments,
  phase,
  stepNumber,
  locale,
  canAdd,
  acting,
  placeholder,
  onAdd,
  completeAction,
  completePlaceholder,
  completeActionsPrefix,
}: Props) {
  const [draft, setDraft] = useState("");
  const filtered = comments.filter((c) => c.phase === phase && c.stepNumber === stepNumber);

  const submitNote = async () => {
    if (!onAdd || !draft.trim()) return;
    await onAdd(draft.trim());
    setDraft("");
  };

  const submitComplete = async () => {
    if (!completeAction || !draft.trim()) return;
    await completeAction.onComplete(draft.trim());
    setDraft("");
  };

  const showNoteForm = canAdd && onAdd && !completeAction;
  const showCompleteForm = Boolean(completeAction);

  return (
    <div className="mt-3 rounded-xl border border-border/50 bg-foreground/[0.02] overflow-hidden">
      <div className="px-3 py-2 border-b border-border/40 flex items-center gap-2 text-[10px] font-bold uppercase tracking-wider text-foreground/45">
        <MessageSquare size={12} />
        {locale.startsWith("en") ? "Comments" : "Комментарии"} ({filtered.length})
      </div>
      <div className="p-3 space-y-2 max-h-48 overflow-y-auto">
        {filtered.length === 0 ? (
          <p className="text-xs text-foreground/40 italic">
            {locale.startsWith("en") ? "No comments yet" : "Комментариев пока нет"}
          </p>
        ) : (
          filtered.map((c) => (
            <div key={c.id} className="rounded-lg bg-background/80 border border-border/40 px-3 py-2">
              <div className="flex items-center justify-between gap-2 mb-1">
                <span className="text-xs font-semibold text-foreground/80">{c.authorName}</span>
                <span className="text-[10px] text-foreground/40 shrink-0">
                  {new Date(c.createdAt).toLocaleString(locale)}
                </span>
              </div>
              <p className="text-sm text-foreground/70 leading-relaxed whitespace-pre-wrap">{c.body}</p>
            </div>
          ))
        )}
      </div>
      {showCompleteForm && (
        <div className="p-3 border-t border-border/40 space-y-2">
          <textarea
            className={cn(
              "w-full rounded-lg border border-border/70 bg-background px-3 py-2 text-sm min-h-[72px] resize-y",
              "focus:outline-none focus:ring-2 focus:ring-atg-blue/30"
            )}
            placeholder={completePlaceholder}
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
          />
          <div className="flex flex-wrap items-center gap-2">
            {completeActionsPrefix}
            <Button
              size="sm"
              disabled={acting || completeAction?.disabled || !draft.trim()}
              onClick={submitComplete}
            >
              <CheckCircle2 size={14} className="mr-1.5" />
              {completeAction?.label}
            </Button>
          </div>
        </div>
      )}
      {showNoteForm && (
        <div className="p-3 border-t border-border/40 flex gap-2">
          <textarea
            className={cn(
              "flex-1 rounded-lg border border-border/70 bg-background px-3 py-2 text-sm min-h-[52px] resize-y",
              "focus:outline-none focus:ring-2 focus:ring-atg-blue/30"
            )}
            placeholder={placeholder}
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
          />
          <Button size="sm" className="self-end shrink-0" disabled={acting || !draft.trim()} onClick={submitNote}>
            <Send size={14} />
          </Button>
        </div>
      )}
    </div>
  );
}
