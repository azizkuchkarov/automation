"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { NotebookPen } from "lucide-react";
import api from "@/lib/api";
import { MemoDepartment, MemoUser } from "@/lib/memo";
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
import { deptLabel } from "@/lib/dcs";
import { cn } from "@/lib/utils";

export default function NewMemoPage() {
  const t = useTranslations("dcs.memo");
  const locale = useLocale();
  const router = useRouter();
  const [allowed, setAllowed] = useState<boolean | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  const [title, setTitle] = useState("");
  const [titleRu, setTitleRu] = useState("");
  const [attachmentFileName, setAttachmentFileName] = useState("");
  const [attachmentStorageKey, setAttachmentStorageKey] = useState("");
  const [requiresTranslation, setRequiresTranslation] = useState(false);

  const [topManagers, setTopManagers] = useState<MemoUser[]>([]);
  const [departments, setDepartments] = useState<MemoDepartment[]>([]);

  const [managerRecipients, setManagerRecipients] = useState<Record<string, boolean>>({});
  const [departmentRecipients, setDepartmentRecipients] = useState<Record<string, boolean>>({});

  useEffect(() => {
    api
      .get("/dcs/memos/permissions")
      .then((r) => {
        const canCreate = Boolean(r.data.canCreate);
        setAllowed(canCreate);
        if (canCreate) {
          Promise.all([api.get("/dcs/memos/top-managers"), api.get("/dcs/memos/departments")]).then(([tm, d]) => {
            setTopManagers(tm.data);
            setDepartments(d.data);
          });
        }
      })
      .catch(() => setAllowed(false));
  }, []);

  const inputClass = dcsInputClass("memo");

  const toggleManager = (id: string) => {
    setManagerRecipients((prev) => {
      if (id in prev) {
        const { [id]: _, ...rest } = prev;
        return rest;
      }
      return { ...prev, [id]: false };
    });
  };

  const toggleDepartment = (id: string) => {
    setDepartmentRecipients((prev) => {
      if (id in prev) {
        const { [id]: _, ...rest } = prev;
        return rest;
      }
      return { ...prev, [id]: false };
    });
  };

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setSubmitting(true);
    try {
      const recipients = [
        ...Object.entries(managerRecipients).map(([userId, forInformation]) => ({ userId, forInformation })),
        ...Object.entries(departmentRecipients).map(([departmentId, forInformation]) => ({
          departmentId,
          forInformation,
        })),
      ];

      const response = await api.post("/dcs/memos", {
        title,
        titleRu: titleRu || null,
        attachmentFileName: attachmentFileName || null,
        attachmentStorageKey: attachmentStorageKey || null,
        requiresTranslation,
        recipients,
      });
      router.push(`/${locale}/automation/documents/${response.data.id}`);
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
        icon={NotebookPen}
        iconClassName="bg-violet-500/10 text-violet-600"
        officeKind="memo"
      />
      <div className="relative flex-1 overflow-auto px-6 py-5">
        <DcsCreateFormCard
          kind="memo"
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
          <DcsFormField label={t("sections.attachments")}>
            <DocumentFileUpload
              folder="memos"
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

          <div className="rounded-xl border border-border/60 p-4 space-y-4">
            <p className="text-sm font-semibold">{t("create.recipientsTitle")}</p>

            <div className="space-y-2">
              <p className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45">
                {t("create.topManagers")}
              </p>
              <div className="grid md:grid-cols-2 gap-2">
                {topManagers.map((manager) => {
                  const selected = manager.id in managerRecipients;
                  return (
                    <div key={manager.id} className="rounded-xl border border-border/60 p-3">
                      <label className="flex items-center gap-2 text-sm cursor-pointer">
                        <input type="checkbox" checked={selected} onChange={() => toggleManager(manager.id)} />
                        <span>{manager.fullName}</span>
                      </label>
                      {selected && (
                        <label className="mt-2 flex items-center gap-2 text-xs text-foreground/60">
                          <input
                            type="checkbox"
                            checked={managerRecipients[manager.id]}
                            onChange={(e) =>
                              setManagerRecipients((prev) => ({ ...prev, [manager.id]: e.target.checked }))
                            }
                          />
                          {t("create.forInformation")}
                        </label>
                      )}
                    </div>
                  );
                })}
              </div>
            </div>

            <div className="space-y-2">
              <p className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45">
                {t("create.departments")}
              </p>
              <div className="grid md:grid-cols-2 gap-2">
                {departments.map((department) => {
                  const selected = department.id in departmentRecipients;
                  return (
                    <div key={department.id} className="rounded-xl border border-border/60 p-3">
                      <label className="flex items-center gap-2 text-sm cursor-pointer">
                        <input type="checkbox" checked={selected} onChange={() => toggleDepartment(department.id)} />
                        <span>{deptLabel(department.name, department.nameEn, locale)}</span>
                      </label>
                      {selected && (
                        <label className="mt-2 flex items-center gap-2 text-xs text-foreground/60">
                          <input
                            type="checkbox"
                            checked={departmentRecipients[department.id]}
                            onChange={(e) =>
                              setDepartmentRecipients((prev) => ({ ...prev, [department.id]: e.target.checked }))
                            }
                          />
                          {t("create.forInformation")}
                        </label>
                      )}
                    </div>
                  );
                })}
              </div>
            </div>
          </div>

        </DcsCreateFormCard>
      </div>
    </div>
  );
}
