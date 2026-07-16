"use client";

import Link from "next/link";
import type { LucideIcon } from "lucide-react";
import { ChevronRight, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { hrPhaseBadgeClass, hrTheme } from "@/components/hr/hrTheme";
import { cn } from "@/lib/utils";

export function HrPageShell({ children }: { children: React.ReactNode }) {
  return (
    <div className={cn("relative flex flex-col flex-1 min-h-0", hrTheme.mesh)}>
      <div className={cn("absolute inset-0 pointer-events-none", hrTheme.grid)} aria-hidden />
      <div className="relative flex flex-col flex-1 min-h-0">{children}</div>
    </div>
  );
}

export function HrPageHeader({
  title,
  subtitle,
  actionHref,
  actionLabel,
  actionIcon: ActionIcon,
}: {
  title: string;
  subtitle?: string;
  actionHref?: string;
  actionLabel?: string;
  actionIcon?: LucideIcon;
}) {
  return (
    <header className="shrink-0 border-b border-slate-200/70 bg-white/70 backdrop-blur-xl px-6 py-5">
      <div className="flex items-start justify-between gap-4">
        <div className="min-w-0">
          <h1 className="text-xl font-semibold tracking-tight text-slate-900">{title}</h1>
          {subtitle && <p className="text-sm text-slate-500 mt-1">{subtitle}</p>}
        </div>
        {actionHref && actionLabel && (
          <Link
            href={actionHref}
            className={cn(
              "inline-flex items-center justify-center h-10 px-4 rounded-xl text-sm font-semibold transition-all shrink-0",
              hrTheme.primaryBtn,
            )}
          >
            {ActionIcon && <ActionIcon size={16} className="mr-1.5" />}
            {actionLabel}
          </Link>
        )}
      </div>
    </header>
  );
}

export function HrSectionTitle({
  title,
  subtitle,
  action,
}: {
  title: string;
  subtitle?: string;
  action?: React.ReactNode;
}) {
  return (
    <div className="flex flex-wrap items-end justify-between gap-3">
      <div>
        <h2 className="text-base font-semibold text-slate-900">{title}</h2>
        {subtitle && <p className="text-sm text-slate-500 mt-0.5">{subtitle}</p>}
      </div>
      {action}
    </div>
  );
}

export function HrLoadingState({ label }: { label: string }) {
  return (
    <div className="flex items-center justify-center gap-2.5 py-20 text-slate-400">
      <Loader2 className="animate-spin" size={20} />
      <span className="text-sm font-medium">{label}</span>
    </div>
  );
}

export function HrEmptyState({
  icon: Icon,
  title,
  description,
  actionHref,
  actionLabel,
}: {
  icon: LucideIcon;
  title: string;
  description?: string;
  actionHref?: string;
  actionLabel?: string;
}) {
  return (
    <div className={cn("px-8 py-14 text-center", hrTheme.card)}>
      <div
        className={cn(
          "mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-2xl text-white",
          hrTheme.iconTile,
        )}
      >
        <Icon size={24} />
      </div>
      <p className="text-base font-semibold text-slate-800">{title}</p>
      {description && <p className="text-sm text-slate-500 mt-1.5 max-w-sm mx-auto">{description}</p>}
      {actionHref && actionLabel && (
        <Link
          href={actionHref}
          className={cn(
            "inline-flex items-center justify-center h-10 px-5 rounded-xl text-sm font-semibold mt-5",
            hrTheme.primaryBtn,
          )}
        >
          {actionLabel}
        </Link>
      )}
    </div>
  );
}

export function HrPhaseBadge({ phase, label }: { phase: string; label: string }) {
  return (
    <span
      className={cn(
        "inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-semibold border",
        hrPhaseBadgeClass(phase),
      )}
    >
      {label}
    </span>
  );
}

export function HrDataTable({
  headers,
  children,
  accent = "default",
}: {
  headers: React.ReactNode[];
  children: React.ReactNode;
  accent?: "default" | "amber";
}) {
  return (
    <div
      className={cn(
        hrTheme.table,
        accent === "amber" && "ring-1 ring-amber-200/60 bg-amber-50/20",
      )}
    >
      <table className="w-full text-sm">
        <thead>
          <tr
            className={cn(
              "border-b text-left",
              accent === "amber"
                ? "border-amber-200/60 bg-amber-50/60"
                : "border-slate-200/70 bg-slate-50/80",
            )}
          >
            {headers.map((header, i) => (
              <th
                key={i}
                className="px-4 py-3.5 text-[11px] font-semibold uppercase tracking-wider text-slate-500"
              >
                {header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100/80">{children}</tbody>
      </table>
    </div>
  );
}

export function HrTableRow({
  children,
  accent = "default",
}: {
  children: React.ReactNode;
  accent?: "default" | "amber";
}) {
  return (
    <tr
      className={cn(
        "transition-colors",
        accent === "amber" ? "hover:bg-amber-50/40" : "hover:bg-slate-50/80",
      )}
    >
      {children}
    </tr>
  );
}

export function HrOpenLink({ href, label }: { href: string; label: string }) {
  return (
    <Link
      href={href}
      className={cn("inline-flex items-center gap-1 text-sm font-semibold", hrTheme.link)}
    >
      {label}
      <ChevronRight size={14} />
    </Link>
  );
}

export function HrPrimaryButton({
  children,
  className,
  ...props
}: React.ComponentProps<typeof Button>) {
  return (
    <Button
      className={cn("rounded-xl font-semibold", hrTheme.primaryBtn, className)}
      {...props}
    >
      {children}
    </Button>
  );
}
