"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import {
  AlertTriangle,
  CheckCircle2,
  FileText,
  Loader2,
  MessageSquareText,
  PenLine,
  ShieldCheck,
  Sparkles,
  XCircle,
} from "lucide-react";
import api, { getApiErrorMessage } from "@/lib/api";
import {
  deptLabel,
  downloadHrBusinessTripCertificate,
  downloadHrBusinessTripOrderPdf,
  downloadHrBusinessTripPdf,
  HrBusinessTripRequest,
} from "@/lib/hrBusinessTrip";
import {
  HrBusinessTripApprovalChain,
  HrBusinessTripHero,
  HrBusinessTripWorkflowShell,
  HrBusinessTripWorkflowStepper,
} from "@/components/hr/HrBusinessTripWorkflowUI";
import { DcsWorkflowCard } from "@/components/dcs/DcsWorkflowUI";
import { EimzoSignDialog } from "@/components/hr/EimzoSignDialog";
import { HrPrimaryButton } from "@/components/hr/HrChrome";
import { hrInputClass } from "@/components/hr/hrTheme";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

type ActionMode = "eimzo" | "decision" | "order" | "certificate" | "submit" | "hr";

function resolveActionMode(permissions: HrBusinessTripRequest["permissions"]): ActionMode {
  if (permissions.canEimzoApprove) return "eimzo";
  if (permissions.canIssueOrder) return "order";
  if (permissions.canGenerateCertificates || permissions.canDeliverCertificates) return "certificate";
  if (permissions.canSubmit) return "submit";
  if (permissions.canHrReview) return "hr";
  return "decision";
}

const MODE_STYLES: Record<
  ActionMode,
  { shell: string; iconBg: string; badge: string }
> = {
  eimzo: {
    shell: "border-amber-500/30 bg-gradient-to-br from-amber-500/[0.08] via-violet-500/[0.05] to-surface shadow-amber-500/10",
    iconBg: "bg-gradient-to-br from-amber-500 to-violet-600",
    badge: "bg-amber-500/15 text-amber-800 border-amber-500/25",
  },
  decision: {
    shell: "border-emerald-500/25 bg-gradient-to-br from-emerald-500/[0.07] via-surface to-red-500/[0.04] shadow-emerald-500/10",
    iconBg: "bg-gradient-to-br from-emerald-500 to-teal-600",
    badge: "bg-emerald-500/15 text-emerald-800 border-emerald-500/25",
  },
  order: {
    shell: "border-sky-500/25 bg-gradient-to-br from-sky-500/[0.08] via-surface to-surface shadow-sky-500/10",
    iconBg: "bg-gradient-to-br from-sky-500 to-blue-600",
    badge: "bg-sky-500/15 text-sky-800 border-sky-500/25",
  },
  certificate: {
    shell: "border-violet-500/25 bg-gradient-to-br from-violet-500/[0.08] via-surface to-surface shadow-violet-500/10",
    iconBg: "bg-gradient-to-br from-violet-500 to-fuchsia-600",
    badge: "bg-violet-500/15 text-violet-800 border-violet-500/25",
  },
  submit: {
    shell: "border-atg-blue/25 bg-gradient-to-br from-atg-blue/[0.08] via-surface to-surface shadow-atg-blue/10",
    iconBg: "bg-gradient-to-br from-atg-blue to-indigo-600",
    badge: "bg-atg-blue/15 text-atg-blue border-atg-blue/25",
  },
  hr: {
    shell: "border-blue-500/25 bg-gradient-to-br from-blue-500/[0.08] via-surface to-surface shadow-blue-500/10",
    iconBg: "bg-gradient-to-br from-blue-500 to-indigo-600",
    badge: "bg-blue-500/15 text-blue-800 border-blue-500/25",
  },
};

interface Props {
  request: HrBusinessTripRequest;
  onUpdated: () => void;
}

function formatDate(value: string, locale: string) {
  return new Date(value).toLocaleDateString(locale.startsWith("en") ? "en-GB" : "ru-RU", {
    day: "2-digit",
    month: "long",
    year: "numeric",
  });
}

export function HrBusinessTripRequestView({ request, onUpdated }: Props) {
  const t = useTranslations("hr.businessTrip");
  const locale = useLocale();
  const router = useRouter();
  const [comment, setComment] = useState("");
  const [acting, setActing] = useState(false);
  const [error, setError] = useState("");
  const [showEimzo, setShowEimzo] = useState(false);
  const { permissions } = request;
  const isTravelerView = Boolean(request.isTravelerView);
  const hasMemoPdf = !isTravelerView && request.hasMemoPdf !== false && request.phase !== "Draft";
  const hasOrderPdf = !isTravelerView && Boolean(request.hasOrderPdf ?? request.orderIssuedAt);
  const hasOrderSigned = !isTravelerView && Boolean(request.hasOrderSigned);
  const hasCertificates = Boolean(request.hasCertificates);
  const showDocuments = hasMemoPdf || hasOrderPdf || hasOrderSigned || hasCertificates;

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

  const hasActions =
    permissions.canSubmit ||
    permissions.canHrReview ||
    permissions.canApprove ||
    permissions.canEimzoApprove ||
    permissions.canIssueOrder ||
    permissions.canGenerateCertificates ||
    permissions.canDeliverCertificates ||
    permissions.canReject;

  return (
    <HrBusinessTripWorkflowShell>
      <HrBusinessTripHero
        request={request}
        locale={locale}
        backLabel={t("backToList")}
        printLabel={isTravelerView ? t("downloadCertificate") : t("downloadPdf")}
        onBack={() => router.push(`/${locale}/hr/business-trip`)}
        onDownload={
          isTravelerView
            ? async () => {
                const traveler = request.travelers.find((tr) => tr.id === request.myTravelerId) ?? request.travelers[0];
                if (!traveler) return;
                try {
                  await downloadHrBusinessTripCertificate(request.id, traveler.id, traveler.certificateNumber);
                } catch {
                  setError(t("downloadCertificateError"));
                }
              }
            : async () => {
                try {
                  await downloadHrBusinessTripPdf(request.id, false);
                } catch {
                  setError(t("downloadError"));
                }
              }
        }
      />

      {!isTravelerView && (
        <HrBusinessTripWorkflowStepper request={request} locale={locale} title={t("workflowTitle")} />
      )}

      {isTravelerView && (
        <div className="rounded-xl border border-violet-200 bg-violet-50/40 px-4 py-3 text-sm text-violet-900 mb-6">
          {t("travelerCertificateHint")}
        </div>
      )}

      <div className="grid lg:grid-cols-5 gap-6">
        <div className="lg:col-span-3 space-y-6">
          <DcsWorkflowCard kind="memo" title={t("detailTitle")}>
            <div className="grid sm:grid-cols-2 gap-4 text-sm">
              <div>
                <p className="text-[11px] font-semibold uppercase tracking-wider text-foreground/40 mb-1">
                  {t("fields.author")}
                </p>
                <p className="font-medium">{request.authorName}</p>
              </div>
              <div>
                <p className="text-[11px] font-semibold uppercase tracking-wider text-foreground/40 mb-1">
                  {t("fields.department")}
                </p>
                <p>{deptLabel(request.departmentName, request.departmentNameEn, locale)}</p>
              </div>
              <div>
                <p className="text-[11px] font-semibold uppercase tracking-wider text-foreground/40 mb-1">
                  {t("fields.requestDate")}
                </p>
                <p>{formatDate(request.requestDate, locale)}</p>
              </div>
              <div>
                <p className="text-[11px] font-semibold uppercase tracking-wider text-foreground/40 mb-1">
                  {t("fields.duration")}
                </p>
                <p>
                  {formatDate(request.dateFrom, locale)} — {formatDate(request.dateTo, locale)} ({request.daysCount})
                </p>
              </div>
              <div className="sm:col-span-2">
                <p className="text-[11px] font-semibold uppercase tracking-wider text-foreground/40 mb-1">
                  {t("fields.purpose")}
                </p>
                <p>{locale.startsWith("en") && request.purposeEn ? request.purposeEn : request.purposeRu}</p>
              </div>
              <div className="sm:col-span-2">
                <p className="text-[11px] font-semibold uppercase tracking-wider text-foreground/40 mb-1">
                  {t("fields.place")}
                </p>
                <p>{locale.startsWith("en") && request.placeEn ? request.placeEn : request.placeRu}</p>
              </div>
              {request.orderNumber && (
                <div>
                  <p className="text-[11px] font-semibold uppercase tracking-wider text-foreground/40 mb-1">
                    {t("fields.orderNumber")}
                  </p>
                  <p className="font-medium">{request.orderNumber}</p>
                </div>
              )}
            </div>
          </DcsWorkflowCard>

          {showDocuments && (
            <DcsWorkflowCard kind="memo" title={t("documentsTitle")} hint={t("documentsHint")}>
              <div className="space-y-3">
                {hasMemoPdf && (
                  <div className="flex items-center justify-between gap-3 rounded-xl border border-border/70 bg-background px-4 py-3">
                    <div className="min-w-0">
                      <p className="text-sm font-medium text-foreground">{t("docMemoPdf")}</p>
                      <p className="text-xs text-foreground/45 truncate">{request.number}.pdf</p>
                    </div>
                    <Button
                      variant="secondary"
                      className="rounded-xl shrink-0"
                      onClick={async () => {
                        try {
                          await downloadHrBusinessTripPdf(request.id, false);
                        } catch {
                          setError(t("downloadError"));
                        }
                      }}
                    >
                      <FileText size={14} className="mr-1" />
                      {t("downloadPdf")}
                    </Button>
                  </div>
                )}

                {hasOrderPdf && (
                  <div className="flex items-center justify-between gap-3 rounded-xl border border-border/70 bg-background px-4 py-3">
                    <div className="min-w-0">
                      <p className="text-sm font-medium text-foreground">{t("docOrderPdf")}</p>
                      <p className="text-xs text-foreground/45 truncate">
                        {request.orderNumber ?? request.number}.pdf
                      </p>
                    </div>
                    <Button
                      variant="secondary"
                      className="rounded-xl shrink-0"
                      onClick={async () => {
                        try {
                          await downloadHrBusinessTripOrderPdf(request.id, request.orderNumber, false);
                        } catch {
                          setError(t("downloadOrderError"));
                        }
                      }}
                    >
                      <FileText size={14} className="mr-1" />
                      {t("downloadOrderPdf")}
                    </Button>
                  </div>
                )}

                {hasOrderSigned && (
                  <div className="flex items-center justify-between gap-3 rounded-xl border border-emerald-200 bg-emerald-50/40 px-4 py-3">
                    <div className="min-w-0">
                      <p className="text-sm font-medium text-emerald-800">{t("docOrderSignedPdf")}</p>
                      <p className="text-xs text-emerald-700/70 truncate">
                        {request.orderNumber ?? request.number}-signed.pdf
                      </p>
                    </div>
                    <Button
                      className="bg-emerald-600 hover:bg-emerald-700 rounded-xl shrink-0"
                      onClick={async () => {
                        try {
                          await downloadHrBusinessTripOrderPdf(request.id, request.orderNumber, true);
                        } catch {
                          setError(t("downloadOrderError"));
                        }
                      }}
                    >
                      <FileText size={14} className="mr-1" />
                      {t("downloadOrderSignedPdf")}
                    </Button>
                  </div>
                )}

                {hasCertificates && (
                  <div className="space-y-2 rounded-xl border border-violet-200 bg-violet-50/30 px-4 py-3">
                    <p className="text-sm font-medium text-violet-900">{t("docCertificates")}</p>
                    {request.travelers
                      .filter((traveler) => traveler.hasCertificate)
                      .map((traveler) => (
                        <div
                          key={traveler.id}
                          className="flex items-center justify-between gap-3 rounded-lg border border-violet-100 bg-background px-3 py-2"
                        >
                          <div className="min-w-0">
                            <p className="text-sm text-foreground">
                              {locale.startsWith("en") ? traveler.displayEn : traveler.displayRu}
                            </p>
                            <p className="text-xs text-foreground/45 truncate">
                              {traveler.certificateNumber ?? t("docCertificateFile")}
                              {traveler.certificateDeliveredAt ? ` · ${t("certificateDelivered")}` : ""}
                            </p>
                          </div>
                          <Button
                            variant="secondary"
                            className="rounded-xl shrink-0"
                            onClick={async () => {
                              try {
                                await downloadHrBusinessTripCertificate(
                                  request.id,
                                  traveler.id,
                                  traveler.certificateNumber,
                                );
                              } catch {
                                setError(t("downloadCertificateError"));
                              }
                            }}
                          >
                            <FileText size={14} className="mr-1" />
                            {t("downloadCertificate")}
                          </Button>
                        </div>
                      ))}
                  </div>
                )}
              </div>
            </DcsWorkflowCard>
          )}

          <DcsWorkflowCard kind="memo" title={t("travelersTitle")}>
            <ol className="list-decimal list-inside space-y-2 text-sm text-foreground/80">
              {request.travelers.map((traveler) => (
                <li key={traveler.id}>
                  {locale.startsWith("en") ? traveler.displayEn : traveler.displayRu}
                </li>
              ))}
            </ol>
          </DcsWorkflowCard>

          {request.timeline.length > 0 && !isTravelerView && (
            <DcsWorkflowCard kind="memo" title={t("timelineTitle")}>
              <ul className="space-y-3 text-sm">
                {request.timeline.map((e) => (
                  <li key={e.id} className="flex gap-3 border-l-2 border-blue-200 pl-3 text-foreground/70">
                    <div>
                      <p className="text-[11px] text-foreground/35">
                        {new Date(e.createdAt).toLocaleString(locale.startsWith("en") ? "en-GB" : "ru-RU")}
                      </p>
                      <p>
                        <strong className="text-foreground/85">{e.actorName}</strong> — {e.action}
                        {e.details ? `: ${e.details}` : ""}
                      </p>
                    </div>
                  </li>
                ))}
              </ul>
            </DcsWorkflowCard>
          )}
        </div>

        <div className="lg:col-span-2 space-y-6">
          {!isTravelerView && (
            <HrBusinessTripApprovalChain request={request} locale={locale} title={t("approversTitle")} />
          )}

          {hasActions && (() => {
            const actionMode = resolveActionMode(permissions);
            const modeStyle = MODE_STYLES[actionMode];
            const ModeIcon =
              actionMode === "eimzo"
                ? PenLine
                : actionMode === "order" || actionMode === "certificate"
                  ? FileText
                  : actionMode === "submit"
                    ? Sparkles
                    : ShieldCheck;
            const actionHint =
              permissions.canIssueOrder
                ? t("orderActionsHint")
                : permissions.canGenerateCertificates || permissions.canDeliverCertificates
                  ? t("certificateActionsHint")
                  : permissions.canEimzoApprove
                    ? t("eimzoActionsHint")
                    : t("actionsHint");
            const modeBadge =
              actionMode === "eimzo"
                ? t("actionModeEimzo")
                : actionMode === "order"
                  ? t("actionModeOrder")
                  : actionMode === "certificate"
                    ? t("actionModeCertificate")
                    : actionMode === "submit"
                      ? t("actionModeSubmit")
                      : actionMode === "hr"
                        ? t("actionModeHr")
                        : t("actionModeDecision");
            const showComment =
              permissions.canApprove ||
              permissions.canReject ||
              permissions.canHrReview ||
              permissions.canEimzoApprove;

            return (
              <div
                className={cn(
                  "relative overflow-hidden rounded-2xl border shadow-xl",
                  modeStyle.shell,
                )}
              >
                <div
                  className="absolute inset-0 opacity-[0.4] pointer-events-none"
                  style={{
                    backgroundImage:
                      "radial-gradient(circle at 1px 1px, var(--border) 1px, transparent 0)",
                    backgroundSize: "20px 20px",
                  }}
                />

                <div className="relative p-5 sm:p-6 space-y-5">
                  <div className="flex items-start gap-3.5">
                    <div
                      className={cn(
                        "flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl text-white shadow-lg",
                        modeStyle.iconBg,
                      )}
                    >
                      <ModeIcon size={22} />
                    </div>
                    <div className="min-w-0 flex-1">
                      <div className="flex flex-wrap items-center gap-2 mb-1">
                        <h3 className="text-base font-bold tracking-tight text-foreground">
                          {t("actionsTitle")}
                        </h3>
                        <span
                          className={cn(
                            "inline-flex items-center rounded-full border px-2 py-0.5 text-[10px] font-bold uppercase tracking-wider",
                            modeStyle.badge,
                          )}
                        >
                          {modeBadge}
                        </span>
                      </div>
                      <p className="text-sm text-foreground/55 leading-relaxed">{actionHint}</p>
                    </div>
                  </div>

                  {showComment && (
                    <div>
                      <label className="mb-2 flex items-center gap-1.5 text-[11px] font-bold uppercase tracking-[0.12em] text-foreground/45">
                        <MessageSquareText size={12} />
                        {t("commentLabel")}
                      </label>
                      <textarea
                        value={comment}
                        onChange={(e) => setComment(e.target.value)}
                        placeholder={t("commentPlaceholder")}
                        rows={3}
                        className={cn(
                          inputClass,
                          "min-h-[88px] resize-y rounded-xl border-border/70 bg-white/70 dark:bg-white/[0.04] shadow-inner",
                          "focus:ring-2 focus:ring-emerald-500/20 focus:border-emerald-500/40",
                        )}
                      />
                      {permissions.canReject && (
                        <p className="mt-1.5 text-[11px] text-foreground/40">{t("commentRejectHint")}</p>
                      )}
                    </div>
                  )}

                  {error && (
                    <div className="flex items-start gap-3 rounded-xl border border-red-500/30 bg-red-500/[0.07] px-4 py-3">
                      <div className="mt-0.5 flex h-7 w-7 shrink-0 items-center justify-center rounded-lg bg-red-500/15 text-red-600">
                        <AlertTriangle size={15} />
                      </div>
                      <div className="min-w-0">
                        <p className="text-sm font-semibold text-red-700 dark:text-red-300">
                          {t("actionBlocked")}
                        </p>
                        <p className="mt-0.5 text-sm text-red-700/85 dark:text-red-300/85 leading-snug">
                          {error}
                        </p>
                      </div>
                    </div>
                  )}

                  <div className="flex flex-col gap-2.5 sm:flex-row sm:flex-wrap sm:items-center pt-1">
                    {permissions.canSubmit && (
                      <HrPrimaryButton
                        disabled={acting}
                        className="h-11 px-5 rounded-xl shadow-md shadow-atg-blue/20"
                        onClick={() =>
                          act(async () => {
                            await api.post(`/hr/business-trips/${request.id}/submit`);
                          })
                        }
                      >
                        {acting ? (
                          <Loader2 size={15} className="animate-spin mr-1.5" />
                        ) : (
                          <Sparkles size={15} className="mr-1.5" />
                        )}
                        {t("submit")}
                      </HrPrimaryButton>
                    )}

                    {permissions.canHrReview && (
                      <HrPrimaryButton
                        disabled={acting}
                        className="h-11 px-5 rounded-xl shadow-md shadow-blue-500/20"
                        onClick={() =>
                          act(async () => {
                            await api.post(`/hr/business-trips/${request.id}/hr-review`, {
                              comment: comment || null,
                            });
                          })
                        }
                      >
                        <ShieldCheck size={15} className="mr-1.5" />
                        {t("hrApprove")}
                      </HrPrimaryButton>
                    )}

                    {permissions.canEimzoApprove && (
                      <Button
                        disabled={acting}
                        className="h-11 px-5 rounded-xl font-semibold bg-gradient-to-r from-amber-500 to-violet-600 text-white hover:from-amber-400 hover:to-violet-500 shadow-md shadow-violet-500/25 border-0"
                        onClick={() => setShowEimzo(true)}
                      >
                        <PenLine size={15} className="mr-1.5" />
                        {t("eimzoApprove")}
                      </Button>
                    )}

                    {permissions.canApprove && (
                      <Button
                        disabled={acting}
                        className="h-11 px-5 rounded-xl font-semibold bg-emerald-600 hover:bg-emerald-500 text-white shadow-md shadow-emerald-600/25"
                        onClick={() =>
                          act(async () => {
                            await api.post(`/hr/business-trips/${request.id}/approve`, {
                              comment: comment || null,
                            });
                          })
                        }
                      >
                        {acting ? (
                          <Loader2 size={15} className="animate-spin mr-1.5" />
                        ) : (
                          <CheckCircle2 size={15} className="mr-1.5" />
                        )}
                        {t("approve")}
                      </Button>
                    )}

                    {permissions.canIssueOrder && (
                      <HrPrimaryButton
                        disabled={acting}
                        className="h-11 px-5 rounded-xl shadow-md shadow-sky-500/20"
                        onClick={() =>
                          act(async () => {
                            await api.post(`/hr/business-trips/${request.id}/issue-order`);
                          })
                        }
                      >
                        {acting ? (
                          <Loader2 size={15} className="animate-spin mr-1.5" />
                        ) : (
                          <FileText size={15} className="mr-1.5" />
                        )}
                        {t("issueOrder")}
                      </HrPrimaryButton>
                    )}

                    {permissions.canGenerateCertificates && (
                      <HrPrimaryButton
                        disabled={acting}
                        className="h-11 px-5 rounded-xl shadow-md shadow-violet-500/20"
                        onClick={() =>
                          act(async () => {
                            await api.post(`/hr/business-trips/${request.id}/generate-certificates`);
                          })
                        }
                      >
                        {acting ? (
                          <Loader2 size={15} className="animate-spin mr-1.5" />
                        ) : (
                          <FileText size={15} className="mr-1.5" />
                        )}
                        {t("generateCertificates")}
                      </HrPrimaryButton>
                    )}

                    {permissions.canDeliverCertificates && (
                      <Button
                        disabled={acting}
                        className="h-11 px-5 rounded-xl font-semibold bg-violet-600 hover:bg-violet-500 text-white shadow-md shadow-violet-600/25"
                        onClick={() =>
                          act(async () => {
                            await api.post(`/hr/business-trips/${request.id}/deliver-certificates`);
                          })
                        }
                      >
                        <CheckCircle2 size={15} className="mr-1.5" />
                        {t("deliverCertificates")}
                      </Button>
                    )}

                    {permissions.canReject && (
                      <Button
                        disabled={acting || !comment.trim()}
                        variant="danger"
                        className="h-11 px-5 rounded-xl font-semibold shadow-md shadow-red-600/20 sm:ml-auto"
                        onClick={() =>
                          act(async () => {
                            await api.post(`/hr/business-trips/${request.id}/reject`, {
                              comment: comment.trim(),
                            });
                          })
                        }
                      >
                        <XCircle size={15} className="mr-1.5" />
                        {t("reject")}
                      </Button>
                    )}
                  </div>
                </div>
              </div>
            );
          })()}
        </div>
      </div>

      {showEimzo && (
        <EimzoSignDialog
          requestId={request.id}
          signingPackageUrl={`/hr/business-trips/${request.id}/order-signing-package`}
          onCancel={() => setShowEimzo(false)}
          onSigned={async (jsonPkcs7, pdfPkcs7, eimzoComment) => {
            await api.post(`/hr/business-trips/${request.id}/sign-order`, {
              comment: eimzoComment || comment || null,
              jsonPkcs7,
              pdfPkcs7,
            });
            setShowEimzo(false);
            onUpdated();
          }}
        />
      )}
    </HrBusinessTripWorkflowShell>
  );
}
