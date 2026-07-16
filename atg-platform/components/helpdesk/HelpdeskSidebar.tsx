"use client";

import Link from "next/link";
import { usePathname, useSearchParams } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import {
  Building2,
  Calculator,
  Headset,
  Inbox,
  Kanban,
  Languages,
  LayoutDashboard,
  List,
  Monitor,
  Plane,
  Plus,
  Truck,
} from "lucide-react";
import { cn } from "@/lib/utils";
import {
  categoryLabel,
  categorySlug,
  categoryFromSlug,
  TICKET_CATEGORIES,
  type HelpDeskCategory,
} from "@/lib/helpdesk";
import { useEffect, useState } from "react";
import { fetchHelpDeskCategories } from "@/lib/helpdeskApi";

const ICONS: Record<string, typeof Monitor> = {
  Monitor,
  Building2,
  Calculator,
  Truck,
  Plane,
  Languages,
};

function resolveCategorySlug(pathname: string, locale: string): string | null {
  const prefix = `/${locale}/helpdesk/`;
  if (!pathname.startsWith(prefix)) return null;
  const rest = pathname.slice(prefix.length);
  const slug = rest.split("/")[0];
  if (!slug || slug === "tickets") return null;
  return categoryFromSlug(slug) ? slug : null;
}

export function HelpdeskSidebar() {
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const locale = useLocale();
  const t = useTranslations("helpdesk");
  const [categories, setCategories] = useState<HelpDeskCategory[]>([]);
  const activeSlug = resolveCategorySlug(pathname, locale);
  const activeCategory = activeSlug ? categoryFromSlug(activeSlug) : null;
  const hubActive = pathname === `/${locale}/helpdesk` || pathname === `/${locale}/helpdesk/`;

  useEffect(() => {
    fetchHelpDeskCategories().then(setCategories).catch(() => setCategories([]));
  }, []);

  const categoryLinks = (categories.length > 0
    ? categories.map((c) => c.category)
    : TICKET_CATEGORIES
  ).map((cat) => {
    const meta = categories.find((c) => c.category === cat);
    const slug = categorySlug(cat);
    return { cat, slug, meta };
  });

  const sectionLinks = activeSlug
    ? [
        { href: `/helpdesk/${activeSlug}/board`, icon: Kanban, key: "board" as const },
        { href: `/helpdesk/${activeSlug}/tickets`, icon: List, key: "tickets" as const },
        { href: `/helpdesk/${activeSlug}/tickets?view=queue`, icon: Inbox, key: "queue" as const },
      ]
    : [];

  return (
    <aside className="flex min-h-[calc(100vh-3.5rem)] w-[252px] shrink-0 flex-col border-r border-border/70 bg-surface">
      <div className="border-b border-border/60 px-4 py-5">
        <Link href={`/${locale}/helpdesk`} className="flex items-center gap-3 group">
          <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-gradient-to-br from-atg-teal to-atg-blue shadow-md shadow-atg-teal/20">
            <Headset size={18} className="text-white" />
          </div>
          <div>
            <p className="text-sm font-semibold leading-tight text-foreground group-hover:text-atg-teal transition-colors">
              HelpDesk
            </p>
            <p className="mt-0.5 text-[10px] uppercase tracking-widest text-foreground/45">ATG Service Desk</p>
          </div>
        </Link>
      </div>

      <nav className="flex-1 space-y-4 overflow-y-auto p-3">
        <div className="space-y-1">
          <p className="px-3 py-1.5 text-[10px] font-bold uppercase tracking-widest text-foreground/35">
            {t("nav.section")}
          </p>
          <Link
            href={`/${locale}/helpdesk`}
            className={cn(
              "flex items-center gap-2.5 rounded-lg px-3 h-9 text-[13px] font-medium transition-all",
              hubActive
                ? "bg-atg-teal/12 text-atg-teal border-l-2 border-atg-teal -ml-px pl-[11px]"
                : "text-foreground/60 hover:bg-foreground/[0.04] hover:text-foreground border-l-2 border-transparent",
            )}
          >
            <LayoutDashboard size={16} />
            {t("nav.hub")}
          </Link>
        </div>

        {activeSlug && activeCategory && (
          <div className="space-y-1">
            <p className="px-3 py-1.5 text-[10px] font-bold uppercase tracking-widest text-foreground/35">
              {t("nav.workspace")}
            </p>
            {sectionLinks.map(({ href, icon: Icon, key }) => {
              const full = `/${locale}${href}`;
              const active =
                key === "queue"
                  ? pathname.includes(`/helpdesk/${activeSlug}/tickets`) &&
                    searchParams.get("view") === "queue"
                  : pathname === full || pathname.startsWith(`${full}/`);

              return (
                <Link
                  key={href}
                  href={full}
                  className={cn(
                    "flex items-center gap-2.5 rounded-lg px-3 h-9 text-[13px] font-medium transition-all",
                    active
                      ? "bg-atg-teal/12 text-atg-teal border-l-2 border-atg-teal -ml-px pl-[11px]"
                      : "text-foreground/60 hover:bg-foreground/[0.04] hover:text-foreground border-l-2 border-transparent",
                  )}
                >
                  <Icon size={16} />
                  {t(`nav.${key}`)}
                </Link>
              );
            })}
          </div>
        )}

        <div className="space-y-1">
          <p className="px-3 py-1.5 text-[10px] font-bold uppercase tracking-widest text-foreground/35">
            {t("nav.directions")}
          </p>
          {categoryLinks.map(({ cat, slug, meta }) => {
            const full = `/${locale}/helpdesk/${slug}/board`;
            const active = activeSlug === slug;
            const Icon = ICONS[meta?.icon ?? "Monitor"] ?? Monitor;
            const label = meta ? categoryLabel(meta, locale) : cat;

            return (
              <Link
                key={cat}
                href={full}
                className={cn(
                  "flex items-center gap-2.5 rounded-lg px-3 min-h-9 py-2 text-[13px] font-medium transition-all",
                  active
                    ? "bg-atg-blue/10 text-atg-blue border-l-2 border-atg-blue -ml-px pl-[11px]"
                    : "text-foreground/60 hover:bg-foreground/[0.04] hover:text-foreground border-l-2 border-transparent",
                )}
              >
                <Icon size={15} className={active ? "text-atg-blue" : "opacity-70"} />
                <span className="leading-snug">{label}</span>
              </Link>
            );
          })}
        </div>
      </nav>

      <div className="space-y-2 border-t border-border/60 p-3">
        <Link
          href={
            activeSlug
              ? `/${locale}/helpdesk/${activeSlug}/new`
              : `/${locale}/helpdesk/tickets/new`
          }
          className="flex h-9 w-full items-center justify-center gap-2 rounded-lg bg-atg-blue text-sm font-medium text-white shadow-sm transition-colors hover:bg-blue-600"
        >
          <Plus size={16} />
          {activeCategory ? t("nav.createIn", { category: categories.find((c) => c.category === activeCategory) ? categoryLabel(categories.find((c) => c.category === activeCategory)!, locale) : activeCategory }) : t("nav.create")}
        </Link>
        <Link
          href={`/${locale}/home`}
          className="flex items-center gap-2 rounded-md px-2 py-1.5 text-xs text-foreground/40 transition-colors hover:bg-foreground/[0.03] hover:text-foreground/70"
        >
          <LayoutDashboard size={14} />
          {t("nav.backHome")}
        </Link>
      </div>
    </aside>
  );
}
