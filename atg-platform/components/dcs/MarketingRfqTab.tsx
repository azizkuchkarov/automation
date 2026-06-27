"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import api, { getApiErrorMessage } from "@/lib/api";
import {
  MarketingRecord,
  RfqDispatch,
  RfqDispatchType,
  rfqDispatchLabel,
} from "@/lib/marketing";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

interface Props {
  documentId: string;
  record: MarketingRecord;
  canEdit: boolean;
  onUpdated: (record: MarketingRecord) => void;
}

const DISPATCH_TYPES: RfqDispatchType[] = ["Vendor", "Distributor", "OpenSource", "AtgSite", "Tenderweek"];

export function MarketingRfqTab({ documentId, record, canEdit, onUpdated }: Props) {
  const t = useTranslations("dcs.marketing.rfq");
  const locale = useLocale();
  const [acting, setActing] = useState(false);
  const [error, setError] = useState("");
  const [dispatchType, setDispatchType] = useState<RfqDispatchType>("Vendor");
  const [recipientName, setRecipientName] = useState("");
  const [recipientEmail, setRecipientEmail] = useState("");
  const [notes, setNotes] = useState("");

  const markPrepared = async () => {
    setActing(true);
    setError("");
    try {
      const r = await api.post(`/marketing/records/by-document/${documentId}/rfq/prepare`);
      onUpdated(r.data);
    } catch (err) {
      setError(getApiErrorMessage(err, t("error")));
    } finally {
      setActing(false);
    }
  };

  const addDispatch = async () => {
    setActing(true);
    setError("");
    try {
      const r = await api.post(`/marketing/records/by-document/${documentId}/rfq/dispatch`, {
        dispatchType,
        recipientName: recipientName || null,
        recipientEmail: recipientEmail || null,
        notes: notes || null,
      });
      onUpdated(r.data);
      setRecipientName("");
      setRecipientEmail("");
      setNotes("");
    } catch (err) {
      setError(getApiErrorMessage(err, t("error")));
    } finally {
      setActing(false);
    }
  };

  const sendFollowup = async (dispatch: RfqDispatch) => {
    setActing(true);
    setError("");
    try {
      const r = await api.post(`/marketing/rfq/dispatches/${dispatch.id}/followup`, {
        phoneCallMade: true,
        notes: t("followupNote"),
      });
      onUpdated(r.data);
    } catch (err) {
      setError(getApiErrorMessage(err, t("error")));
    } finally {
      setActing(false);
    }
  };

  const inputClass = "w-full rounded-lg border border-border/80 bg-background px-3 py-2 text-sm";

  return (
    <div className="space-y-5">
      {error && <p className="text-sm text-red-600">{error}</p>}

      <div className="rounded-xl border border-border/60 p-4">
        <h4 className="text-sm font-bold mb-3">{t("status")}</h4>
        <div className="grid sm:grid-cols-2 gap-2 text-sm">
          <Flag label={t("prepared")} ok={!!record.rfqPreparedAt} />
          <Flag label={t("atgSite")} ok={record.rfqPublishedAtgSite} />
          <Flag label={t("tenderweek")} ok={record.rfqPublishedTenderweek} />
          <Flag label={t("vendor")} ok={record.rfqSentToVendor} />
          <Flag label={t("distributor")} ok={record.rfqSentToDistributor} />
          <Flag label={t("openSearch")} ok={record.rfqOpenSearchDone} />
        </div>
        {canEdit && !record.rfqPreparedAt && (
          <Button size="sm" className="mt-4" disabled={acting} onClick={markPrepared}>
            {t("markPrepared")}
          </Button>
        )}
      </div>

      {canEdit && (
        <div className="rounded-xl border border-border/60 p-4 space-y-3">
          <h4 className="text-sm font-bold">{t("addDispatch")}</h4>
          <select className={inputClass} value={dispatchType} onChange={(e) => setDispatchType(e.target.value as RfqDispatchType)}>
            {DISPATCH_TYPES.map((dt) => (
              <option key={dt} value={dt}>{rfqDispatchLabel(dt, locale)}</option>
            ))}
          </select>
          <input className={inputClass} placeholder={t("recipientName")} value={recipientName} onChange={(e) => setRecipientName(e.target.value)} />
          <input className={inputClass} placeholder={t("recipientEmail")} value={recipientEmail} onChange={(e) => setRecipientEmail(e.target.value)} />
          <textarea className={cn(inputClass, "min-h-[60px]")} placeholder={t("notes")} value={notes} onChange={(e) => setNotes(e.target.value)} />
          <Button size="sm" disabled={acting} onClick={addDispatch}>{t("sendDispatch")}</Button>
        </div>
      )}

      <div className="rounded-xl border border-border/60 overflow-hidden">
        <div className="px-4 py-3 border-b border-border/50 bg-foreground/[0.02]">
          <h4 className="text-sm font-bold">{t("dispatches")} ({record.rfqDispatches.length})</h4>
        </div>
        {record.rfqDispatches.length === 0 ? (
          <p className="px-4 py-8 text-sm text-foreground/40 text-center">{t("noDispatches")}</p>
        ) : (
          <ul className="divide-y divide-border/40">
            {record.rfqDispatches.map((d) => (
              <li key={d.id} className="px-4 py-3 text-sm space-y-1">
                <div className="flex items-center justify-between gap-2">
                  <span className="font-semibold">{rfqDispatchLabel(d.dispatchType, locale)}</span>
                  <span className="text-xs text-foreground/45 tabular-nums">
                    {new Date(d.sentAt).toLocaleDateString(locale)}
                  </span>
                </div>
                {d.recipientName && <p className="text-foreground/65">{d.recipientName}</p>}
                {d.recipientEmail && <p className="text-foreground/50 text-xs">{d.recipientEmail}</p>}
                {d.responseReceivedAt ? (
                  <p className="text-xs text-emerald-600">{t("responseReceived")}</p>
                ) : canEdit && !d.followupSentAt ? (
                  <Button size="sm" variant="secondary" disabled={acting} onClick={() => sendFollowup(d)}>
                    {t("sendFollowup")}
                  </Button>
                ) : d.followupSentAt ? (
                  <p className="text-xs text-amber-600">{t("followupSent")}</p>
                ) : null}
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}

function Flag({ label, ok }: { label: string; ok: boolean }) {
  return (
    <div className={cn("flex items-center gap-2 px-2 py-1.5 rounded-lg", ok ? "bg-emerald-500/10 text-emerald-700" : "bg-foreground/[0.04] text-foreground/45")}>
      <span className={cn("w-2 h-2 rounded-full", ok ? "bg-emerald-500" : "bg-foreground/20")} />
      {label}
    </div>
  );
}
