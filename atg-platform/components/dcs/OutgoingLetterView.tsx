"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { FileText, Paperclip } from "lucide-react";
import api from "@/lib/api";
import {
  currentOutgoingStepIndex,
  OutgoingLetter,
  OutgoingLetterPermissions,
  OutgoingLetterUser,
  OUTGOING_STEP_GROUPS,
  outgoingPhaseLabel,
  outgoingStepLabel,
  TRANSLATION_LANGUAGE_CODES,
  translationLanguageLabel,
  translationLanguagesLabel,
} from "@/lib/outgoingLetter";
import {
  DcsCompletedBanner,
  DcsDetailField,
  DcsDocumentHero,
  DcsErrorAlert,
  DcsSectionCard,
  DcsStatusSidebar,
  DcsWorkflowCard,
  DcsWorkflowLoading,
  DcsWorkflowShell,
  DcsWorkflowStepper,
  dcsInputClass,
} from "@/components/dcs/DcsWorkflowUI";
import { Button } from "@/components/ui/Button";
import { DocumentFileUpload } from "@/components/dcs/DocumentFileUpload";
import { fileDownloadUrl } from "@/lib/files";

interface Props {
  documentId: string;
}

export function OutgoingLetterView({ documentId }: Props) {
  const t = useTranslations("dcs.outgoing");
  const locale = useLocale();
  const router = useRouter();
  const [letter, setLetter] = useState<OutgoingLetter | null>(null);
  const [perms, setPerms] = useState<OutgoingLetterPermissions | null>(null);
  const [loading, setLoading] = useState(true);
  const [acting, setActing] = useState(false);
  const [error, setError] = useState("");

  const [deptHeads, setDeptHeads] = useState<OutgoingLetterUser[]>([]);
  const [topManagers, setTopManagers] = useState<OutgoingLetterUser[]>([]);
  const [coordinators, setCoordinators] = useState<OutgoingLetterUser[]>([]);
  const [deptHeadId, setDeptHeadId] = useState("");
  const [supervisingDeputyId, setSupervisingDeputyId] = useState("");
  const [firstDeputyId, setFirstDeputyId] = useState("");
  const [generalDirectorId, setGeneralDirectorId] = useState("");
  const [sourceLanguage, setSourceLanguage] = useState("");
  const [translatingLanguages, setTranslatingLanguages] = useState<string[]>([]);
  const [selectedCoordinators, setSelectedCoordinators] = useState<string[]>([]);
  const [revisionComment, setRevisionComment] = useState("");
  const [approvalComment, setApprovalComment] = useState("");

  const [editTitle, setEditTitle] = useState("");
  const [editTitleRu, setEditTitleRu] = useState("");
  const [editAddressee, setEditAddressee] = useState("");
  const [attachmentFileName, setAttachmentFileName] = useState("");
  const [attachmentStorageKey, setAttachmentStorageKey] = useState("");

  const load = useCallback(() => {
    setLoading(true);
    Promise.all([
      api.get(`/dcs/outgoing-letters/${documentId}`),
      api.get(`/dcs/outgoing-letters/permissions?documentId=${documentId}`),
    ])
      .then(([l, p]) => {
        setLetter(l.data);
        setPerms(p.data);
        setEditTitle(l.data.title);
        setEditTitleRu(l.data.titleRu ?? "");
        setEditAddressee(l.data.addresseeName ?? "");
        setAttachmentFileName(l.data.attachmentFileName ?? "");
        setAttachmentStorageKey(l.data.attachmentStorageKey ?? "");
        setDeptHeadId(l.data.deptHeadId ?? "");
        setSupervisingDeputyId(l.data.supervisingDeputyId ?? "");
        setFirstDeputyId(l.data.firstDeputyId ?? "");
        setGeneralDirectorId(l.data.generalDirectorId ?? "");
      })
      .finally(() => setLoading(false));
  }, [documentId]);

  useEffect(() => {
    load();
  }, [load]);

  useEffect(() => {
    if (!perms) return;
    if (perms.canSubmitToEds) api.get("/dcs/outgoing-letters/dept-heads").then((r) => setDeptHeads(r.data));
    if (perms.canSubmitToEds || perms.canManageSpecialistCoordination)
      api.get("/dcs/outgoing-letters/top-managers").then((r) => setTopManagers(r.data));
    if (perms.canManageSpecialistCoordination || perms.canManageDepartmentCoordination)
      api.get("/dcs/outgoing-letters/coordinators").then((r) => setCoordinators(r.data));
  }, [perms]);

  const act = async (fn: () => Promise<void>) => {
    setActing(true);
    setError("");
    try {
      await fn();
      load();
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error;
      setError(msg ?? t("error"));
    } finally {
      setActing(false);
    }
  };

  const inputClass = dcsInputClass("outgoing");

  if (loading || !letter || !perms) {
    return <DcsWorkflowLoading label={t("loading")} />;
  }

  const stepIndex = currentOutgoingStepIndex(letter.phase);

  const toggleLang = (code: string) => {
    setTranslatingLanguages((prev) =>
      prev.includes(code) ? prev.filter((c) => c !== code) : [...prev, code]
    );
  };

  return (
    <DcsWorkflowShell kind="outgoing">
      <DcsDocumentHero
        kind="outgoing"
        number={letter.number}
        title={letter.title}
        titleRu={letter.titleRu}
        phaseLabel={outgoingPhaseLabel(letter.phase, locale)}
        backLabel={t("close")}
        printLabel={t("actions.print")}
        onBack={() => router.push(`/${locale}/automation/office/outgoing`)}
        onPrint={() => window.print()}
      />

      <DcsWorkflowStepper
        kind="outgoing"
        title={t("workflow.stepperTitle")}
        steps={OUTGOING_STEP_GROUPS.map((step) => ({ key: step.key, label: outgoingStepLabel(step.key, locale) }))}
        activeIndex={stepIndex}
      />

      <div className="grid lg:grid-cols-3 gap-5">
          <div className="lg:col-span-2 space-y-5">
            <DcsSectionCard kind="outgoing" title={t("sections.letter")} icon={FileText}>
              {letter.addresseeName && (
                <Field label={t("fields.addressee")} value={letter.addresseeName} />
              )}
              {letter.authorName && <Field label={t("fields.author")} value={letter.authorName} />}
              <div className="space-y-2">
                <p className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45">
                  {t("sections.attachments")}
                </p>
                {letter.attachmentFileName && letter.attachmentStorageKey ? (
                  <a
                    href={fileDownloadUrl(letter.attachmentStorageKey)}
                    className="inline-flex items-center gap-2 text-sm text-violet-600 hover:underline"
                  >
                    <Paperclip size={14} />
                    {letter.attachmentFileName}
                  </a>
                ) : (
                  <p className="text-sm text-foreground/40">{t("sections.noAttachments")}</p>
                )}
                {letter.translatedAttachmentFileName && letter.translatedAttachmentStorageKey && (
                  <a
                    href={fileDownloadUrl(letter.translatedAttachmentStorageKey)}
                    className="inline-flex items-center gap-2 text-sm text-indigo-600 hover:underline mt-2"
                  >
                    <Paperclip size={14} />
                    {letter.translatedAttachmentFileName}
                  </a>
                )}
              </div>
              {letter.helpDeskTicketNumber && (
                <Field label={t("workflow.helpDeskTicket")} value={letter.helpDeskTicketNumber} />
              )}
              {letter.translatingLanguages && letter.translatingLanguages.length > 0 && (
                <Field
                  label={t("workflow.translatingLanguage")}
                  value={translationLanguagesLabel(letter.translatingLanguages, locale)}
                />
              )}
            </DcsSectionCard>

            <DcsErrorAlert message={error} />

            {perms.canEditDraft && (
              <WorkflowCard title={t("workflow.editDraftTitle")}>
                <input className={inputClass} value={editTitle} onChange={(e) => setEditTitle(e.target.value)} />
                <input
                  className={inputClass}
                  placeholder={t("fields.subjectRu")}
                  value={editTitleRu}
                  onChange={(e) => setEditTitleRu(e.target.value)}
                />
                <input
                  className={inputClass}
                  placeholder={t("fields.addressee")}
                  value={editAddressee}
                  onChange={(e) => setEditAddressee(e.target.value)}
                />
                <DocumentFileUpload
                  folder="outgoing-letters"
                  disabled={acting}
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
                <Button
                  disabled={acting}
                  onClick={() =>
                    act(async () => {
                      await api.put(`/dcs/outgoing-letters/${documentId}`, {
                        title: editTitle,
                        titleRu: editTitleRu || null,
                        addresseeName: editAddressee || null,
                        attachmentFileName: attachmentFileName || null,
                        attachmentStorageKey: attachmentStorageKey || null,
                        requiresTranslation: letter.requiresTranslation,
                      });
                    })
                  }
                >
                  {t("workflow.saveDraft")}
                </Button>
              </WorkflowCard>
            )}

            {perms.canSendToTranslation && (
              <WorkflowCard title={t("workflow.sendToTranslationTitle")} hint={t("workflow.sendToTranslationHint")}>
                <select className={inputClass} value={sourceLanguage} onChange={(e) => setSourceLanguage(e.target.value)}>
                  <option value="">{t("workflow.selectLanguage")}</option>
                  {TRANSLATION_LANGUAGE_CODES.map((c) => (
                    <option key={c} value={c}>
                      {translationLanguageLabel(c, locale)}
                    </option>
                  ))}
                </select>
                <p className="text-xs text-foreground/45">{t("workflow.translatingLanguageHint")}</p>
                <div className="flex flex-wrap gap-2">
                  {TRANSLATION_LANGUAGE_CODES.map((c) => (
                    <label key={c} className="flex items-center gap-1.5 text-sm cursor-pointer">
                      <input
                        type="checkbox"
                        checked={translatingLanguages.includes(c)}
                        onChange={() => toggleLang(c)}
                      />
                      {translationLanguageLabel(c, locale)}
                    </label>
                  ))}
                </div>
                <Button
                  disabled={acting || !sourceLanguage || translatingLanguages.length === 0}
                  onClick={() =>
                    act(async () => {
                      await api.post(`/dcs/outgoing-letters/${documentId}/send-to-translation`, {
                        sourceLanguage,
                        translatingLanguages,
                      });
                    })
                  }
                >
                  {t("workflow.sendToTranslation")}
                </Button>
              </WorkflowCard>
            )}

            {letter.phase === "TranslationPending" && (
              <WorkflowCard title={t("workflow.translationPendingTitle")} hint={t("workflow.translationPendingHint")} />
            )}

            {perms.canSubmitToEds && (
              <WorkflowCard title={t("workflow.submitToEdsTitle")} hint={t("workflow.submitToEdsHint")}>
                <SelectUser
                  label={t("workflow.deptHead")}
                  users={deptHeads}
                  value={deptHeadId}
                  onChange={setDeptHeadId}
                  locale={locale}
                />
                <SelectUser
                  label={t("workflow.supervisingDeputy")}
                  users={topManagers}
                  value={supervisingDeputyId}
                  onChange={setSupervisingDeputyId}
                  locale={locale}
                  optional
                />
                <SelectUser
                  label={t("workflow.firstDeputy")}
                  users={topManagers}
                  value={firstDeputyId}
                  onChange={setFirstDeputyId}
                  locale={locale}
                  optional
                />
                <SelectUser
                  label={t("workflow.generalDirector")}
                  users={topManagers}
                  value={generalDirectorId}
                  onChange={setGeneralDirectorId}
                  locale={locale}
                  optional
                />
                <Button
                  disabled={acting || !deptHeadId}
                  onClick={() =>
                    act(async () => {
                      await api.post(`/dcs/outgoing-letters/${documentId}/submit-to-eds`, {
                        deptHeadId,
                        supervisingDeputyId: supervisingDeputyId || null,
                        firstDeputyId: firstDeputyId || null,
                        generalDirectorId: generalDirectorId || null,
                      });
                    })
                  }
                >
                  {t("workflow.submitToEds")}
                </Button>
              </WorkflowCard>
            )}

            {perms.canApproveDeptHead && (
              <WorkflowCard title={t("workflow.deptHeadApprovalTitle")}>
                <textarea
                  className={inputClass}
                  rows={2}
                  placeholder={t("workflow.commentOptional")}
                  value={approvalComment}
                  onChange={(e) => setApprovalComment(e.target.value)}
                />
                <div className="flex gap-2">
                  <Button
                    disabled={acting}
                    onClick={() =>
                      act(async () => {
                        await api.post(`/dcs/outgoing-letters/${documentId}/approve-dept-head`, {
                          comment: approvalComment || null,
                        });
                      })
                    }
                  >
                    {t("workflow.approve")}
                  </Button>
                  <Button
                    variant="secondary"
                    disabled={acting || !revisionComment.trim()}
                    onClick={() =>
                      act(async () => {
                        await api.post(`/dcs/outgoing-letters/${documentId}/reject-dept-head`, {
                          comment: revisionComment,
                        });
                      })
                    }
                  >
                    {t("workflow.reject")}
                  </Button>
                </div>
                <textarea
                  className={inputClass}
                  rows={2}
                  placeholder={t("workflow.revisionComment")}
                  value={revisionComment}
                  onChange={(e) => setRevisionComment(e.target.value)}
                />
              </WorkflowCard>
            )}

            {(perms.canManageSpecialistCoordination || perms.canManageDepartmentCoordination) && (
              <WorkflowCard
                title={
                  letter.phase === "SpecialistCoordination"
                    ? t("workflow.specialistCoordinationTitle")
                    : t("workflow.departmentCoordinationTitle")
                }
              >
                <div className="max-h-40 overflow-y-auto space-y-1 border border-border/50 rounded-xl p-3">
                  {coordinators.map((u) => (
                    <label key={u.id} className="flex items-center gap-2 text-sm cursor-pointer">
                      <input
                        type="checkbox"
                        checked={selectedCoordinators.includes(u.id)}
                        onChange={() =>
                          setSelectedCoordinators((prev) =>
                            prev.includes(u.id) ? prev.filter((id) => id !== u.id) : [...prev, u.id]
                          )
                        }
                      />
                      {u.fullName}
                    </label>
                  ))}
                </div>
                {selectedCoordinators.length > 0 && (
                  <Button
                    variant="secondary"
                    disabled={acting}
                    onClick={() =>
                      act(async () => {
                        await api.post(`/dcs/outgoing-letters/${documentId}/coordinators`, {
                          userIds: selectedCoordinators,
                          forDepartment: letter.phase === "DepartmentCoordination",
                        });
                        setSelectedCoordinators([]);
                      })
                    }
                  >
                    {t("workflow.addCoordinators")}
                  </Button>
                )}
                {letter.coordinators.length > 0 && (
                  <ul className="text-sm text-foreground/60 space-y-1">
                    {letter.coordinators.map((c) => (
                      <li key={c.id}>
                        {c.userName}
                        {c.coordinatedAt ? ` — ${new Date(c.coordinatedAt).toLocaleDateString(locale)}` : ""}
                      </li>
                    ))}
                  </ul>
                )}
                <Button
                  disabled={acting}
                  onClick={() =>
                    act(async () => {
                      const ep =
                        letter.phase === "SpecialistCoordination"
                          ? "complete-specialist-coordination"
                          : "complete-department-coordination";
                      await api.post(`/dcs/outgoing-letters/${documentId}/${ep}`);
                    })
                  }
                >
                  {letter.phase === "SpecialistCoordination"
                    ? t("workflow.completeSpecialistCoordination")
                    : t("workflow.completeDepartmentCoordination")}
                </Button>
              </WorkflowCard>
            )}

            {(perms.canApproveSupervisingDeputy ||
              perms.canApproveFirstDeputy ||
              perms.canApproveGeneralDirector) && (
              <WorkflowCard title={t("workflow.leadershipApprovalTitle")}>
                <textarea
                  className={inputClass}
                  rows={2}
                  placeholder={t("workflow.commentOptional")}
                  value={approvalComment}
                  onChange={(e) => setApprovalComment(e.target.value)}
                />
                <div className="flex flex-wrap gap-2">
                  {perms.canApproveSupervisingDeputy && (
                    <Button
                      disabled={acting}
                      onClick={() =>
                        act(async () => {
                          await api.post(`/dcs/outgoing-letters/${documentId}/approve-supervising-deputy`, {
                            comment: approvalComment || null,
                          });
                        })
                      }
                    >
                      {t("workflow.approveSupervisingDeputy")}
                    </Button>
                  )}
                  {perms.canApproveFirstDeputy && (
                    <Button
                      disabled={acting}
                      onClick={() =>
                        act(async () => {
                          await api.post(`/dcs/outgoing-letters/${documentId}/approve-first-deputy`, {
                            comment: approvalComment || null,
                          });
                        })
                      }
                    >
                      {t("workflow.approveFirstDeputy")}
                    </Button>
                  )}
                  {perms.canApproveGeneralDirector && (
                    <Button
                      disabled={acting}
                      onClick={() =>
                        act(async () => {
                          await api.post(`/dcs/outgoing-letters/${documentId}/approve-general-director`, {
                            comment: approvalComment || null,
                          });
                        })
                      }
                    >
                      {t("workflow.approveGeneralDirector")}
                    </Button>
                  )}
                </div>
                <textarea
                  className={inputClass}
                  rows={2}
                  placeholder={t("workflow.revisionComment")}
                  value={revisionComment}
                  onChange={(e) => setRevisionComment(e.target.value)}
                />
                <Button
                  variant="secondary"
                  disabled={acting || !revisionComment.trim()}
                  onClick={() =>
                    act(async () => {
                      await api.post(`/dcs/outgoing-letters/${documentId}/reject-approval`, {
                        comment: revisionComment,
                      });
                    })
                  }
                >
                  {t("workflow.reject")}
                </Button>
              </WorkflowCard>
            )}

            {perms.canFinalizeEds && (
              <WorkflowCard title={t("workflow.finalizeEdsTitle")} hint={t("workflow.finalizeEdsHint")}>
                <Button
                  disabled={acting}
                  onClick={() => act(async () => api.post(`/dcs/outgoing-letters/${documentId}/finalize-eds`))}
                >
                  {t("workflow.finalizeEds")}
                </Button>
              </WorkflowCard>
            )}

            {perms.canSendToRegistrar && (
              <WorkflowCard title={t("workflow.sendToRegistrarTitle")}>
                <Button
                  disabled={acting}
                  onClick={() => act(async () => api.post(`/dcs/outgoing-letters/${documentId}/send-to-registrar`))}
                >
                  {t("workflow.sendToRegistrar")}
                </Button>
              </WorkflowCard>
            )}

            {perms.canRegister && (
              <WorkflowCard title={t("workflow.registerTitle")} hint={t("workflow.registerHint")}>
                <Button
                  disabled={acting}
                  onClick={() => act(async () => api.post(`/dcs/outgoing-letters/${documentId}/register`))}
                >
                  {t("workflow.register")}
                </Button>
              </WorkflowCard>
            )}

            {perms.canConfirmPaperSignature && (
              <WorkflowCard title={t("workflow.paperSignatureTitle")}>
                <Button
                  disabled={acting}
                  onClick={() =>
                    act(async () => api.post(`/dcs/outgoing-letters/${documentId}/confirm-paper-signature`))
                  }
                >
                  {t("workflow.confirmPaperSignature")}
                </Button>
              </WorkflowCard>
            )}

            {perms.canConfirmDispatch && (
              <WorkflowCard title={t("workflow.dispatchTitle")}>
                <Button
                  disabled={acting}
                  onClick={() => act(async () => api.post(`/dcs/outgoing-letters/${documentId}/confirm-dispatch`))}
                >
                  {t("workflow.confirmDispatch")}
                </Button>
              </WorkflowCard>
            )}

            {perms.canArchive && (
              <WorkflowCard title={t("workflow.archiveTitle")} hint={t("workflow.archiveHint")}>
                <Button
                  disabled={acting}
                  onClick={() => act(async () => api.post(`/dcs/outgoing-letters/${documentId}/archive`))}
                >
                  {t("workflow.archive")}
                </Button>
              </WorkflowCard>
            )}

            {letter.phase === "NeedsRevision" && letter.revisionNotes && (
              <WorkflowCard title={t("workflow.revisionTitle")}>
                <p className="text-sm text-amber-700 dark:text-amber-300">{letter.revisionNotes}</p>
              </WorkflowCard>
            )}

            {letter.phase === "Completed" && <DcsCompletedBanner label={t("workflow.completed")} />}
          </div>

          <DcsStatusSidebar
            kind="outgoing"
            title={t("workflow.currentStep")}
            phaseLabel={outgoingPhaseLabel(letter.phase, locale)}
          >
            {letter.deptHeadName && <DcsDetailField label={t("workflow.deptHead")} value={letter.deptHeadName} />}
            {letter.supervisingDeputyName && (
              <DcsDetailField label={t("workflow.supervisingDeputy")} value={letter.supervisingDeputyName} />
            )}
            {letter.firstDeputyName && <DcsDetailField label={t("workflow.firstDeputy")} value={letter.firstDeputyName} />}
            {letter.generalDirectorName && (
              <DcsDetailField label={t("workflow.generalDirector")} value={letter.generalDirectorName} />
            )}
          </DcsStatusSidebar>
        </div>
      </DcsWorkflowShell>
  );
}

function Field({ label, value }: { label: string; value: string }) {
  return <DcsDetailField label={label} value={value} />;
}

function WorkflowCard({
  title,
  hint,
  children,
}: {
  title: string;
  hint?: string;
  children?: React.ReactNode;
}) {
  return (
    <DcsWorkflowCard kind="outgoing" title={title} hint={hint}>
      {children}
    </DcsWorkflowCard>
  );
}

function SelectUser({
  label,
  users,
  value,
  onChange,
  locale,
  optional,
}: {
  label: string;
  users: OutgoingLetterUser[];
  value: string;
  onChange: (v: string) => void;
  locale: string;
  optional?: boolean;
}) {
  return (
    <div>
      <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-1 block">
        {label}
        {optional && " (optional)"}
      </label>
      <select className={dcsInputClass("outgoing")} value={value} onChange={(e) => onChange(e.target.value)}>
        <option value="">—</option>
        {users.map((u) => (
          <option key={u.id} value={u.id}>
            {u.fullName}
            {u.departmentName ? ` (${locale.startsWith("en") ? u.departmentNameEn : u.departmentName})` : ""}
          </option>
        ))}
      </select>
    </div>
  );
}
