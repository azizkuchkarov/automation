"use client";

import { useCallback, useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { ModuleCard, ModuleCardAccent } from "@/components/layout/ModuleCard";
import { HomeHero } from "@/components/home/HomeHero";
import { useAuthStore } from "@/store/authStore";
import { isAdminRole } from "@/lib/utils";
import { fetchHomeModuleCounts, HomeModuleCounts } from "@/lib/platform";
import { Briefcase, Cpu, Headset, Kanban, Settings, Users } from "lucide-react";

const POLL_MS = 60_000;

const accents = {
  admin: {
    gradient: "from-rose-500/10 via-rose-500/5 to-transparent",
    iconWrap: "bg-rose-500/10",
    icon: "text-rose-600",
    glow: "bg-rose-500",
    border: "border-rose-500/15 hover:border-rose-500/30",
  },
  automation: {
    gradient: "from-blue-500/10 via-blue-500/5 to-transparent",
    iconWrap: "bg-blue-500/10",
    icon: "text-blue-600",
    glow: "bg-blue-600",
    border: "border-blue-500/15 hover:border-blue-500/30",
  },
  itAutomation: {
    gradient: "from-cyan-500/10 via-indigo-500/5 to-transparent",
    iconWrap: "bg-cyan-500/10",
    icon: "text-cyan-600",
    glow: "bg-cyan-500",
    border: "border-cyan-500/15 hover:border-cyan-500/30",
  },
  helpdesk: {
    gradient: "from-teal-500/10 via-teal-500/5 to-transparent",
    iconWrap: "bg-teal-500/10",
    icon: "text-teal-600",
    glow: "bg-teal-500",
    border: "border-teal-500/15 hover:border-teal-500/30",
  },
  hr: {
    gradient: "from-violet-500/10 via-violet-500/5 to-transparent",
    iconWrap: "bg-violet-500/10",
    icon: "text-violet-600",
    glow: "bg-violet-500",
    border: "border-violet-500/15 hover:border-violet-500/30",
  },
  tasks: {
    gradient: "from-amber-500/10 via-amber-500/5 to-transparent",
    iconWrap: "bg-amber-500/10",
    icon: "text-amber-600",
    glow: "bg-amber-500",
    border: "border-amber-500/15 hover:border-amber-500/30",
  },
} satisfies Record<string, ModuleCardAccent>;

export default function HomePage() {
  const t = useTranslations("home");
  const tNav = useTranslations("nav");
  const locale = useLocale();
  const user = useAuthStore((s) => s.user);
  const [counts, setCounts] = useState<HomeModuleCounts | null>(null);

  const loadCounts = useCallback(async () => {
    try {
      setCounts(await fetchHomeModuleCounts());
    } catch {
      setCounts({ admin: 0, automation: 0, itAutomation: 0, helpDesk: 0, hr: 0, tasks: 0 });
    }
  }, []);

  useEffect(() => {
    loadCounts();
    const id = setInterval(loadCounts, POLL_MS);
    return () => clearInterval(id);
  }, [loadCounts]);

  const modules = [
    {
      key: "automation" as const,
      href: `/${locale}/automation`,
      icon: Briefcase,
      title: tNav("automation"),
      desc: t("automationDesc"),
      accent: accents.automation,
    },
    {
      key: "itAutomation" as const,
      href: `/${locale}/it-automation`,
      icon: Cpu,
      title: tNav("itAutomation"),
      desc: t("itAutomationDesc"),
      accent: accents.itAutomation,
    },
    {
      key: "helpdesk" as const,
      href: `/${locale}/helpdesk`,
      icon: Headset,
      title: tNav("helpdesk"),
      desc: t("helpdeskDesc"),
      accent: accents.helpdesk,
    },
    {
      key: "hr" as const,
      href: `/${locale}/hr`,
      icon: Users,
      title: tNav("hr"),
      desc: t("hrDesc"),
      accent: accents.hr,
    },
    {
      key: "tasks" as const,
      href: `/${locale}/tasks`,
      icon: Kanban,
      title: tNav("tasks"),
      desc: t("tasksDesc"),
      accent: accents.tasks,
    },
  ];

  const allModules = isAdminRole(user?.role)
    ? [
        {
          key: "admin" as const,
          href: `/${locale}/admin`,
          icon: Settings,
          title: tNav("admin"),
          desc: t("adminDesc"),
          accent: accents.admin,
          featured: true,
        },
        ...modules,
      ]
    : modules;

  const countFor = (key: (typeof allModules)[number]["key"]) => {
    if (!counts) return 0;
    const map = {
      admin: "admin",
      automation: "automation",
      itAutomation: "itAutomation",
      helpdesk: "helpDesk",
      hr: "hr",
      tasks: "tasks",
    } as const;
    return counts[map[key]] ?? 0;
  };

  return (
    <div className="relative min-h-full">
      <div className="pointer-events-none absolute inset-0 overflow-hidden">
        <div className="absolute -top-32 right-0 h-80 w-80 rounded-full bg-atg-blue/10 blur-3xl" />
        <div className="absolute top-64 -left-24 h-72 w-72 rounded-full bg-violet-500/10 blur-3xl" />
        <div className="absolute bottom-0 right-1/3 h-64 w-64 rounded-full bg-teal-500/8 blur-3xl" />
      </div>

      <div className="relative mx-auto max-w-6xl px-4 py-6 sm:px-6 sm:py-8 lg:py-10">
        <div className="space-y-8">
          <HomeHero />

          <section className="space-y-4">
            <div className="flex items-end justify-between gap-4 px-1">
              <div>
                <h2 className="text-xl font-semibold tracking-tight text-foreground">{t("modulesTitle")}</h2>
                <p className="mt-1 text-sm text-foreground/55">{t("modulesSubtitle")}</p>
              </div>
              <p className="hidden text-xs font-medium uppercase tracking-wider text-foreground/35 sm:block">
                {allModules.length} {t("modulesCount")}
              </p>
            </div>

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
              {allModules.map((m) => (
                <ModuleCard
                  key={m.key}
                  href={m.href}
                  icon={m.icon}
                  title={m.title}
                  description={m.desc}
                  accent={m.accent}
                  featured={"featured" in m ? m.featured : false}
                  badgeCount={countFor(m.key)}
                />
              ))}
            </div>
          </section>
        </div>
      </div>
    </div>
  );
}
