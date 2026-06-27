"use client";

import Link from "next/link";
import { useLocale } from "next-intl";
import { ChevronRight, Headset } from "lucide-react";
import { cn } from "@/lib/utils";

interface HelpdeskPageHeaderProps {
  title: string;
  subtitle?: string;
  actions?: React.ReactNode;
  breadcrumb?: string;
  className?: string;
}

export function HelpdeskPageHeader({
  title,
  subtitle,
  actions,
  breadcrumb,
  className,
}: HelpdeskPageHeaderProps) {
  const locale = useLocale();

  return (
    <header
      className={cn(
        "shrink-0 border-b border-border/80 bg-surface/80 backdrop-blur-sm px-6 py-4",
        className
      )}
    >
      <div className="flex items-center gap-1.5 text-xs text-foreground/45 mb-2">
        <Link
          href={`/${locale}/helpdesk/board`}
          className="inline-flex items-center gap-1 hover:text-atg-teal transition-colors"
        >
          <Headset size={12} />
          <span>HelpDesk</span>
        </Link>
        {breadcrumb && (
          <>
            <ChevronRight size={12} className="opacity-50" />
            <span className="text-foreground/60">{breadcrumb}</span>
          </>
        )}
      </div>
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-xl font-semibold tracking-tight text-foreground">{title}</h1>
          {subtitle && (
            <p className="text-sm text-foreground/55 mt-1 max-w-2xl">{subtitle}</p>
          )}
        </div>
        {actions && <div className="flex items-center gap-2 shrink-0">{actions}</div>}
      </div>
    </header>
  );
}
