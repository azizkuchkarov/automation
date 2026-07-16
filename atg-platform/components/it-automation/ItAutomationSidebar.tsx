"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import {
  Building2,
  Cpu,
  FileKey2,
  HardDrive,
  Home,
  Phone,
  Server,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { itAutomationTheme } from "@/components/it-automation/itAutomationTheme";

const nav = [
  { href: "/it-automation", icon: Home, key: "hub", exact: true },
  { href: "/it-automation/licenses", icon: FileKey2, key: "licenses", exact: false },
  { href: "/it-automation/services", icon: Server, key: "services", exact: false },
  { href: "/it-automation/mobile-services", icon: Phone, key: "mobile", exact: false },
  { href: "/it-automation/government-services", icon: Building2, key: "government", exact: false },
  { href: "/it-automation/equipment", icon: HardDrive, key: "equipment", exact: false },
] as const;

export function ItAutomationSidebar() {
  const t = useTranslations("itAutomation");
  const tNav = useTranslations("nav");
  const locale = useLocale();
  const pathname = usePathname();

  return (
    <aside className="flex w-[252px] shrink-0 flex-col border-r border-border/70 bg-surface">
      <div className="flex items-center gap-3 border-b border-border/60 px-4 py-4">
        <div className={cn("rounded-xl p-2.5 text-white", itAutomationTheme.iconTile)}>
          <Cpu size={18} />
        </div>
        <div className="min-w-0">
          <p className="truncate text-sm font-semibold tracking-tight text-foreground">{t("title")}</p>
          <p className="truncate text-[11px] text-foreground/45">{t("subtitle")}</p>
        </div>
      </div>

      <nav className="flex-1 space-y-1 p-3">
        {nav.map(({ href, icon: Icon, key, exact }) => {
          const full = `/${locale}${href}`;
          const active = exact ? pathname === full : pathname.startsWith(full);
          return (
            <Link
              key={href}
              href={full}
              className={cn(
                "flex items-center gap-2.5 rounded-xl px-3 py-2.5 text-sm font-medium transition-colors",
                active
                  ? "bg-cyan-500/10 text-cyan-800 border-l-2 border-cyan-600 dark:text-cyan-300"
                  : "text-foreground/65 hover:bg-foreground/5",
              )}
            >
              <Icon size={16} />
              {t(`nav.${key}`)}
            </Link>
          );
        })}
      </nav>

      <div className="border-t border-border/60 p-3">
        <Link
          href={`/${locale}/home`}
          className="block rounded-xl px-3 py-2 text-xs font-medium text-foreground/50 transition-colors hover:bg-foreground/5 hover:text-foreground"
        >
          {tNav("backToPlatform")}
        </Link>
      </div>
    </aside>
  );
}
