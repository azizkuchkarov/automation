"use client";

import { ShieldCheck } from "lucide-react";
import type { useTranslations } from "next-intl";
import { ProcurementRequest } from "@/lib/procurementRequest";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

interface Props {
  req: ProcurementRequest;
  locale: string;
  t: ReturnType<typeof useTranslations>;
  acting: boolean;
  comment: string;
  setComment: (v: string) => void;
  onConfirm: () => void;
}

export function MarketingStep9RegistrationPanel({
  req,
  locale,
  t,
  acting,
  comment,
  setComment,
  onConfirm,
}: Props) {
  const canConfirm = req.marketingPlanPermissions?.canConfirmRegistration;
  const registered = !!req.marketingPlanRegisteredAt;

  return (
    <div className="mt-4 space-y-4">
      <div className="rounded-xl border border-emerald-500/25 bg-gradient-to-b from-emerald-500/8 to-transparent overflow-hidden">
        <div className="px-5 py-6 text-center">
          <div className={cn(
            "w-14 h-14 rounded-2xl mx-auto flex items-center justify-center mb-3",
            registered ? "bg-emerald-500/15" : "bg-foreground/[0.06]"
          )}>
            <ShieldCheck size={28} className={registered ? "text-emerald-600" : "text-foreground/30"} />
          </div>
          <p className="text-[10px] font-bold uppercase tracking-[0.2em] text-foreground/40 mb-1">
            {t("step9.registrationTitle")}
          </p>
          <p className="font-mono text-xl font-bold text-foreground">
            {registered ? req.marketingPlanRegistrationNumber : t("step9.pendingNumber")}
          </p>
          {req.marketingPlanRegisteredAt && (
            <p className="text-xs text-foreground/50 mt-2">
              {t("regDate")}: {new Date(req.marketingPlanRegisteredAt).toLocaleString(locale)}
            </p>
          )}
        </div>
        <div className="px-5 pb-5">
          <p className="text-xs text-foreground/55 leading-relaxed mb-4">{t("step9.registrationHint")}</p>
          {canConfirm && (
            <div className="space-y-3">
              <textarea
                className="w-full rounded-xl border border-border/70 bg-background px-3 py-2.5 text-sm min-h-[80px]"
                placeholder={t("step9.confirmCommentPlaceholder")}
                value={comment}
                onChange={(e) => setComment(e.target.value)}
              />
              <Button size="sm" disabled={acting || !comment.trim()} onClick={onConfirm}>
                {t("step9.confirmRegistration")}
              </Button>
            </div>
          )}
          {registered && (
            <p className="text-xs text-emerald-700/90 dark:text-emerald-400/90">{t("step9.autoContractsHint")}</p>
          )}
        </div>
      </div>
    </div>
  );
}
