"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { useEffect, useState } from "react";
import {
  Briefcase,
  Calculator,
  ChevronDown,
  ChevronRight,
  CreditCard,
  FilePlus,
  FileSignature,
  FileText,
  Inbox,
  LayoutDashboard,
  Megaphone,
  ScrollText,
  Send,
  Sparkles,
  Truck,
  Users,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { OFFICE_TYPES, PROCUREMENT_TYPES } from "@/lib/dcs";
import type { LucideIcon } from "lucide-react";

const OFFICE_ICONS: Record<string, LucideIcon> = {
  incoming: Inbox,
  outgoing: Send,
  memo: FileText,
  minutes: Users,
  orders: ScrollText,
};

const PROCUREMENT_ICONS: Record<string, LucideIcon> = {
  requests: FilePlus,
  marketing: Megaphone,
  contracts: FileSignature,
  payment: CreditCard,
  accounting: Calculator,
  "supply-section": Truck,
};

function NavLink({
  href,
  label,
  icon: Icon,
}: {
  href: string;
  label: string;
  icon: LucideIcon;
}) {
  const pathname = usePathname();
  const locale = useLocale();
  const full = `/${locale}${href}`;
  const active = pathname === full || pathname.startsWith(`${full}/`);

  return (
    <Link
      href={full}
      className={cn(
        "group relative flex items-center gap-3 rounded-xl px-3 h-10 text-[13px] font-medium transition-all duration-200",
        active
          ? "bg-gradient-to-r from-white/[0.14] to-white/[0.06] text-white shadow-[inset_0_1px_0_rgba(255,255,255,0.12),0_4px_16px_-4px_rgba(0,0,0,0.4)]"
          : "text-white/55 hover:text-white/90 hover:bg-white/[0.06]"
      )}
    >
      {active && (
        <span className="absolute left-0 top-1/2 -translate-y-1/2 w-[3px] h-6 rounded-r-full bg-gradient-to-b from-sky-300 to-blue-500 shadow-[0_0_12px_rgba(56,189,248,0.6)]" />
      )}
      <span
        className={cn(
          "flex h-8 w-8 shrink-0 items-center justify-center rounded-lg transition-all duration-200",
          active
            ? "bg-gradient-to-br from-sky-400/30 to-blue-600/20 ring-1 ring-white/20"
            : "bg-white/[0.04] group-hover:bg-white/[0.08]"
        )}
      >
        <Icon
          size={16}
          className={cn(
            "shrink-0 transition-colors",
            active ? "text-sky-200" : "text-white/45 group-hover:text-white/75"
          )}
        />
      </span>
      <span className="truncate">{label}</span>
    </Link>
  );
}

function NavGroup({
  title,
  slugs,
  icons,
  basePath,
  labelPrefix,
  defaultOpen,
}: {
  title: string;
  slugs: { slug: string }[];
  icons: Record<string, LucideIcon>;
  basePath: string;
  labelPrefix: string;
  defaultOpen: boolean;
}) {
  const pathname = usePathname();
  const t = useTranslations("dcs");
  const [open, setOpen] = useState(defaultOpen);

  useEffect(() => {
    const inGroup = slugs.some((s) => pathname.includes(`${basePath}/${s.slug}`));
    if (inGroup) setOpen(true);
  }, [pathname, slugs, basePath]);

  return (
    <div className="mb-3">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        className="flex items-center gap-2 w-full px-3 py-2 text-[10px] font-bold uppercase tracking-[0.16em] text-white/35 hover:text-white/60 transition-colors"
      >
        {open ? <ChevronDown size={12} /> : <ChevronRight size={12} />}
        <span className="flex-1 text-left">{title}</span>
        <span className="h-px flex-1 max-w-[40px] bg-gradient-to-r from-white/20 to-transparent" />
      </button>
      {open && (
        <div className="space-y-0.5 mt-1">
          {slugs.map(({ slug }) => (
            <NavLink
              key={slug}
              href={`${basePath}/${slug}`}
              label={t(`${labelPrefix}.${slug}`)}
              icon={icons[slug] ?? FileText}
            />
          ))}
        </div>
      )}
    </div>
  );
}

export function DcsSidebar() {
  const locale = useLocale();
  const t = useTranslations("dcs");

  return (
    <aside className="relative w-[272px] shrink-0 flex flex-col min-h-[calc(100vh-3.5rem)] text-white overflow-hidden">
      {/* Deep premium backdrop */}
      <div className="absolute inset-0 bg-[#0b1220]" />
      <div className="absolute inset-0 bg-gradient-to-b from-[#132238] via-[#0f1a2e] to-[#080d16]" />
      <div className="absolute inset-0 bg-[radial-gradient(ellipse_120%_80%_at_0%_0%,rgba(56,189,248,0.15),transparent_50%)]" />
      <div className="absolute inset-0 bg-[radial-gradient(ellipse_80%_50%_at_100%_100%,rgba(59,130,246,0.1),transparent_50%)]" />
      <div className="absolute inset-0 opacity-[0.03] bg-[url('data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSI0IiBoZWlnaHQ9IjQiPgo8cmVjdCB3aWR0aD0iNCIgaGVpZ2h0PSI0IiBmaWxsPSIjZmZmIi8+Cjwvc3ZnPg==')]" />
      <div className="absolute right-0 top-0 bottom-0 w-px bg-gradient-to-b from-transparent via-white/10 to-transparent" />

      <div className="relative px-5 py-6 border-b border-white/[0.06]">
        <div className="flex items-center gap-3.5">
          <div className="relative">
            <div className="absolute -inset-1 rounded-2xl bg-gradient-to-br from-sky-400 to-blue-600 opacity-40 blur-md" />
            <div className="relative w-11 h-11 rounded-xl bg-gradient-to-br from-sky-400 via-blue-500 to-indigo-600 flex items-center justify-center shadow-xl ring-1 ring-white/25">
              <Briefcase size={20} className="text-white drop-shadow-sm" />
            </div>
          </div>
          <div className="min-w-0 flex-1">
            <div className="flex items-center gap-1.5">
              <p className="font-bold text-[15px] leading-tight tracking-tight text-white">{t("brand")}</p>
              <Sparkles size={12} className="text-sky-300/80" />
            </div>
            <p className="text-[9px] text-white/40 uppercase tracking-[0.2em] mt-1.5 truncate font-medium">
              {t("brandSub")}
            </p>
          </div>
        </div>
      </div>

      <nav className="relative flex-1 px-3 py-4 overflow-y-auto scrollbar-thin">
        <NavGroup
          title={t("sections.documentOffice")}
          slugs={OFFICE_TYPES}
          icons={OFFICE_ICONS}
          basePath="/automation/office"
          labelPrefix="types"
          defaultOpen
        />
        <NavGroup
          title={t("sections.procurement")}
          slugs={PROCUREMENT_TYPES}
          icons={PROCUREMENT_ICONS}
          basePath="/automation/procurement"
          labelPrefix="types"
          defaultOpen
        />
      </nav>

      <div className="relative p-4 border-t border-white/[0.06] bg-black/20 backdrop-blur-sm">
        <Link
          href={`/${locale}/home`}
          className="flex items-center gap-2.5 text-xs text-white/45 hover:text-white/85 px-3 py-2.5 rounded-xl hover:bg-white/[0.06] transition-all duration-200"
        >
          <LayoutDashboard size={15} />
          {t("nav.backHome")}
        </Link>
      </div>
    </aside>
  );
}
