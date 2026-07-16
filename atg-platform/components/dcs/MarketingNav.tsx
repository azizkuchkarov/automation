"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { BarChart3, Columns3, Crown, List } from "lucide-react";
import { cn } from "@/lib/utils";
import type { LucideIcon } from "lucide-react";

const LINKS: { href: string; key: string; icon: LucideIcon }[] = [
  { href: "/automation/procurement/marketing", key: "queue", icon: List },
  { href: "/automation/procurement/marketing/board", key: "board", icon: Columns3 },
  { href: "/automation/procurement/marketing/dashboard", key: "dashboard", icon: BarChart3 },
  { href: "/automation/procurement/marketing/leadership", key: "leadership", icon: Crown },
];

export function MarketingNav() {
  const t = useTranslations("dcs.marketing.nav");
  const locale = useLocale();
  const pathname = usePathname();

  return (
    <nav className="shrink-0 border-b border-slate-200/60 bg-white/80 px-6 py-3 backdrop-blur-sm dark:border-white/[0.06] dark:bg-surface/80">
      <div className="flex flex-wrap gap-1.5">
        {LINKS.map(({ href, key, icon: Icon }) => {
          const full = `/${locale}${href}`;
          const active =
            pathname === full ||
            pathname === `${full}/` ||
            (href !== "/automation/procurement/marketing" && pathname.startsWith(full));

          return (
            <Link
              key={key}
              href={full}
              className={cn(
                "inline-flex items-center gap-2 rounded-xl px-3.5 py-2 text-[13px] font-medium transition-all",
                active
                  ? "bg-pink-500/10 text-pink-700 ring-1 ring-pink-500/20 dark:text-pink-300"
                  : "text-foreground/55 hover:bg-foreground/[0.04] hover:text-foreground"
              )}
            >
              <Icon size={16} className="shrink-0 opacity-70" />
              {t(key)}
            </Link>
          );
        })}
      </div>
    </nav>
  );
}
