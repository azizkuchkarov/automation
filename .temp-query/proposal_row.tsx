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
