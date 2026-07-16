"use client";

import { useAuthStore } from "@/store/authStore";
import { localizedDepartmentName, localizedJobTitle, localizedUserName } from "@/lib/utils";
import { Building2, Sparkles } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";

function getGreetingKey(): "greetingMorning" | "greetingAfternoon" | "greetingEvening" {
  const hour = new Date().getHours();
  if (hour < 12) return "greetingMorning";
  if (hour < 18) return "greetingAfternoon";
  return "greetingEvening";
}

export function HomeHero() {
  const t = useTranslations("home");
  const locale = useLocale();
  const user = useAuthStore((s) => s.user);

  const firstName =
    locale.startsWith("en") && user?.firstNameEn
      ? user.firstNameEn
      : user?.firstName ?? "";

  return (
    <section className="relative overflow-hidden rounded-3xl border border-border/60 bg-surface/80 p-6 shadow-sm backdrop-blur-sm sm:p-8 lg:p-10">
      <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(ellipse_at_top_right,_rgba(37,99,235,0.12),_transparent_55%)]" />
      <div className="pointer-events-none absolute -left-20 bottom-0 h-40 w-40 rounded-full bg-violet-500/10 blur-3xl" />

      <div className="relative flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between">
        <div className="max-w-2xl space-y-4">
          <div className="inline-flex items-center gap-2 rounded-full border border-atg-blue/15 bg-atg-blue/5 px-3 py-1 text-xs font-medium text-atg-blue">
            <Sparkles size={14} />
            {t("badge")}
          </div>

          <div className="space-y-2">
            <p className="text-sm font-medium text-foreground/50">
              {t(getGreetingKey())}
              {firstName ? `, ${firstName}` : ""}
            </p>
            <h1 className="text-3xl font-semibold tracking-tight text-foreground sm:text-4xl">
              {t("title")}
            </h1>
            <p className="max-w-xl text-base leading-relaxed text-foreground/60">{t("subtitle")}</p>
          </div>
        </div>

        {user && (
          <div className="grid gap-3 sm:grid-cols-2 lg:min-w-[320px]">
            <div className="rounded-2xl border border-border/60 bg-background/70 px-4 py-3">
              <p className="text-[11px] font-semibold uppercase tracking-wider text-foreground/40">
                {t("organization")}
              </p>
              <div className="mt-1.5 flex items-start gap-2">
                <Building2 size={16} className="mt-0.5 shrink-0 text-atg-blue" />
                <p className="text-sm font-medium leading-snug text-foreground">{user.organizationName}</p>
              </div>
            </div>
            <div className="rounded-2xl border border-border/60 bg-background/70 px-4 py-3">
              <p className="text-[11px] font-semibold uppercase tracking-wider text-foreground/40">
                {t("profile")}
              </p>
              <p className="mt-1.5 text-sm font-medium leading-snug text-foreground">
                {localizedUserName(user, locale)}
              </p>
              {(user.departmentName || localizedJobTitle(user, locale)) && (
                <p className="mt-1 text-xs leading-relaxed text-foreground/50">
                  {[localizedJobTitle(user, locale), localizedDepartmentName(user, locale)]
                    .filter(Boolean)
                    .join(" · ")}
                </p>
              )}
            </div>
          </div>
        )}
      </div>
    </section>
  );
}
