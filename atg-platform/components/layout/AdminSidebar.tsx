"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { LayoutDashboard, Users, Building2, FolderTree, Briefcase, ScrollText, GitBranch, Headset, FileStack, FileSignature, Cpu, Plane } from "lucide-react";
import { cn } from "@/lib/utils";

const items = [
  { href: "/admin", icon: LayoutDashboard, key: "dashboard", exact: true },
  { href: "/admin/users", icon: Users, key: "users" },
  { href: "/admin/organizations", icon: Building2, key: "organizations" },
  { href: "/admin/hierarchy", icon: GitBranch, key: "hierarchy" },
  { href: "/admin/departments", icon: FolderTree, key: "departments" },
  { href: "/admin/positions", icon: Briefcase, key: "positions" },
  { href: "/admin/procurement-roles", icon: FileSignature, key: "procurementRolesNav" },
  { href: "/admin/hr-business-trip-workflow", icon: Plane, key: "hrBusinessTripWorkflowNav" },
  { href: "/admin/it-automation-roles", icon: Cpu, key: "itAutomationRolesNav" },
  { href: "/admin/audit", icon: ScrollText, key: "audit" },
  { href: "/admin/helpdesk", icon: Headset, key: "helpdeskControl" },
  { href: "/admin/dcs", icon: FileStack, key: "dcsControl" },
];

export function AdminSidebar() {
  const pathname = usePathname();
  const locale = useLocale();
  const t = useTranslations("admin");
  const tNav = useTranslations("nav");

  return (
    <aside className="w-56 border-r border-border bg-surface min-h-[calc(100vh-3.5rem)] p-3">
      <nav className="space-y-1">
        {items.map(({ href, icon: Icon, key, exact }) => {
          const full = `/${locale}${href}`;
          const active = exact ? pathname === full : pathname.startsWith(full);
          return (
            <Link
              key={href}
              href={full}
              className={cn(
                "flex items-center gap-2 rounded-md px-3 h-[34px] text-sm transition-colors",
                active ? "bg-atg-blue/20 text-atg-blue" : "hover:bg-border/30"
              )}
            >
              <Icon size={16} />
              {t(key as "dashboard")}
            </Link>
          );
        })}
        <Link href={`/${locale}/home`} className="flex items-center gap-2 rounded-md px-3 h-[34px] text-sm text-foreground/60 hover:bg-border/30 mt-4">
          {tNav("backToPlatform")}
        </Link>
      </nav>
    </aside>
  );
}
