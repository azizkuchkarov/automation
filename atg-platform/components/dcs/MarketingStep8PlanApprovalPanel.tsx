"use client";

import { useEffect, useMemo, useState } from "react";
import { CheckCircle2, Clock, Plus, Shield, XCircle } from "lucide-react";
import type { useTranslations } from "next-intl";
import {
  ProcurementMarketingPlanApprover,
  ProcurementMarketingPlanApproverRole,
  ProcurementRequest,
  ProcurementRequestUser,
  getNextPendingPlanApprover,
  planApproverRoleLabel,
} from "@/lib/procurementRequest";
import { deptLabel } from "@/lib/dcs";
import api from "@/lib/api";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

type ApproverRow = { userId: string; role: ProcurementMarketingPlanApproverRole };

interface Props {
  req: ProcurementRequest;
  locale: string;
  t: ReturnType<typeof useTranslations>;
  acting: boolean;
  onSubmit: (approvers: { userId: string; role: ProcurementMarketingPlanApproverRole }[]) => void;
  onApprove: (comment: string) => void;
  onReject: (comment: string) => void;
}

export function MarketingStep8PlanApprovalPanel({
  req,
  locale,
  t,
  acting,
  onSubmit,
  onApprove,
  onReject,
}: Props) {
  const perms = req.marketingPlanPermissions;
  const approvers = req.marketingPlanApprovers ?? [];
  const [users, setUsers] = useState<ProcurementRequestUser[]>([]);
  const [rows, setRows] = useState<ApproverRow[]>([
    { userId: "", role: "PlanDeputyCeo" },
    { userId: "", role: "PlanCeo" },
  ]);
  const [comment, setComment] = useState("");
  const [showReject, setShowReject] = useState(false);

  useEffect(() => {
    api.get("/dcs/procurement-requests/marketing/plan-approver-users").then((r) => {
      setUsers(r.data ?? []);
    }).catch(() => setUsers([]));
  }, []);

  const nextPending = useMemo(() => getNextPendingPlanApprover(approvers), [approvers]);
  const myPending = nextPending && perms?.canApprove ? nextPending : undefined;
  const showSubmit = perms?.canSubmit;
  const sorted = [...approvers].sort((a, b) => a.sortOrder - b.sortOrder);

  const addCommissionMember = () => {
    const withoutCeo = rows.filter((r) => r.role !== "PlanCeo");
    const ceo = rows.find((r) => r.role === "PlanCeo");
    setRows([
      ...withoutCeo,
      { userId: "", role: "PlanCommissionMember" as const },
      ...(ceo ? [ceo] : [{ userId: "", role: "PlanCeo" as const }]),
    ]);
  };

  const handleSubmit = () => {
    const valid = rows.filter((r) => r.userId);
    if (valid.length === 0 || !valid.some((r) => r.role === "PlanCeo")) return;
    onSubmit(valid);
  };

  return (
    <div className="mt-4 space-y-4">
      {showSubmit && (
        <div className="p-4 rounded-xl border border-violet-500/20 bg-violet-500/5 space-y-3">
          <p className="text-xs font-semibold text-foreground/70">{t("step8.submitTitle")}</p>
          <p className="text-xs text-foreground/50">{t("step8.submitHint")}</p>
          {rows.map((row, i) => (
            <div key={i} className="flex gap-2 items-center flex-wrap">
              <span className="text-xs w-40 shrink-0 text-foreground/50">
                {planApproverRoleLabel(row.role, locale)}
              </span>
              <select
                className="flex-1 min-w-[200px] rounded-xl border border-border/70 bg-background px-3 py-2 text-sm"
                value={row.userId}
                onChange={(e) => {
                  const next = [...rows];
                  next[i] = { ...next[i], userId: e.target.value };
                  setRows(next);
                }}
              >
                <option value="">{t("selectUser")}</option>
                {users.map((u) => (
                  <option key={u.id} value={u.id}>{u.fullName}</option>
                ))}
              </select>
            </div>
          ))}
          <div className="flex flex-wrap gap-2">
            <Button size="sm" variant="secondary" disabled={acting} onClick={addCommissionMember}>
              <Plus size={14} className="mr-1" />
              {t("step8.addCommissionMember")}
            </Button>
            <Button
              size="sm"
              disabled={acting || !rows.some((r) => r.userId && r.role === "PlanCeo")}
              onClick={handleSubmit}
            >
              {t("step8.submitApproval")}
            </Button>
          </div>
        </div>
      )}

      {sorted.length > 0 && (
        <div className="rounded-xl border border-border/60 bg-surface shadow-sm overflow-hidden">
          <div className="px-4 py-3 border-b border-border/50 flex items-center gap-2.5">
            <div className="w-8 h-8 rounded-lg bg-amber-500/12 flex items-center justify-center shrink-0">
              <Shield size={15} className="text-amber-600" />
            </div>
            <div>
              <h3 className="text-sm font-semibold">{t("step8.hierarchyTitle")}</h3>
              <p className="text-[11px] text-foreground/45">{t("step8.hierarchyHint")}</p>
            </div>
          </div>
          <ol className="px-3 py-3 space-y-0">
            {sorted.map((a, index) => (
              <PlanApproverRow
                key={a.id}
                approver={a}
                step={index + 1}
                locale={locale}
                isCurrent={nextPending?.id === a.id}
                isLast={index === sorted.length - 1}
              />
            ))}
          </ol>
        </div>
      )}

      {myPending && (
        <div className="p-4 rounded-xl border border-amber-500/25 bg-amber-500/5 space-y-3">
          <p className="text-sm font-semibold">{t("step8.yourTurn")}</p>
          <textarea
            className="w-full rounded-xl border border-border/70 bg-background px-3 py-2.5 text-sm min-h-[72px]"
            placeholder={t("approveCommentPlaceholder")}
            value={comment}
            onChange={(e) => setComment(e.target.value)}
          />
          <div className="flex flex-wrap gap-2">
            <Button size="sm" disabled={acting} onClick={() => onApprove(comment)}>
              {t("approve")}
            </Button>
            <Button size="sm" variant="secondary" disabled={acting} onClick={() => setShowReject((v) => !v)}>
              {t("reject")}
            </Button>
          </div>
          {showReject && (
            <div className="space-y-2 pt-2 border-t border-amber-500/20">
              <textarea
                className="w-full rounded-xl border border-border/70 bg-background px-3 py-2.5 text-sm min-h-[72px]"
                placeholder={t("rejectReasonPlaceholder")}
                value={comment}
                onChange={(e) => setComment(e.target.value)}
              />
              <Button size="sm" variant="danger" disabled={acting || !comment.trim()} onClick={() => onReject(comment)}>
                {t("confirmReject")}
              </Button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function PlanApproverRow({
  approver,
  step,
  locale,
  isCurrent,
  isLast,
}: {
  approver: ProcurementMarketingPlanApprover;
  step: number;
  locale: string;
  isCurrent: boolean;
  isLast: boolean;
}) {
  const statusIcon =
    approver.status === "Approved" ? (
      <CheckCircle2 size={16} className="text-emerald-600" />
    ) : approver.status === "Rejected" ? (
      <XCircle size={16} className="text-red-500" />
    ) : isCurrent ? (
      <Clock size={16} className="text-amber-500" />
    ) : (
      <span className="w-4 h-4 rounded-full border-2 border-foreground/15" />
    );

  return (
    <li className="flex gap-3">
      <div className="flex flex-col items-center">
        <div className={cn("w-7 h-7 rounded-full flex items-center justify-center text-[10px] font-bold", isCurrent ? "bg-amber-500/15 text-amber-700" : "bg-foreground/[0.04] text-foreground/40")}>
          {step}
        </div>
        {!isLast && <div className="w-px flex-1 min-h-[12px] bg-border/60 my-1" />}
      </div>
      <div className={cn("flex-1 pb-3", isLast && "pb-0")}>
        <div className="flex items-start gap-2">
          {statusIcon}
          <div className="min-w-0 flex-1">
            <p className="text-sm font-medium">{approver.userName}</p>
            <p className="text-[11px] text-foreground/45">
              {planApproverRoleLabel(approver.role, locale)}
              {approver.departmentName && ` · ${deptLabel(approver.departmentName, approver.departmentNameEn ?? "", locale)}`}
            </p>
            {approver.comment && (
              <p className="text-xs text-foreground/55 mt-1 italic">&ldquo;{approver.comment}&rdquo;</p>
            )}
          </div>
        </div>
      </div>
    </li>
  );
}
