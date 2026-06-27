"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import api from "@/lib/api";
import { DcsPageHeader } from "@/components/dcs/DcsPageHeader";
import { Button } from "@/components/ui/Button";
import { Inbox } from "lucide-react";

export default function RegisterIncomingLetterPage() {
  const t = useTranslations("dcs.incoming");
  const locale = useLocale();
  const router = useRouter();
  const [allowed, setAllowed] = useState<boolean | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  const [title, setTitle] = useState("");
  const [titleRu, setTitleRu] = useState("");
  const [incomingNumber, setIncomingNumber] = useState("");
  const [incomingDate, setIncomingDate] = useState("");
  const [recordBook, setRecordBook] = useState("");
  const [senderName, setSenderName] = useState("");
  const [receiverName, setReceiverName] = useState("");
  const [attachmentFileName, setAttachmentFileName] = useState("");

  useEffect(() => {
    api.get("/dcs/incoming-letters/permissions").then((r) => {
      setAllowed(r.data.isRegistrar);
    }).catch(() => setAllowed(false));
  }, []);

  const inputClass =
    "w-full rounded-lg border border-border/80 bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-atg-teal/30";

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setSubmitting(true);
    try {
      const r = await api.post("/dcs/incoming-letters", {
        title,
        titleRu: titleRu || null,
        incomingNumber: incomingNumber || null,
        incomingDate: incomingDate || null,
        recordBook: recordBook || null,
        senderName: senderName || null,
        receiverName: receiverName || null,
        attachmentFileName: attachmentFileName || null,
        translationRequestCount: 0,
      });
      router.push(`/${locale}/automation/documents/${r.data.id}`);
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error;
      setError(msg ?? t("register.error"));
    } finally {
      setSubmitting(false);
    }
  };

  if (allowed === null) {
    return <div className="flex-1 flex items-center justify-center text-foreground/40 text-sm">{t("loading")}</div>;
  }

  if (!allowed) {
    return (
      <div className="flex-1 flex items-center justify-center text-foreground/50 text-sm px-6 text-center">
        {t("register.noAccess")}
      </div>
    );
  }

  return (
    <>
      <DcsPageHeader
        title={t("register.title")}
        subtitle={t("register.subtitle")}
        breadcrumb={t("register.title")}
        icon={Inbox}
        iconClassName="bg-sky-500/10 text-sky-600"
      />
      <div className="flex-1 overflow-auto px-6 py-5">
        <form onSubmit={submit} className="max-w-2xl rounded-xl border border-border/80 bg-surface p-6 space-y-4 shadow-sm">
          <Field label={t("fields.subjectEn")}><input className={inputClass} value={title} onChange={(e) => setTitle(e.target.value)} required /></Field>
          <Field label={t("fields.subjectRu")}><input className={inputClass} value={titleRu} onChange={(e) => setTitleRu(e.target.value)} /></Field>
          <div className="grid sm:grid-cols-2 gap-4">
            <Field label={t("fields.inNum")}><input className={inputClass} value={incomingNumber} onChange={(e) => setIncomingNumber(e.target.value)} /></Field>
            <Field label={t("fields.inDate")}><input type="date" className={inputClass} value={incomingDate} onChange={(e) => setIncomingDate(e.target.value)} /></Field>
          </div>
          <Field label={t("fields.recordBook")}><input className={inputClass} value={recordBook} onChange={(e) => setRecordBook(e.target.value)} /></Field>
          <Field label={t("fields.otherSenders")}><input className={inputClass} value={senderName} onChange={(e) => setSenderName(e.target.value)} /></Field>
          <Field label={t("fields.receiver")}><input className={inputClass} value={receiverName} onChange={(e) => setReceiverName(e.target.value)} /></Field>
          <Field label={t("sections.attachments")}><input className={inputClass} placeholder={t("register.fileName")} value={attachmentFileName} onChange={(e) => setAttachmentFileName(e.target.value)} /></Field>
          {error && <p className="text-sm text-red-600">{error}</p>}
          <Button type="submit" disabled={submitting}>{submitting ? t("register.submitting") : t("register.submit")}</Button>
        </form>
      </div>
    </>
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
