"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { FileText, Paperclip, Users } from "lucide-react";
import api from "@/lib/api";
import {
  currentOrderStepIndex,
  Order,
  OrderPermissions,
  OrderUser,
  ORDER_STEP_GROUPS,
  orderPhaseLabel,
  orderStepLabel,
} from "@/lib/order";
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
import { cn } from "@/lib/utils";

interface Props {
  documentId: string;
}

export function OrderView({ documentId }: Props) {
  const t = useTranslations("dcs.order");
  const locale = useLocale();
  const router = useRouter();
  const [order, setOrder] = useState<Order | null>(null);
  const [perms, setPerms] = useState<OrderPermissions | null>(null);
  const [loading, setLoading] = useState(true);
  const [acting, setActing] = useState(false);
  const [error, setError] = useState("");

  const [deptHeads, setDeptHeads] = useState<OrderUser[]>([]);
  const [topManagers, setTopManagers] = useState<OrderUser[]>([]);
  const [coordinators, setCoordinators] = useState<OrderUser[]>([]);
  const [distributionTargets, setDistributionTargets] = useState<OrderUser[]>([]);

  const [deptHeadId, setDeptHeadId] = useState("");
  const [supervisingDeputyId, setSupervisingDeputyId] = useState("");
  const [firstDeputyId, setFirstDeputyId] = useState("");
  const [generalDirectorId, setGeneralDirectorId] = useState("");
  const [requiresSpecialistCoordination, setRequiresSpecialistCoordination] = useState(true);
  const [selectedCoordinators, setSelectedCoordinators] = useState<string[]>([]);
  const [distributionUserIds, setDistributionUserIds] = useState<string[]>([]);
  const [scanAttachmentFileName, setScanAttachmentFileName] = useState("");
  const [scanAttachmentStorageKey, setScanAttachmentStorageKey] = useState("");
  const [commentBody, setCommentBody] = useState("");
  const [revisionComment, setRevisionComment] = useState("");
  const [approvalComment, setApprovalComment] = useState("");

  const [editTitle, setEditTitle] = useState("");
  const [editTitleRu, setEditTitleRu] = useState("");
  const [attachmentFileName, setAttachmentFileName] = useState("");
  const [attachmentStorageKey, setAttachmentStorageKey] = useState("");

  const load = useCallback(() => {
    setLoading(true);
    Promise.all([api.get(`/dcs/orders/${documentId}`), api.get(`/dcs/orders/permissions?documentId=${documentId}`)])
      .then(([o, p]) => {
        setOrder(o.data);
        setPerms(p.data);
        setEditTitle(o.data.title);
        setEditTitleRu(o.data.titleRu ?? "");
        setAttachmentFileName(o.data.attachmentFileName ?? "");
        setAttachmentStorageKey(o.data.attachmentStorageKey ?? "");
        setScanAttachmentFileName(o.data.scanAttachmentFileName ?? "");
        setScanAttachmentStorageKey(o.data.scanAttachmentStorageKey ?? "");
        setDeptHeadId(o.data.deptHeadId ?? "");
        setSupervisingDeputyId(o.data.supervisingDeputyId ?? "");
        setFirstDeputyId(o.data.firstDeputyId ?? "");
        setGeneralDirectorId(o.data.generalDirectorId ?? "");
      })
      .finally(() => setLoading(false));
  }, [documentId]);

  useEffect(() => {
    load();
  }, [load]);

  useEffect(() => {
    if (!perms) return;
    if (perms.canSubmitForApproval) {
      api.get("/dcs/orders/dept-heads").then((r) => setDeptHeads(r.data));
      api.get("/dcs/orders/top-managers").then((r) => setTopManagers(r.data));
    }
    if (perms.canManageSpecialistCoordination || perms.canManageDepartmentCoordination) {
      api.get("/dcs/orders/coordinators").then((r) => setCoordinators(r.data));
    }
    if (perms.canDistribute) {
      api.get(`/dcs/orders/${documentId}/distribution-targets`).then((r) => setDistributionTargets(r.data));
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

  const inputClass = dcsInputClass("orders");

  if (loading || !order || !perms) {
    return <DcsWorkflowLoading label={t("loading")} />;
  }

  const stepIndex = currentOrderStepIndex(order.phase);

  return (
    <DcsWorkflowShell kind="orders">
      <DcsDocumentHero
        kind="orders"
        number={order.number}
        title={order.title}
        titleRu={order.titleRu}
        phaseLabel={orderPhaseLabel(order.phase, locale)}
        backLabel={t("close")}
        printLabel={t("actions.print")}
        onBack={() => router.push(`/${locale}/automation/office/orders`)}
        onPrint={() => window.print()}
      />
      <DcsWorkflowStepper
        kind="orders"
        title={t("workflow.stepperTitle")}
        steps={ORDER_STEP_GROUPS.map((step) => ({ key: step.key, label: orderStepLabel(step.key, locale) }))}
        activeIndex={stepIndex}
      />

        <div className="grid lg:grid-cols-3 gap-5">
          <div className="lg:col-span-2 space-y-5">
            <DcsSectionCard kind="orders" title={t("sections.order")} icon={FileText}>
              {order.authorName && <Field label={t("fields.author")} value={order.authorName} />}
              {order.deptHeadName && <Field label={t("workflow.deptHead")} value={order.deptHeadName} />}
              {order.legalHeadName && <Field label={t("workflow.legalHead")} value={order.legalHeadName} />}
              {order.supervisingDeputyName && (
                <Field label={t("workflow.supervisingDeputy")} value={order.supervisingDeputyName} />
              )}
              {order.firstDeputyName && <Field label={t("workflow.firstDeputy")} value={order.firstDeputyName} />}
              {order.generalDirectorName && (
                <Field label={t("workflow.generalDirector")} value={order.generalDirectorName} />
              )}
              <div className="space-y-2">
                <p className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45">
                  {t("sections.attachments")}
                </p>
                {order.attachmentFileName && order.attachmentStorageKey ? (
                  <a
                    href={fileDownloadUrl(order.attachmentStorageKey)}
                    className="inline-flex items-center gap-2 text-sm text-orange-600 hover:underline"
                  >
                    <Paperclip size={14} />
                    {order.attachmentFileName}
                  </a>
                ) : (
                  <p className="text-sm text-foreground/40">{t("sections.noAttachments")}</p>
                )}
                {order.scanAttachmentFileName && order.scanAttachmentStorageKey && (
                  <a
                    href={fileDownloadUrl(order.scanAttachmentStorageKey)}
                    className="inline-flex items-center gap-2 text-sm text-sky-600 hover:underline mt-2"
                  >
                    <Paperclip size={14} />
                    {order.scanAttachmentFileName}
                  </a>
                )}
              </div>
              <div className="space-y-2">
                <p className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45">
                  {t("sections.recipients")}
                </p>
                {order.recipients.length === 0 ? (
                  <p className="text-sm text-foreground/40">{t("sections.noRecipients")}</p>
                ) : (
                  <ul className="space-y-2">
                    {order.recipients.map((recipient) => (
                      <li
                        key={recipient.id}
                        className="flex items-center justify-between gap-3 rounded-xl border border-border/50 px-3 py-2"
                      >
                        <div className="flex items-center gap-2.5 text-sm">
                          <Users size={15} className="text-orange-500/70" />
                          <span className="font-medium">{recipient.userName}</span>
                        </div>
                        <span className="text-xs text-foreground/45">
                          {recipient.notifiedAt
                            ? new Date(recipient.notifiedAt).toLocaleDateString(locale)
                            : t("workflow.pending")}
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
                  folder="orders"
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
                  disabled={acting || !editTitle.trim()}
                  onClick={() =>
                    act(async () => {
                      await api.put(`/dcs/orders/${documentId}`, {
                        title: editTitle,
                        titleRu: editTitleRu || null,
                        attachmentFileName: attachmentFileName || null,
                        attachmentStorageKey: attachmentStorageKey || null,
                      });
                    })
                  }
                >
                  {t("workflow.saveDraft")}
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
                <SelectUser
                  label={t("workflow.supervisingDeputy")}
                  users={topManagers}
                  value={supervisingDeputyId}
                  onChange={setSupervisingDeputyId}
                  locale={locale}
                />
                <SelectUser
                  label={t("workflow.firstDeputy")}
                  users={topManagers}
                  value={firstDeputyId}
                  onChange={setFirstDeputyId}
                  locale={locale}
                />
                <SelectUser
                  label={t("workflow.generalDirector")}
                  users={topManagers}
                  value={generalDirectorId}
                  onChange={setGeneralDirectorId}
                  locale={locale}
                />
                <label className="flex items-center gap-2 text-sm">
                  <input
                    type="checkbox"
                    checked={requiresSpecialistCoordination}
                    onChange={(e) => setRequiresSpecialistCoordination(e.target.checked)}
                  />
                  {t("workflow.requiresSpecialistCoordination")}
                </label>
                <Button
                  disabled={
                    acting || !deptHeadId || !supervisingDeputyId || !firstDeputyId || !generalDirectorId
                  }
                  onClick={() =>
                    act(async () => {
                      await api.post(`/dcs/orders/${documentId}/submit-for-approval`, {
                        deptHeadId,
                        supervisingDeputyId,
                        firstDeputyId,
                        generalDirectorId,
                        requiresSpecialistCoordination,
                      });
                    })
                  }
                >
                  {t("workflow.submitForApproval")}
                </Button>
              </WorkflowCard>
            )}

            {(perms.canManageSpecialistCoordination || perms.canManageDepartmentCoordination) && (
              <WorkflowCard
                title={
                  order.phase === "SpecialistCoordination"
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
                        await api.post(`/dcs/orders/${documentId}/coordinators`, {
                          userIds: selectedCoordinators,
                          forDepartment: order.phase === "DepartmentCoordination",
                        });
                        setSelectedCoordinators([]);
                      })
                    }
                  >
                    {t("workflow.addCoordinators")}
                  </Button>
                )}
                {order.coordinators.length > 0 && (
                  <ul className="text-sm text-foreground/60 space-y-1">
                    {order.coordinators.map((c) => (
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
                        order.phase === "SpecialistCoordination"
                          ? "complete-specialist-coordination"
                          : "complete-department-coordination";
                      await api.post(`/dcs/orders/${documentId}/${ep}`);
                    })
                  }
                >
                  {order.phase === "SpecialistCoordination"
                    ? t("workflow.completeSpecialistCoordination")
                    : t("workflow.completeDepartmentCoordination")}
                </Button>
              </WorkflowCard>
            )}

            {(perms.canApproveDeptHead || perms.canRejectDeptHead) && (
              <WorkflowCard title={t("workflow.deptHeadApprovalTitle")}>
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
                        act(async () => {
                          await api.post(`/dcs/orders/${documentId}/approve-dept-head`, {
                            comment: approvalComment || null,
                          });
                        })
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
                        act(async () => {
                          await api.post(`/dcs/orders/${documentId}/reject-dept-head`, {
                            comment: revisionComment,
                          });
                        })
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

            {(perms.canApproveLegal || perms.canRejectLegal) && (
              <WorkflowCard title={t("workflow.legalApprovalTitle")}>
                <textarea
                  className={inputClass}
                  rows={2}
                  placeholder={t("workflow.commentOptional")}
                  value={approvalComment}
                  onChange={(e) => setApprovalComment(e.target.value)}
                />
                <div className="flex gap-2">
                  {perms.canApproveLegal && (
                    <Button
                      disabled={acting}
                      onClick={() =>
                        act(async () => {
                          await api.post(`/dcs/orders/${documentId}/approve-legal`, {
                            comment: approvalComment || null,
                          });
                        })
                      }
                    >
                      {t("workflow.approve")}
                    </Button>
                  )}
                  {perms.canRejectLegal && (
                    <Button
                      variant="secondary"
                      disabled={acting || !revisionComment.trim()}
                      onClick={() =>
                        act(async () => {
                          await api.post(`/dcs/orders/${documentId}/reject-legal`, {
                            comment: revisionComment,
                          });
                        })
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

            {(perms.canApproveSupervisingDeputy ||
              perms.canApproveFirstDeputy ||
              perms.canApproveGeneralDirector ||
              perms.canRejectApproval) && (
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
                          await api.post(`/dcs/orders/${documentId}/approve-supervising-deputy`, {
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
                          await api.post(`/dcs/orders/${documentId}/approve-first-deputy`, {
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
                          await api.post(`/dcs/orders/${documentId}/approve-general-director`, {
                            comment: approvalComment || null,
                          });
                        })
                      }
                    >
                      {t("workflow.approveGeneralDirector")}
                    </Button>
                  )}
                </div>
                {perms.canRejectApproval && (
                  <>
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
                          await api.post(`/dcs/orders/${documentId}/reject-approval`, {
                            comment: revisionComment,
                          });
                        })
                      }
                    >
                      {t("workflow.reject")}
                    </Button>
                  </>
                )}
              </WorkflowCard>
            )}

            {perms.canFinalizeEds && (
              <WorkflowCard title={t("workflow.finalizeEdsTitle")} hint={t("workflow.finalizeEdsHint")}>
                <Button
                  disabled={acting}
                  onClick={() => act(async () => api.post(`/dcs/orders/${documentId}/finalize-eds`))}
                >
                  {t("workflow.finalizeEds")}
                </Button>
              </WorkflowCard>
            )}

            {perms.canSendToRegistrar && (
              <WorkflowCard title={t("workflow.sendToRegistrarTitle")}>
                <Button
                  disabled={acting}
                  onClick={() => act(async () => api.post(`/dcs/orders/${documentId}/send-to-registrar`))}
                >
                  {t("workflow.sendToRegistrar")}
                </Button>
              </WorkflowCard>
            )}

            {perms.canRegister && (
              <WorkflowCard title={t("workflow.registerTitle")} hint={t("workflow.registerHint")}>
                <Button disabled={acting} onClick={() => act(async () => api.post(`/dcs/orders/${documentId}/register`))}>
                  {t("workflow.register")}
                </Button>
              </WorkflowCard>
            )}

            {perms.canConfirmPaperSignature && (
              <WorkflowCard title={t("workflow.paperSignatureTitle")}>
                <Button
                  disabled={acting}
                  onClick={() => act(async () => api.post(`/dcs/orders/${documentId}/confirm-paper-signature`))}
                >
                  {t("workflow.confirmPaperSignature")}
                </Button>
              </WorkflowCard>
            )}

            {perms.canUploadScan && (
              <WorkflowCard title={t("workflow.scanUploadTitle")} hint={t("workflow.scanUploadHint")}>
                <DocumentFileUpload
                  folder="orders"
                  disabled={acting}
                  onUploaded={(fileName, storageKey) => {
                    setScanAttachmentFileName(fileName);
                    setScanAttachmentStorageKey(storageKey);
                  }}
                  labels={{
                    uploading: t("create.uploading"),
                    attached: t("create.attached"),
                    pick: t("workflow.pickScan"),
                  }}
                />
                <Button
                  disabled={acting || !scanAttachmentFileName || !scanAttachmentStorageKey}
                  onClick={() =>
                    act(async () => {
                      await api.post(`/dcs/orders/${documentId}/upload-scan`, {
                        fileName: scanAttachmentFileName,
                        storageKey: scanAttachmentStorageKey,
                      });
                    })
                  }
                >
                  {t("workflow.uploadScan")}
                </Button>
              </WorkflowCard>
            )}

            {perms.canDistribute && (
              <WorkflowCard title={t("workflow.distributeTitle")} hint={t("workflow.distributeHint")}>
                <div className="max-h-48 overflow-y-auto space-y-1 border border-border/50 rounded-xl p-3">
                  {distributionTargets.map((u) => (
                    <label key={u.id} className="flex items-center gap-2 text-sm cursor-pointer">
                      <input
                        type="checkbox"
                        checked={distributionUserIds.includes(u.id)}
                        onChange={() =>
                          setDistributionUserIds((prev) =>
                            prev.includes(u.id) ? prev.filter((id) => id !== u.id) : [...prev, u.id]
                          )
                        }
                      />
                      {u.fullName}
                    </label>
                  ))}
                </div>
                <Button
                  disabled={acting || distributionUserIds.length === 0}
                  onClick={() =>
                    act(async () => {
                      await api.post(`/dcs/orders/${documentId}/distribute`, {
                        userIds: distributionUserIds,
                      });
                      setDistributionUserIds([]);
                    })
                  }
                >
                  {t("workflow.distribute")}
                </Button>
              </WorkflowCard>
            )}

            {perms.canArchive && (
              <WorkflowCard title={t("workflow.archiveTitle")} hint={t("workflow.archiveHint")}>
                <Button disabled={acting} onClick={() => act(async () => api.post(`/dcs/orders/${documentId}/archive`))}>
                  {t("workflow.archive")}
                </Button>
              </WorkflowCard>
            )}

            <WorkflowCard title={t("workflow.comments")}>
              <div className="space-y-2 mb-3">
                {order.comments.length === 0 ? (
                  <p className="text-xs text-foreground/40">{t("workflow.noComments")}</p>
                ) : (
                  order.comments.map((comment) => (
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
                    await api.post(`/dcs/orders/${documentId}/comments`, { body: commentBody });
                    setCommentBody("");
                  })
                }
              >
                {t("workflow.addComment")}
              </Button>
            </WorkflowCard>

            {order.phase === "NeedsRevision" && order.revisionNotes && (
              <WorkflowCard title={t("workflow.revisionTitle")}>
                <p className="text-sm text-amber-700 dark:text-amber-300">{order.revisionNotes}</p>
              </WorkflowCard>
            )}

            {order.phase === "Completed" && <DcsCompletedBanner label={t("workflow.completed")} />}
          </div>

          <DcsStatusSidebar kind="orders" title={t("workflow.currentStep")} phaseLabel={orderPhaseLabel(order.phase, locale)}>
            {order.deptHeadName && <DcsDetailField label={t("workflow.deptHead")} value={order.deptHeadName} />}
            {order.legalHeadName && <DcsDetailField label={t("workflow.legalHead")} value={order.legalHeadName} />}
            {order.supervisingDeputyName && (
              <DcsDetailField label={t("workflow.supervisingDeputy")} value={order.supervisingDeputyName} />
            )}
            {order.firstDeputyName && <DcsDetailField label={t("workflow.firstDeputy")} value={order.firstDeputyName} />}
            {order.generalDirectorName && (
              <DcsDetailField label={t("workflow.generalDirector")} value={order.generalDirectorName} />
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
    <DcsWorkflowCard kind="orders" title={title} hint={hint}>
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
  users: OrderUser[];
  value: string;
  onChange: (v: string) => void;
  locale: string;
}) {
  return (
    <div>
      <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-1 block">
        {label}
      </label>
      <select className={dcsInputClass("orders")} value={value} onChange={(e) => onChange(e.target.value)}>
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
