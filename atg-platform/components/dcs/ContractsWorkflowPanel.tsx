"use client";

import { CheckCircle2, Circle, UserCheck, UserPlus } from "lucide-react";
import type { useTranslations } from "next-intl";
import {
  ProcurementRequest,
  ProcurementRequestUser,
  ProcurementStepComment,
  contractsSubPhaseLabel,
} from "@/lib/procurementRequest";
import { StepCommentThread } from "@/components/dcs/StepCommentThread";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

interface Props {
  req: ProcurementRequest;
  locale: string;
  t: ReturnType<typeof useTranslations>;
  acting: boolean;
  contractsPerms?: ProcurementRequest["contractsPermissions"];
  contractsWorkers: ProcurementRequestUser[];
  selectedEngineer: string;
  setSelectedEngineer: (v: string) => void;
  stepComment: string;
  setStepComment: (v: string) => void;
  onAssign: () => void;
  onAccept: () => void;
}

export function ContractsWorkflowPanel({
  req,
  locale,
  t,
  acting,
  contractsPerms,
  contractsWorkers,
  selectedEngineer,
  setSelectedEngineer,
  stepComment,
  setStepComment,
  onAssign,
  onAccept,
}: Props) {
  const inputClass = cn(
    "w-full rounded-xl border border-border/70 bg-background px-3 py-2.5 text-sm",
    "focus:outline-none focus:ring-2 focus:ring-indigo-500/25"
  );
  const comments: ProcurementStepComment[] = req.stepComments ?? [];
  const stepComments = comments.filter((c) => c.phase === "Contracts" && c.stepNumber === 1);
  const done = req.contractsSubPhase === "InProgress" || req.contractsSubPhase === "Completed";
  const active = req.phase === "Contracts" && !done;

  return (
    <section className="rounded-2xl border border-indigo-500/20 bg-surface shadow-sm overflow-hidden">
      <div className="px-5 py-4 border-b border-indigo-500/15 bg-gradient-to-r from-indigo-500/8 via-transparent to-transparent">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 className="text-sm font-bold">{t("contractsWorkflow")}</h2>
            <p className="text-xs text-foreground/50 mt-1">{t("contractsWorkflowHint")}</p>
          </div>
          <span className="px-2.5 py-1 rounded-full bg-indigo-500/12 text-indigo-700 dark:text-indigo-300 font-semibold text-xs">
            {contractsSubPhaseLabel(req.contractsSubPhase, locale)}
          </span>
        </div>
      </div>

      <div className="p-4">
        <div
          className={cn(
            "rounded-xl border transition-all",
            active ? "border-indigo-500/35 bg-indigo-500/[0.04] shadow-sm" : "border-border/50",
            done && "border-emerald-500/20 bg-emerald-500/[0.03]"
          )}
        >
          <div className="p-4">
            <div className="flex gap-3">
              {done ? (
                <CheckCircle2 size={20} className="text-emerald-600 shrink-0 mt-0.5" />
              ) : (
                <Circle size={20} className={cn("shrink-0 mt-0.5", active ? "text-indigo-500" : "text-foreground/20")} />
              )}
              <div className="flex-1 min-w-0">
                <p className="text-[10px] font-bold uppercase tracking-wider text-foreground/40">
                  {t("step")} 1
                </p>
                <p className="text-sm font-semibold text-foreground">{t("contractsAssignStepTitle")}</p>
                <p className="text-xs text-foreground/55 mt-1.5 leading-relaxed">{t("contractsAssignStepHint")}</p>

                {req.contractsSpecialistName && (
                  <p className="text-xs text-foreground/60 mt-2">
                    {t("contractsEngineer")}: <span className="font-medium">{req.contractsSpecialistName}</span>
                  </p>
                )}

                {active && contractsPerms?.canAssign && (
                  <AssignEngineerForm
                    t={t}
                    inputClass={inputClass}
                    contractsWorkers={contractsWorkers}
                    selectedEngineer={selectedEngineer}
                    setSelectedEngineer={setSelectedEngineer}
                    stepComment={stepComment}
                    setStepComment={setStepComment}
                    acting={acting}
                    onAssign={onAssign}
                  />
                )}

                {active && contractsPerms?.canAccept && (
                  <div className="mt-4 p-4 rounded-xl border-2 border-emerald-500/40 bg-emerald-500/8 space-y-3">
                    <p className="text-xs font-semibold text-foreground/80 flex items-center gap-2">
                      <UserCheck size={14} className="text-emerald-600" />
                      {t("acceptContracts")}
                    </p>
                    <p className="text-xs text-foreground/55">{t("acceptContractsHintEngineer")}</p>
                    <textarea
                      className={cn(inputClass, "min-h-[64px]")}
                      placeholder={t("acceptCommentPlaceholder")}
                      value={stepComment}
                      onChange={(e) => setStepComment(e.target.value)}
                    />
                    <Button size="sm" disabled={acting || !stepComment.trim()} onClick={onAccept}>
                      <UserCheck size={14} className="mr-1.5" />
                      {t("acceptContracts")}
                    </Button>
                  </div>
                )}

                {active && !contractsPerms?.canAssign && !contractsPerms?.canAccept
                  && req.contractsSubPhase === "WaitingAccept" && (
                  <p className="mt-4 text-xs text-amber-700/90 dark:text-amber-400/90 border-l-2 border-amber-500/40 pl-2.5">
                    {t("waitingEngineerAccept")}
                  </p>
                )}

                {stepComments.length > 0 && (
                  <div className="mt-4">
                    <StepCommentThread
                      comments={stepComments}
                      phase="Contracts"
                      stepNumber={1}
                      locale={locale}
                      acting={acting}
                      canAdd={false}
                    />
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}

function AssignEngineerForm({
  t,
  inputClass,
  contractsWorkers,
  selectedEngineer,
  setSelectedEngineer,
  stepComment,
  setStepComment,
  acting,
  onAssign,
}: {
  t: ReturnType<typeof useTranslations>;
  inputClass: string;
  contractsWorkers: ProcurementRequestUser[];
  selectedEngineer: string;
  setSelectedEngineer: (v: string) => void;
  stepComment: string;
  setStepComment: (v: string) => void;
  acting: boolean;
  onAssign: () => void;
}) {
  const engineerId = selectedEngineer.trim();
  const commentOk = stepComment.trim().length > 0;
  const canSubmit = Boolean(engineerId && commentOk);

  return (
    <div className="mt-4 p-4 rounded-xl border border-indigo-500/20 bg-indigo-500/5 space-y-3">
      <p className="text-xs font-semibold text-foreground/70 flex items-center gap-2">
        <UserPlus size={14} className="text-indigo-600" />
        {t("assignEngineer")}
      </p>
      <p className="text-xs text-foreground/50">{t("assignEngineerHint")}</p>
      <select
        className={inputClass}
        value={selectedEngineer}
        onChange={(e) => setSelectedEngineer(e.target.value)}
      >
        <option value="">{t("selectUser")}</option>
        {contractsWorkers.length === 0 ? (
          <option value="" disabled>
            {t("noContractsWorkers")}
          </option>
        ) : (
          contractsWorkers.map((u) => (
            <option key={u.id} value={u.id}>{u.fullName}</option>
          ))
        )}
      </select>
      {!engineerId && (
        <p className="text-[11px] text-amber-600/90">{t("selectEngineerRequired")}</p>
      )}
      <textarea
        className={cn(inputClass, "min-h-[64px]")}
        placeholder={t("assignCommentPlaceholder")}
        value={stepComment}
        onChange={(e) => setStepComment(e.target.value)}
      />
      {!commentOk && (
        <p className="text-[11px] text-amber-600/90">{t("assignCommentRequired")}</p>
      )}
      <Button size="sm" disabled={acting || !canSubmit} onClick={onAssign}>
        <UserPlus size={14} className="mr-1.5" />
        {t("assignEngineer")}
      </Button>
    </div>
  );
}
