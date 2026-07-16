"use client";

import {
  Check,
  Circle,
  FileSpreadsheet,
  FileText,
  Flag,
  Download,
} from "lucide-react";
import { cn } from "@/lib/utils";

export type ChainStepStatus = "done" | "active" | "waiting";

export interface ApprovalChainStep {
  id: string;
  levelLabel: string;
  name: string;
  statusLabel: string;
  status: ChainStepStatus;
  isYou?: boolean;
  youHint?: string;
}

export interface ChainDocument {
  id: string;
  fileName: string;
  kindLabel: string;
}

interface Props {
  title: string;
  steps: ApprovalChainStep[];
  documentsTitle: string;
  documents: ChainDocument[];
  emptyDocsHint: string;
  finalLabel: string;
  finalName: string;
}

export function RequestApprovalChainPreview({
  title,
  steps,
  documentsTitle,
  documents,
  emptyDocsHint,
  finalLabel,
  finalName,
}: Props) {
  return (
    <aside className="rounded-xl border border-slate-200 bg-white shadow-sm dark:border-white/10 dark:bg-slate-900/40">
      <div className="border-b border-slate-100 px-5 py-4 dark:border-white/[0.06]">
        <h2 className="text-[15px] font-semibold tracking-tight text-slate-800 dark:text-slate-100">
          {title}
        </h2>
      </div>

      <div className="px-5 py-5">
        <ol className="relative space-y-0">
          {steps.map((step, index) => {
            const isLast = index === steps.length - 1;
            return (
              <li key={step.id} className="relative flex gap-3 pb-5 last:pb-0">
                {!isLast && (
                  <span
                    className={cn(
                      "absolute left-[11px] top-7 bottom-0 w-px",
                      step.status === "done" ? "bg-slate-300" : "bg-slate-200 dark:bg-white/10",
                    )}
                    aria-hidden
                  />
                )}
                <span
                  className={cn(
                    "relative z-[1] mt-0.5 flex h-6 w-6 shrink-0 items-center justify-center rounded-full",
                    step.status === "done" && "bg-emerald-500 text-white",
                    step.status === "active" && "border-2 border-sky-600 bg-white dark:bg-slate-900",
                    step.status === "waiting" && "border-2 border-slate-200 bg-white dark:border-white/15 dark:bg-slate-900",
                  )}
                >
                  {step.status === "done" ? (
                    <Check size={13} strokeWidth={3} />
                  ) : (
                    <Circle
                      size={8}
                      className={cn(
                        step.status === "active" ? "fill-sky-600 text-sky-600" : "fill-transparent text-transparent",
                      )}
                    />
                  )}
                </span>

                <div className="min-w-0 flex-1 pt-0.5">
                  <p
                    className={cn(
                      "text-[10px] font-bold uppercase tracking-[0.12em]",
                      step.status === "waiting" ? "text-slate-400" : "text-slate-500",
                    )}
                  >
                    {step.levelLabel}
                  </p>
                  <p
                    className={cn(
                      "mt-0.5 text-sm font-semibold",
                      step.status === "waiting" ? "text-slate-400" : "text-slate-800 dark:text-slate-100",
                    )}
                  >
                    {step.name}
                  </p>
                  <p
                    className={cn(
                      "mt-0.5 text-xs font-semibold uppercase tracking-wide",
                      step.status === "done" && "text-emerald-600",
                      step.status === "active" && "text-sky-700 dark:text-sky-400",
                      step.status === "waiting" && "text-slate-400",
                    )}
                  >
                    {step.statusLabel}
                  </p>
                  {step.isYou && step.youHint && (
                    <div className="mt-2 rounded-lg border border-sky-200 bg-sky-50 px-3 py-2 text-xs font-medium text-sky-800 dark:border-sky-500/30 dark:bg-sky-500/10 dark:text-sky-200">
                      {step.youHint}
                    </div>
                  )}
                </div>
              </li>
            );
          })}

          <li className="relative flex gap-3 pt-1">
            <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full border-2 border-slate-200 bg-white text-slate-400 dark:border-white/15 dark:bg-slate-900">
              <Flag size={12} />
            </span>
            <div className="min-w-0 pt-0.5">
              <p className="text-[10px] font-bold uppercase tracking-[0.12em] text-slate-400">
                {finalLabel}
              </p>
              <p className="mt-0.5 text-sm font-medium text-slate-400">{finalName}</p>
            </div>
          </li>
        </ol>
      </div>

      <div className="border-t border-slate-100 px-5 py-4 dark:border-white/[0.06]">
        <h3 className="mb-3 text-sm font-semibold text-slate-800 dark:text-slate-100">
          {documentsTitle} ({documents.length})
        </h3>
        {documents.length === 0 ? (
          <p className="text-xs text-slate-400">{emptyDocsHint}</p>
        ) : (
          <ul className="space-y-2">
            {documents.map((doc) => {
              const isPdf = doc.fileName.toLowerCase().endsWith(".pdf");
              return (
                <li
                  key={doc.id}
                  className="flex items-center gap-3 rounded-lg border border-slate-200 bg-white px-3 py-2.5 dark:border-white/10 dark:bg-white/[0.03]"
                >
                  <span
                    className={cn(
                      "flex h-8 w-8 shrink-0 items-center justify-center rounded-md",
                      isPdf ? "bg-red-50 text-red-600" : "bg-sky-50 text-sky-600",
                    )}
                  >
                    {isPdf ? <FileText size={16} /> : <FileSpreadsheet size={16} />}
                  </span>
                  <div className="min-w-0 flex-1">
                    <p className="truncate text-sm font-medium text-slate-800 dark:text-slate-100">
                      {doc.fileName}
                    </p>
                    <p className="text-[11px] text-slate-400">{doc.kindLabel}</p>
                  </div>
                  <Download size={15} className="shrink-0 text-slate-300" />
                </li>
              );
            })}
          </ul>
        )}
      </div>
    </aside>
  );
}
