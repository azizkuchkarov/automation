"use client";

import { useLocale, useTranslations } from "next-intl";
import {
  Building2,
  Calculator,
  FilePlus,
  ClipboardList,
  CreditCard,
  FileSignature,
  FileText,
  Inbox,
  Megaphone,
  Package,
  ScrollText,
  Send,
  Truck,
  UserCheck,
  UserCog,
  Users,
  Wrench,
  type LucideIcon,
} from "lucide-react";
import {
  DcsCategoryRouting,
  DcsOrgRouting,
  DcsStaff,
  DocumentType,
  deptLabel,
  typeLabel,
} from "@/lib/dcs";
import { cn } from "@/lib/utils";

const OFFICE_ORDER: DocumentType[] = [
  "Incoming",
  "Outgoing",
  "Memo",
  "MinutesOfMeeting",
  "Order",
];

const PROCUREMENT_ORDER: DocumentType[] = [
  "ProcurementRequest",
  "TechnicalAssignment",
  "MaterialServiceRequisition",
  "Marketing",
  "Contract",
  "Payment",
  "Accounting",
  "SupplySection",
];

const TYPE_ICONS: Record<DocumentType, LucideIcon> = {
  Incoming: Inbox,
  Outgoing: Send,
  Memo: FileText,
  MinutesOfMeeting: Users,
  Order: ScrollText,
  ProcurementRequest: FilePlus,
  TechnicalAssignment: ClipboardList,
  MaterialServiceRequisition: Package,
  Marketing: Megaphone,
  Contract: FileSignature,
  Payment: CreditCard,
  Accounting: Calculator,
  SupplySection: Truck,
};

const COLOR_MAP: Record<string, string> = {
  "atg-blue": "border-atg-blue/30 bg-atg-blue/5",
  "atg-teal": "border-atg-teal/30 bg-atg-teal/5",
  violet: "border-violet-500/30 bg-violet-500/5",
  indigo: "border-indigo-500/30 bg-indigo-500/5",
  orange: "border-orange-500/30 bg-orange-500/5",
  teal: "border-teal-500/30 bg-teal-500/5",
  pink: "border-pink-500/30 bg-pink-500/5",
  emerald: "border-emerald-500/30 bg-emerald-500/5",
  amber: "border-amber-500/30 bg-amber-500/5",
  slate: "border-slate-500/30 bg-slate-500/5",
  purple: "border-purple-500/30 bg-purple-500/5",
  sky: "border-sky-500/30 bg-sky-500/5",
};

function jobTitle(staff: DcsStaff, locale: string) {
  const title = locale.startsWith("en") ? staff.jobTitleEn : staff.jobTitleRu;
  return title ?? staff.role;
}

function StaffList({
  items,
  empty,
  variant,
}: {
  items: DcsStaff[];
  empty: string;
  variant: "assigner" | "handler" | "registrar";
}) {
  const locale = useLocale();

  if (items.length === 0) {
    return <p className="text-xs text-foreground/40 italic py-2">{empty}</p>;
  }

  const styles = {
    assigner: "border-violet-500/20 bg-violet-500/5",
    handler: "border-atg-teal/20 bg-atg-teal/5",
    registrar: "border-sky-500/30 bg-sky-500/8 ring-1 ring-sky-500/15",
  };

  return (
    <ul className="space-y-2">
      {items.map((person) => (
        <li
          key={person.id}
          className={cn("rounded-lg border px-3 py-2.5 text-sm", styles[variant])}
        >
          <div className="font-medium leading-tight">{person.fullName}</div>
          <div className="text-xs text-foreground/50 mt-0.5">{jobTitle(person, locale)}</div>
          <div className="text-xs text-foreground/40 mt-0.5 font-mono">{person.email}</div>
        </li>
      ))}
    </ul>
  );
}

function OrgRouteCard({ route }: { route: DcsOrgRouting }) {
  const t = useTranslations("dcs.admin");
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
            {t("draftCount", { count: route.draftCount })}
          </span>
          <span className="rounded-md bg-atg-blue/10 px-2 py-1 text-atg-blue">
            {t("activeCount", { count: route.activeCount })}
          </span>
        </div>
      </div>

      {route.designatedRegistrar && (
        <div>
          <div className="flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wider text-sky-600 dark:text-sky-400 mb-2">
            <UserCheck size={14} />
            {t("registrar")}
          </div>
          <StaffList
            items={[route.designatedRegistrar]}
            empty=""
            variant="registrar"
          />
        </div>
      )}

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
            {t("handlers")}
          </div>
          <StaffList items={route.handlers} empty={t("noHandlers")} variant="handler" />
        </div>
      </div>
    </div>
  );
}

function SectionGroup({
  title,
  order,
  categories,
}: {
  title: string;
  order: DocumentType[];
  categories: DcsCategoryRouting[];
}) {
  const ordered = order
    .map((type) => categories.find((c) => c.type === type))
    .filter(Boolean) as DcsCategoryRouting[];

  if (ordered.length === 0) return null;

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2">
        <Building2 size={16} className="text-foreground/40" />
        <h2 className="text-sm font-bold uppercase tracking-wider text-foreground/50">{title}</h2>
      </div>
      {ordered.map((cat) => (
        <CategoryPanel key={cat.type} category={cat} />
      ))}
    </div>
  );
}

function CategoryPanel({ category }: { category: DcsCategoryRouting }) {
  const locale = useLocale();
  const Icon = TYPE_ICONS[category.type] ?? FileText;
  const colorClass = COLOR_MAP[category.color] ?? "border-border bg-surface/30";

  return (
    <section className={cn("rounded-2xl border p-5 space-y-4", colorClass)}>
      <div className="flex items-center gap-3">
        <div className="w-10 h-10 rounded-xl bg-background/60 border border-border/50 flex items-center justify-center">
          <Icon size={20} className="text-foreground/70" />
        </div>
        <div>
          <h3 className="text-lg font-bold">{typeLabel(category, locale)}</h3>
          <p className="text-xs text-foreground/45 font-mono">{category.type}</p>
        </div>
      </div>
      <div className="grid gap-3 lg:grid-cols-2">
        {category.routes.map((route) => (
          <OrgRouteCard key={`${category.type}-${route.organizationCode}`} route={route} />
        ))}
      </div>
    </section>
  );
}

interface DcsCategoryControlProps {
  categories: DcsCategoryRouting[];
}

export function DcsCategoryControl({ categories }: DcsCategoryControlProps) {
  const t = useTranslations("dcs.admin");

  return (
    <div className="space-y-8">
      <div>
        <h2 className="font-semibold text-sm">{t("routingTitle")}</h2>
        <p className="text-xs text-foreground/50 mt-0.5">{t("routingSubtitle")}</p>
      </div>
      <SectionGroup
        title={t("documentOffice")}
        order={OFFICE_ORDER}
        categories={categories}
      />
      <SectionGroup
        title={t("procurement")}
        order={PROCUREMENT_ORDER}
        categories={categories}
      />
    </div>
  );
}
