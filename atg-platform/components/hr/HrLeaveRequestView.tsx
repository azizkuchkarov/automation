"use client";

import { useState } from "react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";
import { ArrowLeft, CheckCircle2, Loader2, XCircle } from "lucide-react";
import api, { getApiErrorMessage } from "@/lib/api";
import {
  approverRoleLabel,
  approverStatusClass,
  deptLabel,
  HrLeaveRequest,
  itemTypeLabel,
  phaseLabel,
} from "@/lib/hrLeave";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

interface Props {
  request: HrLeaveRequest;
  onUpdated: () => void;
}

function formatDate(value: string, locale: string) {
  return new Date(value).toLocaleDateString(locale.startsWith("en") ? "en-GB" : "ru-RU", {
    day: "2-digit",
    month: "long",
    year: "numeric",
  });
}

export function HrLeaveRequestView({ request, onUpdated }: Props) {
  const t = useTranslations("hr.leave");
  const locale = useLocale();
  const [comment, setComment] = useState("");
  const [acting, setActing] = useState(false);
  const [error, setError] = useState("");
  const { permissions } = request;

  const act = async (fn: () => Promise<void>) => {
    setError("");
    setActing(true);
    try {
      await fn();
      onUpdated();
      setComment("");
    } catch (err: unknown) {
      setError(getApiErrorMessage(err) ?? t("actionError"));
    } finally {
      setActing(false);
    }
  };

  const inputClass =
    "w-full rounded-lg border border-border/80 bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-violet-500/30";

  return (
    <div className="flex flex-col flex-1 min-h-0">
      <header className="shrink-0 border-b border-border/80 bg-surface px-6 py-5">
        <Link
          href={`/${locale}/hr`}
          className="inline-flex items-center gap-1 text-xs text-foreground/45 hover:text-foreground mb-3"
        >
          <ArrowLeft size={14} />
          {t("backToList")}
        </Link>
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <h1 className="text-xl font-semibold text-foreground">
              {t("detailTitle")}{" "}
              <span className="text-violet-700">{request.number}</span>
            </h1>
            <p className="text-sm text-foreground/50 mt-1">
              {phaseLabel(request.phase, locale)}
              {request.hrTaskNumber ? ` · ${t("task")} ${request.hrTaskNumber}` : ""}
            </p>
          </div>
        </div>
      </header>

      <div className="flex-1 overflow-y-auto px-6 py-5">
        <div className="max-w-4xl space-y-6">
          <section className="rounded-xl border border-border/80 bg-surface p-5 shadow-sm grid sm:grid-cols-2 gap-4 text-sm">
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-wider text-foreground/40 mb-1">
                {t("fields.author")}
              </p>
              <p>{request.authorName}</p>
            </div>
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-wider text-foreground/40 mb-1">
                {t("fields.department")}
              </p>
              <p>{deptLabel(request.departmentName, request.departmentNameEn, locale)}</p>
            </div>
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-wider text-foreground/40 mb-1">
                {t("fields.organization")}
              </p>
              <p>{request.organizationName}</p>
            </div>
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-wider text-foreground/40 mb-1">
                {t("fields.hrDepartment")}
              </p>
              <p>{deptLabel(request.hrDepartmentName, request.hrDepartmentNameEn, locale)}</p>
            </div>
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-wider text-foreground/40 mb-1">
                {t("fields.periodLabel")}
              </p>
              <p>{request.periodLabel}</p>
            </div>
            <div>
              <p className="text-[11px] font-semibold uppercase tracking-wider text-foreground/40 mb-1">
                {t("fields.requestDate")}
              </p>
              <p>{formatDate(request.requestDate, locale)}</p>
            </div>
          </section>

          <section className="space-y-3">
            <h2 className="text-sm font-semibold text-foreground">{t("itemsTitle")}</h2>
            {request.items.map((item, index) => (
              <div key={item.id} className="rounded-xl border border-border/80 bg-surface p-4 shadow-sm">
                <p className="text-xs font-semibold uppercase tracking-wider text-violet-600 mb-2">
                  {index + 1}. {itemTypeLabel(item.type, locale)}
                </p>
                <p className="text-sm leading-relaxed mb-2">{locale.startsWith("en") ? item.textEn : item.textRu}</p>
                {locale.startsWith("en") && item.textRu && (
                  <p className="text-xs text-foreground/45 leading-relaxed border-t border-border/50 pt-2">
                    {item.textRu}
                  </p>
                )}
                {!locale.startsWith("en") && item.textEn && (
                  <p className="text-xs text-foreground/45 leading-relaxed border-t border-border/50 pt-2">
                    {item.textEn}
                  </p>
                )}
              </div>
            ))}
          </section>

          {request.approvers.length > 0 && (
            <section className="space-y-3">
              <h2 className="text-sm font-semibold text-foreground">{t("approversTitle")}</h2>
              <div className="rounded-xl border border-border/80 bg-surface overflow-hidden shadow-sm">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-border/60 bg-foreground/[0.02] text-left">
                      <th className="px-4 py-2.5 font-medium text-foreground/50">{t("columns.role")}</th>
                      <th className="px-4 py-2.5 font-medium text-foreground/50">{t("columns.approver")}</th>
                      <th className="px-4 py-2.5 font-medium text-foreground/50">{t("columns.status")}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {request.approvers.map((a) => (
                      <tr key={a.id} className="border-b border-border/40 last:border-0">
                        <td className="px-4 py-2.5 text-foreground/70">
                          {approverRoleLabel(a.role, locale)}
                        </td>
                        <td className="px-4 py-2.5">{a.userName}</td>
                        <td className="px-4 py-2.5">
                          <span
                            className={cn(
                              "inline-flex px-2 py-0.5 rounded-full text-xs border",
                              approverStatusClass(a.status)
                            )}
                          >
                            {a.status}
                          </span>
                          {a.comment && (
                            <p className="text-xs text-foreground/45 mt-1">{a.comment}</p>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>
          )}

          {request.timeline.length > 0 && (
            <section className="space-y-3">
              <h2 className="text-sm font-semibold text-foreground">{t("timelineTitle")}</h2>
              <ul className="space-y-2">
                {request.timeline.map((ev) => (
                  <li
                    key={ev.id}
                    className="rounded-lg border border-border/60 bg-surface px-4 py-3 text-sm"
                  >
                    <div className="flex flex-wrap items-center justify-between gap-2">
                      <span className="font-medium">{ev.actorName}</span>
                      <span className="text-xs text-foreground/40">
                        {new Date(ev.createdAt).toLocaleString(locale)}
                      </span>
                    </div>
                    <p className="text-foreground/60 mt-0.5">{ev.action}</p>
                    {ev.details && <p className="text-xs text-foreground/45 mt-1">{ev.details}</p>}
                  </li>
                ))}
              </ul>
            </section>
          )}

          {(permissions.canSubmit ||
            permissions.canHrReview ||
            permissions.canApprove ||
            permissions.canReject) && (
            <section className="rounded-xl border border-violet-200 bg-violet-50/50 p-5 space-y-3">
              <h2 className="text-sm font-semibold text-foreground">{t("actionsTitle")}</h2>
              <textarea
                rows={2}
                value={comment}
                onChange={(e) => setComment(e.target.value)}
                placeholder={t("commentPlaceholder")}
                className={inputClass}
              />
              <div className="flex flex-wrap gap-2">
                {permissions.canSubmit && (
                  <Button
                    disabled={acting}
                    className="bg-violet-600 hover:bg-violet-700"
                    onClick={() =>
                      act(async () => {
                        await api.post(`/hr/leave-requests/${request.id}/submit`);
                      })
                    }
                  >
                    {acting ? <Loader2 size={14} className="animate-spin mr-1" /> : <CheckCircle2 size={14} className="mr-1" />}
                    {t("submit")}
                  </Button>
                )}
                {permissions.canHrReview && (
                  <Button
                    disabled={acting}
                    className="bg-violet-600 hover:bg-violet-700"
                    onClick={() =>
                      act(async () => {
                        await api.post(`/hr/leave-requests/${request.id}/hr-review`, { comment: comment || null });
                      })
                    }
                  >
                    {t("hrApprove")}
                  </Button>
                )}
                {permissions.canApprove && (
                  <Button
                    disabled={acting}
                    className="bg-emerald-600 hover:bg-emerald-700"
                    onClick={() =>
                      act(async () => {
                        await api.post(`/hr/leave-requests/${request.id}/approve`, { comment: comment || null });
                      })
                    }
                  >
                    {t("approve")}
                  </Button>
                )}
                {permissions.canReject && (
                  <Button
                    disabled={acting || !comment.trim()}
                    variant="danger"
                    onClick={() =>
                      act(async () => {
                        await api.post(`/hr/leave-requests/${request.id}/reject`, { comment: comment.trim() });
                      })
                    }
                  >
                    <XCircle size={14} className="mr-1" />
                    {t("reject")}
                  </Button>
                )}
              </div>
              {error && <p className="text-sm text-red-600">{error}</p>}
            </section>
          )}
        </div>
      </div>
    </div>
  );
}
