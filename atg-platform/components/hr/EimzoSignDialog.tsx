"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { Loader2, PenLine, ShieldCheck } from "lucide-react";
import api, { getApiErrorMessage } from "@/lib/api";
import { fetchMe } from "@/lib/auth";
import { EimzoCertificate, listEimzoCertificates, signAttachedBase64, signDetachedBase64 } from "@/lib/eimzo/client";
import { HrPrimaryButton } from "@/components/hr/HrChrome";
import { hrInputClass } from "@/components/hr/hrTheme";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

export interface HrSigningPackage {
  jsonBase64: string;
  pdfBase64: string;
  payloadSha256: string;
  number: string;
}

interface Props {
  requestId: string;
  signingPackageUrl: string;
  onSigned: (jsonPkcs7: string, pdfPkcs7: string, comment: string) => Promise<void>;
  onCancel: () => void;
}

type Step = "loading" | "pinpp" | "cert" | "signing" | "done" | "error";

export function EimzoSignDialog({ requestId, signingPackageUrl, onSigned, onCancel }: Props) {
  const t = useTranslations("hr.leave.eimzo");
  const [step, setStep] = useState<Step>("loading");
  const [error, setError] = useState("");
  const [package_, setPackage] = useState<HrSigningPackage | null>(null);
  const [certs, setCerts] = useState<EimzoCertificate[]>([]);
  const [selectedCert, setSelectedCert] = useState<EimzoCertificate | null>(null);
  const [comment, setComment] = useState("");
  const [progress, setProgress] = useState("");
  const [pinpp, setPinpp] = useState("");
  const [savingPinpp, setSavingPinpp] = useState(false);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const [pkgRes, certList, me] = await Promise.all([
          api.get<HrSigningPackage>(signingPackageUrl),
          listEimzoCertificates(),
          fetchMe(),
        ]);
        if (cancelled) return;
        setPackage(pkgRes.data);
        setCerts(certList);
        if (!me.pinpp?.trim()) {
          setStep("pinpp");
          return;
        }
        setStep(certList.length ? "cert" : "error");
        if (!certList.length) setError(t("noCertificates"));
      } catch (err: unknown) {
        if (cancelled) return;
        setStep("error");
        setError(getApiErrorMessage(err) ?? t("loadError"));
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [requestId, signingPackageUrl, t]);

  const savePinpp = async () => {
    const value = pinpp.trim();
    if (value.length !== 14 || !/^\d+$/.test(value)) {
      setError(t("pinppInvalid"));
      return;
    }
    setSavingPinpp(true);
    setError("");
    try {
      await api.patch("/auth/me/pinpp", { pinpp: value });
      setStep(certs.length ? "cert" : "error");
      if (!certs.length) setError(t("noCertificates"));
    } catch (err: unknown) {
      setError(getApiErrorMessage(err) ?? t("pinppSaveError"));
    } finally {
      setSavingPinpp(false);
    }
  };

  const sign = async () => {
    if (!package_ || !selectedCert) return;
    setStep("signing");
    setError("");
    try {
      setProgress(t("signingJson"));
      setError("");
      const jsonPkcs7 = await signDetachedBase64(package_.jsonBase64, selectedCert);
      setProgress(t("signingPdf"));
      const pdfPkcs7 = await signAttachedBase64(package_.pdfBase64, selectedCert);
      setProgress(t("submitting"));
      await onSigned(jsonPkcs7, pdfPkcs7, comment);
      setStep("done");
    } catch (err: unknown) {
      setStep("cert");
      setError(getApiErrorMessage(err) ?? t("signError"));
    }
  };

  const inputClass =
    hrInputClass();

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <div className="w-full max-w-lg rounded-xl border border-border bg-surface shadow-xl">
        <div className="flex items-center gap-2 border-b border-border px-5 py-4">
          <ShieldCheck className="text-blue-700" size={20} />
          <h2 className="text-base font-semibold">{t("title")}</h2>
        </div>

        <div className="px-5 py-4 space-y-4">
          {step === "loading" && (
            <div className="flex items-center gap-2 text-sm text-foreground/50 py-6 justify-center">
              <Loader2 className="animate-spin" size={18} />
              {t("loading")}
            </div>
          )}

          {step === "error" && <p className="text-sm text-red-600">{error}</p>}

          {step === "pinpp" && (
            <>
              <p className="text-sm font-medium text-foreground">{t("pinppTitle")}</p>
              <p className="text-sm text-foreground/60">{t("pinppHint")}</p>
              <div>
                <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
                  {t("pinppTitle")}
                </label>
                <input
                  type="text"
                  inputMode="numeric"
                  maxLength={14}
                  value={pinpp}
                  onChange={(e) => setPinpp(e.target.value.replace(/\D/g, ""))}
                  placeholder={t("pinppPlaceholder")}
                  className={inputClass}
                  disabled={savingPinpp}
                />
              </div>
              {error && <p className="text-sm text-red-600">{error}</p>}
            </>
          )}

          {(step === "cert" || step === "signing") && package_ && (
            <>
              <p className="text-sm text-foreground/60">{t("hint", { number: package_.number })}</p>
              <div>
                <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
                  {t("selectCertificate")}
                </label>
                <select
                  className={cn(inputClass, "h-10")}
                  value={selectedCert ? certs.indexOf(selectedCert) : ""}
                  onChange={(e) => {
                    const idx = Number(e.target.value);
                    setSelectedCert(Number.isNaN(idx) || idx < 0 ? null : certs[idx] ?? null);
                  }}
                  disabled={step === "signing"}
                >
                  <option value="">{t("selectCertificatePlaceholder")}</option>
                  {certs.map((c, i) => (
                    <option key={i} value={i}>
                      {(c as { CN?: string }).CN ?? c.alias ?? c.name ?? `Key ${i + 1}`}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
                  {t("comment")}
                </label>
                <textarea
                  rows={2}
                  value={comment}
                  onChange={(e) => setComment(e.target.value)}
                  className={inputClass}
                  disabled={step === "signing"}
                />
              </div>
              {error && <p className="text-sm text-red-600">{error}</p>}
              {step === "signing" && (
                <div className="space-y-1">
                  <p className="text-sm text-blue-800 flex items-center gap-2">
                    <Loader2 className="animate-spin" size={14} />
                    {progress}
                  </p>
                  <p className="text-xs text-foreground/50">{t("enterPinHint")}</p>
                </div>
              )}
            </>
          )}

          {step === "done" && <p className="text-sm text-emerald-700 py-4">{t("success")}</p>}
        </div>

        <div className="flex justify-end gap-2 border-t border-border px-5 py-4">
          <Button variant="secondary" onClick={onCancel} disabled={step === "signing" || savingPinpp}>
            {t("cancel")}
          </Button>
          {step === "pinpp" && (
            <HrPrimaryButton
              disabled={pinpp.trim().length !== 14 || savingPinpp}
              onClick={savePinpp}
            >
              {savingPinpp ? <Loader2 className="animate-spin mr-1" size={14} /> : null}
              {t("pinppSave")}
            </HrPrimaryButton>
          )}
          {(step === "cert" || step === "signing") && (
            <HrPrimaryButton disabled={!selectedCert || step === "signing"} onClick={sign}>
              {step === "signing" ? (
                <Loader2 className="animate-spin mr-1" size={14} />
              ) : (
                <PenLine size={14} className="mr-1" />
              )}
              {t("signAndApprove")}
            </HrPrimaryButton>
          )}
        </div>
      </div>
    </div>
  );
}
