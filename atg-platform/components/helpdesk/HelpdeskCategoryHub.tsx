"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import {
  ArrowUpRight,
  Building2,
  Calculator,
  Languages,
  Monitor,
  Plane,
  Truck,
} from "lucide-react";
import {
  TICKET_CATEGORIES,
  categoryIconColor,
  categoryLabel,
  categoryPath,
  categorySlug,
  countActiveTickets,
  type HelpDeskCategory,
  type TicketCategory,
} from "@/lib/helpdesk";
import { fetchHelpDeskBoard, fetchHelpDeskCategories } from "@/lib/helpdeskApi";
import { cn } from "@/lib/utils";

const ICONS: Record<string, typeof Monitor> = {
  Monitor,
  Building2,
  Calculator,
  Truck,
  Plane,
  Languages,
};

const CATEGORY_DESC_KEY: Record<TicketCategory, string> = {
  IT: "it",
  Administration: "administration",
  Accountant: "accountant",
  Transport: "transport",
  TravelTickets: "travel",
  Translator: "translator",
};

export function HelpdeskCategoryHub() {
  const t = useTranslations("helpdesk");
  const locale = useLocale();
  const [categories, setCategories] = useState<HelpDeskCategory[]>([]);
  const [counts, setCounts] = useState<Partial<Record<TicketCategory, number>>>({});

  useEffect(() => {
    fetchHelpDeskCategories().then(setCategories).catch(() => setCategories([]));
  }, []);

  useEffect(() => {
    const cats = categories.length > 0 ? categories.map((c) => c.category) : TICKET_CATEGORIES;
    Promise.all(
      cats.map(async (cat) => {
        try {
          const board = await fetchHelpDeskBoard(cat);
          return [cat, countActiveTickets(board)] as const;
        } catch {
          return [cat, 0] as const;
        }
      }),
    ).then((rows) => setCounts(Object.fromEntries(rows)));
  }, [categories]);

  const list: HelpDeskCategory[] =
    categories.length > 0
      ? categories
      : TICKET_CATEGORIES.map((category) => ({
          category,
          nameEn: category,
          nameRu: category,
          icon: "Monitor",
          color: "atg-blue",
        }));

  return (
    <div className="flex-1 overflow-auto">
      <div className="border-b border-border/60 bg-surface/80 px-6 py-8">
        <p className="text-xs font-semibold uppercase tracking-wider text-atg-teal">{t("hub.badge")}</p>
        <h1 className="mt-2 text-2xl font-semibold tracking-tight text-foreground">{t("hub.title")}</h1>
        <p className="mt-2 max-w-2xl text-sm leading-relaxed text-foreground/55">{t("hub.subtitle")}</p>
      </div>

      <div className="grid gap-4 p-6 sm:grid-cols-2 xl:grid-cols-3">
        {list.map((c) => {
          const cat = c.category;
          const Icon = ICONS[c.icon] ?? Monitor;
          const active = counts[cat] ?? 0;
          const href = categoryPath(locale, cat, "board");

          return (
            <Link
              key={cat}
              href={href}
              className="group relative flex flex-col overflow-hidden rounded-2xl border border-border/70 bg-surface p-5 shadow-sm transition-all hover:-translate-y-0.5 hover:border-atg-teal/30 hover:shadow-lg hover:shadow-slate-900/5"
            >
              {active > 0 && (
                <span className="absolute top-4 right-4 flex h-[22px] min-w-[22px] items-center justify-center rounded-full bg-red-500 px-1.5 text-[11px] font-bold text-white ring-2 ring-surface">
                  {active > 99 ? "99+" : active}
                </span>
              )}

              <div className="flex items-start justify-between gap-3">
                <div className={cn("rounded-xl p-3", categoryIconColor(cat))}>
                  <Icon size={24} />
                </div>
                <div className="flex h-8 w-8 items-center justify-center rounded-full border border-border/60 text-foreground/35 opacity-0 transition-all group-hover:opacity-100">
                  <ArrowUpRight size={15} />
                </div>
              </div>

              <h2 className="mt-4 text-lg font-semibold tracking-tight text-foreground">
                {categoryLabel(c, locale)}
              </h2>
              <p className="mt-2 text-sm leading-relaxed text-foreground/55">
                {t(`hub.categories.${CATEGORY_DESC_KEY[cat]}`)}
              </p>

              <div className="mt-5 flex flex-wrap gap-2 text-[11px] font-medium">
                <span className="rounded-full bg-foreground/[0.04] px-2.5 py-1 text-foreground/50">
                  {t("hub.activeCount", { count: active })}
                </span>
              </div>
            </Link>
          );
        })}
      </div>
    </div>
  );
}
