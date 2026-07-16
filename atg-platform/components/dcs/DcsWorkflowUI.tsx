"use client";

import { ArrowLeft, Check, CheckCircle2, Circle, Loader2, Printer, X, type LucideIcon } from "lucide-react";
import { dcsTheme } from "@/components/dcs/dcsTheme";
import { officeDocTheme, type OfficeDocKind } from "@/components/dcs/officeDocTheme";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

/* ─── Shell & loading ─── */

export function DcsWorkflowShell({
  kind,
  children,
  className,
}: {
  kind: OfficeDocKind;
  children: React.ReactNode;
  className?: string;
}) {
  const theme = officeDocTheme(kind);
  return (
    <div
      className={cn(
        "relative flex-1 overflow-auto before:pointer-events-none before:absolute before:inset-0",
        theme.meshBg,
        className
      )}
    >
      <div className={cn("absolute inset-0 pointer-events-none", dcsTheme.gridOverlay)} aria-hidden />
      <div className="relative max-w-6xl mx-auto p-4 md:p-6 lg:p-8 space-y-6">{children}</div>
    </div>
  );
}

export function DcsWorkflowLoading({ label }: { label: string }) {
  return (
    <div className="flex-1 flex flex-col items-center justify-center gap-3 text-foreground/40 py-24">
      <div className="relative">
        <div className="absolute inset-0 rounded-full bg-sky-500/20 blur-xl animate-pulse" />
        <Loader2 className="relative animate-spin" size={28} strokeWidth={1.5} />
      </div>
      <span className="text-sm font-medium tracking-wide">{label}</span>
    </div>
  );
}

/* ─── Document hero header ─── */

export function DcsDocumentHero({
  kind,
  number,
  title,
  titleRu,
  phaseLabel,
  backLabel,
  printLabel,
  onBack,
  onPrint,
  icon: IconOverride,
  meta,
}: {
  kind: OfficeDocKind;
  number: string;
  title: string;
  titleRu?: string | null;
  phaseLabel: string;
  backLabel: string;
  printLabel: string;
  onBack: () => void;
  onPrint?: () => void;
  icon?: LucideIcon;
  meta?: React.ReactNode;
}) {
  const theme = officeDocTheme(kind);
  const Icon = IconOverride ?? theme.icon;

  return (
    <div className={cn("relative overflow-hidden rounded-2xl", dcsTheme.premiumCard)}>
      <div className={cn("absolute inset-x-0 top-0 h-1 bg-gradient-to-r opacity-90", theme.accentLine)} />
      <div className={cn("absolute inset-x-0 top-0 h-32 bg-gradient-to-b pointer-events-none", theme.headerGlow)} />

      <div className="relative p-5 md:p-6">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div className="flex gap-4 min-w-0 flex-1">
            <div className="relative shrink-0 hidden sm:block">
              <div
                className={cn(
                  "absolute -inset-1 rounded-2xl blur-md opacity-60",
                  theme.iconBg.replace("text-", "bg-").split(" ")[0]
                )}
              />
              <div
                className={cn(
                  "relative w-12 h-12 rounded-2xl flex items-center justify-center ring-1",
                  theme.iconBg,
                  theme.iconRing
                )}
              >
                <Icon size={22} strokeWidth={1.6} />
              </div>
            </div>

            <div className="min-w-0 flex-1">
              <button
                type="button"
                onClick={onBack}
                className="inline-flex items-center gap-1.5 text-xs font-medium text-foreground/45 hover:text-foreground transition-colors mb-3"
              >
                <ArrowLeft size={14} />
                {backLabel}
              </button>

              <div className="flex flex-wrap items-center gap-2 mb-2">
                <span
                  className={cn(
                    "inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg text-[10px] font-bold uppercase tracking-[0.12em] border",
                    theme.phaseBadgeActive
                  )}
                >
                  <span className="w-1.5 h-1.5 rounded-full bg-current animate-pulse" />
                  {phaseLabel}
                </span>
                <span className={cn("font-mono text-[11px] font-bold tracking-wide", theme.numberText)}>
                  {number}
                </span>
              </div>

              <h1 className="text-xl md:text-2xl font-bold tracking-tight leading-snug text-foreground">{title}</h1>
              {titleRu && <p className="text-sm text-foreground/55 mt-1.5 leading-relaxed">{titleRu}</p>}
              {meta && <div className="flex flex-wrap gap-2 mt-3">{meta}</div>}
            </div>
          </div>

          <div className="flex items-center gap-2 shrink-0">
            {onPrint && (
              <Button size="sm" variant="secondary" onClick={onPrint} className="rounded-xl">
                <Printer size={14} className="mr-1.5" />
                {printLabel}
              </Button>
            )}
            <button
              type="button"
              onClick={onBack}
              className="p-2.5 rounded-xl text-foreground/40 hover:text-foreground hover:bg-foreground/5 transition-colors"
              aria-label={backLabel}
            >
              <X size={18} />
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

export function DcsMetaPill({ children }: { children: React.ReactNode }) {
  return (
    <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg text-[11px] font-medium bg-foreground/[0.04] border border-border/50 text-foreground/60">
      {children}
    </span>
  );
}

/* ─── Workflow stepper ─── */

export interface DcsStepItem {
  key: string;
  label: string;
}

export function DcsWorkflowStepper({
  kind,
  title,
  steps,
  activeIndex,
  hint,
}: {
  kind: OfficeDocKind;
  title: string;
  steps: DcsStepItem[];
  activeIndex: number;
  hint?: string;
}) {
  const theme = officeDocTheme(kind);
  const safeIndex = Math.min(Math.max(activeIndex, 0), steps.length - 1);
  const progress = steps.length > 1 ? Math.round((safeIndex / (steps.length - 1)) * 100) : 0;
  const currentStep = steps[safeIndex];
  const isComplete = safeIndex === steps.length - 1;
  const progressBarClass =
    theme.accent === "violet"
      ? "from-violet-500 to-purple-400"
      : theme.accent === "amber"
        ? "from-amber-500 to-orange-400"
        : theme.accent === "orange"
          ? "from-orange-500 to-amber-400"
          : "from-sky-500 to-cyan-400";
  const activeShadowClass =
    theme.accent === "violet"
      ? "shadow-violet-500/20"
      : theme.accent === "amber"
        ? "shadow-amber-500/20"
        : theme.accent === "orange"
          ? "shadow-orange-500/20"
          : "shadow-sky-500/20";

  return (
    <div
      className={cn(
        "relative overflow-hidden rounded-2xl border border-slate-200/70 dark:border-white/[0.08]",
        "bg-white/90 dark:bg-surface/90 backdrop-blur-md",
        "shadow-[0_1px_2px_rgba(15,23,42,0.04),0_12px_40px_-12px_rgba(15,23,42,0.08)]"
      )}
    >
      <div className={cn("absolute inset-x-0 top-0 h-[3px] bg-gradient-to-r opacity-90", theme.accentLine)} />
      <div className={cn("absolute inset-x-0 top-0 h-28 bg-gradient-to-b pointer-events-none", theme.headerGlow)} />

      <div className="relative px-5 py-5 md:px-6 md:py-6">
        <div className="flex flex-wrap items-center justify-between gap-3 mb-6">
          <p className="text-[11px] font-bold uppercase tracking-[0.16em] text-foreground/45">{title}</p>
          <div className="flex items-center gap-2">
            {currentStep && (
              <span
                className={cn(
                  "hidden sm:inline-flex text-xs font-semibold px-3 py-1.5 rounded-full border",
                  isComplete
                    ? "bg-emerald-500/10 text-emerald-700 dark:text-emerald-300 border-emerald-500/25"
                    : theme.phaseBadge
                )}
              >
                {currentStep.label}
              </span>
            )}
            <span className="inline-flex items-center gap-1.5 text-xs font-bold tabular-nums px-2.5 py-1.5 rounded-full bg-slate-100/90 dark:bg-white/[0.06] text-foreground/55 border border-slate-200/60 dark:border-white/[0.08]">
              {isComplete && <Check size={12} className="text-emerald-500" strokeWidth={3} />}
              {progress}%
            </span>
          </div>
        </div>

        <div className="overflow-x-auto pb-1 -mx-1 px-1 scrollbar-thin">
          <div className="relative min-w-[720px] px-2">
            <div className="absolute left-6 right-6 top-[22px] h-[3px] rounded-full bg-slate-200/80 dark:bg-white/[0.08]" />
            <div
              className={cn(
                "absolute left-6 top-[22px] h-[3px] rounded-full transition-all duration-700 ease-out",
                isComplete ? "bg-gradient-to-r from-emerald-500 to-emerald-400" : cn("bg-gradient-to-r", progressBarClass)
              )}
              style={{
                width: `calc((100% - 3rem) * ${steps.length > 1 ? safeIndex / (steps.length - 1) : 0})`,
              }}
            />

            <div className="relative flex justify-between">
              {steps.map((step, idx) => {
                const done = idx < safeIndex;
                const active = idx === safeIndex;

                return (
                  <div key={step.key} className="flex flex-col items-center w-[72px] shrink-0">
                    <div
                      className={cn(
                        "relative z-[1] w-11 h-11 rounded-full flex items-center justify-center transition-all duration-300",
                        active &&
                          !isComplete &&
                          cn(
                            theme.stepActive,
                            "ring-[5px]",
                            theme.stepActiveRing,
                            "shadow-lg",
                            activeShadowClass,
                            "scale-105"
                          ),
                        active &&
                          isComplete &&
                          "bg-gradient-to-br from-emerald-500 to-teal-500 text-white ring-[5px] ring-emerald-500/20 shadow-lg shadow-emerald-500/25 scale-105",
                        done &&
                          !active &&
                          "bg-gradient-to-br from-emerald-500 to-emerald-600 text-white shadow-md shadow-emerald-500/20",
                        !done &&
                          !active &&
                          "bg-white dark:bg-slate-900 text-foreground/35 border-2 border-slate-200/90 dark:border-white/10 shadow-sm"
                      )}
                    >
                      {done && !active ? (
                        <Check size={18} strokeWidth={3} />
                      ) : active ? (
                        isComplete ? (
                          <Check size={18} strokeWidth={3} />
                        ) : (
                          <span className="relative flex h-3 w-3">
                            <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-white/50 opacity-60" />
                            <span className="relative inline-flex h-3 w-3 rounded-full bg-white" />
                          </span>
                        )
                      ) : (
                        <span className="text-[12px] font-bold tabular-nums">{idx + 1}</span>
                      )}
                    </div>

                    <p
                      className={cn(
                        "text-[11px] font-semibold mt-3 leading-snug text-center max-w-[80px] transition-colors",
                        active && !isComplete && theme.sectionAccent,
                        active && isComplete && "text-emerald-700 dark:text-emerald-300",
                        done && !active && "text-emerald-600/90 dark:text-emerald-400/90",
                        !done && !active && "text-foreground/40"
                      )}
                    >
                      {step.label}
                    </p>
                  </div>
                );
              })}
            </div>
          </div>
        </div>

        {hint && (
          <div
            className={cn(
              "mt-5 pt-4 border-t border-slate-200/70 dark:border-white/[0.08] flex items-start gap-3 rounded-xl px-3.5 py-3",
              isComplete ? "bg-emerald-500/[0.05]" : cn(theme.workflowCardBg, "bg-opacity-60")
            )}
          >
            <div
              className={cn(
                "mt-0.5 flex h-7 w-7 shrink-0 items-center justify-center rounded-lg",
                isComplete ? "bg-emerald-500/15 text-emerald-600" : cn(theme.iconBg, "ring-1", theme.iconRing)
              )}
            >
              {isComplete ? <CheckCircle2 size={15} /> : <Circle size={14} />}
            </div>
            <p className="text-sm text-foreground/60 leading-relaxed">{hint}</p>
          </div>
        )}
      </div>
    </div>
  );
}

/* ─── Tabs ─── */

export function DcsTabBar<T extends string>({
  kind,
  tabs,
  active,
  onChange,
}: {
  kind: OfficeDocKind;
  tabs: { key: T; label: string; count?: number }[];
  active: T;
  onChange: (key: T) => void;
}) {
  const theme = officeDocTheme(kind);
  return (
    <div className="flex gap-1 p-1.5 border-b border-border/50 bg-slate-50/50 dark:bg-white/[0.02]">
      {tabs.map(({ key, label, count }) => (
        <button
          key={key}
          type="button"
          onClick={() => onChange(key)}
          className={cn(
            "relative px-4 py-2.5 text-sm font-medium rounded-xl transition-all",
            active === key
              ? cn("bg-white dark:bg-white/[0.06] shadow-sm", theme.tabActive)
              : "text-foreground/45 hover:text-foreground/70 hover:bg-foreground/[0.03]"
          )}
        >
          {label}
          {count !== undefined && count > 0 && (
            <span className="ml-1.5 text-[10px] font-bold opacity-60">({count})</span>
          )}
          {active === key && (
            <span className={cn("absolute bottom-0 left-3 right-3 h-0.5 rounded-full", theme.tabIndicator)} />
          )}
        </button>
      ))}
    </div>
  );
}

/* ─── Cards & fields ─── */

export function DcsSectionCard({
  kind,
  title,
  icon: Icon,
  children,
  className,
}: {
  kind: OfficeDocKind;
  title: string;
  icon?: LucideIcon;
  children: React.ReactNode;
  className?: string;
}) {
  const theme = officeDocTheme(kind);
  return (
    <div className={cn("p-5 md:p-6 space-y-4", dcsTheme.premiumCard, className)}>
      <div className="flex items-center gap-2.5 pb-1 border-b border-border/40">
        {Icon && (
          <div className={cn("w-8 h-8 rounded-lg flex items-center justify-center", theme.iconBg)}>
            <Icon size={16} strokeWidth={1.8} />
          </div>
        )}
        <h2 className={cn("text-sm font-bold tracking-tight", theme.sectionAccent)}>{title}</h2>
      </div>
      {children}
    </div>
  );
}

export function DcsWorkflowCard({
  kind,
  title,
  hint,
  children,
  variant = "default",
}: {
  kind: OfficeDocKind;
  title: string;
  hint?: string;
  children?: React.ReactNode;
  variant?: "default" | "warning" | "success";
}) {
  const theme = officeDocTheme(kind);
  const variantStyles = {
    default: cn("border-l-[3px]", theme.workflowCardBorder, theme.workflowCardBg),
    warning: "border-l-[3px] border-l-amber-500 bg-gradient-to-r from-amber-500/[0.06] to-transparent",
    success: "border-l-[3px] border-l-emerald-500 bg-gradient-to-r from-emerald-500/[0.06] to-transparent",
  };

  return (
    <div className={cn("rounded-2xl border border-slate-200/80 dark:border-white/[0.08] p-5 space-y-4 shadow-sm", variantStyles[variant])}>
      <div>
        <h3 className="text-sm font-bold text-foreground">{title}</h3>
        {hint && <p className="text-xs text-foreground/50 mt-1.5 leading-relaxed">{hint}</p>}
      </div>
      {children}
    </div>
  );
}

export function DcsDetailField({ label, value }: { label: string; value: string }) {
  return (
    <div className="group">
      <p className="text-[10px] font-bold uppercase tracking-[0.1em] text-foreground/40 mb-1">{label}</p>
      <div className="min-h-[2.5rem] px-3.5 py-2 rounded-xl border border-border/50 bg-foreground/[0.02] text-sm leading-relaxed">
        {value}
      </div>
    </div>
  );
}

export function DcsFormField({ label, children, hint }: { label: string; children: React.ReactNode; hint?: string }) {
  return (
    <div>
      <label className="text-[10px] font-bold uppercase tracking-[0.1em] text-foreground/45 mb-1.5 block">
        {label}
      </label>
      {children}
      {hint && <p className="text-[11px] text-foreground/40 mt-1.5">{hint}</p>}
    </div>
  );
}

export function dcsInputClass(kind: OfficeDocKind) {
  const theme = officeDocTheme(kind);
  return cn(
    "w-full rounded-xl border border-slate-200/80 dark:border-white/10 bg-white/80 dark:bg-white/[0.04] px-3.5 py-2.5 text-sm shadow-sm transition-all",
    "placeholder:text-foreground/35 focus:outline-none focus:ring-2",
    theme.inputFocus
  );
}

export function DcsErrorAlert({ message }: { message: string }) {
  if (!message) return null;
  return (
    <div className="flex items-start gap-2.5 rounded-xl border border-red-500/25 bg-red-500/[0.06] px-4 py-3 text-sm text-red-700 dark:text-red-300">
      <span className="shrink-0 mt-0.5 w-1.5 h-1.5 rounded-full bg-red-500" />
      {message}
    </div>
  );
}

export function DcsCompletedBanner({ label }: { label: string }) {
  return (
    <div className="flex items-center gap-3 rounded-2xl border border-emerald-500/25 bg-gradient-to-r from-emerald-500/[0.08] to-transparent px-5 py-4">
      <div className="w-10 h-10 rounded-xl bg-emerald-500/15 flex items-center justify-center">
        <CheckCircle2 size={20} className="text-emerald-600" />
      </div>
      <p className="text-sm font-semibold text-emerald-700 dark:text-emerald-300">{label}</p>
    </div>
  );
}

export function DcsStatusSidebar({
  kind,
  title,
  phaseLabel,
  children,
}: {
  kind: OfficeDocKind;
  title: string;
  phaseLabel: string;
  children?: React.ReactNode;
}) {
  const theme = officeDocTheme(kind);
  return (
    <div className={cn("p-5 h-fit space-y-4 sticky top-6", dcsTheme.premiumCard)}>
      <div>
        <p className="text-[10px] font-bold uppercase tracking-[0.12em] text-foreground/40 mb-2">{title}</p>
        <span
          className={cn(
            "inline-flex items-center px-2.5 py-1 rounded-lg text-xs font-semibold border",
            theme.phaseBadgeActive
          )}
        >
          {phaseLabel}
        </span>
      </div>
      {children && <div className="space-y-3 pt-2 border-t border-border/40">{children}</div>}
    </div>
  );
}

/* ─── Create form layout ─── */

export function DcsCreateFormCard({
  kind,
  children,
  onSubmit,
  error,
  submitLabel,
  submittingLabel,
  submitting,
}: {
  kind: OfficeDocKind;
  children: React.ReactNode;
  onSubmit: (e: React.FormEvent) => void;
  error?: string;
  submitLabel: string;
  submittingLabel: string;
  submitting: boolean;
}) {
  const theme = officeDocTheme(kind);
  return (
    <form
      onSubmit={onSubmit}
      className={cn("relative max-w-2xl overflow-hidden rounded-2xl", dcsTheme.premiumCard)}
    >
      <div className={cn("absolute inset-x-0 top-0 h-1 bg-gradient-to-r", theme.accentLine)} />
      <div className="p-6 md:p-8 space-y-5">{children}</div>
      <div className="px-6 md:px-8 pb-6 md:pb-8 space-y-4 border-t border-border/40 pt-5 bg-slate-50/50 dark:bg-white/[0.02]">
        <DcsErrorAlert message={error ?? ""} />
        <Button
          type="submit"
          disabled={submitting}
          className={cn("w-full sm:w-auto h-11 px-8 font-semibold rounded-xl text-white border-0 shadow-lg", theme.primaryBtn)}
        >
          {submitting ? submittingLabel : submitLabel}
        </Button>
      </div>
    </form>
  );
}

export function DcsCheckboxCard({
  checked,
  onChange,
  label,
  hint,
}: {
  checked: boolean;
  onChange: (v: boolean) => void;
  label: string;
  hint?: string;
}) {
  return (
    <label className="flex items-start gap-3.5 rounded-xl border border-border/60 bg-foreground/[0.02] px-4 py-3.5 cursor-pointer hover:bg-foreground/[0.04] transition-colors">
      <input
        type="checkbox"
        className="mt-0.5 rounded border-border/60 text-sky-600 focus:ring-sky-500/30"
        checked={checked}
        onChange={(e) => onChange(e.target.checked)}
      />
      <div>
        <p className="text-sm font-medium">{label}</p>
        {hint && <p className="text-xs text-foreground/45 mt-0.5 leading-relaxed">{hint}</p>}
      </div>
    </label>
  );
}
