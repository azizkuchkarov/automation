"use client";

import { useState } from "react";
import {
  CheckCircle2,
  Clock,
  MessageSquare,
  Shield,
  XCircle,
} from "lucide-react";
import type { useTranslations } from "next-intl";
import {
  ProcurementApprover,
  ProcurementRequest,
  APPROVER_ROLE_ORDER,
  approverRoleLabel,
  getNextPendingApprover,
} from "@/lib/procurementRequest";
import { deptLabel } from "@/lib/dcs";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

const ROLE_ORDER = APPROVER_ROLE_ORDER;

interface Props {
  req: ProcurementRequest;
  locale: string;
  t: ReturnType<typeof useTranslations>;
  acting: boolean;
  myPendingApproval?: ProcurementRequest["approvers"][0];
  onApprove: (comment: string) => void;
  onReject: (comment: string) => void;
}

export function ProcurementApproversHierarchy({
  req,
  locale,
  t,
  acting,
  myPendingApproval,
  onApprove,
  onReject,
}: Props) {
  const [comment, setComment] = useState("");
  const [showRejectForm, setShowRejectForm] = useState(false);

  const sorted = [...req.approvers].sort(
    (a, b) => ROLE_ORDER.indexOf(a.role) - ROLE_ORDER.indexOf(b.role)
  );

  const nextPending = getNextPendingApprover(req.approvers);
  const approvedCount = sorted.filter((a) => a.status === "Approved").length;
  const progress = sorted.length > 0 ? Math.round((approvedCount / sorted.length) * 100) : 0;
  const allDone = sorted.every((a) => a.status !== "Pending");

  if (sorted.length === 0) {
    return (
      <div className="rounded-xl border border-dashed border-border/60 px-5 py-10 text-center text-sm text-foreground/45">
        {t("noApprovers")}
      </div>
    );
  }

  return (
    <div className="space-y-3">
      <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm dark:border-white/10 dark:bg-slate-900/40">
        {/* Compact header */}
        <div className="px-4 py-3 border-b border-border/50 flex items-center justify-between gap-3">
          <div className="flex items-center gap-2.5 min-w-0">
            <div className="w-8 h-8 rounded-lg bg-amber-500/12 flex items-center justify-center shrink-0">
              <Shield size={15} className="text-amber-600" />
            </div>
            <div className="min-w-0">
              <h2 className="text-sm font-semibold leading-tight">{t("approversHierarchyTitle")}</h2>
              <p className="text-[11px] text-foreground/45 truncate">{t("approversHierarchyHint")}</p>
            </div>
          </div>
          <div className="text-right shrink-0">
            <span className="text-xs font-bold tabular-nums text-foreground">
              {approvedCount}/{sorted.length}
            </span>
            <span className="text-[10px] text-foreground/40 ml-1.5">
              {allDone ? t("statusApproved") : t("statusPending")}
            </span>
          </div>
        </div>

        <div className="px-4 pt-2 pb-1">
          <div className="h-1 rounded-full bg-foreground/[0.06] overflow-hidden">
            <div
              className="h-full rounded-full bg-gradient-to-r from-emerald-500 to-emerald-400 transition-all duration-500"
              style={{ width: `${progress}%` }}
            />
          </div>
        </div>

        {/* Compact timeline */}
        <ol className="px-3 py-3 space-y-0">
          {sorted.map((approver, index) => {
            const isLast = index === sorted.length - 1;
            const isCurrent = nextPending?.id === approver.id;
            const isQueued = approver.status === "Pending" && !isCurrent;

            return (
              <ApproverRow
                key={approver.id}
                approver={approver}
                step={index + 1}
                locale={locale}
                t={t}
                isLast={isLast}
                isCurrent={isCurrent}
                isQueued={isQueued}
              />
            );
          })}
        </ol>
      </div>

      {myPendingApproval && (
        <div className="space-y-3 rounded-xl border border-amber-300 bg-amber-50 p-4 dark:border-amber-500/40 dark:bg-amber-500/10">
          <p className="text-sm font-semibold text-amber-950 dark:text-amber-100">{t("approvalPending")}</p>
          <textarea
            className={cn(
              "w-full rounded-lg border border-border/70 bg-background px-3 py-2 text-sm min-h-[64px]",
              "focus:outline-none focus:ring-2 focus:ring-amber-500/25"
            )}
            placeholder={t("approveCommentPlaceholder")}
            value={comment}
            onChange={(e) => setComment(e.target.value)}
          />
          <div className="flex flex-wrap gap-2">
            <Button size="sm" disabled={acting} onClick={() => onApprove(comment)}>
              {t("approve")}
            </Button>
            <Button
              size="sm"
              variant="secondary"
              disabled={acting}
              onClick={() => setShowRejectForm((v) => !v)}
            >
              {t("reject")}
            </Button>
          </div>
          {showRejectForm && (
            <div className="rounded-lg border border-red-500/20 bg-red-500/[0.03] p-3 space-y-2">
              <p className="text-[11px] font-medium text-red-700 dark:text-red-400">
                {t("rejectReasonRequired")}
              </p>
              <textarea
                className={cn(
                  "w-full rounded-lg border border-red-500/20 bg-background px-3 py-2 text-sm min-h-[56px]",
                  "focus:outline-none focus:ring-2 focus:ring-red-500/20"
                )}
                placeholder={t("rejectReasonPlaceholder")}
                value={comment}
                onChange={(e) => setComment(e.target.value)}
              />
              <Button
                size="sm"
                variant="secondary"
                disabled={acting || !comment.trim()}
                onClick={() => onReject(comment.trim())}
              >
                {t("confirmReject")}
              </Button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function ApproverRow({
  approver,
  step,
  locale,
  t,
  isLast,
  isCurrent,
  isQueued,
}: {
  approver: ProcurementApprover;
  step: number;
  locale: string;
  t: ReturnType<typeof useTranslations>;
  isLast: boolean;
  isCurrent: boolean;
  isQueued: boolean;
}) {
  const role = approverRoleLabel(approver.role, locale);
  const department = deptLabel(
    approver.departmentName ?? "",
    approver.departmentNameEn ?? "",
    locale
  );
  const jobTitle = locale.startsWith("en")
    ? approver.jobTitleEn ?? approver.jobTitleRu
    : approver.jobTitleRu ?? approver.jobTitleEn;

  const metaParts = [department, jobTitle].filter(Boolean);
  const metaLine = metaParts.length > 0 ? metaParts.join(" · ") : t("approverNoDepartment");

  const statusLabel =
    approver.status === "Approved"
      ? t("statusApproved")
      : approver.status === "Rejected"
        ? t("statusRejected")
        : isCurrent
          ? t("statusPending")
          : t("statusQueued");

  const nodeColor =
    approver.status === "Approved"
      ? "bg-emerald-500 text-white border-emerald-500"
      : approver.status === "Rejected"
        ? "bg-red-500 text-white border-red-500"
        : isCurrent
          ? "bg-amber-500 text-white border-amber-500 shadow-sm shadow-amber-500/30"
          : "bg-background text-foreground/35 border-border/70";

  const railColor =
    approver.status === "Approved" ? "bg-emerald-400/50" : "bg-border/60";

  return (
    <li className={cn("relative flex gap-2.5", !isLast && "pb-2", isQueued && "opacity-55")}>
      {/* Rail + node */}
      <div className="flex flex-col items-center w-6 shrink-0 pt-0.5">
        <div
          className={cn(
            "w-6 h-6 rounded-full border flex items-center justify-center shrink-0 z-[1]",
            nodeColor
          )}
        >
          {approver.status === "Approved" ? (
            <CheckCircle2 size={13} strokeWidth={2.5} />
          ) : approver.status === "Rejected" ? (
            <XCircle size={13} strokeWidth={2.5} />
          ) : isCurrent ? (
            <Clock size={12} strokeWidth={2.5} />
          ) : (
            <span className="text-[10px] font-bold leading-none">{step}</span>
          )}
        </div>
        {!isLast && <div className={cn("w-px flex-1 min-h-[0.5rem] mt-1", railColor)} />}
      </div>

      {/* Card */}
      <div
        className={cn(
          "flex-1 min-w-0 rounded-lg border px-3 py-2.5 mb-1 transition-all",
          approver.status === "Approved" && "border-emerald-500/20 bg-emerald-500/[0.03]",
          approver.status === "Rejected" && "border-red-500/20 bg-red-500/[0.03]",
          approver.status === "Pending" && "border-border/50 bg-background/50",
          isCurrent && "border-amber-500/35 ring-1 ring-amber-500/15 bg-amber-500/[0.03]"
        )}
      >
        {/* Row 1: role + status */}
        <div className="flex items-center justify-between gap-2">
          <span className="text-[10px] font-bold uppercase tracking-wide text-foreground/40 truncate">
            {role}
          </span>
          <StatusPill status={approver.status} isCurrent={isCurrent} label={statusLabel} />
        </div>

        {/* Row 2: avatar + name + meta */}
        <div className="flex items-center gap-2.5 mt-1.5 min-w-0">
          <div
            className={cn(
              "w-8 h-8 rounded-full flex items-center justify-center text-[11px] font-bold shrink-0",
              approver.status === "Approved"
                ? "bg-emerald-500/15 text-emerald-700 dark:text-emerald-300"
                : approver.status === "Rejected"
                  ? "bg-red-500/15 text-red-700"
                  : isCurrent
                    ? "bg-amber-500/15 text-amber-700 dark:text-amber-300"
                    : "bg-foreground/[0.06] text-foreground/50"
            )}
            title={approver.userEmail}
          >
            {userInitials(approver.userName)}
          </div>
          <div className="min-w-0 flex-1">
            <p className="text-sm font-semibold text-foreground leading-tight truncate">
              {approver.userName}
            </p>
            <p className="text-[11px] text-foreground/50 truncate mt-0.5" title={metaLine}>
              {metaLine}
            </p>
          </div>
        </div>

        {/* Row 3: comment + time */}
        {(approver.comment || approver.decidedAt) && (
          <div className="mt-2 pt-2 border-t border-border/30 flex items-start gap-2 min-w-0">
            {approver.comment && (
              <MessageSquare size={11} className="text-foreground/30 shrink-0 mt-0.5" />
            )}
            <div className="flex-1 min-w-0">
              {approver.comment && (
                <p className="text-xs text-foreground/65 leading-snug line-clamp-2 whitespace-pre-wrap">
                  {approver.comment}
                </p>
              )}
              {approver.decidedAt && (
                <p className="text-[10px] text-foreground/35 mt-0.5 tabular-nums">
                  {new Date(approver.decidedAt).toLocaleString(locale, {
                    day: "2-digit",
                    month: "2-digit",
                    year: "numeric",
                    hour: "2-digit",
                    minute: "2-digit",
                  })}
                </p>
              )}
            </div>
          </div>
        )}
      </div>
    </li>
  );
}

function StatusPill({
  status,
  isCurrent,
  label,
}: {
  status: ProcurementApprover["status"];
  isCurrent: boolean;
  label: string;
}) {
  return (
    <span
      className={cn(
        "text-[9px] font-bold uppercase tracking-wide px-1.5 py-0.5 rounded shrink-0",
        status === "Approved" && "bg-emerald-500/12 text-emerald-700 dark:text-emerald-300",
        status === "Rejected" && "bg-red-500/12 text-red-700",
        status === "Pending" && isCurrent && "bg-amber-500/15 text-amber-700 dark:text-amber-300",
        status === "Pending" && !isCurrent && "bg-foreground/[0.06] text-foreground/40"
      )}
    >
      {label}
    </span>
  );
}

function userInitials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length >= 2) {
    return (parts[0][0] + parts[1][0]).toUpperCase();
  }
  return name.slice(0, 2).toUpperCase();
}
