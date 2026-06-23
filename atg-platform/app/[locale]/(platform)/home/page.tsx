"use client";

import { useTranslations } from "next-intl";
import { useLocale } from "next-intl";
import { ModuleCard } from "@/components/layout/ModuleCard";
import { useAuthStore } from "@/store/authStore";
import { isAdminRole } from "@/lib/utils";
import { Briefcase, Headset, Users, Kanban, Settings } from "lucide-react";

export default function HomePage() {
  const t = useTranslations("home");
  const tNav = useTranslations("nav");
  const locale = useLocale();
  const user = useAuthStore((s) => s.user);

  const modules = [
    { href: `/${locale}/automation`, icon: Briefcase, title: tNav("automation"), desc: t("automationDesc"), color: "#2563eb" },
    { href: `/${locale}/helpdesk`, icon: Headset, title: tNav("helpdesk"), desc: t("helpdeskDesc"), color: "#0d9488" },
    { href: `/${locale}/hr`, icon: Users, title: tNav("hr"), desc: t("hrDesc"), color: "#7c3aed" },
    { href: `/${locale}/tasks`, icon: Kanban, title: tNav("tasks"), desc: t("tasksDesc"), color: "#d97706" },
  ];

  const allModules = isAdminRole(user?.role)
    ? [
        {
          href: `/${locale}/admin`,
          icon: Settings,
          title: tNav("admin"),
          desc: locale === "ru" ? "Управление пользователями и организациями" : "Manage users and organizations",
          color: "#dc2626",
        },
        ...modules,
      ]
    : modules;

  return (
    <div className="p-6 max-w-5xl mx-auto">
      <h1 className="text-2xl font-semibold mb-6">{t("title")}</h1>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        {allModules.map((m) => (
          <ModuleCard key={m.href} href={m.href} icon={m.icon} title={m.title} description={m.desc} color={m.color} />
        ))}
      </div>
    </div>
  );
}
