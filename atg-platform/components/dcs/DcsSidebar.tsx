"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { useEffect, useRef, useState } from "react";
import {
  Briefcase,
  Calculator,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  CreditCard,
  FilePlus,
  FileSignature,
  FileText,
  Globe2,
  Inbox,
  LayoutDashboard,
  MapPin,
  Megaphone,
  PanelLeftClose,
  PanelLeftOpen,
  ScrollText,
  Send,
  Sparkles,
  Truck,
  Users,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { CONTRACTS_MENU, OFFICE_TYPES, PROCUREMENT_TYPES } from "@/lib/dcs";
import type { LucideIcon } from "lucide-react";

const STORAGE_KEY = "dcs-sidebar-collapsed";

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
  accounting: Calculator,
  "supply-section": Truck,
};

const CONTRACTS_ICONS: Record<string, LucideIcon> = {
  local: MapPin,
  international: Globe2,
  payment: CreditCard,
};

function NavLink({
  href,
  label,
  icon: Icon,
  indent,
  collapsed,
}: {
  href: string;
  label: string;
  icon: LucideIcon;
  indent?: boolean;
  collapsed?: boolean;
}) {
  const pathname = usePathname();
  const locale = useLocale();
  const full = `/${locale}${href}`;
  const active = pathname === full || pathname.startsWith(`${full}/`);

  return (
    <Link
      href={full}
      title={collapsed ? label : undefined}
      className={cn(
        "group relative flex items-center rounded-xl text-[13px] font-medium transition-all duration-200",
        collapsed ? "justify-center h-11 w-11 mx-auto" : "gap-3 px-3 h-10",
        !collapsed && indent && "pl-5",
        active
          ? "bg-gradient-to-r from-white/[0.14] to-white/[0.06] text-white shadow-[inset_0_1px_0_rgba(255,255,255,0.12),0_4px_16px_-4px_rgba(0,0,0,0.4)]"
          : "text-white/55 hover:text-white/90 hover:bg-white/[0.06]",
      )}
    >
      {active && !collapsed && (
        <span className="absolute left-0 top-1/2 -translate-y-1/2 w-[3px] h-6 rounded-r-full bg-gradient-to-b from-sky-300 to-blue-500 shadow-[0_0_12px_rgba(56,189,248,0.6)]" />
      )}
      {active && collapsed && (
        <span className="absolute left-0 top-1/2 -translate-y-1/2 w-[3px] h-5 rounded-r-full bg-gradient-to-b from-sky-300 to-blue-500" />
      )}
      <span
        className={cn(
          "flex shrink-0 items-center justify-center rounded-lg transition-all duration-200",
          collapsed ? "h-9 w-9" : "h-8 w-8",
          active
            ? "bg-gradient-to-br from-sky-400/30 to-blue-600/20 ring-1 ring-white/20"
            : "bg-white/[0.04] group-hover:bg-white/[0.08]",
        )}
      >
        <Icon
          size={16}
          className={cn(
            "shrink-0 transition-colors",
            active ? "text-sky-200" : "text-white/45 group-hover:text-white/75",
          )}
        />
      </span>
      {!collapsed && <span className="truncate">{label}</span>}
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
  childrenAfter,
  collapsed,
}: {
  title: string;
  slugs: { slug: string }[];
  icons: Record<string, LucideIcon>;
  basePath: string;
  labelPrefix: string;
  defaultOpen: boolean;
  childrenAfter?: React.ReactNode;
  collapsed?: boolean;
}) {
  const pathname = usePathname();
  const t = useTranslations("dcs");
  const [open, setOpen] = useState(defaultOpen);

  useEffect(() => {
    const inGroup =
      slugs.some((s) => pathname.includes(`${basePath}/${s.slug}`)) ||
      pathname.includes(`${basePath}/contracts`);
    if (inGroup) setOpen(true);
  }, [pathname, slugs, basePath]);

  if (collapsed) {
    return (
      <div className="mb-2 space-y-0.5">
        <div className="mx-3 my-2 h-px bg-white/10" />
        {slugs.map(({ slug }) => (
          <NavLink
            key={slug}
            href={`${basePath}/${slug}`}
            label={t(`${labelPrefix}.${slug}`)}
            icon={icons[slug] ?? FileText}
            collapsed
          />
        ))}
        {childrenAfter}
      </div>
    );
  }

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
          {childrenAfter}
        </div>
      )}
    </div>
  );
}

function ContractsNavGroup({ collapsed }: { collapsed?: boolean }) {
  const pathname = usePathname();
  const t = useTranslations("dcs");
  const [open, setOpen] = useState(false);
  const [flyoutOpen, setFlyoutOpen] = useState(false);
  const rootRef = useRef<HTMLDivElement>(null);
  const isContractsActive = pathname.includes("/automation/procurement/contracts");

  useEffect(() => {
    if (pathname.includes("/automation/procurement/contracts")) setOpen(true);
  }, [pathname]);

  useEffect(() => {
    if (!flyoutOpen) return;
    const onPointerDown = (e: MouseEvent) => {
      if (!rootRef.current?.contains(e.target as Node)) setFlyoutOpen(false);
    };
    document.addEventListener("mousedown", onPointerDown);
    return () => document.removeEventListener("mousedown", onPointerDown);
  }, [flyoutOpen]);

  if (collapsed) {
    return (
      <div ref={rootRef} className="relative">
        <button
          type="button"
          title={t("types.contracts")}
          onClick={() => setFlyoutOpen((v) => !v)}
          className={cn(
            "group relative flex h-11 w-11 mx-auto items-center justify-center rounded-xl transition-all duration-200",
            isContractsActive
              ? "bg-gradient-to-r from-white/[0.14] to-white/[0.06] text-white"
              : "text-white/55 hover:text-white/90 hover:bg-white/[0.06]",
          )}
        >
          {isContractsActive && (
            <span className="absolute left-0 top-1/2 -translate-y-1/2 w-[3px] h-5 rounded-r-full bg-gradient-to-b from-sky-300 to-blue-500" />
          )}
          <span
            className={cn(
              "flex h-9 w-9 items-center justify-center rounded-lg transition-all duration-200",
              isContractsActive
                ? "bg-gradient-to-br from-sky-400/30 to-blue-600/20 ring-1 ring-white/20"
                : "bg-white/[0.04] group-hover:bg-white/[0.08]",
            )}
          >
            <FileSignature
              size={16}
              className={cn(
                isContractsActive ? "text-sky-200" : "text-white/45 group-hover:text-white/75",
              )}
            />
          </span>
        </button>
        {flyoutOpen && (
          <div className="absolute left-full top-0 z-50 ml-2 min-w-[200px] rounded-xl border border-white/10 bg-[#0f1a2e] p-1.5 shadow-2xl shadow-black/40">
            <p className="px-3 py-2 text-[10px] font-bold uppercase tracking-[0.14em] text-white/40">
              {t("types.contracts")}
            </p>
            {CONTRACTS_MENU.map(({ slug }) => (
              <NavLink
                key={slug}
                href={`/automation/procurement/contracts/${slug}`}
                label={t(`contractsMenu.${slug}`)}
                icon={CONTRACTS_ICONS[slug] ?? FileText}
              />
            ))}
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="mt-0.5">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        className={cn(
          "group relative flex w-full items-center gap-3 rounded-xl px-3 h-10 text-[13px] font-medium transition-all duration-200",
          isContractsActive
            ? "bg-gradient-to-r from-white/[0.10] to-white/[0.04] text-white"
            : "text-white/55 hover:text-white/90 hover:bg-white/[0.06]",
        )}
      >
        <span
          className={cn(
            "flex h-8 w-8 shrink-0 items-center justify-center rounded-lg transition-all duration-200",
            isContractsActive
              ? "bg-gradient-to-br from-sky-400/30 to-blue-600/20 ring-1 ring-white/20"
              : "bg-white/[0.04] group-hover:bg-white/[0.08]",
          )}
        >
          <FileSignature
            size={16}
            className={cn(
              isContractsActive ? "text-sky-200" : "text-white/45 group-hover:text-white/75",
            )}
          />
        </span>
        <span className="flex-1 truncate text-left">{t("types.contracts")}</span>
        {open ? <ChevronDown size={14} className="opacity-50" /> : <ChevronRight size={14} className="opacity-50" />}
      </button>
      {open && (
        <div className="mt-0.5 space-y-0.5 border-l border-white/10 ml-7 pl-1">
          {CONTRACTS_MENU.map(({ slug }) => (
            <NavLink
              key={slug}
              href={`/automation/procurement/contracts/${slug}`}
              label={t(`contractsMenu.${slug}`)}
              icon={CONTRACTS_ICONS[slug] ?? FileText}
              indent
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
  const [collapsed, setCollapsed] = useState(false);
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
    try {
      setCollapsed(localStorage.getItem(STORAGE_KEY) === "1");
    } catch {
      /* ignore */
    }
  }, []);

  const toggleCollapsed = () => {
    setCollapsed((prev) => {
      const next = !prev;
      try {
        localStorage.setItem(STORAGE_KEY, next ? "1" : "0");
      } catch {
        /* ignore */
      }
      return next;
    });
  };

  const beforeContracts = PROCUREMENT_TYPES.filter((s) =>
    ["requests", "marketing"].includes(s.slug),
  );
  const afterContracts = PROCUREMENT_TYPES.filter(
    (s) => !["requests", "marketing"].includes(s.slug),
  );

  return (
    <aside
      className={cn(
        "relative shrink-0 flex flex-col min-h-[calc(100vh-3.5rem)] text-white overflow-hidden transition-[width] duration-300 ease-in-out",
        collapsed ? "w-[76px]" : "w-[272px]",
      )}
    >
      <div className="absolute inset-0 bg-[#0b1220]" />
      <div className="absolute inset-0 bg-gradient-to-b from-[#132238] via-[#0f1a2e] to-[#080d16]" />
      <div className="absolute inset-0 bg-[radial-gradient(ellipse_120%_80%_at_0%_0%,rgba(56,189,248,0.15),transparent_50%)]" />
      <div className="absolute inset-0 bg-[radial-gradient(ellipse_80%_50%_at_100%_100%,rgba(59,130,246,0.1),transparent_50%)]" />
      <div className="absolute inset-0 opacity-[0.03] bg-[url('data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSI0IiBoZWlnaHQ9IjQiPgo8cmVjdCB3aWR0aD0iNCIgaGVpZ2h0PSI0IiBmaWxsPSIjZmZmIi8+Cjwvc3ZnPg==')]" />
      <div className="absolute right-0 top-0 bottom-0 w-px bg-gradient-to-b from-transparent via-white/10 to-transparent" />

      <button
        type="button"
        onClick={toggleCollapsed}
        aria-label={collapsed ? t("nav.expandSidebar") : t("nav.collapseSidebar")}
        title={collapsed ? t("nav.expandSidebar") : t("nav.collapseSidebar")}
        className={cn(
          "absolute z-20 -right-3 top-[4.5rem] flex h-6 w-6 items-center justify-center rounded-full",
          "border border-white/15 bg-[#132238] text-white/70 shadow-lg shadow-black/30",
          "hover:bg-[#1a3050] hover:text-white hover:border-white/25 transition-all duration-200",
        )}
      >
        {collapsed ? <ChevronRight size={14} /> : <ChevronLeft size={14} />}
      </button>

      <div
        className={cn(
          "relative border-b border-white/[0.06] transition-all duration-300",
          collapsed ? "px-3 py-5" : "px-5 py-6",
        )}
      >
        <div className={cn("flex items-center", collapsed ? "justify-center" : "gap-3.5")}>
          <div className="relative shrink-0">
            <div className="absolute -inset-1 rounded-2xl bg-gradient-to-br from-sky-400 to-blue-600 opacity-40 blur-md" />
            <div
              className={cn(
                "relative rounded-xl bg-gradient-to-br from-sky-400 via-blue-500 to-indigo-600 flex items-center justify-center shadow-xl ring-1 ring-white/25",
                collapsed ? "w-10 h-10" : "w-11 h-11",
              )}
            >
              <Briefcase size={collapsed ? 18 : 20} className="text-white drop-shadow-sm" />
            </div>
          </div>
          {!collapsed && (
            <div className="min-w-0 flex-1">
              <div className="flex items-center gap-1.5">
                <p className="font-bold text-[15px] leading-tight tracking-tight text-white">{t("brand")}</p>
                <Sparkles size={12} className="text-sky-300/80" />
              </div>
              <p className="text-[9px] text-white/40 uppercase tracking-[0.2em] mt-1.5 truncate font-medium">
                {t("brandSub")}
              </p>
            </div>
          )}
        </div>
      </div>

      <nav className={cn("relative flex-1 overflow-y-auto scrollbar-thin", collapsed ? "px-2 py-3" : "px-3 py-4")}>
        <NavGroup
          title={t("sections.documentOffice")}
          slugs={OFFICE_TYPES}
          icons={OFFICE_ICONS}
          basePath="/automation/office"
          labelPrefix="types"
          defaultOpen
          collapsed={mounted && collapsed}
        />
        <NavGroup
          title={t("sections.procurement")}
          slugs={beforeContracts}
          icons={PROCUREMENT_ICONS}
          basePath="/automation/procurement"
          labelPrefix="types"
          defaultOpen
          collapsed={mounted && collapsed}
          childrenAfter={
            <>
              <ContractsNavGroup collapsed={mounted && collapsed} />
              {afterContracts.map(({ slug }) => (
                <NavLink
                  key={slug}
                  href={`/automation/procurement/${slug}`}
                  label={t(`types.${slug}`)}
                  icon={PROCUREMENT_ICONS[slug] ?? FileText}
                  collapsed={mounted && collapsed}
                />
              ))}
            </>
          }
        />
      </nav>

      <div
        className={cn(
          "relative border-t border-white/[0.06] bg-black/20 backdrop-blur-sm",
          collapsed ? "p-2" : "p-4",
        )}
      >
        {collapsed ? (
          <Link
            href={`/${locale}/home`}
            title={t("nav.backHome")}
            className="flex h-11 w-11 mx-auto items-center justify-center rounded-xl text-white/45 hover:text-white/85 hover:bg-white/[0.06] transition-all duration-200"
          >
            <LayoutDashboard size={18} />
          </Link>
        ) : (
          <Link
            href={`/${locale}/home`}
            className="flex items-center gap-2.5 text-xs text-white/45 hover:text-white/85 px-3 py-2.5 rounded-xl hover:bg-white/[0.06] transition-all duration-200"
          >
            <LayoutDashboard size={15} />
            {t("nav.backHome")}
          </Link>
        )}

        <button
          type="button"
          onClick={toggleCollapsed}
          className={cn(
            "mt-2 flex items-center rounded-xl text-white/40 hover:text-white/75 hover:bg-white/[0.06] transition-all duration-200",
            collapsed ? "h-9 w-11 mx-auto justify-center" : "gap-2.5 w-full px-3 py-2 text-xs",
          )}
        >
          {collapsed ? <PanelLeftOpen size={16} /> : <PanelLeftClose size={15} />}
          {!collapsed && <span>{t("nav.collapseSidebar")}</span>}
        </button>
      </div>
    </aside>
  );
}
