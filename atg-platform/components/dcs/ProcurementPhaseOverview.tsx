"use client";

import {
  Building2,
  Clock,
  Megaphone,
  User,
  Wrench,
} from "lucide-react";
import {
  ProcurementRequest,
  marketingSubPhaseLabel,
  phaseLabel,
} from "@/lib/procurementRequest";
import { deptLabel } from "@/lib/dcs";
import { cn } from "@/lib/utils";

interface Props {
  req: ProcurementRequest;
  locale: string;
  isTas: boolean;
}

function PhaseHeader({
  icon: Icon,
  title,
  accent,
  progress,
  status,
}: {
  icon: typeof Wrench;
  title: string;
  accent: string;
  progress: number;
  status: "pending" | "active" | "completed" | "skipped";
}) {
  return (
    <div className={cn("px-5 py-4 border-b", accent)}>
      <div className="flex items-start justify-between gap-3">
        <div className="flex gap-3 min-w-0">
          <div className={cn(
            "w-10 h-10 rounded-xl flex items-center justify-center shrink-0",
            status === "completed" ? "bg-emerald-500/15 text-emerald-600" :
            status === "active" ? "bg-sky-500/15 text-sky-600" :
            "bg-foreground/[0.06] text-foreground/40"
          )}>
            <Icon size={18} />
          </div>
          <div className="min-w-0">
            <h3 className="text-sm font-bold text-foreground leading-snug">{title}</h3>
          </div>
        </div>
        <div className="text-right shrink-0">
          <p className="text-xl font-bold tabular-nums text-foreground">{progress}%</p>
          <p className="text-[10px] text-foreground/40 uppercase tracking-wider">
            {status === "completed" ? "Done" : status === "active" ? "Active" : "—"}
          </p>
        </div>
      </div>
      <div className="mt-3 h-1.5 rounded-full bg-foreground/[0.06] overflow-hidden">
        <div
          className={cn(
            "h-full rounded-full transition-all duration-500",
            status === "completed" ? "bg-emerald-500" : "bg-gradient-to-r from-sky-500 to-blue-600"
          )}
          style={{ width: `${progress}%` }}
        />
      </div>
    </div>
  );
}

export function ProcurementPhaseOverview({ req, locale, isTas }: Props) {
  const initiationTitle = isTas
    ? (locale.startsWith("en") ? "BMGMC Technical Affairs" : "BMGMC — Технический отдел")
    : deptLabel(
        req.initiatorDepartmentName ?? req.departmentName,
        req.initiatorDepartmentNameEn ?? req.departmentNameEn,
        locale
      );

  const taDone = req.phase !== "InProgress" && req.phase !== "AwaitingApproval"
    ? (isTas ? 100 : req.phase === "Marketing" || req.phase === "Contracts" || req.phase === "Completed" ? 100 : 0)
    : req.phase === "AwaitingApproval" ? 90 : Math.round(((req.currentStep - 1) / Math.max(req.steps.length - 1, 1)) * 100);

  const expressInitiationDone =
    req.phase === "Marketing" || req.phase === "Contracts" || req.phase === "Completed"
      ? 100
      : req.phase === "AwaitingApproval"
        ? 50
        : req.isRegistered
          ? 100
          : 0;

  const initiationProgress = isTas ? taDone : expressInitiationDone;

  const taStatus: "pending" | "active" | "completed" | "skipped" =
    !isTas ? "skipped" :
    req.phase === "InProgress" || req.phase === "AwaitingApproval" ? "active" :
    "completed";

  const expressInitiationStatus: "pending" | "active" | "completed" | "skipped" =
    req.phase === "Marketing" || req.phase === "Contracts" || req.phase === "Completed"
      ? "completed"
      : req.phase === "AwaitingApproval"
        ? "active"
        : req.isRegistered
          ? "completed"
          : "pending";

  const initiationStatus = isTas ? taStatus : expressInitiationStatus;

  const mktDone =
    req.marketingSubPhase === "Completed" ||
    req.phase === "Contracts" ||
    req.phase === "Completed"
      ? 100
      : req.phase !== "Marketing"
        ? 0
        : Math.round(((req.marketingCurrentStep - 1) / req.marketingSteps.length) * 100);

  const mktStatus: "pending" | "active" | "completed" | "skipped" =
    req.phase === "Marketing" ? (req.marketingSubPhase === "Completed" ? "completed" : "active") :
    req.phase === "Contracts" || req.phase === "Completed" ? "completed" : "pending";

  const marketingStatusText =
    mktStatus === "completed"
      ? marketingSubPhaseLabel("Completed", locale)
      : req.phase === "Marketing"
        ? marketingSubPhaseLabel(req.marketingSubPhase, locale)
        : "—";

  const approvalActive = req.phase === "AwaitingApproval";

  return (
    <div className="grid lg:grid-cols-2 gap-5">
      {/* Initiation: TAS workflow or opening department */}
      <section className="rounded-2xl border border-border/70 bg-surface shadow-sm overflow-hidden">
        <PhaseHeader
          icon={isTas ? Wrench : Building2}
          title={initiationTitle}
          accent={
            isTas
              ? "border-sky-500/15 bg-gradient-to-r from-sky-500/5 to-transparent"
              : "border-slate-500/15 bg-gradient-to-r from-slate-500/5 to-transparent"
          }
          progress={initiationProgress}
          status={initiationStatus}
        />
        <div className="p-4 space-y-3">
          {isTas ? (
            <>
              <div className="grid grid-cols-2 gap-2 text-xs">
                <MetaChip icon={User} label={locale.startsWith("en") ? "Initiator" : "Инициатор"} value={req.initiatorName ?? "—"} />
                <MetaChip icon={Building2} label={locale.startsWith("en") ? "Responsible" : "Ответственный"} value={req.tasResponsibleName ?? req.assigneeName ?? "—"} />
                {req.eamNumber && <MetaChip icon={Clock} label="EAM" value={req.eamNumber} />}
              </div>
              {approvalActive && (
                <div className="rounded-xl border border-amber-500/30 bg-amber-500/8 px-3 py-2 text-xs text-amber-800">
                  {locale.startsWith("en") ? "Awaiting MR/SR approval" : "Ожидает согласования ЛЗМ/ЛЗУ"}
                </div>
              )}
            </>
          ) : (
            <>
              <div className="grid grid-cols-2 gap-2 text-xs">
                <MetaChip
                  icon={User}
                  label={locale.startsWith("en") ? "Opened by" : "Открыл"}
                  value={req.authorName}
                />
                <MetaChip
                  icon={User}
                  label={locale.startsWith("en") ? "Initiator" : "Инициатор"}
                  value={req.initiatorName ?? req.authorName}
                />
                <MetaChip
                  icon={Building2}
                  label={locale.startsWith("en") ? "Department" : "Подразделение"}
                  value={deptLabel(
                    req.initiatorDepartmentName ?? req.departmentName,
                    req.initiatorDepartmentNameEn ?? req.departmentNameEn,
                    locale
                  )}
                />
                {req.eamNumber && <MetaChip icon={Clock} label="EAM" value={req.eamNumber} />}
              </div>
              <p className="text-sm text-foreground/55 leading-relaxed">
                {locale.startsWith("en")
                  ? "Direct request from the initiating department — approval flow without BMGMC Technical Affairs steps."
                  : "Прямая заявка от подразделения-инициатора — согласование без этапов BMGMC Technical Affairs."}
              </p>
            </>
          )}
          <div className="pt-2 border-t border-border/40 text-xs text-foreground/45">
            {locale.startsWith("en") ? "Current phase" : "Текущая фаза"}: <span className="font-medium text-foreground/70">{phaseLabel(req.phase, locale)}</span>
          </div>
        </div>
      </section>

      {/* Marketing */}
      <section className="rounded-2xl border border-border/70 bg-surface shadow-sm overflow-hidden">
        <PhaseHeader
          icon={Megaphone}
          title={locale.startsWith("en") ? "Marketing & Tender Management" : "Департамент маркетинга и тендеров"}
          accent="border-violet-500/15 bg-gradient-to-r from-violet-500/5 to-transparent"
          progress={mktDone}
          status={mktStatus}
        />
        <div className="p-4 space-y-3">
          <div className="grid grid-cols-2 gap-2 text-xs">
            <MetaChip
              icon={User}
              label={locale.startsWith("en") ? "Specialist" : "Специалист"}
              value={req.marketingSpecialistName ?? (locale.startsWith("en") ? "Not assigned" : "Не назначен")}
            />
            <MetaChip
              icon={Clock}
              label={locale.startsWith("en") ? "Status" : "Статус"}
              value={marketingStatusText}
            />
          </div>
          {req.phase !== "Marketing" && mktStatus !== "completed" ? (
            <div className="rounded-xl border border-dashed border-border/60 px-4 py-6 text-center text-sm text-foreground/45">
              {locale.startsWith("en")
                ? "Marketing starts after registration and MR/SR approval."
                : "Маркетинг начинается после регистрации и согласования ЛЗМ/ЛЗУ."}
            </div>
          ) : (
            <p className="text-xs text-foreground/50 leading-relaxed">
              {locale.startsWith("en")
                ? `${req.marketingCurrentStep} / ${req.marketingSteps.length} steps — see Marketing tab for full workflow.`
                : `${req.marketingCurrentStep} / ${req.marketingSteps.length} этапов — полный процесс на вкладке «Маркетинг».`}
            </p>
          )}
          {req.marketingSubPhase === "WaitingAccept" && (
            <div className="rounded-xl border border-amber-500/30 bg-amber-500/8 px-3 py-2 text-xs text-amber-800">
              {locale.startsWith("en")
                ? `Awaiting acceptance by ${req.marketingSpecialistName ?? "assigned specialist"}`
                : `Ожидает принятия: ${req.marketingSpecialistName ?? "назначенный специалист"}`}
            </div>
          )}
        </div>
      </section>
    </div>
  );
}

function MetaChip({
  icon: Icon,
  label,
  value,
}: {
  icon: typeof User;
  label: string;
  value: string;
}) {
  return (
    <div className="rounded-lg border border-border/50 bg-foreground/[0.02] px-2.5 py-2">
      <p className="text-[10px] font-bold uppercase tracking-wider text-foreground/35 flex items-center gap-1">
        <Icon size={10} /> {label}
      </p>
      <p className="font-medium text-foreground/80 mt-0.5 truncate">{value}</p>
    </div>
  );
}
