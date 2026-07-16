"use client";

import Image from "next/image";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { LogOut, Settings } from "lucide-react";
import { useAuthStore } from "@/store/authStore";
import { cn, isAdminRole, localizedDepartmentName, localizedUserName } from "@/lib/utils";
import { Badge } from "@/components/ui/Badge";
import { LanguageToggle } from "@/components/ui/LanguageToggle";
import { logout } from "@/lib/auth";
import { NotificationBell } from "@/components/layout/NotificationBell";

function userInitials(user: { firstName: string; lastName: string; firstNameEn?: string; lastNameEn?: string }, locale: string) {
  const first = locale.startsWith("en") && user.firstNameEn ? user.firstNameEn : user.firstName;
  const last = locale.startsWith("en") && user.lastNameEn ? user.lastNameEn : user.lastName;
  return `${first.charAt(0)}${last.charAt(0)}`.toUpperCase();
}

export function TopBar() {
  const user = useAuthStore((s) => s.user);
  const router = useRouter();
  const locale = useLocale();
  const t = useTranslations("common");

  const handleLogout = async () => {
    await logout();
    router.push(`/${locale}/login`);
  };

  return (
    <header className="sticky top-0 z-50 border-b border-border/60 bg-surface/85 backdrop-blur-xl">
      <div className="mx-auto flex h-14 max-w-7xl items-center justify-between gap-4 px-4 sm:px-6">
        <Link href={`/${locale}/home`} className="flex min-w-0 items-center gap-3">
          <Image
            src="/atg-logo.png"
            alt="Asia Trans Gas"
            width={148}
            height={44}
            priority
            className="h-9 w-auto max-w-[148px] object-contain object-left"
          />
          <div className="hidden min-w-0 border-l border-border/60 pl-3 lg:block">
            <p className="truncate text-sm font-semibold tracking-tight text-foreground">ATG Unified Platform</p>
            <p className="truncate text-[11px] text-foreground/45">Enterprise workspace</p>
          </div>
        </Link>

        <div className="flex items-center gap-2 sm:gap-3">
          {user && (
            <div className="hidden items-center gap-2 rounded-full border border-border/60 bg-background/70 py-1 pl-1 pr-3 md:flex">
              <div className="flex h-8 w-8 items-center justify-center rounded-full bg-gradient-to-br from-[#1B4F8C] to-blue-700 text-xs font-semibold text-white shadow-sm">
                {userInitials(user, locale)}
              </div>
              <div className="max-w-[180px] text-left">
                <p className="truncate text-sm font-medium leading-tight text-foreground">
                  {localizedUserName(user, locale)}
                </p>
                {user.departmentName && (
                  <p className="truncate text-[11px] leading-tight text-foreground/45">
                    {localizedDepartmentName(user, locale)}
                  </p>
                )}
              </div>
            </div>
          )}

          {user && (
            <div className="hidden items-center gap-2 xl:flex">
              <Badge className="border border-border/60 bg-background/80 font-medium text-foreground/70">
                {user.role}
              </Badge>
              <span className="max-w-[160px] truncate text-xs text-foreground/50">{user.organizationName}</span>
            </div>
          )}

          {user && isAdminRole(user.role) && (
            <Link
              href={`/${locale}/admin`}
              className="hidden items-center gap-1.5 rounded-full border border-atg-blue/20 bg-atg-blue/8 px-3 py-1.5 text-xs font-medium text-atg-blue transition-colors hover:bg-atg-blue/12 sm:inline-flex"
            >
              <Settings size={14} />
              Admin
            </Link>
          )}

          <NotificationBell />

          <LanguageToggle />

          <button
            type="button"
            onClick={handleLogout}
            className={cn(
              "inline-flex items-center gap-1.5 rounded-full border border-red-500/15 bg-red-500/5 px-3 py-1.5",
              "text-xs font-medium text-red-600 transition-colors hover:bg-red-500/10",
            )}
          >
            <LogOut size={14} />
            <span className="hidden sm:inline">{t("logout")}</span>
          </button>
        </div>
      </div>
    </header>
  );
}
