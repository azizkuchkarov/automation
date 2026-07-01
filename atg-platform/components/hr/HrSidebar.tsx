"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { Inbox, LayoutDashboard, List, Plus, Users } from "lucide-react";
import { cn } from "@/lib/utils";

const links = [
  { href: "/hr", icon: List, key: "mine", exact: true },
  { href: "/hr/queue", icon: Inbox, key: "queue" },
];

export function HrSidebar() {
  const pathname = usePathname();
  const locale = useLocale();
  const t = useTranslations("hr");

  return (
    <aside className="w-[240px] shrink-0 border-r border-border bg-surface flex flex-col min-h-[calc(100vh-3.5rem)]">
      <div className="px-4 py-5 border-b border-border/60">
        <div className="flex items-center gap-3">
          <div className="w-9 h-9 rounded-lg bg-gradient-to-br from-violet-600 to-purple-700 flex items-center justify-center shadow-md shadow-violet-500/20">
            <Users size={18} className="text-white" />
          </div>
          <div>
            <p className="font-semibold text-sm text-foreground leading-tight">{t("title")}</p>
            <p className="text-[10px] text-foreground/45 uppercase tracking-widest mt-0.5">
              {t("subtitle")}
            </p>
          </div>
        </div>
      </div>

      <nav className="flex-1 p-3 space-y-1">
        <p className="px-3 py-1.5 text-[10px] font-bold uppercase tracking-widest text-foreground/35">
          {t("nav.section")}
        </p>
        {links.map(({ href, icon: Icon, key, exact }) => {
          const full = `/${locale}${href}`;
          const active = exact
            ? pathname === full
            : pathname === full || pathname.startsWith(`${full}/`);

          return (
            <Link
              key={href}
              href={full}
              className={cn(
                "flex items-center gap-2.5 rounded-lg px-3 h-9 text-[13px] font-medium transition-all",
                active
                  ? "bg-violet-500/12 text-violet-700 border-l-2 border-violet-600 -ml-px pl-[11px] shadow-sm"
                  : "text-foreground/60 hover:bg-foreground/[0.04] hover:text-foreground border-l-2 border-transparent"
              )}
            >
              <Icon size={16} className={active ? "text-violet-600" : "opacity-70"} />
              {t(`nav.${key}`)}
            </Link>
          );
        })}
      </nav>

      <div className="p-3 space-y-2 border-t border-border/60">
        <Link
          href={`/${locale}/hr/leave/new`}
          className="flex items-center justify-center gap-2 w-full h-9 rounded-lg bg-violet-600 text-white text-sm font-medium hover:bg-violet-700 transition-colors shadow-sm"
        >
          <Plus size={16} />
          {t("nav.createLeave")}
        </Link>
        <Link
          href={`/${locale}/home`}
          className="flex items-center gap-2 text-xs text-foreground/40 hover:text-foreground/70 px-2 py-1.5 rounded-md hover:bg-foreground/[0.03] transition-colors"
        >
          <LayoutDashboard size={14} />
          {t("nav.backHome")}
        </Link>
      </div>
    </aside>
  );
}
