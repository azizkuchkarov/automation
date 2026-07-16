import Link from "next/link";
import { ArrowUpRight, LucideIcon } from "lucide-react";
import { cn } from "@/lib/utils";

export interface ModuleCardAccent {
  gradient: string;
  iconWrap: string;
  icon: string;
  glow: string;
  border: string;
}

interface ModuleCardProps {
  href: string;
  icon: LucideIcon;
  title: string;
  description: string;
  accent: ModuleCardAccent;
  featured?: boolean;
  badgeCount?: number;
}

export function ModuleCard({
  href,
  icon: Icon,
  title,
  description,
  accent,
  featured = false,
  badgeCount = 0,
}: ModuleCardProps) {
  return (
    <Link
      href={href}
      className={cn(
        "group relative flex flex-col overflow-hidden rounded-2xl border bg-surface/90 p-6 shadow-sm backdrop-blur-sm",
        "transition-all duration-300 ease-out",
        "hover:-translate-y-1 hover:shadow-xl hover:shadow-slate-900/5",
        accent.border,
        featured ? "sm:col-span-2 lg:col-span-1" : "",
      )}
    >
      {badgeCount > 0 && (
        <span
          className="absolute top-4 right-4 z-20 flex h-[22px] min-w-[22px] items-center justify-center rounded-full bg-red-500 px-1.5 text-[11px] font-bold text-white shadow-md ring-2 ring-surface"
          aria-label={`${badgeCount} new`}
        >
          {badgeCount > 99 ? "99+" : badgeCount}
        </span>
      )}
      <div
        className={cn(
          "pointer-events-none absolute inset-0 bg-gradient-to-br opacity-0 transition-opacity duration-300 group-hover:opacity-100",
          accent.gradient,
        )}
      />
      <div
        className={cn(
          "pointer-events-none absolute -right-8 -top-8 h-32 w-32 rounded-full blur-2xl opacity-0 transition-opacity duration-300 group-hover:opacity-60",
          accent.glow,
        )}
      />

      <div className="relative flex items-start justify-between gap-4">
        <div className={cn("rounded-xl p-3.5 ring-1 ring-inset ring-black/5", accent.iconWrap)}>
          <Icon size={28} className={cn("transition-transform duration-300 group-hover:scale-110", accent.icon)} />
        </div>
        <div
          className={cn(
            "flex h-9 w-9 items-center justify-center rounded-full border border-border/60 bg-background/80 text-foreground/40",
            "opacity-0 transition-all duration-300 group-hover:opacity-100 group-hover:translate-x-0.5 group-hover:-translate-y-0.5",
          )}
        >
          <ArrowUpRight size={16} />
        </div>
      </div>

      <div className="relative mt-5 flex flex-1 flex-col">
        <h3 className="text-lg font-semibold tracking-tight text-foreground">{title}</h3>
        <p className="mt-2 text-sm leading-relaxed text-foreground/55">{description}</p>
      </div>

      <div
        className={cn(
          "relative mt-6 h-0.5 w-10 rounded-full opacity-60 transition-all duration-300 group-hover:w-16",
          accent.glow,
        )}
      />
    </Link>
  );
}
