"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useLocale } from "next-intl";
import { ChevronRight, Briefcase, type LucideIcon } from "lucide-react";
import { cn } from "@/lib/utils";
import { dcsTheme } from "@/components/dcs/dcsTheme";

interface DcsPageHeaderProps {
  title: string;
  subtitle?: string;
  actions?: React.ReactNode;
  breadcrumb?: string;
  icon?: LucideIcon;
  iconClassName?: string;
  className?: string;
}

export function DcsPageHeader({
  title,
  subtitle,
  actions,
  breadcrumb,
  icon: Icon,
  iconClassName,
  className,
}: DcsPageHeaderProps) {
  const locale = useLocale();
  const pathname = usePathname();
  const section = pathname.includes("/office/")
    ? { label: "Document Office", href: "/automation/office/incoming" }
    : { label: "Procurement", href: "/automation/procurement/requests" };

  return (
    <header className={cn("relative shrink-0 overflow-hidden", className)}>
      {/* Accent line */}
      <div className="absolute inset-x-0 top-0 h-[3px] bg-gradient-to-r from-transparent via-sky-500 to-transparent opacity-80" />
      <div className="absolute inset-x-0 top-0 h-24 bg-gradient-to-b from-sky-500/[0.06] to-transparent pointer-events-none" />

      <div className={cn("relative px-6 py-6 border-b border-slate-200/60 dark:border-white/[0.06]", dcsTheme.glassPanel)}>
        <div className="flex items-center gap-1.5 text-xs text-foreground/45 mb-4">
          <Link
            href={`/${locale}/automation/procurement/requests`}
            className="inline-flex items-center gap-1.5 hover:text-sky-600 dark:hover:text-sky-400 transition-colors font-medium"
          >
            <Briefcase size={13} />
            <span>DCS</span>
          </Link>
          <ChevronRight size={12} className="opacity-40" />
          <Link
            href={`/${locale}${section.href}`}
            className="hover:text-sky-600 dark:hover:text-sky-400 transition-colors"
          >
            {section.label}
          </Link>
          {breadcrumb && (
            <>
              <ChevronRight size={12} className="opacity-40" />
              <span className="text-foreground/70 font-medium">{breadcrumb}</span>
            </>
          )}
        </div>

        <div className="flex items-start justify-between gap-6">
          <div className="flex items-start gap-5 min-w-0">
            {Icon && (
              <div className="relative shrink-0">
                <div className="absolute -inset-1 rounded-2xl bg-gradient-to-br from-sky-400/30 to-blue-600/20 blur-md opacity-70" />
                <div
                  className={cn(
                    "relative w-14 h-14 rounded-2xl flex items-center justify-center shadow-lg ring-1 ring-white/50 dark:ring-white/10",
                    iconClassName ?? "bg-gradient-to-br from-sky-500/15 to-blue-600/10 text-sky-600 dark:text-sky-400"
                  )}
                >
                  <Icon size={26} strokeWidth={1.6} />
                </div>
              </div>
            )}
            <div className="min-w-0 pt-0.5">
              <h1 className="text-[1.65rem] font-bold tracking-tight text-foreground leading-tight bg-gradient-to-br from-foreground to-foreground/70 bg-clip-text">
                {title}
              </h1>
              {subtitle && (
                <p className="text-sm text-foreground/50 mt-2 leading-relaxed max-w-2xl">{subtitle}</p>
              )}
            </div>
          </div>
          {actions && <div className="shrink-0 pt-1">{actions}</div>}
        </div>
      </div>
    </header>
  );
}
