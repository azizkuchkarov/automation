"use client";

import { useState } from "react";
import Link from "next/link";
import { useLocale } from "next-intl";
import { useQueryClient } from "@tanstack/react-query";
import { ExternalLink, Globe, Loader2, Upload, Gavel, Download } from "lucide-react";
import api, { getApiErrorMessage } from "@/lib/api";
import { downloadMarketingFile } from "@/lib/files";
import {
  MarketingRfqChannelRequest,
  rfqChannelLabel,
  uploadMarketingFile,
} from "@/lib/marketing";
import {
  marketingRecordKey,
  useInvalidateMarketingRecord,
  useMarketingRecord,
} from "@/lib/hooks/useMarketingRecord";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

interface Props {
  documentId: string;
  canEdit: boolean;
  acting: boolean;
  t: (key: string) => string;
}

export function MarketingStep3RfqPanel(props: Props) {
  return <MarketingStep4RfqPanel {...props} />;
}

export function MarketingStep4RfqPanel({ documentId, canEdit, acting, t }: Props) {
  const locale = useLocale();
  const queryClient = useQueryClient();
  const invalidateRecord = useInvalidateMarketingRecord(documentId);
  const { data: record, isLoading: loading } = useMarketingRecord(documentId);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [uploading, setUploading] = useState(false);
  const [downloading, setDownloading] = useState(false);
  const [commercialDeadline, setCommercialDeadline] = useState("");

  const patchRecord = (data: NonNullable<typeof record>) => {
    queryClient.setQueryData(marketingRecordKey(documentId), data);
  };

  const run = async (fn: () => Promise<void>) => {
    setBusy(true);
    setError("");
    try {
      await fn();
      await invalidateRecord();
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
      patchRecord(r.data);
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

  const completeTender = () => run(() =>
    api.post(`/marketing/records/by-document/${documentId}/rfq/channels/tenderweek/complete`).then(() => {})
  );

  const registerAndGenerate = () => run(async () => {
    const deadline = commercialDeadline || record?.rfqCommercialProposalDeadline?.slice(0, 10);
    if (!deadline) {
      setError(t("commercialDeadlineRequired"));
      return;
    }
    const r = await api.post(`/marketing/records/by-document/${documentId}/rfq/register-generate`, {
      commercialProposalDeadline: deadline,
    });
    patchRecord(r.data);
  });

  const onDownload = async () => {
    if (!record?.rfqDocumentStorageKey) return;
    setDownloading(true);
    setError("");
    try {
      await downloadMarketingFile(record.rfqDocumentStorageKey, record.rfqDocumentFileName);
    } catch (err) {
      setError(getApiErrorMessage(err, t("downloadError")));
    } finally {
      setDownloading(false);
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

      {(record.portalNumber?.startsWith("ATG-CP-") ?? false) && (
        <div className="rounded-lg border border-emerald-500/30 bg-emerald-500/5 px-3 py-2.5">
          <p className="text-[11px] font-semibold uppercase tracking-wide text-foreground/45">{t("registrationNumber")}</p>
          <p className="text-sm font-bold text-emerald-800 dark:text-emerald-300 mt-0.5 font-mono">{record.portalNumber}</p>
        </div>
      )}

      {canEdit && (
        <div className="rounded-lg border border-border/60 p-3 space-y-3">
          <Field label={t("commercialDeadline")}>
            <input
              type="date"
              className={inputClass}
              value={commercialDeadline || record.rfqCommercialProposalDeadline?.slice(0, 10) || ""}
              onChange={(e) => setCommercialDeadline(e.target.value)}
              required
            />
          </Field>
          <Button
            size="sm"
            disabled={acting || busy || !(commercialDeadline || record.rfqCommercialProposalDeadline)}
            onClick={registerAndGenerate}
          >
            {busy ? <Loader2 size={14} className="animate-spin mr-1.5" /> : null}
            {busy ? t("generatingRfq") : record.rfqDocumentStorageKey ? t("regenerateRfq") : t("generateRfq")}
          </Button>
        </div>
      )}

      <div className="rounded-lg border border-border/60 p-3 space-y-2">
        <p className="text-xs font-semibold text-foreground/70">{t("rfqDocument")}</p>
        {record.rfqDocumentFileName ? (
          <div className="flex flex-wrap items-center gap-2">
            <p className="text-sm font-medium text-emerald-700 dark:text-emerald-400 font-mono">
              {record.rfqDocumentFileName}
            </p>
            {record.rfqDocumentStorageKey && (
              <Button
                size="sm"
                variant="secondary"
                disabled={acting || downloading || busy}
                onClick={onDownload}
              >
                {downloading ? (
                  <Loader2 size={14} className="animate-spin mr-1.5" />
                ) : (
                  <Download size={14} className="mr-1.5" />
                )}
                {downloading ? t("downloading") : t("downloadRfq")}
              </Button>
            )}
          </div>
        ) : (
          <p className="text-xs text-amber-700/90">{t("rfqDocumentRequired")}</p>
        )}
        {record.rfqCommercialProposalDeadline && (
          <p className="text-xs text-foreground/55">
            {t("commercialDeadline")}: {new Date(record.rfqCommercialProposalDeadline).toLocaleDateString(locale)}
          </p>
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

      {canEdit && record.rfqDocumentStorageKey && channels.length === 0 && (
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
            <ChannelCard
              key={ch.id}
              channel={ch}
              locale={locale}
              t={t}
              canEdit={canEdit}
              acting={acting}
              busy={busy}
              onCompleteTender={completeTender}
            />
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

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-1.5 block">{label}</label>
      {children}
    </div>
  );
}

function ChannelCard({
  channel,
  locale,
  t,
  canEdit,
  acting,
  busy,
  onCompleteTender,
}: {
  channel: MarketingRfqChannelRequest;
  locale: string;
  t: (key: string) => string;
  canEdit: boolean;
  acting: boolean;
  busy: boolean;
  onCompleteTender: () => void;
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
          {isAtg ? t("atgCloseHint") : t("tenderEngineerHint")}
        </p>
      )}
      {!done && !isAtg && canEdit && (
        <Button size="sm" variant="secondary" className="mt-2" disabled={acting || busy} onClick={onCompleteTender}>
          {t("tenderComplete")}
        </Button>
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
