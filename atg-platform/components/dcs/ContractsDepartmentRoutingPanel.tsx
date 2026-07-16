"use client";

import { useState } from "react";
import { ArrowRight, Building2, FileSignature, Globe2, MapPin } from "lucide-react";
import type { useTranslations } from "next-intl";
import {
  ContractsProcurementSectionType,
  ProcurementRequest,
  contractsSectionLabel,
} from "@/lib/procurementRequest";
import { cn } from "@/lib/utils";

interface Props {
  req: ProcurementRequest;
  locale: string;
  t: ReturnType<typeof useTranslations>;
  acting: boolean;
  canRoute: boolean;
  selectedSection: ContractsProcurementSectionType | "";
  setSelectedSection: (v: ContractsProcurementSectionType | "") => void;
  stepComment: string;
  setStepComment: (v: string) => void;
  onRouteSection: (section?: ContractsProcurementSectionType) => void;
}

const OPTIONS: {
  id: ContractsProcurementSectionType;
  icon: typeof MapPin;
  accent: string;
  ring: string;
  bg: string;
}[] = [
  {
    id: "Domestic",
    icon: MapPin,
    accent: "text-emerald-700 dark:text-emerald-300",
    ring: "ring-emerald-500/30 border-emerald-500/40",
    bg: "from-emerald-500/10 to-teal-500/5",
  },
  {
    id: "International",
    icon: Globe2,
    accent: "text-indigo-700 dark:text-indigo-300",
    ring: "ring-indigo-500/30 border-indigo-500/40",
    bg: "from-indigo-500/10 to-violet-500/5",
  },
];

export function ContractsDepartmentRoutingPanel({
  req,
  locale,
  t,
  acting,
  canRoute,
  selectedSection,
  setSelectedSection,
  stepComment,
  setStepComment,
  onRouteSection,
}: Props) {
  const [touched, setTouched] = useState(false);
  const commentOk = stepComment.trim().length > 0;
  const canSubmit = Boolean(selectedSection && commentOk);

  if (!canRoute) {
    return (
      <div className="rounded-2xl border border-amber-200 bg-amber-50/80 p-6 dark:border-amber-500/30 dark:bg-amber-500/10">
        <div className="flex items-start gap-3">
          <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-amber-500/15 text-amber-700">
            <Building2 size={18} />
          </div>
          <div>
            <h2 className="text-base font-semibold text-amber-950 dark:text-amber-100">
              {t("contractsDeptWaitingTitle")}
            </h2>
            <p className="mt-1 text-sm text-amber-900/70 dark:text-amber-100/70">
              {t("waitingDeptHeadRoute")}
            </p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <section className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-white/10 dark:bg-slate-900/40">
      <div className="border-b border-slate-100 bg-gradient-to-r from-indigo-500/10 via-sky-500/5 to-transparent px-6 py-5 dark:border-white/[0.06]">
        <div className="flex items-start gap-3">
          <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-indigo-600 text-white shadow-md shadow-indigo-600/25">
            <FileSignature size={20} />
          </div>
          <div className="min-w-0">
            <p className="text-[10px] font-bold uppercase tracking-[0.16em] text-indigo-600/80">
              {t("contractsDeptBadge")}
            </p>
            <h2 className="mt-0.5 text-lg font-semibold text-slate-900 dark:text-slate-50">
              {t("contractsDeptRouteTitle")}
            </h2>
            <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
              {t("contractsDeptRouteHint")}
            </p>
            <p className="mt-2 font-mono text-xs font-semibold text-sky-700 dark:text-sky-300">
              {req.number}
            </p>
          </div>
        </div>
      </div>

      <div className="space-y-5 p-6">
        <div>
          <p className="mb-3 text-[11px] font-bold uppercase tracking-wider text-slate-400">
            {t("contractsSelectSection")}
          </p>
          <div className="grid gap-3 sm:grid-cols-2">
            {OPTIONS.map((opt) => {
              const selected = selectedSection === opt.id;
              const Icon = opt.icon;
              return (
                <button
                  key={opt.id}
                  type="button"
                  disabled={acting}
                  onClick={() => {
                    setSelectedSection(opt.id);
                    setTouched(true);
                  }}
                  className={cn(
                    "rounded-2xl border bg-gradient-to-br p-5 text-left transition-all",
                    opt.bg,
                    selected
                      ? cn("ring-2 shadow-md", opt.ring)
                      : "border-slate-200 hover:border-slate-300 dark:border-white/10 dark:hover:border-white/20",
                  )}
                >
                  <div className="flex items-start gap-3">
                    <span
                      className={cn(
                        "flex h-10 w-10 items-center justify-center rounded-xl bg-white shadow-sm dark:bg-white/10",
                        opt.accent,
                      )}
                    >
                      <Icon size={18} />
                    </span>
                    <div className="min-w-0">
                      <p className={cn("text-sm font-bold", selected ? opt.accent : "text-slate-800 dark:text-slate-100")}>
                        {opt.id === "Domestic"
                          ? t("contractsTabs.domestic")
                          : t("contractsTabs.international")}
                      </p>
                      <p className="mt-1 text-xs leading-relaxed text-slate-500">
                        {opt.id === "Domestic"
                          ? t("contractsSectionDomHint")
                          : t("contractsSectionIntHint")}
                      </p>
                      <p className="mt-2 text-[11px] font-medium text-slate-400">
                        {contractsSectionLabel(opt.id, locale)}
                      </p>
                    </div>
                  </div>
                </button>
              );
            })}
          </div>
          {touched && !selectedSection && (
            <p className="mt-2 text-xs text-amber-600">{t("contractsSelectSectionRequired")}</p>
          )}
        </div>

        <div>
          <label className="mb-1.5 block text-[11px] font-bold uppercase tracking-wider text-slate-400">
            {t("routeSectionCommentPlaceholder")}
          </label>
          <textarea
            className="min-h-[88px] w-full rounded-xl border border-slate-200 bg-slate-50/80 px-3.5 py-3 text-sm focus:border-indigo-400 focus:outline-none focus:ring-2 focus:ring-indigo-500/20 dark:border-white/10 dark:bg-white/[0.03]"
            placeholder={t("routeSectionCommentPlaceholder")}
            value={stepComment}
            onChange={(e) => {
              setStepComment(e.target.value);
              setTouched(true);
            }}
          />
          {touched && !commentOk && (
            <p className="mt-1.5 text-xs text-amber-600">{t("assignCommentRequired")}</p>
          )}
        </div>

        <div className="flex flex-wrap items-center gap-3 border-t border-slate-100 pt-4 dark:border-white/[0.06]">
          <button
            type="button"
            disabled={acting || !canSubmit}
            onClick={() => {
              setTouched(true);
              if (!canSubmit) return;
              onRouteSection(selectedSection || undefined);
            }}
            className="inline-flex items-center gap-2 rounded-xl bg-indigo-600 px-5 py-2.5 text-sm font-bold text-white shadow-md shadow-indigo-600/25 transition hover:bg-indigo-500 disabled:opacity-50"
          >
            {t("routeToSection")}
            <ArrowRight size={16} />
          </button>
          {selectedSection && (
            <p className="text-xs text-slate-500">
              {t("contractsRouteAutoHint", {
                section:
                  selectedSection === "Domestic"
                    ? t("contractsTabs.domestic")
                    : t("contractsTabs.international"),
              })}
            </p>
          )}
        </div>
      </div>
    </section>
  );
}
