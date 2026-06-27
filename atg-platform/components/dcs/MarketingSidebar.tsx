"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { BarChart3, Columns3, Crown, List, Megaphone } from "lucide-react";
import { cn } from "@/lib/utils";
import type { LucideIcon } from "lucide-react";

const LINKS: { href: string; key: string; icon: LucideIcon }[] = [
  { href: "/automation/procurement/marketing", key: "queue", icon: List },
  { href: "/automation/procurement/marketing/board", key: "board", icon: Columns3 },
  { href: "/automation/procurement/marketing/dashboard", key: "dashboard", icon: BarChart3 },
  { href: "/automation/procurement/marketing/leadership", key: "leadership", icon: Crown },
];

export function MarketingSidebar() {
  const t = useTranslations("dcs.marketing.nav");
  const locale = useLocale();
  const pathname = usePathname();

  return (
    <aside className="w-56 shrink-0 border-r border-border/60 bg-surface/80 backdrop-blur-sm flex flex-col">
      <div className="px-4 py-5 border-b border-border/50">
        <div className="flex items-center gap-2.5">
          <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-pink-500/15 text-pink-600 dark:text-pink-400">
            <Megaphone size={18} />
          </span>
          <div>
            <p className="text-xs font-bold uppercase tracking-wider text-foreground/40">HO-MKT</p>
            <p className="text-sm font-semibold leading-tight">{t("section")}</p>
          </div>
        </div>
      </div>
      <nav className="p-3 space-y-1">
        {LINKS.map(({ href, key, icon: Icon }) => {
          const full = `/${locale}${href}`;
          const active = pathname === full || (href !== "/automation/procurement/marketing" && pathname.startsWith(full));
          const isQueue = href === "/automation/procurement/marketing";
          const queueActive = pathname === full || pathname === `${full}/`;
          return (
            <Link
              key={key}
              href={full}
              className={cn(
                "flex items-center gap-2.5 rounded-xl px-3 py-2.5 text-[13px] font-medium transition-all",
                (isQueue ? queueActive : active)
                  ? "bg-pink-500/10 text-pink-700 dark:text-pink-300 ring-1 ring-pink-500/20"
                  : "text-foreground/55 hover:text-foreground hover:bg-foreground/[0.04]"
              )}
            >
              <Icon size={16} className="shrink-0 opacity-70" />
              {t(key)}
            </Link>
          );
        })}
      </nav>
    </aside>
  );
}
