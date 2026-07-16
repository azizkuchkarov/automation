"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import {
  AlertTriangle,
  Building2,
  Cpu,
  FileKey2,
  HardDrive,
  Phone,
  Server,
  Shield,
} from "lucide-react";
import { CATEGORY_SLUGS, fetchItAutomationHub, type ItAssetCategory, type ItAutomationHub } from "@/lib/itAutomation";
import { cn } from "@/lib/utils";
import { itAutomationTheme } from "@/components/it-automation/itAutomationTheme";

const ICONS: Record<string, typeof FileKey2> = {
  License: FileKey2,
  Service: Server,
  MobileService: Phone,
  GovernmentService: Building2,
  Equipment: HardDrive,
};

const ACCENTS: Record<string, string> = {
  License: "from-cyan-600 to-sky-500",
  Service: "from-indigo-600 to-violet-500",
  MobileService: "from-emerald-600 to-teal-500",
  GovernmentService: "from-amber-600 to-orange-500",
  Equipment: "from-slate-600 to-slate-500",
};

export function ItAutomationHub() {
  const t = useTranslations("itAutomation");
  const locale = useLocale();
  const [hub, setHub] = useState<ItAutomationHub | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchItAutomationHub()
      .then(setHub)
      .catch(() => setHub({ categories: [], expiringSoonTotal: 0 }))
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className={cn("relative flex-1 overflow-y-auto", itAutomationTheme.meshBg)}>
      <div className="mx-auto max-w-6xl px-6 py-8 sm:py-10">
        <div className="mb-8 flex flex-wrap items-end justify-between gap-4">
          <div>
            <p className={itAutomationTheme.sectionLabel}>{t("title")}</p>
            <h1 className="mt-1 text-2xl font-semibold tracking-tight text-foreground">{t("landingTitle")}</h1>
            <p className="mt-2 max-w-2xl text-sm text-foreground/55">{t("hubSubtitle")}</p>
          </div>
          {hub && hub.expiringSoonTotal > 0 && (
            <div className="flex items-center gap-2 rounded-xl border border-amber-500/30 bg-amber-500/10 px-4 py-2.5 text-sm font-medium text-amber-800 dark:text-amber-300">
              <AlertTriangle size={16} />
              {t("expiringBanner", { count: hub.expiringSoonTotal })}
            </div>
          )}
        </div>

        {loading ? (
          <p className="text-sm text-foreground/45">{t("loading")}</p>
        ) : (
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
            {(hub?.categories ?? []).map((cat) => {
              const key = cat.category as ItAssetCategory;
              const Icon = ICONS[cat.category] ?? Cpu;
              const slug = CATEGORY_SLUGS[key] ?? cat.category.toLowerCase();
              const accent = ACCENTS[cat.category] ?? "from-cyan-600 to-indigo-500";
              return (
                <Link
                  key={cat.category}
                  href={`/${locale}/it-automation/${slug}`}
                  className={cn(
                    "group relative overflow-hidden p-5 transition-all duration-300 hover:-translate-y-0.5 hover:shadow-lg",
                    itAutomationTheme.card,
                  )}
                >
                  <div className="flex items-start justify-between gap-3">
                    <div className={cn("rounded-xl p-3 text-white shadow-md bg-gradient-to-br", accent)}>
                      <Icon size={22} />
                    </div>
                    {cat.expiringSoon > 0 && (
                      <span className="rounded-full bg-amber-500/15 px-2.5 py-1 text-[11px] font-bold text-amber-700 dark:text-amber-300">
                        {cat.expiringSoon} {t("expiringSoon")}
                      </span>
                    )}
                  </div>
                  <h2 className="mt-4 text-lg font-semibold tracking-tight text-foreground">
                    {t(`categories.${cat.category}` as "categories.License")}
                  </h2>
                  <p className="mt-1 text-xs text-foreground/45">
                    {t(`categoryDesc.${cat.category}` as "categoryDesc.License")}
                  </p>
                  <div className="mt-5 flex items-center justify-between text-sm">
                    <span className="font-medium text-foreground/70">
                      {cat.total} {t("items")}
                    </span>
                    {cat.responsibleUserName && (
                      <span className="flex items-center gap-1 truncate text-xs text-foreground/40">
                        <Shield size={12} />
                        {cat.responsibleUserName}
                      </span>
                    )}
                  </div>
                </Link>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}
