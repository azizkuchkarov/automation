"use client";

import { useEffect, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { Loader2, Upload, Download, FileText } from "lucide-react";
import api, { getApiErrorMessage } from "@/lib/api";
import {
  MarketingPlanRegistrationMethod,
  MarketingProcurementPlan,
  MarketingRecord,
  planRegistrationMethodLabel,
  uploadMarketingFile,
} from "@/lib/marketing";
import {
  marketingRecordKey,
  useInvalidateMarketingRecord,
  useMarketingRecord,
} from "@/lib/hooks/useMarketingRecord";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";
const METHODS: MarketingPlanRegistrationMethod[] = [
  "Tender",
  "BestOfferSelection",
  "LocalProcurement",
  "SmallProcurement",
  "QuotationRequest",
  "DirectContract",
];

interface Props {
  documentId: string;
  canEdit: boolean;
  acting: boolean;
  locale: string;
  t: (key: string) => string;
}

function latestPlan(record: MarketingRecord | null): MarketingProcurementPlan | undefined {
  if (!record?.plans?.length) return undefined;
  return [...record.plans].sort((a, b) => b.version - a.version)[0];
}

function methodHasTemplate(method: MarketingPlanRegistrationMethod) {
  return method !== "Tender";
}

export function MarketingStep6PlanPanel({ documentId, canEdit, acting, locale, t }: Props) {
  const queryClient = useQueryClient();
  const invalidateRecord = useInvalidateMarketingRecord(documentId);
  const { data: record, isLoading: loading } = useMarketingRecord(documentId);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [uploading, setUploading] = useState(false);
  const [downloading, setDownloading] = useState(false);
  const [selectedMethod, setSelectedMethod] = useState<MarketingPlanRegistrationMethod>("BestOfferSelection");

  useEffect(() => {
    const plan = latestPlan(record ?? null);
    if (plan?.registrationMethod) setSelectedMethod(plan.registrationMethod);
  }, [record]);

  const patchRecord = (data: MarketingRecord) => {
    queryClient.setQueryData(marketingRecordKey(documentId), data);
  };

  const plan = latestPlan(record ?? null);  const registeredMethod = plan?.registrationMethod;
  const effectiveMethod = registeredMethod ?? selectedMethod;
  const hasTemplate = methodHasTemplate(effectiveMethod);
  const methodChanged = registeredMethod != null && registeredMethod !== selectedMethod;
  const uploadInputId = `plan-upload-${documentId}`;

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

  const registerAndGenerate = () => run(async () => {
    const r = await api.post(`/marketing/records/by-document/${documentId}/plan/register-generate`, {
      registrationMethod: selectedMethod,
    });
    patchRecord(r.data);
    if (r.data?.plans?.length) {
      const p = latestPlan(r.data);
      if (p?.registrationMethod) setSelectedMethod(p.registrationMethod);
    }
  });
  const onDownloadTemplate = async () => {
    if (!plan?.registrationNumber) return;
    setDownloading(true);
    setError("");
    try {
      const response = await api.get(
        `/marketing/records/by-document/${documentId}/plan/download-template`,
        { responseType: "blob" }
      );
      const blob = response.data as Blob;
      const contentType = String(response.headers["content-type"] ?? blob.type);
      if (contentType.includes("application/json") || contentType.includes("text/plain")) {
        const text = await blob.text();
        try {
          const json = JSON.parse(text) as { error?: string };
          throw new Error(json.error ?? t("downloadError"));
        } catch {
          throw new Error(text || t("downloadError"));
        }
      }
      const fileName = plan.templateFileName ?? `${plan.registrationNumber}.docx`;
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = fileName;
      link.style.display = "none";
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.setTimeout(() => URL.revokeObjectURL(url), 1000);
    } catch (err) {
      setError(getApiErrorMessage(err, t("downloadError")));
    } finally {
      setDownloading(false);
    }
  };

  const onUpload = async (file: File) => {
    setUploading(true);
    setError("");
    try {
      const uploaded = await uploadMarketingFile(file, "marketing/plan");
      const r = await api.post(`/marketing/records/by-document/${documentId}/plan/document`, {
        storageKey: uploaded.key,
        fileName: file.name,
      });
      patchRecord(r.data);    } catch (err) {
      setError(getApiErrorMessage(err, t("uploadError")));
    } finally {
      setUploading(false);
    }
  };

  if (loading) {
    return (
      <div className="mt-4 flex items-center gap-2 text-xs text-foreground/50">
        <Loader2 size={14} className="animate-spin" />
        {t("loading")}
      </div>
    );
  }

  return (
    <div className="mt-4 space-y-4 rounded-xl border border-violet-500/20 bg-violet-500/[0.03] p-4">
      <div>
        <p className="text-xs font-semibold text-foreground/80">{t("title")}</p>
        <p className="text-xs text-foreground/55 mt-1 leading-relaxed">{t("hint")}</p>
      </div>

      {error && (
        <p className="text-xs text-red-600 dark:text-red-400">{error}</p>
      )}

      <div className="space-y-2">
        <label className="text-xs font-medium text-foreground/70">{t("methodLabel")}</label>
        <select
          className={cn(
            "w-full rounded-xl border border-border/70 bg-background px-3 py-2.5 text-sm",
            "focus:outline-none focus:ring-2 focus:ring-violet-500/25",
            !canEdit && "opacity-60 cursor-not-allowed"
          )}
          value={selectedMethod}
          disabled={!canEdit || busy || acting}
          onChange={(e) => setSelectedMethod(e.target.value as MarketingPlanRegistrationMethod)}
        >
          {METHODS.map((m) => (
            <option key={m} value={m}>
              {planRegistrationMethodLabel(m, locale)}
            </option>
          ))}
        </select>
      </div>

      {canEdit && (
        <Button size="sm" disabled={busy || acting} onClick={registerAndGenerate}>
          {busy ? <Loader2 size={14} className="mr-1.5 animate-spin" /> : <FileText size={14} className="mr-1.5" />}
          {methodHasTemplate(selectedMethod) ? t("registerGenerate") : t("registerOnly")}
        </Button>
      )}

      {methodChanged && (
        <p className="text-xs text-amber-700/90 dark:text-amber-400/90 border-l-2 border-amber-500/40 pl-2.5">
          {t("methodChangedHint")}
        </p>
      )}

      {plan?.registrationNumber && (
        <div className="rounded-lg border border-emerald-500/25 bg-emerald-500/8 px-3 py-2.5">
          <p className="text-[10px] font-bold uppercase tracking-wider text-emerald-700/70 dark:text-emerald-400/70">
            {t("registrationNumber")}
          </p>
          <p className="text-sm font-semibold font-mono text-emerald-800 dark:text-emerald-300 mt-0.5">
            {plan.registrationNumber}
          </p>
          <p className="text-xs text-foreground/50 mt-1">
            {planRegistrationMethodLabel(registeredMethod ?? selectedMethod, locale)}
          </p>
        </div>
      )}

      {plan?.registrationNumber && !hasTemplate && (
        <p className="text-xs text-foreground/55">{t("tenderNoTemplateHint")}</p>
      )}

      {plan?.registrationNumber && hasTemplate && (
        <div className="rounded-lg border border-border/60 bg-background/50 p-3 space-y-3">
          <p className="text-xs font-semibold text-foreground/70">{t("templateDocument")}</p>

          <div className="flex flex-wrap items-center gap-2">
            <p className="text-sm font-medium text-emerald-700 dark:text-emerald-400 font-mono">
              {plan.templateFileName ?? `${plan.registrationNumber}.docx`}
            </p>
            <Button
              size="sm"
              variant="secondary"
              disabled={acting || downloading || busy}
              onClick={onDownloadTemplate}
            >
                {downloading ? (
                  <Loader2 size={14} className="animate-spin mr-1.5" />
                ) : (
                  <Download size={14} className="mr-1.5" />
                )}
                {downloading ? t("downloading") : t("downloadTemplate")}
              </Button>
          </div>

          <p className="text-xs text-foreground/50">{t("editAndUploadHint")}</p>

          {canEdit && (
            <div className="pt-1 border-t border-border/50 space-y-2">
              <p className="text-xs font-medium text-foreground/70">{t("uploadCompleted")}</p>
              <input
                type="file"
                id={uploadInputId}
                className="hidden"
                accept=".doc,.docx,.pdf"
                disabled={uploading || acting || busy}
                onChange={(e) => {
                  const file = e.target.files?.[0];
                  if (file) void onUpload(file);
                  e.target.value = "";
                }}
              />
              <Button
                size="sm"
                variant="secondary"
                disabled={uploading || acting || busy}
                onClick={() => document.getElementById(uploadInputId)?.click()}
              >
                {uploading ? (
                  <Loader2 size={14} className="animate-spin mr-1.5" />
                ) : (
                  <Upload size={14} className="mr-1.5" />
                )}
                {plan.attachmentKey ? t("replaceDocument") : t("uploadDocument")}
              </Button>
              {plan.attachmentKey && (
                <p className="text-xs text-emerald-600 dark:text-emerald-400">{t("documentUploaded")}</p>
              )}
              {!plan.attachmentKey && (
                <p className="text-xs text-amber-700/90 dark:text-amber-400/90">{t("uploadRequired")}</p>
              )}
            </div>
          )}
        </div>
      )}

      {!canEdit && (
        <p className="text-xs text-foreground/50">{t("readOnly")}</p>
      )}
    </div>
  );
}
