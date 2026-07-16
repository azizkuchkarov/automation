"use client";

import { useState } from "react";
import { Check, Loader2, Plus, Upload, X } from "lucide-react";
import api, { getApiErrorMessage } from "@/lib/api";
import { MarketingInitiatorReviewStatus, MarketingOffer, uploadMarketingFile } from "@/lib/marketing";
import {
  useInvalidateMarketingRecord,
  useMarketingRecord,
} from "@/lib/hooks/useMarketingRecord";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";



interface Props {

  documentId: string;

  canEdit: boolean;

  canReview: boolean;

  canReviewEngineer: boolean;

  acting: boolean;

  t: (key: string) => string;

}



export function MarketingStep4ProposalsPanel({

  documentId,

  canEdit,

  canReview,

  canReviewEngineer,

  acting,

  t,

}: Props) {
  const invalidateRecord = useInvalidateMarketingRecord(documentId);
  const { data: record, isLoading: loading } = useMarketingRecord(documentId);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [companyName, setCompanyName] = useState("");
  const [price, setPrice] = useState("");
  const [attachmentKey, setAttachmentKey] = useState<string | undefined>();
  const [fileName, setFileName] = useState("");
  const [engineerRejectComment, setEngineerRejectComment] = useState<Record<string, string>>({});
  const [initiatorRejectComment, setInitiatorRejectComment] = useState<Record<string, string>>({});

  const refreshRecord = async () => {
    await invalidateRecord();
  };

  const addProposal = async () => {

    if (!companyName.trim()) return;

    setBusy(true);

    setError("");

    try {

      await api.post(`/marketing/records/by-document/${documentId}/offers`, {

        companyName: companyName.trim(),

        offerAmount: price ? Number(price) : null,

        currency: "UZS",

        vatIncluded: false,

        deliveryIncluded: false,

        source: "Manual",

        attachmentKey,

      });

      setCompanyName("");

      setPrice("");

      setAttachmentKey(undefined);

      setFileName("");

      await refreshRecord();

    } catch (err) {

      setError(getApiErrorMessage(err, t("error")));

    } finally {

      setBusy(false);

    }

  };



  const reviewEngineer = async (offerId: string, action: "approve" | "reject") => {

    setBusy(true);

    setError("");

    try {

      await api.post(`/marketing/offers/${offerId}/engineer-review`, {

        action,

        comment: action === "reject" ? engineerRejectComment[offerId] : undefined,

      });

      await refreshRecord();

    } catch (err) {

      setError(getApiErrorMessage(err, t("error")));

    } finally {

      setBusy(false);

    }

  };



  const reviewInitiator = async (offerId: string, action: "approve" | "reject") => {

    setBusy(true);

    setError("");

    try {

      await api.post(`/marketing/offers/${offerId}/initiator-review`, {

        action,

        comment: action === "reject" ? initiatorRejectComment[offerId] : undefined,

      });

      await refreshRecord();

    } catch (err) {

      setError(getApiErrorMessage(err, t("error")));

    } finally {

      setBusy(false);

    }

  };



  const onUpload = async (file: File) => {

    setBusy(true);

    try {

      const uploaded = await uploadMarketingFile(file, "marketing/offers");

      setAttachmentKey(uploaded.key);

      setFileName(file.name);

    } catch (err) {

      setError(getApiErrorMessage(err, t("uploadError")));

    } finally {

      setBusy(false);

    }

  };



  if (loading) {

    return (

      <div className="flex items-center gap-2 text-sm text-foreground/50 py-4">

        <Loader2 size={16} className="animate-spin" />

        {t("loading")}

      </div>

    );

  }



  const offers = record?.offers ?? [];

  const inputClass = "w-full rounded-xl border border-border/70 bg-background px-3 py-2 text-sm";



  return (

    <div className="mt-4 space-y-4 rounded-xl border border-violet-500/25 bg-violet-500/[0.03] p-4">

      <div>

        <h4 className="text-sm font-bold">{t("proposalsTitle")}</h4>

        <p className="text-xs text-foreground/55 mt-1">{t("proposalsHint")}</p>

      </div>



      {error && <p className="text-sm text-red-600">{error}</p>}



      {canEdit && (

        <div className="rounded-lg border border-border/60 p-3 space-y-3">

          <p className="text-xs font-semibold text-foreground/70 flex items-center gap-1.5">

            <Plus size={14} />

            {t("addProposal")}

          </p>

          <div className="grid gap-3 lg:grid-cols-[minmax(0,1.4fr)_minmax(160px,0.7fr)_auto_auto]">

            <input

              className={inputClass}

              placeholder={t("companyName")}

              value={companyName}

              onChange={(e) => setCompanyName(e.target.value)}

            />

            <input

              className={inputClass}

              type="number"

              placeholder={t("price")}

              value={price}

              onChange={(e) => setPrice(e.target.value)}

            />

            <div className="flex items-center gap-2 flex-wrap">

              <input

                type="file"

                id={`prop-upload-${documentId}`}

                className="hidden"

                accept=".pdf,.doc,.docx"

                onChange={(e) => {

                  const f = e.target.files?.[0];

                  if (f) onUpload(f);

                  e.target.value = "";

                }}

              />

              <Button

                size="sm"

                variant="secondary"

                disabled={acting || busy}

                onClick={() => document.getElementById(`prop-upload-${documentId}`)?.click()}

              >

                <Upload size={14} className="mr-1.5" />

                {fileName || t("techDocument")}

              </Button>

            </div>

            <Button size="sm" disabled={acting || busy || !companyName.trim()} onClick={addProposal}>

              <Plus size={14} className="mr-1.5" />

              {t("add")}

            </Button>

          </div>

        </div>

      )}



      <div className="space-y-2">

        {offers.length === 0 ? (

          <p className="text-xs text-foreground/45 text-center py-4">{t("noProposals")}</p>

        ) : (

          <div className="overflow-x-auto rounded-lg border border-border/60 bg-background">

            <table className="min-w-full text-sm">

              <thead className="bg-muted/40 text-[11px] uppercase tracking-wide text-foreground/55">

                <tr>

                  <th className="px-4 py-3 text-left">{t("companyName")}</th>

                  <th className="px-4 py-3 text-left">{t("techDocument")}</th>

                  <th className="px-4 py-3 text-left">{t("price")}</th>

                  <th className="px-4 py-3 text-left">{t("marketingEngineerApprove")}</th>

                  <th className="px-4 py-3 text-left">{t("tasApprove")}</th>

                </tr>

              </thead>

              <tbody>

                {offers.map((offer) => (

                  <ProposalRow

                    key={offer.id}

                    offer={offer}

                    marketingEngineerName={
                      offer.engineerReviewedByName
                      ?? record?.marketingExecutorName
                      ?? "—"
                    }

                    requestorName={
                      offer.initiatorReviewedByName
                      ?? record?.tasResponsibleName
                      ?? record?.initiatorFullName
                      ?? "—"
                    }

                    canReviewEngineer={canReviewEngineer}

                    canReviewInitiator={canReview}

                    acting={acting}

                    busy={busy}

                    engineerRejectComment={engineerRejectComment[offer.id] ?? ""}

                    initiatorRejectComment={initiatorRejectComment[offer.id] ?? ""}

                    onEngineerRejectCommentChange={(v) =>

                      setEngineerRejectComment((s) => ({ ...s, [offer.id]: v }))

                    }

                    onInitiatorRejectCommentChange={(v) =>

                      setInitiatorRejectComment((s) => ({ ...s, [offer.id]: v }))

                    }

                    onReviewEngineer={reviewEngineer}

                    onReviewInitiator={reviewInitiator}

                    t={t}

                  />

                ))}

              </tbody>

            </table>

          </div>

        )}

      </div>

    </div>

  );

}



function ProposalRow({
  offer,
  marketingEngineerName,
  requestorName,
  canReviewEngineer,
  canReviewInitiator,
  acting,
  busy,
  engineerRejectComment,
  initiatorRejectComment,
  onEngineerRejectCommentChange,
  onInitiatorRejectCommentChange,
  onReviewEngineer,
  onReviewInitiator,
  t,
}: {
  offer: MarketingOffer;
  marketingEngineerName: string;
  requestorName: string;
  canReviewEngineer: boolean;
  canReviewInitiator: boolean;
  acting: boolean;
  busy: boolean;
  engineerRejectComment: string;
  initiatorRejectComment: string;
  onEngineerRejectCommentChange: (v: string) => void;
  onInitiatorRejectCommentChange: (v: string) => void;
  onReviewEngineer: (id: string, action: "approve" | "reject") => void;
  onReviewInitiator: (id: string, action: "approve" | "reject") => void;
  t: (key: string) => string;
}) {
  return (
    <tr className="border-t border-slate-100 align-top dark:border-white/[0.06]">
      <td className="px-4 py-4">
        <p className="font-semibold text-slate-900 dark:text-slate-50">{offer.companyName}</p>
      </td>
      <td className="px-4 py-4">
        {offer.attachmentKey ? (
          <span className="inline-flex rounded-md bg-sky-50 px-2.5 py-1 text-xs font-semibold text-sky-700 ring-1 ring-sky-200 dark:bg-sky-500/10 dark:text-sky-300 dark:ring-sky-500/30">
            {t("techDocAttached")}
          </span>
        ) : (
          <span className="text-xs text-slate-400">{t("notUploaded")}</span>
        )}
      </td>
      <td className="px-4 py-4">
        <span className="font-semibold tabular-nums text-slate-900 dark:text-slate-50">
          {offer.offerAmount != null
            ? `${offer.offerAmount.toLocaleString()} ${offer.currency}`
            : "—"}
        </span>
      </td>
      <td className="min-w-[220px] px-4 py-4">
        <ReviewCell
          personName={marketingEngineerName}
          personRole={t("marketingEngineerRole")}
          status={offer.engineerReviewStatus}
          reviewedAt={offer.engineerReviewedAt}
          comment={offer.engineerReviewComment}
          canReview={canReviewEngineer}
          acting={acting}
          busy={busy}
          rejectComment={engineerRejectComment}
          onRejectCommentChange={onEngineerRejectCommentChange}
          onReview={(action) => onReviewEngineer(offer.id, action)}
          t={t}
        />
      </td>
      <td className="min-w-[220px] px-4 py-4">
        <ReviewCell
          personName={requestorName}
          personRole={t("requestorRole")}
          status={offer.initiatorReviewStatus}
          reviewedAt={offer.initiatorReviewedAt}
          comment={offer.initiatorReviewComment}
          canReview={canReviewInitiator}
          acting={acting}
          busy={busy}
          rejectComment={initiatorRejectComment}
          onRejectCommentChange={onInitiatorRejectCommentChange}
          onReview={(action) => onReviewInitiator(offer.id, action)}
          t={t}
        />
      </td>
    </tr>
  );
}

function ReviewCell({
  personName,
  personRole,
  status,
  reviewedAt,
  comment,
  canReview,
  acting,
  busy,
  rejectComment,
  onRejectCommentChange,
  onReview,
  t,
}: {
  personName: string;
  personRole: string;
  status: MarketingInitiatorReviewStatus;
  reviewedAt?: string;
  comment?: string;
  canReview: boolean;
  acting: boolean;
  busy: boolean;
  rejectComment: string;
  onRejectCommentChange: (v: string) => void;
  onReview: (action: "approve" | "reject") => void;
  t: (key: string) => string;
}) {
  const initials =
    personName
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((p) => p[0]?.toUpperCase() ?? "")
      .join("") || "?";

  return (
    <div
      className={cn(
        "space-y-2.5 rounded-xl border p-3",
        status === "Approved" &&
          "border-emerald-200 bg-emerald-50/80 dark:border-emerald-500/30 dark:bg-emerald-500/10",
        status === "Rejected" &&
          "border-red-200 bg-red-50/80 dark:border-red-500/30 dark:bg-red-500/10",
        status === "Pending" &&
          "border-slate-200 bg-slate-50/80 dark:border-white/10 dark:bg-white/[0.03]",
      )}
    >
      <div className="flex min-w-0 items-center gap-2.5">
        <div
          className={cn(
            "flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-[11px] font-bold",
            status === "Approved" && "bg-emerald-500 text-white",
            status === "Rejected" && "bg-red-500 text-white",
            status === "Pending" &&
              "bg-slate-200 text-slate-600 dark:bg-white/10 dark:text-slate-300",
          )}
        >
          {status === "Approved" ? (
            <Check size={14} strokeWidth={3} />
          ) : status === "Rejected" ? (
            <X size={14} strokeWidth={3} />
          ) : (
            initials
          )}
        </div>
        <div className="min-w-0 flex-1">
          <p className="truncate text-sm font-semibold text-slate-900 dark:text-slate-50">
            {personName}
          </p>
          <p className="truncate text-[11px] text-slate-500">{personRole}</p>
        </div>
      </div>

      <div
        className={cn(
          "inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-[11px] font-bold uppercase tracking-wide",
          status === "Approved" && "bg-emerald-500 text-white shadow-sm shadow-emerald-500/25",
          status === "Rejected" && "bg-red-500 text-white shadow-sm shadow-red-500/25",
          status === "Pending" &&
            "bg-amber-100 text-amber-800 ring-1 ring-amber-200 dark:bg-amber-500/15 dark:text-amber-200 dark:ring-amber-500/30",
        )}
      >
        {status === "Approved" && <Check size={12} strokeWidth={3} />}
        {status === "Rejected" && <X size={12} strokeWidth={3} />}
        {t(`status.${status}`)}
      </div>

      {reviewedAt && (
        <p className="text-[10px] tabular-nums text-slate-400">
          {new Date(reviewedAt).toLocaleString(undefined, {
            day: "2-digit",
            month: "short",
            hour: "2-digit",
            minute: "2-digit",
          })}
        </p>
      )}

      {canReview && status === "Pending" && (
        <div className="space-y-2 pt-0.5">
          <input
            className="w-full rounded-lg border border-slate-200 bg-white px-2.5 py-2 text-xs focus:border-sky-400 focus:outline-none focus:ring-2 focus:ring-sky-500/20 dark:border-white/10 dark:bg-white/[0.04]"
            placeholder={t("rejectReason")}
            value={rejectComment}
            onChange={(e) => onRejectCommentChange(e.target.value)}
          />
          <div className="grid grid-cols-2 gap-2">
            <button
              type="button"
              disabled={acting || busy}
              onClick={() => onReview("approve")}
              className="inline-flex items-center justify-center gap-1.5 rounded-lg bg-emerald-600 px-3 py-2 text-xs font-bold text-white shadow-sm shadow-emerald-600/25 transition hover:bg-emerald-500 disabled:opacity-50"
            >
              <Check size={14} strokeWidth={2.5} />
              {t("approve")}
            </button>
            <button
              type="button"
              disabled={acting || busy || !rejectComment.trim()}
              onClick={() => onReview("reject")}
              className="inline-flex items-center justify-center gap-1.5 rounded-lg bg-white px-3 py-2 text-xs font-bold text-red-600 ring-1 ring-red-200 transition hover:bg-red-50 disabled:opacity-50 dark:bg-transparent dark:ring-red-500/40 dark:hover:bg-red-500/10"
            >
              <X size={14} strokeWidth={2.5} />
              {t("reject")}
            </button>
          </div>
        </div>
      )}

      {comment && (
        <p className="rounded-md border-l-2 border-slate-300 bg-white/70 px-2 py-1.5 text-xs text-slate-600 dark:border-white/20 dark:bg-white/[0.03] dark:text-slate-300">
          {comment}
        </p>
      )}
    </div>
  );
}

