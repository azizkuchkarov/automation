"use client";

import Link from "next/link";
import { useTranslations } from "next-intl";
import { FileSearch, Plus, Sparkles } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { dcsTheme } from "@/components/dcs/dcsTheme";
import { cn } from "@/lib/utils";

interface DcsEmptyStateProps {
  newHref?: string;
  typeLabel: string;
}

export function DcsEmptyState({ newHref, typeLabel }: DcsEmptyStateProps) {
  const t = useTranslations("dcs");

  return (
    <div className="flex flex-col items-center justify-center py-24 px-8 text-center">
      <div className="relative mb-8">
        <div className="absolute inset-0 rounded-3xl bg-gradient-to-br from-sky-400/20 to-blue-600/20 blur-2xl scale-150" />
        <div className={cn("relative w-20 h-20 rounded-2xl flex items-center justify-center", dcsTheme.premiumCard)}>
          <FileSearch size={32} className="text-sky-500/80" strokeWidth={1.5} />
          <Sparkles size={14} className="absolute -top-1 -right-1 text-sky-400" />
        </div>
      </div>
      <h3 className="text-lg font-bold text-foreground tracking-tight">{t("list.emptyTitle")}</h3>
      <p className="text-sm text-foreground/45 mt-2 max-w-md leading-relaxed">
        {t("list.emptyHint", { type: typeLabel })}
      </p>
      {newHref && (
        <Link href={newHref} className="mt-8">
          <Button size="sm" className={cn("h-10 px-5 rounded-xl font-semibold", dcsTheme.primaryBtn)}>
            <Plus size={15} className="mr-1.5" />
            {t("list.emptyCta")}
          </Button>
        </Link>
      )}
    </div>
  );
}

export function DcsListSkeleton() {
  return (
    <div className="divide-y divide-slate-100 dark:divide-white/[0.04]">
      {Array.from({ length: 6 }).map((_, i) => (
        <div key={i} className="flex items-center gap-4 px-6 py-4 animate-pulse">
          <div className="w-8 h-8 rounded-xl bg-gradient-to-br from-slate-200 to-slate-100 dark:from-white/10 dark:to-white/5" />
          <div className="w-28 h-4 rounded-lg bg-slate-200/80 dark:bg-white/10" />
          <div className="flex-1 h-4 rounded-lg bg-slate-200/80 dark:bg-white/10 max-w-md" />
          <div className="w-20 h-7 rounded-full bg-slate-200/80 dark:bg-white/10" />
          <div className="w-32 h-4 rounded-lg bg-slate-200/80 dark:bg-white/10 hidden sm:block" />
          <div className="w-24 h-4 rounded-lg bg-slate-200/80 dark:bg-white/10 hidden md:block" />
        </div>
      ))}
    </div>
  );
}
