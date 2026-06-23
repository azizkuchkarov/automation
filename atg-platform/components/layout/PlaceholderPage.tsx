"use client";

import { useTranslations } from "next-intl";
import { LucideIcon } from "lucide-react";

export default function PlaceholderPage({ icon: Icon, titleKey }: { icon: LucideIcon; titleKey: string }) {
  const t = useTranslations("nav");
  const tCommon = useTranslations("common");
  return (
    <div className="flex flex-col items-center justify-center min-h-[60vh] gap-4 text-foreground/60">
      <Icon size={48} className="opacity-40" />
      <h2 className="text-xl">{t(titleKey as "automation")}</h2>
      <p>{tCommon("comingSoon")}</p>
    </div>
  );
}
