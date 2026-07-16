"use client";

import { ChevronLeft, ChevronRight } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

interface Props {
  viewStep: number;
  totalSteps: number;
  workflowStep: number;
  stepLabel: string;
  previousLabel: string;
  nextLabel: string;
  viewCompletedHint?: string;
  viewUpcomingHint?: string;
  accent?: "sky" | "violet" | "amber";
  onPrevious: () => void;
  onNext: () => void;
  onSelectStep?: (step: number) => void;
}

export function WorkflowStepNavigator({
  viewStep,
  totalSteps,
  workflowStep,
  stepLabel,
  previousLabel,
  nextLabel,
  viewCompletedHint,
  viewUpcomingHint,
  accent = "sky",
  onPrevious,
  onNext,
  onSelectStep,
}: Props) {
  const canPrev = viewStep > 1;
  const canNext = viewStep < totalSteps;
  const isViewingCurrent = viewStep === workflowStep;
  const isUpcoming = viewStep > workflowStep;
  const dotActive =
    accent === "violet" ? "bg-violet-500" : accent === "amber" ? "bg-amber-500" : "bg-sky-500";
  const dotDone = "bg-emerald-500";

  return (
    <div
      className={cn(
        "border-t",
        accent === "violet"
          ? "border-violet-500/15"
          : accent === "amber"
            ? "border-amber-500/15"
            : "border-sky-500/15"
      )}
    >
      {onSelectStep && (
        <div className="px-5 pt-3 flex flex-wrap justify-center gap-1.5">
          {Array.from({ length: totalSteps }, (_, i) => i + 1).map((n) => {
            const done = n < workflowStep;
            const current = n === workflowStep;
            const selected = n === viewStep;
            return (
              <button
                key={n}
                type="button"
                title={`${stepLabel} ${n}`}
                onClick={() => onSelectStep(n)}
                className={cn(
                  "h-2 rounded-full transition-all",
                  selected ? "w-6" : "w-2",
                  done ? dotDone : current ? dotActive : "bg-foreground/15",
                  selected && !done && !current && "ring-2 ring-offset-1 ring-foreground/20"
                )}
              />
            );
          })}
        </div>
      )}

      <div className="flex items-center justify-between gap-3 px-4 py-3">
        <Button variant="secondary" size="sm" disabled={!canPrev} onClick={onPrevious}>
          <ChevronLeft size={16} className="mr-1" />
          {previousLabel}
        </Button>

        <div className="text-center text-xs min-w-0">
          <p className="font-semibold text-foreground tabular-nums">
            {stepLabel} {viewStep}
            <span className="text-foreground/40 font-normal"> / {totalSteps}</span>
          </p>
          {!isViewingCurrent && (
            <p className="text-[10px] text-foreground/45 mt-0.5 truncate">
              {isUpcoming ? viewUpcomingHint : viewCompletedHint}
            </p>
          )}
        </div>

        <Button variant="secondary" size="sm" disabled={!canNext} onClick={onNext}>
          {nextLabel}
          <ChevronRight size={16} className="ml-1" />
        </Button>
      </div>
    </div>
  );

}
