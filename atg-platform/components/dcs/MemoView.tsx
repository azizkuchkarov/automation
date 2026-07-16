"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { FileText, Paperclip, Users } from "lucide-react";
import api from "@/lib/api";
import {
  currentMemoStepIndex,
  Memo,
  MemoDepartment,
  MemoPermissions,
  MemoUser,
  MEMO_STEP_GROUPS,
  memoPhaseLabel,
  memoStepLabel,
  TRANSLATION_LANGUAGE_CODES,
  translationLanguageLabel,
  translationLanguagesLabel,
} from "@/lib/memo";
import { deptLabel } from "@/lib/dcs";
import {
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
import { cn } from "@/lib/utils";

interface Props {
  documentId: string;
}

export function MemoView({ documentId }: Props) {
  const t = useTranslations("dcs.memo");
  const locale = useLocale();
  const router = useRouter();
  const [memo, setMemo] = useState<Memo | null>(null);
  const [perms, setPerms] = useState<MemoPermissions | null>(null);
  const [loading, setLoading] = useState(true);
  const [acting, setActing] = useState(false);
  const [error, setError] = useState("");

  const [topManagers, setTopManagers] = useState<MemoUser[]>([]);
  const [deptHeads, setDeptHeads] = useState<MemoUser[]>([]);
  const [departments, setDepartments] = useState<MemoDepartment[]>([]);
  const [workers, setWorkers] = useState<MemoUser[]>([]);
  const [coordinators, setCoordinators] = useState<MemoUser[]>([]);

  const [editTitle, setEditTitle] = useState("");
  const [editTitleRu, setEditTitleRu] = useState("");
  const [attachmentFileName, setAttachmentFileName] = useState("");
  const [attachmentStorageKey, setAttachmentStorageKey] = useState("");
  const [requiresTranslation, setRequiresTranslation] = useState(false);

  const [sourceLanguage, setSourceLanguage] = useState("");
  const [translatingLanguages, setTranslatingLanguages] = useState<string[]>([]);

  const [deptHeadId, setDeptHeadId] = useState("");
  const [requiresSpecialistCoordination, setRequiresSpecialistCoordination] = useState(false);
  const [requiresTopManagementResolution, setRequiresTopManagementResolution] = useState(false);
  const [resolutionManagerId, setResolutionManagerId] = useState("");
  const [selectedCoordinators, setSelectedCoordinators] = useState<string[]>([]);
  const [selectedTopManagers, setSelectedTopManagers] = useState<string[]>([]);
  const [selectedRecipientDepartments, setSelectedRecipientDepartments] = useState<string[]>([]);
  const [targetDepartmentId, setTargetDepartmentId] = useState("");
  const [routeComment, setRouteComment] = useState("");

  const [assigneeId, setAssigneeId] = useState("");
  const [assignmentTask, setAssignmentTask] = useState("");
  const [dueDate, setDueDate] = useState("");

  const [commentBody, setCommentBody] = useState("");
  const [revisionComment, setRevisionComment] = useState("");
  const [approvalComment, setApprovalComment] = useState("");

  const load = useCallback(() => {
    setLoading(true);
    Promise.all([api.get(`/dcs/memos/${documentId}`), api.get(`/dcs/memos/permissions?documentId=${documentId}`)])
      .then(([m, p]) => {
        setMemo(m.data);
        setPerms(p.data);
        setEditTitle(m.data.title);
        setEditTitleRu(m.data.titleRu ?? "");
        setAttachmentFileName(m.data.attachmentFileName ?? "");
        setAttachmentStorageKey(m.data.attachmentStorageKey ?? "");
        setRequiresTranslation(Boolean(m.data.requiresTranslation));
        setDeptHeadId(m.data.deptHeadId ?? "");
        setResolutionManagerId(m.data.resolutionManagerId ?? "");
        setRequiresTopManagementResolution(Boolean(m.data.requiresTopManagementResolution));
      })
      .finally(() => setLoading(false));
  }, [documentId]);

  useEffect(() => {
    load();
  }, [load]);

  useEffect(() => {
    if (!perms) return;
    if (perms.canSubmitForApproval) api.get("/dcs/memos/dept-heads").then((r) => setDeptHeads(r.data));
    if (perms.canSubmitForApproval || perms.canManageSpecialistCoordination)
      api.get("/dcs/memos/coordinators").then((r) => setCoordinators(r.data));
    if (perms.canInformRecipients || perms.canRegisterAndDistribute || perms.canActAsTopManagement)
      api.get("/dcs/memos/top-managers").then((r) => setTopManagers(r.data));
    if (perms.canRouteToDepartment || perms.canInformRecipients || perms.canRegisterAndDistribute) {
      api.get("/dcs/memos/departments").then((r) => setDepartments(r.data));
    }
    if (perms.canAssignWorker) {
      api.get(`/dcs/memos/${documentId}/workers`).then((r) => setWorkers(r.data));
    }
  }, [perms, documentId]);

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

  const inputClass = dcsInputClass("memo");

  if (loading || !memo || !perms) {
    return <DcsWorkflowLoading label={t("loading")} />;
  }

  const stepIndex = currentMemoStepIndex(memo.phase);

  const toggleLang = (code: string) => {
    setTranslatingLanguages((prev) => (prev.includes(code) ? prev.filter((c) => c !== code) : [...prev, code]));
  };

  return (
    <DcsWorkflowShell kind="memo">
      <DcsDocumentHero
        kind="memo"
        number={memo.number}
        title={memo.title}
        titleRu={memo.titleRu}
        phaseLabel={memoPhaseLabel(memo.phase, locale)}
        backLabel={t("close")}
        printLabel={t("actions.print")}
        onBack={() => router.push(`/${locale}/automation/office/memo`)}
        onPrint={() => window.print()}
      />
      <DcsWorkflowStepper
        kind="memo"
        title={t("workflow.stepperTitle")}
        steps={MEMO_STEP_GROUPS.map((step) => ({ key: step.key, label: memoStepLabel(step.key, locale) }))}
        activeIndex={stepIndex}
      />

        <div className="grid lg:grid-cols-3 gap-5">
          <div className="lg:col-span-2 space-y-5">
            <DcsSectionCard kind="memo" title={t("sections.memo")} icon={FileText}>
              {memo.authorName && <Field label={t("fields.author")} value={memo.authorName} />}
              {memo.assigneeName && <Field label={t("workflow.assignee")} value={memo.assigneeName} />}
              {memo.deptHeadName && <Field label={t("workflow.deptHead")} value={memo.deptHeadName} />}
              {memo.resolutionManagerName && (
                <Field label={t("workflow.resolutionManager")} value={memo.resolutionManagerName} />
              )}
              {memo.routedDepartmentName && (
                <Field label={t("workflow.routedDepartment")} value={memo.routedDepartmentName} />
              )}
              {memo.assignmentTask && <Field label={t("workflow.assignmentTask")} value={memo.assignmentTask} />}
              {memo.dueDate && <Field label={t("workflow.dueDate")} value={new Date(memo.dueDate).toLocaleDateString(locale)} />}
              {memo.sourceLanguage && (
                <Field label={t("workflow.sourceLanguage")} value={translationLanguageLabel(memo.sourceLanguage, locale)} />
              )}
              {memo.translatingLanguages && memo.translatingLanguages.length > 0 && (
                <Field
                  label={t("workflow.translatingLanguage")}
                  value={translationLanguagesLabel(memo.translatingLanguages, locale)}
                />
              )}
              <div className="space-y-2">
                <p className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45">
                  {t("sections.attachments")}
                </p>
                {memo.attachmentFileName && memo.attachmentStorageKey ? (
                  <a
                    href={fileDownloadUrl(memo.attachmentStorageKey)}
                    className="inline-flex items-center gap-2 text-sm text-violet-600 hover:underline"
                  >
                    <Paperclip size={14} />
                    {memo.attachmentFileName}
                  </a>
                ) : (
                  <p className="text-sm text-foreground/40">{t("sections.noAttachments")}</p>
                )}
                {memo.translatedAttachmentFileName && memo.translatedAttachmentStorageKey && (
                  <a
                    href={fileDownloadUrl(memo.translatedAttachmentStorageKey)}
                    className="inline-flex items-center gap-2 text-sm text-indigo-600 hover:underline mt-2"
                  >
                    <Paperclip size={14} />
                    {memo.translatedAttachmentFileName}
                  </a>
                )}
              </div>
              <div className="space-y-2">
                <p className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45">
                  {t("sections.recipients")}
                </p>
                {memo.recipients.length === 0 ? (
                  <p className="text-sm text-foreground/40">{t("sections.noRecipients")}</p>
                ) : (
                  <ul className="space-y-2">
                    {memo.recipients.map((recipient) => (
                      <li
                        key={recipient.id}
                        className="flex items-center justify-between gap-3 rounded-xl border border-border/50 px-3 py-2"
                      >
                        <div className="flex items-center gap-2.5 text-sm">
                          <Users size={15} className="text-violet-500/70" />
                          <span className="font-medium">
                            {recipient.userName ??
                              deptLabel(recipient.departmentName ?? "", recipient.departmentNameEn ?? "", locale)}
                          </span>
                        </div>
                        <span
                          className={cn(
                            "text-xs font-semibold",
                            recipient.forInformation ? "text-emerald-600" : "text-violet-600"
                          )}
                        >
                          {recipient.forInformation ? t("workflow.forInformation") : t("workflow.forExecution")}
                        </span>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
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
                <DocumentFileUpload
                  folder="memos"
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
                <label className="flex items-start gap-3 rounded-xl border border-border/60 px-4 py-3 cursor-pointer">
                  <input
                    type="checkbox"
                    className="mt-0.5"
                    checked={requiresTranslation}
                    onChange={(e) => setRequiresTranslation(e.target.checked)}
                  />
                  <div>
                    <p className="text-sm font-medium">{t("create.requiresTranslation")}</p>
                    <p className="text-xs text-foreground/45 mt-0.5">{t("create.requiresTranslationHint")}</p>
                  </div>
                </label>
                <Button
                  disabled={acting || !editTitle.trim()}
                  onClick={() =>
                    act(async () => {
                      await api.put(`/dcs/memos/${documentId}`, {
                        title: editTitle,
                        titleRu: editTitleRu || null,
                        attachmentFileName: attachmentFileName || null,
                        attachmentStorageKey: attachmentStorageKey || null,
                        requiresTranslation,
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
                  {TRANSLATION_LANGUAGE_CODES.map((code) => (
                    <option key={code} value={code}>
                      {translationLanguageLabel(code, locale)}
                    </option>
                  ))}
                </select>
                <div className="flex flex-wrap gap-2">
                  {TRANSLATION_LANGUAGE_CODES.filter((code) => code !== sourceLanguage).map((code) => (
                    <label key={code} className="flex items-center gap-1.5 text-sm cursor-pointer">
                      <input
                        type="checkbox"
                        checked={translatingLanguages.includes(code)}
                        disabled={!sourceLanguage}
                        onChange={() => toggleLang(code)}
                      />
                      {translationLanguageLabel(code, locale)}
                    </label>
                  ))}
                </div>
                <Button
                  disabled={acting || !sourceLanguage || translatingLanguages.length === 0}
                  onClick={() =>
                    act(async () => {
                      await api.post(`/dcs/memos/${documentId}/send-to-translation`, {
                        sourceLanguage,
                        translatingLanguages,
                      });
                      setSourceLanguage("");
                      setTranslatingLanguages([]);
                    })
                  }
                >
                  {t("workflow.sendToTranslation")}
                </Button>
              </WorkflowCard>
            )}

            {perms.canSubmitForApproval && (
              <WorkflowCard title={t("workflow.submitForApprovalTitle")} hint={t("workflow.submitForApprovalHint")}>
                <SelectUser
                  label={t("workflow.deptHead")}
                  users={deptHeads}
                  value={deptHeadId}
                  onChange={setDeptHeadId}
                  locale={locale}
                />
                <label className="flex items-center gap-2 text-sm">
                  <input
                    type="checkbox"
                    checked={requiresSpecialistCoordination}
                    onChange={(e) => setRequiresSpecialistCoordination(e.target.checked)}
                  />
                  {t("workflow.specialistCoordinationTitle")}
                </label>
                <Button
                  disabled={acting || !deptHeadId}
                  onClick={() =>
                    act(() =>
                      api.post(`/dcs/memos/${documentId}/submit-for-approval`, {
                        deptHeadId,
                        requiresSpecialistCoordination,
                      })
                    )
                  }
                >
                  {t("workflow.submitForApproval")}
                </Button>
              </WorkflowCard>
            )}

            {perms.canManageSpecialistCoordination && (
              <WorkflowCard
                title={t("workflow.specialistCoordinationTitle")}
                hint={t("workflow.specialistCoordinationHint")}
              >
                <div className="max-h-40 overflow-y-auto space-y-1 border border-border/50 rounded-xl p-3">
                  {coordinators.map((user) => (
                    <label key={user.id} className="flex items-center gap-2 text-sm cursor-pointer">
                      <input
                        type="checkbox"
                        checked={selectedCoordinators.includes(user.id)}
                        onChange={() =>
                          setSelectedCoordinators((prev) =>
                            prev.includes(user.id) ? prev.filter((id) => id !== user.id) : [...prev, user.id]
                          )
                        }
                      />
                      {user.fullName}
                    </label>
                  ))}
                </div>
                <Button
                  variant="secondary"
                  disabled={acting || selectedCoordinators.length === 0}
                  onClick={() =>
                    act(async () => {
                      await api.post(`/dcs/memos/${documentId}/coordinators`, { userIds: selectedCoordinators });
                      setSelectedCoordinators([]);
                    })
                  }
                >
                  {t("workflow.addCoordinators")}
                </Button>
                <Button
                  disabled={acting}
                  onClick={() => act(() => api.post(`/dcs/memos/${documentId}/complete-specialist-coordination`))}
                >
                  {t("workflow.completeSpecialistCoordination")}
                </Button>
              </WorkflowCard>
            )}

            {(perms.canApproveDeptHead || perms.canRejectDeptHead) && (
              <WorkflowCard title={t("workflow.deptHeadDecisionTitle")}>
                <textarea
                  className={inputClass}
                  rows={2}
                  placeholder={t("workflow.commentOptional")}
                  value={approvalComment}
                  onChange={(e) => setApprovalComment(e.target.value)}
                />
                <div className="flex gap-2">
                  {perms.canApproveDeptHead && (
                    <Button
                      disabled={acting}
                      onClick={() =>
                        act(() =>
                          api.post(`/dcs/memos/${documentId}/approve-dept-head`, {
                            comment: approvalComment || null,
                          })
                        )
                      }
                    >
                      {t("workflow.approve")}
                    </Button>
                  )}
                  {perms.canRejectDeptHead && (
                    <Button
                      variant="secondary"
                      disabled={acting || !revisionComment.trim()}
                      onClick={() =>
                        act(() =>
                          api.post(`/dcs/memos/${documentId}/reject-dept-head`, {
                            comment: revisionComment,
                          })
                        )
                      }
                    >
                      {t("workflow.reject")}
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
              </WorkflowCard>
            )}

            {perms.canRegisterAndDistribute && (
              <WorkflowCard
                title={t("workflow.registerAndDistributeTitle")}
                hint={t("workflow.registerAndDistributeHint")}
              >
                <label className="flex items-center gap-2 text-sm">
                  <input
                    type="checkbox"
                    checked={requiresTopManagementResolution}
                    onChange={(e) => setRequiresTopManagementResolution(e.target.checked)}
                  />
                  {t("workflow.requiresTopManagementResolution")}
                </label>
                {requiresTopManagementResolution && (
                  <SelectUser
                    label={t("workflow.resolutionManager")}
                    users={topManagers}
                    value={resolutionManagerId}
                    onChange={setResolutionManagerId}
                    locale={locale}
                  />
                )}
                <Button
                  disabled={acting || (requiresTopManagementResolution && !resolutionManagerId)}
                  onClick={() =>
                    act(() =>
                      api.post(`/dcs/memos/${documentId}/register-and-distribute`, {
                        requiresTopManagementResolution,
                        resolutionManagerId: requiresTopManagementResolution ? resolutionManagerId : null,
                      })
                    )
                  }
                >
                  {t("workflow.registerAndDistribute")}
                </Button>
              </WorkflowCard>
            )}

            {perms.canInformRecipients && (
              <WorkflowCard title={t("workflow.informRecipientsTitle")} hint={t("workflow.informRecipientsHint")}>
                <p className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45">{t("workflow.topManagers")}</p>
                <div className="grid sm:grid-cols-2 gap-2">
                  {topManagers.map((manager) => (
                    <label key={manager.id} className="flex items-center gap-2 text-sm cursor-pointer">
                      <input
                        type="checkbox"
                        checked={selectedTopManagers.includes(manager.id)}
                        onChange={() =>
                          setSelectedTopManagers((prev) =>
                            prev.includes(manager.id) ? prev.filter((id) => id !== manager.id) : [...prev, manager.id]
                          )
                        }
                      />
                      {manager.fullName}
                    </label>
                  ))}
                </div>
                <p className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mt-2">
                  {t("workflow.departments")}
                </p>
                <div className="grid sm:grid-cols-2 gap-2">
                  {departments.map((department) => (
                    <label key={department.id} className="flex items-center gap-2 text-sm cursor-pointer">
                      <input
                        type="checkbox"
                        checked={selectedRecipientDepartments.includes(department.id)}
                        onChange={() =>
                          setSelectedRecipientDepartments((prev) =>
                            prev.includes(department.id)
                              ? prev.filter((id) => id !== department.id)
                              : [...prev, department.id]
                          )
                        }
                      />
                      {deptLabel(department.name, department.nameEn, locale)}
                    </label>
                  ))}
                </div>
                <Button
                  disabled={acting || (selectedTopManagers.length === 0 && selectedRecipientDepartments.length === 0)}
                  onClick={() =>
                    act(async () => {
                      await api.post(`/dcs/memos/${documentId}/inform-recipients`, {
                        recipients: [
                          ...selectedTopManagers.map((id) => ({ userId: id, forInformation: true })),
                          ...selectedRecipientDepartments.map((id) => ({ departmentId: id, forInformation: true })),
                        ],
                      });
                      setSelectedTopManagers([]);
                      setSelectedRecipientDepartments([]);
                    })
                  }
                >
                  {t("workflow.informRecipients")}
                </Button>
              </WorkflowCard>
            )}

            {perms.canRouteToDepartment && (
              <WorkflowCard title={t("workflow.routeToDepartmentTitle")} hint={t("workflow.routeToDepartmentHint")}>
                <select className={inputClass} value={targetDepartmentId} onChange={(e) => setTargetDepartmentId(e.target.value)}>
                  <option value="">{t("workflow.selectDept")}</option>
                  {departments.map((department) => (
                    <option key={department.id} value={department.id}>
                      {deptLabel(department.name, department.nameEn, locale)}
                    </option>
                  ))}
                </select>
                <textarea
                  className={inputClass}
                  rows={2}
                  placeholder={t("workflow.comment")}
                  value={routeComment}
                  onChange={(e) => setRouteComment(e.target.value)}
                />
                <Button
                  disabled={acting || !targetDepartmentId}
                  onClick={() =>
                    act(async () => {
                      await api.post(`/dcs/memos/${documentId}/route-to-department`, {
                        targetDepartmentId,
                        comment: routeComment || null,
                      });
                      setRouteComment("");
                    })
                  }
                >
                  {t("workflow.routeToDepartment")}
                </Button>
              </WorkflowCard>
            )}

            {perms.canAssignWorker && (
              <WorkflowCard title={t("workflow.assignWorkerTitle")} hint={t("workflow.assignWorkerHint")}>
                <select className={inputClass} value={assigneeId} onChange={(e) => setAssigneeId(e.target.value)}>
                  <option value="">{t("workflow.selectWorker")}</option>
                  {workers.map((worker) => (
                    <option key={worker.id} value={worker.id}>
                      {worker.fullName}
                    </option>
                  ))}
                </select>
                <input
                  className={inputClass}
                  placeholder={t("workflow.assignmentTask")}
                  value={assignmentTask}
                  onChange={(e) => setAssignmentTask(e.target.value)}
                />
                <input type="date" className={inputClass} value={dueDate} onChange={(e) => setDueDate(e.target.value)} />
                <Button
                  disabled={acting || !assigneeId}
                  onClick={() =>
                    act(async () => {
                      await api.post(`/dcs/memos/${documentId}/assign-worker`, {
                        assigneeId,
                        assignmentTask: assignmentTask || null,
                        dueDate: dueDate || null,
                      });
                    })
                  }
                >
                  {t("workflow.assignWorker")}
                </Button>
              </WorkflowCard>
            )}

            {perms.canAcceptExecution && (
              <WorkflowCard title={t("workflow.acceptExecutionTitle")} hint={t("workflow.acceptExecutionHint")}>
                <Button disabled={acting} onClick={() => act(() => api.post(`/dcs/memos/${documentId}/accept-execution`))}>
                  {t("workflow.acceptExecution")}
                </Button>
              </WorkflowCard>
            )}

            {perms.canReportCompletion && (
              <WorkflowCard title={t("workflow.reportCompletionTitle")} hint={t("workflow.reportCompletionHint")}>
                <Button disabled={acting} onClick={() => act(() => api.post(`/dcs/memos/${documentId}/report-completion`))}>
                  {t("workflow.reportCompletion")}
                </Button>
              </WorkflowCard>
            )}

            {perms.canRequestRevision && (
              <WorkflowCard title={t("workflow.requestRevisionTitle")}>
                <textarea
                  className={inputClass}
                  rows={2}
                  placeholder={t("workflow.revisionComment")}
                  value={revisionComment}
                  onChange={(e) => setRevisionComment(e.target.value)}
                />
                <Button
                  disabled={acting || !revisionComment.trim()}
                  onClick={() =>
                    act(async () => {
                      await api.post(`/dcs/memos/${documentId}/request-revision`, { body: revisionComment });
                      setRevisionComment("");
                    })
                  }
                >
                  {t("workflow.requestRevision")}
                </Button>
              </WorkflowCard>
            )}

            {perms.canAcceptCompletion && (
              <WorkflowCard title={t("workflow.acceptCompletionTitle")} hint={t("workflow.acceptCompletionHint")}>
                <Button disabled={acting} onClick={() => act(() => api.post(`/dcs/memos/${documentId}/accept-completion`))}>
                  {t("workflow.acceptCompletion")}
                </Button>
              </WorkflowCard>
            )}

            {perms.canArchive && (
              <WorkflowCard title={t("workflow.archiveTitle")} hint={t("workflow.archiveHint")}>
                <Button disabled={acting} onClick={() => act(() => api.post(`/dcs/memos/${documentId}/archive`))}>
                  {t("workflow.archive")}
                </Button>
              </WorkflowCard>
            )}

            <WorkflowCard title={t("workflow.comments")}>
              <div className="space-y-2 mb-3">
                {memo.comments.length === 0 ? (
                  <p className="text-xs text-foreground/40">{t("workflow.noComments")}</p>
                ) : (
                  memo.comments.map((comment) => (
                    <div key={comment.id} className="text-sm border border-border/50 rounded-xl p-3 bg-foreground/[0.02]">
                      <p className="font-semibold text-xs text-foreground/45">{comment.authorName}</p>
                      <p className="mt-1">{comment.body}</p>
                      <p className="text-[10px] text-foreground/35 mt-1">
                        {new Date(comment.createdAt).toLocaleString(locale)}
                      </p>
                    </div>
                  ))
                )}
              </div>
              <textarea
                className={inputClass}
                rows={2}
                placeholder={t("workflow.comment")}
                value={commentBody}
                onChange={(e) => setCommentBody(e.target.value)}
              />
              <Button
                variant="secondary"
                disabled={acting || !commentBody.trim()}
                onClick={() =>
                  act(async () => {
                    await api.post(`/dcs/memos/${documentId}/comments`, { body: commentBody });
                    setCommentBody("");
                  })
                }
              >
                {t("workflow.addComment")}
              </Button>
            </WorkflowCard>
          </div>

          <DcsStatusSidebar kind="memo" title={t("workflow.currentStep")} phaseLabel={memoPhaseLabel(memo.phase, locale)}>
            {memo.deptHeadName && <DcsDetailField label={t("workflow.deptHead")} value={memo.deptHeadName} />}
            {memo.resolutionManagerName && (
              <DcsDetailField label={t("workflow.resolutionManager")} value={memo.resolutionManagerName} />
            )}
            {memo.assigneeName && <DcsDetailField label={t("workflow.assignee")} value={memo.assigneeName} />}
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
    <DcsWorkflowCard kind="memo" title={title} hint={hint}>
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
}: {
  label: string;
  users: MemoUser[];
  value: string;
  onChange: (v: string) => void;
  locale: string;
}) {
  return (
    <div>
      <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-1 block">{label}</label>
      <select className={dcsInputClass("memo")} value={value} onChange={(e) => onChange(e.target.value)}>
        <option value="">—</option>
        {users.map((user) => (
          <option key={user.id} value={user.id}>
            {user.fullName}
            {user.departmentName ? ` (${locale.startsWith("en") ? user.departmentNameEn : user.departmentName})` : ""}
          </option>
        ))}
      </select>
    </div>
  );
}
