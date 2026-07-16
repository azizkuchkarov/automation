"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import api from "@/lib/api";
import { DcsPageHeader } from "@/components/dcs/DcsPageHeader";
import { DocumentFileUpload } from "@/components/dcs/DocumentFileUpload";
import { dcsTheme } from "@/components/dcs/dcsTheme";
import {
  DcsCheckboxCard,
  DcsCreateFormCard,
  DcsFormField,
  DcsWorkflowLoading,
  dcsInputClass,
} from "@/components/dcs/DcsWorkflowUI";
import { cn } from "@/lib/utils";
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
  const [attachmentStorageKey, setAttachmentStorageKey] = useState("");
  const [requiresTranslation, setRequiresTranslation] = useState(false);

  useEffect(() => {
    api
      .get("/dcs/incoming-letters/permissions")
      .then((r) => setAllowed(r.data.isRegistrar))
      .catch(() => setAllowed(false));
  }, []);

  const inputClass = dcsInputClass("incoming");

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
        attachmentStorageKey: attachmentStorageKey || null,
        translationRequestCount: 0,
        requiresTranslation,
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
    return <DcsWorkflowLoading label={t("loading")} />;
  }

  if (!allowed) {
    return (
      <div className="flex-1 flex items-center justify-center text-foreground/50 text-sm px-6 text-center">
        {t("register.noAccess")}
      </div>
    );
  }

  return (
    <div className={cn("relative flex flex-col flex-1", dcsTheme.meshBg)}>
      <div className={cn("absolute inset-0 pointer-events-none", dcsTheme.gridOverlay)} aria-hidden />
      <DcsPageHeader
        title={t("register.title")}
        subtitle={t("register.subtitle")}
        breadcrumb={t("register.title")}
        icon={Inbox}
        iconClassName="bg-sky-500/10 text-sky-600"
        officeKind="incoming"
      />
      <div className="relative flex-1 overflow-auto px-6 py-5">
        <DcsCreateFormCard
          kind="incoming"
          onSubmit={submit}
          error={error}
          submitLabel={t("register.submit")}
          submittingLabel={t("register.submitting")}
          submitting={submitting}
        >
          <DcsFormField label={t("fields.subjectEn")}>
            <input className={inputClass} value={title} onChange={(e) => setTitle(e.target.value)} required />
          </DcsFormField>
          <DcsFormField label={t("fields.subjectRu")}>
            <input className={inputClass} value={titleRu} onChange={(e) => setTitleRu(e.target.value)} />
          </DcsFormField>
          <div className="grid sm:grid-cols-2 gap-4">
            <DcsFormField label={t("fields.inNum")}>
              <input
                className={inputClass}
                value={incomingNumber}
                onChange={(e) => setIncomingNumber(e.target.value)}
              />
            </DcsFormField>
            <DcsFormField label={t("fields.inDate")}>
              <input
                type="date"
                className={inputClass}
                value={incomingDate}
                onChange={(e) => setIncomingDate(e.target.value)}
              />
            </DcsFormField>
          </div>
          <DcsFormField label={t("fields.recordBook")}>
            <input className={inputClass} value={recordBook} onChange={(e) => setRecordBook(e.target.value)} />
          </DcsFormField>
          <DcsFormField label={t("fields.otherSenders")}>
            <input className={inputClass} value={senderName} onChange={(e) => setSenderName(e.target.value)} />
          </DcsFormField>
          <DcsFormField label={t("fields.receiver")}>
            <input className={inputClass} value={receiverName} onChange={(e) => setReceiverName(e.target.value)} />
          </DcsFormField>
          <DcsFormField label={t("sections.attachments")}>
            <DocumentFileUpload
              folder="incoming-letters"
              disabled={submitting}
              onUploaded={(fileName, storageKey) => {
                setAttachmentFileName(fileName);
                setAttachmentStorageKey(storageKey);
              }}
              labels={{
                uploading: t("register.uploading"),
                attached: t("register.attached"),
                pick: t("register.pickFile"),
              }}
            />
            {attachmentFileName && (
              <p className="text-xs text-foreground/50 mt-2">{attachmentFileName}</p>
            )}
          </DcsFormField>
          <DcsCheckboxCard
            checked={requiresTranslation}
            onChange={setRequiresTranslation}
            label={t("register.requiresTranslation")}
            hint={t("register.requiresTranslationHint")}
          />
        </DcsCreateFormCard>
      </div>
    </div>
  );
}
