"use client";

import { useLocale, useTranslations } from "next-intl";
import {
  Building2,
  Calculator,
  Languages,
  Monitor,
  Plane,
  Truck,
  UserCog,
  Wrench,
  type LucideIcon,
} from "lucide-react";
import {
  HelpDeskCategoryRouting,
  HelpDeskOrgRouting,
  HelpDeskStaff,
  TicketCategory,
  categoryLabel,
  deptLabel,
} from "@/lib/helpdesk";
import { cn } from "@/lib/utils";

const CATEGORY_ORDER: TicketCategory[] = [
  "IT",
  "Administration",
  "Accountant",
  "Translator",
  "TravelTickets",
  "Transport",
];

const CATEGORY_ICONS: Record<TicketCategory, LucideIcon> = {
  IT: Monitor,
  Administration: Building2,
  Accountant: Calculator,
  Transport: Truck,
  TravelTickets: Plane,
  Translator: Languages,
};

const COLOR_MAP: Record<string, string> = {
  "atg-blue": "border-atg-blue/30 bg-atg-blue/5",
  "atg-teal": "border-atg-teal/30 bg-atg-teal/5",
  emerald: "border-emerald-500/30 bg-emerald-500/5",
  "atg-purple": "border-atg-purple/30 bg-atg-purple/5",
  "atg-amber": "border-amber-500/30 bg-amber-500/5",
  cyan: "border-cyan-500/30 bg-cyan-500/5",
};

function jobTitle(staff: HelpDeskStaff, locale: string) {
  const title = locale.startsWith("en") ? staff.jobTitleEn : staff.jobTitleRu;
  return title ?? staff.role;
}

function StaffList({
  items,
  empty,
  variant,
}: {
  items: HelpDeskStaff[];
  empty: string;
  variant: "assigner" | "engineer";
}) {
  const locale = useLocale();

  if (items.length === 0) {
    return <p className="text-xs text-foreground/40 italic py-2">{empty}</p>;
  }

  return (
    <ul className="space-y-2">
      {items.map((person) => (
        <li
          key={person.id}
          className={cn(
            "rounded-lg border px-3 py-2.5 text-sm",
            variant === "assigner"
              ? "border-violet-500/20 bg-violet-500/5"
              : "border-atg-teal/20 bg-atg-teal/5"
          )}
        >
          <div className="font-medium leading-tight">{person.fullName}</div>
          <div className="text-xs text-foreground/50 mt-0.5">{jobTitle(person, locale)}</div>
          <div className="text-xs text-foreground/40 mt-0.5 font-mono">{person.email}</div>
        </li>
      ))}
    </ul>
  );
}

function OrgRouteCard({ route }: { route: HelpDeskOrgRouting }) {
  const t = useTranslations("helpdesk.admin");
  const locale = useLocale();

  return (
    <div className="rounded-xl border border-border bg-surface/50 p-4 space-y-4">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div>
          <div className="text-xs font-semibold uppercase tracking-wider text-foreground/45">
            {route.organizationCode}
          </div>
          <div className="font-semibold text-sm mt-0.5">
            {deptLabel(route.departmentName, route.departmentNameEn, locale)}
          </div>
          <div className="text-xs text-foreground/40 font-mono mt-0.5">{route.departmentCode}</div>
        </div>
        <div className="flex gap-2 text-xs">
          <span className="rounded-md bg-slate-500/10 px-2 py-1 text-foreground/60">
            {t("openCount", { count: route.openTickets })}
          </span>
          <span className="rounded-md bg-atg-blue/10 px-2 py-1 text-atg-blue">
            {t("activeCount", { count: route.activeTickets })}
          </span>
        </div>
      </div>

      <div className="grid md:grid-cols-2 gap-4">
        <div>
          <div className="flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wider text-violet-600 dark:text-violet-400 mb-2">
            <UserCog size={14} />
            {t("assigners")}
          </div>
          <StaffList items={route.assigners} empty={t("noAssigners")} variant="assigner" />
        </div>
        <div>
          <div className="flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wider text-atg-teal mb-2">
            <Wrench size={14} />
            {t("engineers")}
          </div>
          <StaffList items={route.engineers} empty={t("noEngineers")} variant="engineer" />
        </div>
      </div>
    </div>
  );
}

function CategoryPanel({ category }: { category: HelpDeskCategoryRouting }) {
  const locale = useLocale();
  const Icon = CATEGORY_ICONS[category.category] ?? Monitor;
  const colorClass = COLOR_MAP[category.color] ?? "border-border bg-surface/30";

  return (
    <section className={cn("rounded-2xl border p-5 space-y-4", colorClass)}>
      <div className="flex items-center gap-3">
        <div className="w-10 h-10 rounded-xl bg-background/60 border border-border/50 flex items-center justify-center">
          <Icon size={20} className="text-foreground/70" />
        </div>
        <div>
          <h2 className="text-lg font-bold">{categoryLabel(category, locale)}</h2>
          <p className="text-xs text-foreground/45 font-mono">{category.category}</p>
        </div>
      </div>

      <div className="grid gap-3 lg:grid-cols-2">
        {category.routes.map((route) => (
          <OrgRouteCard key={`${category.category}-${route.organizationCode}`} route={route} />
        ))}
      </div>
    </section>
  );
}

interface HelpDeskCategoryControlProps {
  categories: HelpDeskCategoryRouting[];
}

export function HelpDeskCategoryControl({ categories }: HelpDeskCategoryControlProps) {
  const t = useTranslations("helpdesk.admin");

  const ordered = CATEGORY_ORDER.map((key) => categories.find((c) => c.category === key)).filter(
    Boolean
  ) as HelpDeskCategoryRouting[];

  return (
    <div className="space-y-4">
      <div>
        <h2 className="font-semibold text-sm">{t("routingTitle")}</h2>
        <p className="text-xs text-foreground/50 mt-0.5">{t("routingSubtitle")}</p>
      </div>
      {ordered.map((cat) => (
        <CategoryPanel key={cat.category} category={cat} />
      ))}
    </div>
  );
}
