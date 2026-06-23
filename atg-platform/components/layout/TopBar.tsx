"use client";

import { useAuthStore } from "@/store/authStore";
import { isAdminRole } from "@/lib/utils";
import { Badge } from "@/components/ui/Badge";
import { LanguageToggle } from "@/components/ui/LanguageToggle";
import { ThemeToggle } from "@/components/ui/ThemeToggle";
import { logout } from "@/lib/auth";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import Link from "next/link";
import { Settings } from "lucide-react";

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
    <header className="h-14 border-b border-border bg-surface flex items-center justify-between px-4">
      <div className="flex items-center gap-3">
        <div className="w-8 h-8 rounded bg-atg-blue flex items-center justify-center text-white font-bold text-sm">ATG</div>
        <span className="font-medium hidden sm:inline">ATG Unified Platform</span>
      </div>
      <div className="flex items-center gap-3">
        {user && (
          <div className="flex items-center gap-2 text-sm">
            <span>{user.fullName}</span>
            <Badge className="bg-border/50">{user.role}</Badge>
            {isAdminRole(user.role) && (
              <Link
                href={`/${locale}/admin`}
                className="inline-flex items-center gap-1 rounded-md bg-atg-blue/20 text-atg-blue px-2.5 py-1 text-xs font-medium hover:bg-atg-blue/30"
              >
                <Settings size={14} />
                Admin
              </Link>
            )}
            <span className="text-xs text-foreground/60 hidden md:inline">{user.organizationName}</span>
          </div>
        )}
        <ThemeToggle />
        <LanguageToggle />
        <button onClick={handleLogout} className="text-sm text-red-400 hover:underline">{t("logout")}</button>
      </div>
    </header>
  );
}
