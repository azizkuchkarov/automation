"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { Briefcase, Inbox, LayoutDashboard, List, Plus, Users } from "lucide-react";
import { hrTheme } from "@/components/hr/hrTheme";
import { cn } from "@/lib/utils";

const links = [
  { href: "/hr", icon: List, key: "mine", exact: true },
  { href: "/hr/queue", icon: Inbox, key: "queue" },
  { href: "/hr/business-trip", icon: Briefcase, key: "businessTrip" },
  { href: "/hr/business-trip/queue", icon: Inbox, key: "businessTripQueue" },
];

export function HrSidebar() {
  const pathname = usePathname();
  const locale = useLocale();
  const t = useTranslations("hr");

  return (
    <aside className="w-[248px] shrink-0 border-r border-slate-200/80 bg-white/90 backdrop-blur-xl flex flex-col min-h-[calc(100vh-3.5rem)]">
      <div className="px-4 py-5 border-b border-slate-200/70">
        <div className="flex items-center gap-3">
          <div
            className={cn(
              "w-10 h-10 rounded-xl flex items-center justify-center text-white",
              hrTheme.iconTile,
            )}
          >
            <Users size={18} />
          </div>
          <div>
            <p className="font-semibold text-sm text-slate-900 leading-tight">{t("title")}</p>
            <p className="text-[10px] text-slate-400 uppercase tracking-widest mt-0.5">
              {t("subtitle")}
            </p>
          </div>
        </div>
      </div>

      <nav className="flex-1 p-3 space-y-1">
        <p className={cn("px-3 py-1.5", hrTheme.sectionLabel)}>{t("nav.section")}</p>
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
                "flex items-center gap-2.5 rounded-xl px-3 h-10 text-[13px] font-medium transition-all",
                active
                  ? cn(hrTheme.activeNav, "-ml-px pl-[11px] shadow-sm")
                  : "text-slate-500 hover:bg-slate-50 hover:text-slate-800 border-l-2 border-transparent",
              )}
            >
              <Icon size={16} className={active ? "text-blue-700" : "opacity-70"} />
              {t(`nav.${key}`)}
            </Link>
          );
        })}
      </nav>

      <div className="p-3 space-y-2 border-t border-slate-200/70">
        <Link
          href={`/${locale}/hr/leave/new`}
          className={cn(
            "flex items-center justify-center gap-2 w-full h-10 rounded-xl text-sm font-semibold transition-all",
            hrTheme.primaryBtn,
          )}
        >
          <Plus size={16} />
          {t("nav.createLeave")}
        </Link>
        <Link
          href={`/${locale}/hr/business-trip/new`}
          className={cn(
            "flex items-center justify-center gap-2 w-full h-10 rounded-xl text-sm font-semibold transition-colors",
            hrTheme.secondaryBtn,
          )}
        >
          <Briefcase size={16} />
          {t("nav.createBusinessTrip")}
        </Link>
        <Link
          href={`/${locale}/home`}
          className="flex items-center gap-2 text-xs text-slate-400 hover:text-slate-700 px-2 py-2 rounded-lg hover:bg-slate-50 transition-colors"
        >
          <LayoutDashboard size={14} />
          {t("nav.backHome")}
        </Link>
      </div>
    </aside>
  );
}
