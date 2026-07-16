"use client";

import { useTranslations } from "next-intl";
import { CreditCard } from "lucide-react";
import { DcsPageHeader } from "@/components/dcs/DcsPageHeader";

export default function ContractsPaymentPage() {
  const t = useTranslations("dcs.contractsQueue");

  return (
    <>
      <DcsPageHeader
        title={t("paymentTitle")}
        subtitle={t("paymentSubtitle")}
        breadcrumb={t("paymentTitle")}
        icon={CreditCard}
        iconClassName="bg-emerald-500/10 text-emerald-600 dark:text-emerald-400"
      />
      <div className="flex-1 overflow-auto px-6 py-10">
        <div className="mx-auto max-w-lg rounded-2xl border border-border/70 bg-surface p-10 text-center shadow-sm">
          <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-2xl bg-emerald-500/10 text-emerald-600">
            <CreditCard size={26} />
          </div>
          <h2 className="text-base font-bold text-foreground">{t("paymentTitle")}</h2>
          <p className="mt-2 text-sm leading-relaxed text-foreground/55">{t("paymentHint")}</p>
          <span className="mt-5 inline-flex rounded-full bg-foreground/[0.06] px-3 py-1 text-[11px] font-bold uppercase tracking-wider text-foreground/45">
            {t("comingSoon")}
          </span>
        </div>
      </div>
    </>
  );
}
