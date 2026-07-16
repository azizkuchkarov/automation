"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useLocale } from "next-intl";
import { ChevronRight, Briefcase, type LucideIcon } from "lucide-react";
import { cn } from "@/lib/utils";
import { dcsTheme } from "@/components/dcs/dcsTheme";
import { officeDocTheme, type OfficeDocKind } from "@/components/dcs/officeDocTheme";

interface DcsPageHeaderProps {
  title: string;
  subtitle?: string;
  actions?: React.ReactNode;
  breadcrumb?: string;
  icon?: LucideIcon;
  iconClassName?: string;
  /** Per document-type accent (incoming, outgoing, memo, orders) */
  officeKind?: OfficeDocKind;
  className?: string;
}

export function DcsPageHeader({
  title,
  subtitle,
  actions,
  breadcrumb,
  icon: Icon,
  iconClassName,
  officeKind,
  className,
}: DcsPageHeaderProps) {
  const locale = useLocale();
  const pathname = usePathname();
  const officeTheme = officeKind ? officeDocTheme(officeKind) : null;
  const section = pathname.includes("/office/")
    ? { label: "Document Office", href: "/automation/office/incoming" }
    : { label: "Procurement", href: "/automation/procurement/requests" };

  return (
    <header className={cn("relative shrink-0 overflow-hidden", className)}>
      <div
        className={cn(
          "absolute inset-x-0 top-0 h-[3px] bg-gradient-to-r opacity-80",
          officeTheme?.accentLine ?? "from-transparent via-sky-500 to-transparent"
        )}
      />
      <div
        className={cn(
          "absolute inset-x-0 top-0 h-24 bg-gradient-to-b pointer-events-none",
          officeTheme?.headerGlow ?? "from-sky-500/[0.06] to-transparent"
        )}
      />

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
                <div
                  className={cn(
                    "absolute -inset-1 rounded-2xl blur-md opacity-70",
                    officeTheme
                      ? officeTheme.iconBg.replace(/text-\S+/g, "").trim()
                      : "bg-gradient-to-br from-sky-400/30 to-blue-600/20"
                  )}
                />
                <div
                  className={cn(
                    "relative w-14 h-14 rounded-2xl flex items-center justify-center shadow-lg ring-1",
                    officeTheme ? cn(officeTheme.iconBg, officeTheme.iconRing) : "ring-white/50 dark:ring-white/10 bg-gradient-to-br from-sky-500/15 to-blue-600/10 text-sky-600 dark:text-sky-400",
                    iconClassName
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
