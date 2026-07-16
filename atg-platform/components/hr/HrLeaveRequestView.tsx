"use client";

import { useState } from "react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";
import { ArrowLeft, CheckCircle2, Download, Loader2, PenLine, ShieldCheck, XCircle } from "lucide-react";
import api, { getApiErrorMessage } from "@/lib/api";
import {
  approverRoleLabel,
  approverStatusClass,
  deptLabel,
  downloadHrLeaveFile,
  HrLeaveRequest,
  itemTypeLabel,
  phaseLabel,
} from "@/lib/hrLeave";
import { EimzoSignDialog } from "@/components/hr/EimzoSignDialog";
import { HrPageShell, HrPrimaryButton } from "@/components/hr/HrChrome";
import { hrInputClass, hrTheme } from "@/components/hr/hrTheme";
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
  const [showEimzo, setShowEimzo] = useState(false);
  const [downloading, setDownloading] = useState<string | null>(null);
  const [downloadError, setDownloadError] = useState("");
  const { permissions } = request;

  const downloadFile = async (kind: "pdf" | "signed-pdf" | "signed-pkcs7" | "json-signature", key: string) => {
    setDownloadError("");
    setDownloading(key);
    try {
      await downloadHrLeaveFile(request.id, kind);
    } catch (err: unknown) {
      setDownloadError(getApiErrorMessage(err) ?? t("downloadError"));
    } finally {
      setDownloading(null);
    }
  };

  const hasSignedPdf = request.signatures?.some((s) => s.kind === "PdfAttached");
  const hasJsonSignature = request.signatures?.some((s) => s.kind === "JsonDetached");

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

  const inputClass = hrInputClass();

  return (
    <HrPageShell>
      <header className="shrink-0 border-b border-slate-200/70 bg-white/70 backdrop-blur-xl px-6 py-5">
        <Link
          href={`/${locale}/hr`}
          className="inline-flex items-center gap-1 text-xs text-slate-400 hover:text-slate-700 mb-3"
        >
          <ArrowLeft size={14} />
          {t("backToList")}
        </Link>
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <h1 className="text-xl font-semibold tracking-tight text-slate-900">
              {t("detailTitle")}{" "}
              <span className={hrTheme.accentText}>{request.number}</span>
            </h1>
            <p className="text-sm text-slate-500 mt-1">
              {phaseLabel(request.phase, locale)}
              {request.hrTaskNumber ? ` · ${t("task")} ${request.hrTaskNumber}` : ""}
            </p>
          </div>
        </div>
      </header>

      <div className="flex-1 overflow-y-auto px-6 py-6">
        <div className="max-w-4xl space-y-6">
          <section className={cn("p-5 md:p-6 grid sm:grid-cols-2 gap-4 text-sm", hrTheme.card)}>
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
                <p className="text-xs font-semibold uppercase tracking-wider text-blue-700 mb-2">
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

          {request.signatures?.length > 0 && (
            <section className="space-y-3">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <h2 className="text-sm font-semibold text-foreground flex items-center gap-2">
                  <ShieldCheck size={16} className="text-blue-700" />
                  {t("signaturesTitle")}
                </h2>
                <div className="flex flex-wrap gap-2">
                  <Button
                    type="button"
                    variant="secondary"
                    size="sm"
                    disabled={!!downloading}
                    onClick={() => downloadFile("pdf", "pdf")}
                  >
                    {downloading === "pdf" ? (
                      <Loader2 className="animate-spin mr-1" size={14} />
                    ) : (
                      <Download size={14} className="mr-1" />
                    )}
                    {t("downloadUnsignedPdf")}
                  </Button>
                  {hasSignedPdf && (
                    <>
                      <Button
                        type="button"
                        size="sm"
                        className="bg-blue-700 hover:bg-blue-800"
                        disabled={!!downloading}
                        onClick={() => downloadFile("signed-pdf", "signed-pdf")}
                      >
                        {downloading === "signed-pdf" ? (
                          <Loader2 className="animate-spin mr-1" size={14} />
                        ) : (
                          <Download size={14} className="mr-1" />
                        )}
                        {t("downloadSignedPdf")}
                      </Button>
                      <Button
                        type="button"
                        variant="secondary"
                        size="sm"
                        disabled={!!downloading}
                        onClick={() => downloadFile("signed-pkcs7", "signed-pkcs7")}
                      >
                        {downloading === "signed-pkcs7" ? (
                          <Loader2 className="animate-spin mr-1" size={14} />
                        ) : (
                          <Download size={14} className="mr-1" />
                        )}
                        {t("downloadSignedPkcs7")}
                      </Button>
                    </>
                  )}
                  {hasJsonSignature && (
                    <Button
                      type="button"
                      variant="secondary"
                      size="sm"
                      disabled={!!downloading}
                      onClick={() => downloadFile("json-signature", "json-signature")}
                    >
                      {downloading === "json-signature" ? (
                        <Loader2 className="animate-spin mr-1" size={14} />
                      ) : (
                        <Download size={14} className="mr-1" />
                      )}
                      {t("downloadJsonSignature")}
                    </Button>
                  )}
                </div>
              </div>
              {hasSignedPdf && (
                <p className="text-xs text-foreground/50">{t("downloadSignedHint")}</p>
              )}
              {downloadError && <p className="text-sm text-red-600">{downloadError}</p>}
              <div className="rounded-xl border border-border/80 bg-surface overflow-hidden shadow-sm">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-border/60 bg-foreground/[0.02] text-left">
                      <th className="px-4 py-2.5 font-medium text-foreground/50">{t("columns.signer")}</th>
                      <th className="px-4 py-2.5 font-medium text-foreground/50">{t("columns.signatureKind")}</th>
                      <th className="px-4 py-2.5 font-medium text-foreground/50">{t("columns.signedAt")}</th>
                      <th className="px-4 py-2.5 font-medium text-foreground/50 w-28" />
                    </tr>
                  </thead>
                  <tbody>
                    {request.signatures.map((sig) => (
                      <tr key={sig.id} className="border-b border-border/40 last:border-0">
                        <td className="px-4 py-2.5">
                          {sig.signerName}
                          {sig.signerPinpp && (
                            <p className="text-xs text-foreground/45 mt-0.5">PINPP: {sig.signerPinpp}</p>
                          )}
                        </td>
                        <td className="px-4 py-2.5 text-foreground/70">
                          {sig.kind === "JsonDetached" ? t("signatureKinds.json") : t("signatureKinds.pdf")}
                        </td>
                        <td className="px-4 py-2.5 text-foreground/60">
                          {new Date(sig.signedAt).toLocaleString(locale)}
                        </td>
                        <td className="px-4 py-2.5 text-right">
                          {sig.kind === "PdfAttached" && (
                            <button
                              type="button"
                              className="inline-flex items-center gap-1 text-xs text-blue-800 hover:underline disabled:opacity-50"
                              disabled={!!downloading}
                              onClick={() => downloadFile("signed-pdf", `row-${sig.id}`)}
                            >
                              {downloading === `row-${sig.id}` ? (
                                <Loader2 className="animate-spin" size={12} />
                              ) : (
                                <Download size={12} />
                              )}
                              {t("download")}
                            </button>
                          )}
                          {sig.kind === "JsonDetached" && (
                            <button
                              type="button"
                              className="inline-flex items-center gap-1 text-xs text-blue-800 hover:underline disabled:opacity-50"
                              disabled={!!downloading}
                              onClick={() => downloadFile("json-signature", `row-${sig.id}`)}
                            >
                              {downloading === `row-${sig.id}` ? (
                                <Loader2 className="animate-spin" size={12} />
                              ) : (
                                <Download size={12} />
                              )}
                              {t("download")}
                            </button>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>
          )}

          {(permissions.canSubmit ||
            permissions.canHrReview ||
            permissions.canApprove ||
            permissions.canEimzoApprove ||
            permissions.canReject) && (
            <section className={cn("p-5 md:p-6 space-y-3 ring-1 ring-blue-200/70 bg-blue-50/40", hrTheme.card)}>
              <h2 className="text-sm font-semibold text-slate-900">{t("actionsTitle")}</h2>
              <textarea
                rows={2}
                value={comment}
                onChange={(e) => setComment(e.target.value)}
                placeholder={t("commentPlaceholder")}
                className={inputClass}
              />
              <div className="flex flex-wrap gap-2">
                {permissions.canSubmit && (
                  <HrPrimaryButton
                    disabled={acting}
                    onClick={() =>
                      act(async () => {
                        await api.post(`/hr/leave-requests/${request.id}/submit`);
                      })
                    }
                  >
                    {acting ? <Loader2 size={14} className="animate-spin mr-1" /> : <CheckCircle2 size={14} className="mr-1" />}
                    {t("submit")}
                  </HrPrimaryButton>
                )}
                {permissions.canHrReview && (
                  <HrPrimaryButton
                    disabled={acting}
                    onClick={() =>
                      act(async () => {
                        await api.post(`/hr/leave-requests/${request.id}/hr-review`, { comment: comment || null });
                      })
                    }
                  >
                    {t("hrApprove")}
                  </HrPrimaryButton>
                )}
                {permissions.canEimzoApprove && (
                  <HrPrimaryButton disabled={acting} onClick={() => setShowEimzo(true)}>
                    <PenLine size={14} className="mr-1" />
                    {t("eimzoApprove")}
                  </HrPrimaryButton>
                )}
                {permissions.canApprove && (
                  <Button
                    disabled={acting}
                    className="bg-emerald-600 hover:bg-emerald-700 rounded-xl"
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
                    className="rounded-xl"
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

      {showEimzo && (
        <EimzoSignDialog
          requestId={request.id}
          signingPackageUrl={`/hr/leave-requests/${request.id}/signing-package`}
          onCancel={() => setShowEimzo(false)}
          onSigned={async (jsonPkcs7, pdfPkcs7, eimzoComment) => {
            await api.post(`/hr/leave-requests/${request.id}/approve`, {
              comment: eimzoComment || comment || null,
              jsonPkcs7,
              pdfPkcs7,
            });
            setShowEimzo(false);
            onUpdated();
          }}
        />
      )}
    </HrPageShell>
  );
}
