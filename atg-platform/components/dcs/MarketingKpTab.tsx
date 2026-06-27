"use client";

import { useRef, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import api, { getApiErrorMessage } from "@/lib/api";
import {
  MarketingOffer,
  MarketingOfferSource,
  MarketingRecord,
  uploadMarketingFile,
} from "@/lib/marketing";
import { DocumentFileUpload } from "@/components/dcs/DocumentFileUpload";
import { fileDownloadUrl } from "@/lib/files";
import { Button } from "@/components/ui/Button";

const SOURCES: MarketingOfferSource[] = ["Manual", "Vendor", "Distributor", "AtgSite", "Tenderweek", "OpenSource"];

interface Props {
  documentId: string;
  record: MarketingRecord;
  canEdit: boolean;
  onUpdated: (record: MarketingRecord) => void;
}

export function MarketingKpTab({ documentId, record, canEdit, onUpdated }: Props) {
  const t = useTranslations("dcs.marketing.kp");
  const locale = useLocale();
  const fileRef = useRef<HTMLInputElement>(null);
  const [acting, setActing] = useState(false);
  const [error, setError] = useState("");
  const [companyName, setCompanyName] = useState("");
  const [offerAmount, setOfferAmount] = useState("");
  const [currency, setCurrency] = useState("UZS");
  const [source, setSource] = useState<MarketingOfferSource>("Manual");
  const [attachmentKey, setAttachmentKey] = useState<string | undefined>();

  const reload = async () => {
    const r = await api.get(`/marketing/records/by-document/${documentId}`);
    onUpdated(r.data);
  };

  const addOffer = async () => {
    if (!companyName.trim()) return;
    setActing(true);
    setError("");
    try {
      await api.post(`/marketing/records/by-document/${documentId}/offers`, {
        companyName: companyName.trim(),
        offerAmount: offerAmount ? Number(offerAmount) : null,
        currency,
        vatIncluded: true,
        deliveryIncluded: false,
        source,
        attachmentKey: attachmentKey ?? null,
      });
      await reload();
      setCompanyName("");
      setOfferAmount("");
      setAttachmentKey(undefined);
    } catch (err) {
      setError(getApiErrorMessage(err, t("error")));
    } finally {
      setActing(false);
    }
  };

  const updateCompliance = async (offer: MarketingOffer, meets: boolean) => {
    setActing(true);
    setError("");
    try {
      await api.put(`/marketing/offers/${offer.id}/compliance`, {
        meetsTz: meets,
        rejectionReason: meets ? null : t("defaultReject"),
      });
      await reload();
    } catch (err) {
      setError(getApiErrorMessage(err, t("error")));
    } finally {
      setActing(false);
    }
  };

  const toggleAffiliation = async (offer: MarketingOffer) => {
    setActing(true);
    setError("");
    try {
      await api.put(`/marketing/offers/${offer.id}/affiliation`, {
        isAffiliated: !offer.isAffiliated,
        note: offer.affiliationNote ?? null,
      });
      await reload();
    } catch (err) {
      setError(getApiErrorMessage(err, t("error")));
    } finally {
      setActing(false);
    }
  };

  const onFilePick = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setActing(true);
    setError("");
    try {
      const result = await uploadMarketingFile(file, `marketing/${documentId}`);
      setAttachmentKey(result.key);
    } catch (err) {
      setError(getApiErrorMessage(err, t("uploadError")));
    } finally {
      setActing(false);
      if (fileRef.current) fileRef.current.value = "";
    }
  };

  const inputClass = "w-full rounded-lg border border-border/80 bg-background px-3 py-2 text-sm";
  const summary = record.offersSummary;

  return (
    <div className="space-y-5">
      {error && <p className="text-sm text-red-600">{error}</p>}

      {summary && (
        <div className="grid sm:grid-cols-3 gap-3 text-sm">
          <Stat label={t("compliant")} value={summary.compliantCount} />
          <Stat
            label={t("avgAmount")}
            value={summary.averageCompliantAmount != null ? `${summary.averageCompliantAmount.toLocaleString(locale)} UZS` : "—"}
          />
          <Stat label={t("affiliated")} value={summary.affiliatedCount} />
        </div>
      )}

      {canEdit && (
        <div className="rounded-xl border border-border/60 p-4 space-y-3">
          <h4 className="text-sm font-bold">{t("addOffer")}</h4>
          <input className={inputClass} placeholder={t("company")} value={companyName} onChange={(e) => setCompanyName(e.target.value)} />
          <div className="grid sm:grid-cols-2 gap-2">
            <input className={inputClass} placeholder={t("amount")} value={offerAmount} onChange={(e) => setOfferAmount(e.target.value)} />
            <input className={inputClass} placeholder={t("currency")} value={currency} onChange={(e) => setCurrency(e.target.value)} />
          </div>
          <select className={inputClass} value={source} onChange={(e) => setSource(e.target.value as MarketingOfferSource)}>
            {SOURCES.map((s) => (
              <option key={s} value={s}>{s}</option>
            ))}
          </select>
          <div className="flex flex-wrap items-center gap-2">
            <input ref={fileRef} type="file" className="text-sm" onChange={onFilePick} />
            {attachmentKey && <span className="text-xs text-emerald-600">{t("fileAttached")}</span>}
          </div>
          <Button size="sm" disabled={acting || !companyName.trim()} onClick={addOffer}>{t("saveOffer")}</Button>
        </div>
      )}

      <div className="rounded-xl border border-border/60 overflow-hidden">
        <div className="px-4 py-3 border-b border-border/50 bg-foreground/[0.02]">
          <h4 className="text-sm font-bold">{t("offers")} ({record.offers.length})</h4>
        </div>
        {record.offers.length === 0 ? (
          <p className="px-4 py-8 text-sm text-foreground/40 text-center">{t("noOffers")}</p>
        ) : (
          <ul className="divide-y divide-border/40">
            {record.offers.map((offer) => (
              <OfferCard
                key={offer.id}
                offer={offer}
                locale={locale}
                canEdit={canEdit}
                acting={acting}
                t={t}
                onCompliance={updateCompliance}
                onAffiliation={toggleAffiliation}
              />
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}

function Stat({ label, value }: { label: string; value: string | number }) {
  return (
    <div className="rounded-xl border border-border/60 p-3">
      <p className="text-[10px] font-bold uppercase tracking-wider text-foreground/40">{label}</p>
      <p className="font-semibold mt-1 tabular-nums">{value}</p>
    </div>
  );
}

function OfferCard({
  offer, locale, canEdit, acting, t, onCompliance, onAffiliation,
}: {
  offer: MarketingOffer;
  locale: string;
  canEdit: boolean;
  acting: boolean;
  t: ReturnType<typeof useTranslations>;
  onCompliance: (o: MarketingOffer, meets: boolean) => void;
  onAffiliation: (o: MarketingOffer) => void;
}) {
  return (
    <li className="px-4 py-4 text-sm space-y-2">
      <div className="flex items-start justify-between gap-2">
        <div>
          <p className="font-semibold">{offer.companyName}</p>
          {offer.offerAmount != null && (
            <p className="text-foreground/65 tabular-nums">
              {offer.offerAmount.toLocaleString(locale)} {offer.currency}
            </p>
          )}
        </div>
        <span className="text-[10px] uppercase tracking-wider text-foreground/40">{offer.source}</span>
      </div>
      <div className="flex flex-wrap gap-2">
        {offer.meetsTzRequirements === true && (
          <span className="text-xs px-2 py-0.5 rounded-full bg-emerald-500/10 text-emerald-700">{t("compliantTag")}</span>
        )}
        {offer.meetsTzRequirements === false && (
          <span className="text-xs px-2 py-0.5 rounded-full bg-red-500/10 text-red-700">{t("rejectedTag")}</span>
        )}
        {offer.isAffiliated && (
          <span className="text-xs px-2 py-0.5 rounded-full bg-amber-500/10 text-amber-700">{t("affiliatedTag")}</span>
        )}
      </div>
      {offer.attachmentKey && (
        <a
          href={fileDownloadUrl(offer.attachmentKey)}
          className="text-xs text-atg-blue hover:underline"
          target="_blank"
          rel="noreferrer"
        >
          {t("download")}
        </a>
      )}
      {canEdit && (
        <div className="flex flex-wrap gap-2 pt-1">
          {offer.meetsTzRequirements !== true && (
            <Button size="sm" variant="secondary" disabled={acting} onClick={() => onCompliance(offer, true)}>
              {t("markCompliant")}
            </Button>
          )}
          {offer.meetsTzRequirements !== false && (
            <Button size="sm" variant="secondary" disabled={acting} onClick={() => onCompliance(offer, false)}>
              {t("markRejected")}
            </Button>
          )}
          <Button size="sm" variant="secondary" disabled={acting} onClick={() => onAffiliation(offer)}>
            {offer.isAffiliated ? t("clearAffiliation") : t("markAffiliated")}
          </Button>
        </div>
      )}
    </li>
  );
}
