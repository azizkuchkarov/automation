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
import { Send } from "lucide-react";

export default function NewOutgoingLetterPage() {
  const t = useTranslations("dcs.outgoing");
  const locale = useLocale();
  const router = useRouter();
  const [allowed, setAllowed] = useState<boolean | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  const [title, setTitle] = useState("");
  const [titleRu, setTitleRu] = useState("");
  const [addresseeName, setAddresseeName] = useState("");
  const [attachmentFileName, setAttachmentFileName] = useState("");
  const [attachmentStorageKey, setAttachmentStorageKey] = useState("");
  const [requiresTranslation, setRequiresTranslation] = useState(false);

  useEffect(() => {
    api
      .get("/dcs/outgoing-letters/permissions")
      .then((r) => setAllowed(r.data.canCreate))
      .catch(() => setAllowed(false));
  }, []);

  const inputClass = dcsInputClass("outgoing");

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setSubmitting(true);
    try {
      const r = await api.post("/dcs/outgoing-letters", {
        title,
        titleRu: titleRu || null,
        addresseeName: addresseeName || null,
        attachmentFileName: attachmentFileName || null,
        attachmentStorageKey: attachmentStorageKey || null,
        requiresTranslation,
      });
      router.push(`/${locale}/automation/documents/${r.data.id}`);
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error;
      setError(msg ?? t("create.error"));
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
        {t("create.noAccess")}
      </div>
    );
  }

  return (
    <div className={cn("relative flex flex-col flex-1", dcsTheme.meshBg)}>
      <div className={cn("absolute inset-0 pointer-events-none", dcsTheme.gridOverlay)} aria-hidden />
      <DcsPageHeader
        title={t("create.title")}
        subtitle={t("create.subtitle")}
        breadcrumb={t("create.title")}
        icon={Send}
        iconClassName="bg-violet-500/10 text-violet-600"
        officeKind="outgoing"
      />
      <div className="relative flex-1 overflow-auto px-6 py-5">
        <DcsCreateFormCard
          kind="outgoing"
          onSubmit={submit}
          error={error}
          submitLabel={t("create.submit")}
          submittingLabel={t("create.submitting")}
          submitting={submitting}
        >
          <DcsFormField label={t("fields.subjectEn")}>
            <input className={inputClass} value={title} onChange={(e) => setTitle(e.target.value)} required />
          </DcsFormField>
          <DcsFormField label={t("fields.subjectRu")}>
            <input className={inputClass} value={titleRu} onChange={(e) => setTitleRu(e.target.value)} />
          </DcsFormField>
          <DcsFormField label={t("fields.addressee")}>
            <input className={inputClass} value={addresseeName} onChange={(e) => setAddresseeName(e.target.value)} />
          </DcsFormField>
          <DcsFormField label={t("sections.attachments")}>
            <DocumentFileUpload
              folder="outgoing-letters"
              disabled={submitting}
              onUploaded={(fileName, storageKey) => {
                setAttachmentFileName(fileName);
                setAttachmentStorageKey(storageKey);
              }}
              labels={{
                uploading: t("create.uploading"),
                attached: t("create.attached"),
                pick: t("create.pickFile"),
              }}
            />
            {attachmentFileName && <p className="text-xs text-foreground/50 mt-2">{attachmentFileName}</p>}
          </DcsFormField>
          <DcsCheckboxCard
            checked={requiresTranslation}
            onChange={setRequiresTranslation}
            label={t("create.requiresTranslation")}
            hint={t("create.requiresTranslationHint")}
          />
        </DcsCreateFormCard>
      </div>
    </div>
  );
}
