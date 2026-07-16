"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { Columns3, List } from "lucide-react";
import {
  ContractsProcurementSectionType,
  contractsSectionLabel,
} from "@/lib/procurementRequest";
import { cn } from "@/lib/utils";
import type { LucideIcon } from "lucide-react";

const LINKS: { suffix: string; key: string; icon: LucideIcon }[] = [
  { suffix: "", key: "queue", icon: List },
  { suffix: "/board", key: "board", icon: Columns3 },
];

export function ContractsSectionNav({ section }: { section: ContractsProcurementSectionType }) {
  const t = useTranslations("dcs.contractsQueue.nav");
  const locale = useLocale();
  const pathname = usePathname();
  const base =
    section === "Domestic"
      ? `/${locale}/automation/procurement/contracts/local`
      : `/${locale}/automation/procurement/contracts/international`;
  const accent =
    section === "Domestic"
      ? "bg-amber-500/10 text-amber-800 ring-amber-500/20 dark:text-amber-300"
      : "bg-sky-500/10 text-sky-800 ring-sky-500/20 dark:text-sky-300";

  return (
    <nav className="shrink-0 border-b border-slate-200/60 bg-white/80 px-6 py-3 backdrop-blur-sm dark:border-white/[0.06] dark:bg-surface/80">
      <div className="flex flex-wrap items-center gap-3">
        <span className={cn("rounded-lg px-2.5 py-1 text-[11px] font-bold uppercase tracking-wide ring-1", accent)}>
          {contractsSectionLabel(section, locale)}
        </span>
        <div className="flex flex-wrap gap-1.5">
          {LINKS.map(({ suffix, key, icon: Icon }) => {
            const full = `${base}${suffix}`;
            const active = pathname === full || pathname === `${full}/`;

            return (
              <Link
                key={key}
                href={full}
                className={cn(
                  "inline-flex items-center gap-2 rounded-xl px-3.5 py-2 text-[13px] font-medium transition-all",
                  active
                    ? cn("ring-1", accent)
                    : "text-foreground/55 hover:bg-foreground/[0.04] hover:text-foreground",
                )}
              >
                <Icon size={16} className="shrink-0 opacity-70" />
                {t(key)}
              </Link>
            );
          })}
        </div>
      </div>
    </nav>
  );
}
