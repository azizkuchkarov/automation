"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { useLocale } from "next-intl";
import { ExternalLink, Globe, Loader2, Upload, Gavel } from "lucide-react";
import api, { getApiErrorMessage } from "@/lib/api";
import {
  MarketingRecord,
  MarketingRfqChannelRequest,
  rfqChannelLabel,
  uploadMarketingFile,
} from "@/lib/marketing";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

interface Props {
  documentId: string;
  canEdit: boolean;
  acting: boolean;
  t: (key: string) => string;
}

export function MarketingStep4RfqPanel({ documentId, canEdit, acting, t }: Props) {
  const locale = useLocale();
  const [record, setRecord] = useState<MarketingRecord | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [uploading, setUploading] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const r = await api.get(`/marketing/records/by-document/${documentId}`);
      setRecord(r.data);
    } catch (err) {
      setError(getApiErrorMessage(err, t("loadError")));
    } finally {
      setLoading(false);
    }
  }, [documentId, t]);

  useEffect(() => {
    load();
  }, [load]);

  const run = async (fn: () => Promise<void>) => {
    setBusy(true);
    setError("");
    try {
      await fn();
      await load();
    } catch (err) {
      setError(getApiErrorMessage(err, t("error")));
    } finally {
      setBusy(false);
    }
  };

  const onUpload = async (file: File) => {
    setUploading(true);
    setError("");
    try {
      const uploaded = await uploadMarketingFile(file, "marketing/rfq");
      const r = await api.post(`/marketing/records/by-document/${documentId}/rfq/document`, {
        storageKey: uploaded.key,
        fileName: file.name,
      });
      setRecord(r.data);
    } catch (err) {
      setError(getApiErrorMessage(err, t("uploadError")));
    } finally {
      setUploading(false);
    }
  };

  const openAtg = () => run(() =>
    api.post(`/marketing/records/by-document/${documentId}/rfq/channels/atg-website`).then(() => {})
  );

  const openTender = () => run(() =>
    api.post(`/marketing/records/by-document/${documentId}/rfq/channels/tenderweek`).then(() => {})
  );

  if (loading) {
    return (
      <div className="flex items-center gap-2 text-sm text-foreground/50 py-4">
        <Loader2 size={16} className="animate-spin" />
        {t("loading")}
      </div>
    );
  }

  if (!record) return null;

  const channels = record.rfqChannelRequests ?? [];
  const hasOpenAtg = channels.some((c) => c.channel === "AtgWebsite" && c.status === "Open");
  const hasOpenTender = channels.some((c) => c.channel === "Tenderweek" && c.status === "Open");
  const allChannelsClosed = channels.length > 0 && channels.every((c) => c.status === "Completed");
  const inputClass = "w-full rounded-xl border border-border/70 bg-background px-3 py-2.5 text-sm";

  return (
    <div className="mt-4 space-y-4 rounded-xl border border-violet-500/25 bg-violet-500/[0.03] p-4">
      <div>
        <h4 className="text-sm font-bold text-foreground">{t("step4Title")}</h4>
        <p className="text-xs text-foreground/55 mt-1 leading-relaxed">{t("step4Hint")}</p>
      </div>

      {error && <p className="text-sm text-red-600">{error}</p>}

      <div className="rounded-lg border border-border/60 p-3 space-y-2">
        <p className="text-xs font-semibold text-foreground/70">{t("rfqDocument")}</p>
        {record.rfqDocumentFileName ? (
          <p className="text-sm font-medium text-emerald-700 dark:text-emerald-400 flex items-center gap-2">
            <Upload size={14} />
            {record.rfqDocumentFileName}
          </p>
        ) : (
          <p className="text-xs text-amber-700/90">{t("rfqDocumentRequired")}</p>
        )}
        {canEdit && (
          <div>
            <input
              type="file"
              id={`rfq-upload-${documentId}`}
              className="hidden"
              accept=".pdf,.doc,.docx,.xls,.xlsx"
              disabled={acting || uploading || busy}
              onChange={(e) => {
                const f = e.target.files?.[0];
                if (f) onUpload(f);
                e.target.value = "";
              }}
            />
            <Button
              size="sm"
              variant="secondary"
              disabled={acting || uploading || busy}
              onClick={() => document.getElementById(`rfq-upload-${documentId}`)?.click()}
            >
              {uploading ? <Loader2 size={14} className="animate-spin mr-1.5" /> : <Upload size={14} className="mr-1.5" />}
              {record.rfqDocumentFileName ? t("replaceRfq") : t("uploadRfq")}
            </Button>
          </div>
        )}
      </div>

      {canEdit && record.rfqDocumentStorageKey && (
        <div className="grid sm:grid-cols-2 gap-2">
          <Button
            size="sm"
            variant="secondary"
            disabled={acting || busy || hasOpenAtg}
            onClick={openAtg}
            className="justify-start"
          >
            <Globe size={14} className="mr-2 shrink-0" />
            {hasOpenAtg ? t("atgRequestOpen") : t("openAtgWebsite")}
          </Button>
          <Button
            size="sm"
            variant="secondary"
            disabled={acting || busy || hasOpenTender}
            onClick={openTender}
            className="justify-start"
          >
            <Gavel size={14} className="mr-2 shrink-0" />
            {hasOpenTender ? t("tenderRequestOpen") : t("openTenderweek")}
          </Button>
        </div>
      )}

      {channels.length > 0 && (
        <div className="space-y-2">
          <p className="text-xs font-semibold text-foreground/70">{t("channelRequests")}</p>
          {channels.map((ch) => (
            <ChannelCard key={ch.id} channel={ch} locale={locale} t={t} />
          ))}
        </div>
      )}

      {allChannelsClosed && (
        <p className="text-xs text-emerald-700 dark:text-emerald-400 border-l-2 border-emerald-500/50 pl-2.5">
          {t("channelsReady")}
        </p>
      )}

      {!canEdit && channels.length === 0 && (
        <p className={cn(inputClass, "text-foreground/45 text-center py-3")}>{t("readOnly")}</p>
      )}
    </div>
  );
}

function ChannelCard({
  channel,
  locale,
  t,
}: {
  channel: MarketingRfqChannelRequest;
  locale: string;
  t: (key: string) => string;
}) {
  const done = channel.status === "Completed";
  const isAtg = channel.channel === "AtgWebsite";

  return (
    <div
      className={cn(
        "rounded-lg border px-3 py-2.5 text-sm",
        done ? "border-emerald-500/30 bg-emerald-500/5" : "border-amber-500/30 bg-amber-500/5"
      )}
    >
      <div className="flex items-start justify-between gap-2">
        <div>
          <p className="font-semibold">{rfqChannelLabel(channel.channel, locale)}</p>
          {channel.externalNumber && (
            <p className="text-xs text-foreground/50 font-mono mt-0.5">{channel.externalNumber}</p>
          )}
          {channel.assignedUserName && (
            <p className="text-xs text-foreground/55 mt-0.5">{channel.assignedUserName}</p>
          )}
        </div>
        <span
          className={cn(
            "text-[10px] font-bold uppercase px-2 py-0.5 rounded-full shrink-0",
            done ? "bg-emerald-500/15 text-emerald-700" : "bg-amber-500/15 text-amber-700"
          )}
        >
          {done ? t("channelCompleted") : t("channelOpen")}
        </span>
      </div>
      {!done && (
        <p className="text-xs text-foreground/50 mt-2 leading-relaxed">
          {isAtg ? t("atgCloseHint") : t("tenderCloseHint")}
        </p>
      )}
      {channel.helpDeskTicketId && (
        <Link
          href={`/${locale}/helpdesk/tickets/${channel.helpDeskTicketId}`}
          className="inline-flex items-center gap-1 text-xs text-atg-blue hover:underline mt-2"
        >
          <ExternalLink size={12} />
          {t("viewHelpDesk")}
        </Link>
      )}
      {channel.workTaskId && (
        <Link
          href={`/${locale}/tasks/list`}
          className="inline-flex items-center gap-1 text-xs text-atg-blue hover:underline mt-2 ml-3"
        >
          <ExternalLink size={12} />
          {t("viewTask")}
        </Link>
      )}
    </div>
  );
}
