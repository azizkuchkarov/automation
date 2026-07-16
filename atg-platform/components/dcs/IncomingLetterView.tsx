"use client";



import { useCallback, useEffect, useState } from "react";

import { useRouter } from "next/navigation";

import { useLocale, useTranslations } from "next-intl";

import {
  FileText,
  Paperclip,
  Users,
} from "lucide-react";

import api from "@/lib/api";

import {

  currentStepIndex,

  IncomingLetter,

  IncomingLetterDepartment,

  IncomingLetterPermissions,

  IncomingLetterUser,

  INCOMING_LETTER_STEPS,

  phaseHint,

  phaseLabel,

  stepLabel,

  translationLanguageLabel,

  translationLanguagesLabel,

  TRANSLATION_LANGUAGE_CODES,

} from "@/lib/incomingLetter";

import { deptLabel } from "@/lib/dcs";

import {
  DcsErrorAlert,
  DcsDetailField,
  DcsDocumentHero,
  DcsTabBar,
  DcsWorkflowCard,
  DcsWorkflowLoading,
  DcsWorkflowShell,
  DcsWorkflowStepper,
  DcsSectionCard,
  dcsInputClass,
} from "@/components/dcs/DcsWorkflowUI";

import { Button } from "@/components/ui/Button";

import { fileDownloadUrl } from "@/lib/files";

import { cn } from "@/lib/utils";



type TabKey = "letter" | "distribution" | "connected";



interface Props {

  documentId: string;

}



function formatDate(value?: string, locale?: string) {

  if (!value) return "—";

  return new Date(value).toLocaleDateString(locale?.startsWith("en") ? "en-GB" : "ru-RU", {

    day: "2-digit",

    month: "2-digit",

    year: "numeric",

  });

}



export function IncomingLetterView({ documentId }: Props) {

  const t = useTranslations("dcs.incoming");

  const locale = useLocale();

  const router = useRouter();

  const [letter, setLetter] = useState<IncomingLetter | null>(null);

  const [perms, setPerms] = useState<IncomingLetterPermissions | null>(null);

  const [loading, setLoading] = useState(true);

  const [tab, setTab] = useState<TabKey>("letter");

  const [acting, setActing] = useState(false);

  const [error, setError] = useState("");



  const [topManagers, setTopManagers] = useState<IncomingLetterUser[]>([]);

  const [selectedResolutionManagers, setSelectedResolutionManagers] = useState<string[]>([]);

  const [selectedManagers, setSelectedManagers] = useState<string[]>([]);

  const [departments, setDepartments] = useState<IncomingLetterDepartment[]>([]);

  const [targetDept, setTargetDept] = useState("");

  const [workers, setWorkers] = useState<IncomingLetterUser[]>([]);

  const [assigneeId, setAssigneeId] = useState("");

  const [assignmentTask, setAssignmentTask] = useState("");

  const [dueDate, setDueDate] = useState("");

  const [requiresResponse, setRequiresResponse] = useState(false);

  const [routeComment, setRouteComment] = useState("");

  const [assignComment, setAssignComment] = useState("");

  const [revisionComment, setRevisionComment] = useState("");

  const [newComment, setNewComment] = useState("");

  const [sourceLanguage, setSourceLanguage] = useState("");

  const [translatingLanguages, setTranslatingLanguages] = useState<string[]>([]);



  const load = useCallback(() => {

    setLoading(true);

    Promise.all([

      api.get(`/dcs/incoming-letters/${documentId}`),

      api.get(`/dcs/incoming-letters/permissions?documentId=${documentId}`),

    ])

      .then(([l, p]) => {

        setLetter(l.data);

        setPerms(p.data);

      })

      .finally(() => setLoading(false));

  }, [documentId]);



  useEffect(() => {

    load();

  }, [load]);



  useEffect(() => {

    if (!perms) return;

    if (perms.canSendForResolution || perms.canInformAdditional)

      api.get("/dcs/incoming-letters/top-managers").then((r) => setTopManagers(r.data));

    if (perms.canRoute) api.get("/dcs/incoming-letters/departments").then((r) => setDepartments(r.data));

    if (perms.canAssign) api.get(`/dcs/incoming-letters/${documentId}/workers`).then((r) => setWorkers(r.data));

  }, [perms, documentId]);



  const act = async (fn: () => Promise<void>) => {

    setActing(true);

    setError("");

    try {

      await fn();

      load();

      setSelectedResolutionManagers([]);

      setSelectedManagers([]);

    } catch (err: unknown) {

      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error;

      setError(msg ?? t("error"));

    } finally {

      setActing(false);

    }

  };



  if (loading || !letter || !perms) {
    return <DcsWorkflowLoading label={t("loading")} />;
  }



  const stepIndex = currentStepIndex(letter.phase);

  const tabs: { key: TabKey; label: string }[] = [

    { key: "letter", label: t("tabs.letter") },

    { key: "distribution", label: t("tabs.distribution") },

    { key: "connected", label: t("tabs.connected", { count: 0 }) },

  ];



  return (
    <DcsWorkflowShell kind="incoming">
      <DcsDocumentHero
        kind="incoming"
        number={letter.number}
        title={letter.title}
        titleRu={letter.titleRu}
        phaseLabel={phaseLabel(letter.phase, locale)}
        backLabel={t("close")}
        printLabel={t("actions.print")}
        onBack={() => router.push(`/${locale}/automation/office/incoming`)}
        onPrint={() => window.print()}
      />



      <DcsWorkflowStepper
        kind="incoming"
        title={t("workflow.stepperTitle")}
        steps={INCOMING_LETTER_STEPS.map((step) => ({ key: step.id, label: stepLabel(step.id, locale) }))}
        activeIndex={stepIndex}
        hint={phaseHint(letter.phase, locale)}
      />



        <DcsWorkflowCard kind="incoming" title={t("workflow.currentStep")}>
          <DcsTabBar kind="incoming" tabs={tabs} active={tab} onChange={setTab} />



          <div className="p-5 space-y-5">

            {tab === "letter" && (

              <>

                <DcsSectionCard kind="incoming" title={t("tabs.letter")} icon={FileText}>
                  <LetterFields letter={letter} locale={locale} t={t} />
                </DcsSectionCard>

                <DcsErrorAlert message={error} />



                {perms.canSendToTranslation && (

                  <WorkflowPanel title={t("workflow.sendToTranslationTitle")} accent="violet">

                    <p className="text-xs text-foreground/50 mb-3">{t("workflow.sendToTranslationHint")}</p>

                    <div className="grid sm:grid-cols-2 gap-3 mb-3">

                      <div>

                        <label className="block text-[10px] font-bold uppercase tracking-wider text-foreground/40 mb-1">

                          {t("workflow.sourceLanguage")}

                        </label>

                        <select
                          className={inputClass}
                          value={sourceLanguage}
                          onChange={(e) => {
                            setSourceLanguage(e.target.value);
                            setTranslatingLanguages((prev) => prev.filter((c) => c !== e.target.value));
                          }}
                        >

                          <option value="">{t("workflow.selectLanguage")}</option>

                          {TRANSLATION_LANGUAGE_CODES.map((code) => (

                            <option key={code} value={code}>{translationLanguageLabel(code, locale)}</option>

                          ))}

                        </select>

                      </div>

                      <div>

                        <label className="block text-[10px] font-bold uppercase tracking-wider text-foreground/40 mb-1">

                          {t("workflow.translatingLanguage")}

                        </label>

                        <p className="text-[11px] text-foreground/45 mb-2">{t("workflow.translatingLanguageHint")}</p>

                        <div className="grid grid-cols-2 gap-2 rounded-lg border border-border/70 bg-background/60 p-3">

                          {TRANSLATION_LANGUAGE_CODES.filter((c) => c !== sourceLanguage).map((code) => {

                            const checked = translatingLanguages.includes(code);

                            return (

                              <label key={code} className="flex items-center gap-2 text-sm cursor-pointer">

                                <input

                                  type="checkbox"

                                  checked={checked}

                                  disabled={!sourceLanguage}

                                  onChange={(e) => {

                                    setTranslatingLanguages((prev) =>

                                      e.target.checked

                                        ? [...prev, code]

                                        : prev.filter((c) => c !== code)

                                    );

                                  }}

                                  className="rounded border-border text-violet-600 focus:ring-violet-500/30"

                                />

                                <span>{translationLanguageLabel(code, locale)}</span>

                              </label>

                            );

                          })}

                        </div>

                      </div>

                    </div>

                    <Button

                      size="sm"

                      disabled={acting || !sourceLanguage || translatingLanguages.length === 0}

                      onClick={() =>

                        act(() =>

                          api.post(`/dcs/incoming-letters/${documentId}/send-to-translation`, {

                            sourceLanguage,

                            translatingLanguages,

                          })

                        )

                      }

                    >

                      {t("workflow.sendToTranslation")}

                    </Button>

                  </WorkflowPanel>

                )}



                {perms.canCompleteTranslation && (

                  <WorkflowPanel title={t("workflow.completeTranslationTitle")} accent="violet">

                    <Button size="sm" disabled={acting} onClick={() => act(() => api.post(`/dcs/incoming-letters/${documentId}/complete-translation`))}>

                      {t("workflow.completeTranslation")}

                    </Button>

                  </WorkflowPanel>

                )}



                {perms.canRegisterInEds && (

                  <WorkflowPanel title={t("workflow.registerInEdsTitle")} accent="slate">

                    <p className="text-xs text-foreground/50 mb-3">{t("workflow.registerInEdsHint")}</p>

                    <Button size="sm" disabled={acting} onClick={() => act(() => api.post(`/dcs/incoming-letters/${documentId}/register-in-eds`))}>

                      {t("workflow.registerInEds")}

                    </Button>

                  </WorkflowPanel>

                )}



                {perms.canSendForResolution && (

                  <WorkflowPanel title={t("workflow.sendForResolutionTitle")} accent="violet">

                    <p className="text-xs text-foreground/50 mb-3">{t("workflow.sendForResolutionHint")}</p>

                    <div className="grid sm:grid-cols-2 gap-2 max-h-48 overflow-auto mb-3">

                      {topManagers.map((m) => (

                        <label

                          key={m.id}

                          className={cn(

                            "flex items-center gap-2.5 text-sm rounded-xl border px-3 py-2.5 cursor-pointer",

                            selectedResolutionManagers.includes(m.id)

                              ? "border-violet-500/40 bg-violet-500/8"

                              : "border-border/60"

                          )}

                        >

                          <input

                            type="checkbox"

                            checked={selectedResolutionManagers.includes(m.id)}

                            onChange={(e) =>

                              setSelectedResolutionManagers(

                                e.target.checked

                                  ? [...selectedResolutionManagers, m.id]

                                  : selectedResolutionManagers.filter((id) => id !== m.id)

                              )

                            }

                          />

                          {m.fullName}

                        </label>

                      ))}

                    </div>

                    <Button

                      size="sm"

                      disabled={acting || selectedResolutionManagers.length === 0}

                      onClick={() =>

                        act(() =>

                          api.post(`/dcs/incoming-letters/${documentId}/send-for-resolution`, {

                            resolutionManagerIds: selectedResolutionManagers,

                          })

                        )

                      }

                    >

                      {t("workflow.sendForResolution")}

                    </Button>

                  </WorkflowPanel>

                )}



                {perms.canInformAdditional && (

                  <WorkflowPanel title={t("workflow.informAdditionalTitle")} accent="violet">

                    <div className="grid sm:grid-cols-2 gap-2 max-h-48 overflow-auto mb-3">

                      {topManagers

                        .filter((m) =>

                          m.id !== letter.resolutionManagerId

                          && !letter.recipients.some((r) => r.userId === m.id)

                        )

                        .map((m) => (

                          <label

                            key={m.id}

                            className={cn(

                              "flex items-center gap-2.5 text-sm rounded-xl border px-3 py-2.5 cursor-pointer",

                              selectedManagers.includes(m.id)

                                ? "border-violet-500/40 bg-violet-500/8"

                                : "border-border/60"

                            )}

                          >

                            <input

                              type="checkbox"

                              checked={selectedManagers.includes(m.id)}

                              onChange={(e) =>

                                setSelectedManagers(

                                  e.target.checked

                                    ? [...selectedManagers, m.id]

                                    : selectedManagers.filter((id) => id !== m.id)

                                )

                              }

                            />

                            <span className="font-medium">{m.fullName}</span>

                          </label>

                        ))}

                    </div>

                    <Button

                      size="sm"

                      disabled={acting || selectedManagers.length === 0}

                      onClick={() =>

                        act(() =>

                          api.post(`/dcs/incoming-letters/${documentId}/inform-additional`, {

                            topManagerIds: selectedManagers,

                          })

                        )

                      }

                    >

                      {t("workflow.informAdditional")}

                    </Button>

                  </WorkflowPanel>

                )}



                {perms.canRoute && (

                  <WorkflowPanel title={t("workflow.routeTitle")} accent="amber">

                    <select className={inputClass} value={targetDept} onChange={(e) => setTargetDept(e.target.value)}>

                      <option value="">{t("workflow.selectDept")}</option>

                      {departments.map((d) => (

                        <option key={d.id} value={d.id}>{deptLabel(d.name, d.nameEn, locale)}</option>

                      ))}

                    </select>

                    <textarea className={cn(inputClass, "mt-2 min-h-[72px]")} rows={2} placeholder={t("workflow.comment")} value={routeComment} onChange={(e) => setRouteComment(e.target.value)} />

                    <Button size="sm" className="mt-2" disabled={acting || !targetDept} onClick={() => act(async () => { await api.post(`/dcs/incoming-letters/${documentId}/route`, { targetDepartmentId: targetDept, comment: routeComment || null }); setRouteComment(""); })}>

                      {t("workflow.route")}

                    </Button>

                  </WorkflowPanel>

                )}



                {perms.canAssign && (

                  <WorkflowPanel title={t("workflow.assignTitle")} accent="sky">

                    <select className={inputClass} value={assigneeId} onChange={(e) => setAssigneeId(e.target.value)}>

                      <option value="">{t("workflow.selectWorker")}</option>

                      {workers.map((w) => (

                        <option key={w.id} value={w.id}>{w.fullName}</option>

                      ))}

                    </select>

                    <input className={cn(inputClass, "mt-2")} placeholder={t("workflow.assignmentTask")} value={assignmentTask} onChange={(e) => setAssignmentTask(e.target.value)} />

                    <input type="date" className={cn(inputClass, "mt-2")} value={dueDate} onChange={(e) => setDueDate(e.target.value)} />

                    <textarea className={cn(inputClass, "mt-2 min-h-[64px]")} rows={2} placeholder={t("workflow.comment")} value={assignComment} onChange={(e) => setAssignComment(e.target.value)} />

                    <Button size="sm" className="mt-2" disabled={acting || !assigneeId} onClick={() => act(async () => { await api.post(`/dcs/incoming-letters/${documentId}/assign`, { assigneeId, assignmentTask: assignmentTask || null, dueDate: dueDate || null, comment: assignComment || null }); setAssignComment(""); })}>

                      {t("workflow.assign")}

                    </Button>

                  </WorkflowPanel>

                )}



                {perms.canAccept && (

                  <WorkflowPanel title={t("workflow.acceptTitle")} accent="sky">

                    <p className="text-xs text-foreground/50 mb-3">{t("workflow.acceptHint")}</p>

                    <label className="flex items-center gap-2 text-sm mb-3">

                      <input type="checkbox" checked={requiresResponse} onChange={(e) => setRequiresResponse(e.target.checked)} />

                      {t("workflow.requiresResponse")}

                    </label>

                    {requiresResponse && (

                      <p className="text-xs text-amber-600 dark:text-amber-400 mb-3">{t("workflow.outgoingHint")}</p>

                    )}

                    <Button size="sm" disabled={acting} onClick={() => act(() => api.post(`/dcs/incoming-letters/${documentId}/accept`, { requiresResponse }))}>

                      {t("workflow.accept")}

                    </Button>

                  </WorkflowPanel>

                )}



                {perms.canReport && (

                  <WorkflowPanel title={t("workflow.reportTitle")} accent="emerald">

                    <Button size="sm" disabled={acting} onClick={() => act(() => api.post(`/dcs/incoming-letters/${documentId}/report`))}>

                      {t("workflow.report")}

                    </Button>

                  </WorkflowPanel>

                )}



                {perms.canRequestRevision && (

                  <WorkflowPanel title={t("workflow.revisionTitle")} accent="amber">

                    <textarea className={cn(inputClass, "min-h-[72px]")} rows={2} placeholder={t("workflow.revisionComment")} value={revisionComment} onChange={(e) => setRevisionComment(e.target.value)} />

                    <Button size="sm" className="mt-2" disabled={acting || !revisionComment.trim()} onClick={() => act(async () => { await api.post(`/dcs/incoming-letters/${documentId}/request-revision`, { body: revisionComment }); setRevisionComment(""); })}>

                      {t("workflow.requestRevision")}

                    </Button>

                  </WorkflowPanel>

                )}



                {perms.canAcceptCompletion && (

                  <WorkflowPanel title={t("workflow.acceptCompletionTitle")} accent="emerald">

                    <Button size="sm" disabled={acting} onClick={() => act(() => api.post(`/dcs/incoming-letters/${documentId}/accept-completion`))}>

                      {t("workflow.acceptCompletion")}

                    </Button>

                  </WorkflowPanel>

                )}



                {perms.canArchive && (

                  <WorkflowPanel title={t("workflow.archiveTitle")} accent="slate">

                    <p className="text-xs text-foreground/50 mb-3">{t("workflow.archiveHint")}</p>

                    <Button size="sm" disabled={acting} onClick={() => act(() => api.post(`/dcs/incoming-letters/${documentId}/archive`))}>

                      {t("workflow.archive")}

                    </Button>

                  </WorkflowPanel>

                )}



                {letter.requiresResponse && letter.phase !== "Completed" && (

                  <div className="rounded-xl border border-amber-500/30 bg-amber-500/5 px-4 py-3 text-sm text-amber-800 dark:text-amber-200">

                    {t("workflow.responseRequiredBanner")}

                  </div>

                )}



                {perms.canComment && (

                  <WorkflowPanel title={t("workflow.comments")} accent="slate">

                    <div className="space-y-2 mb-3">

                      {letter.comments.length === 0 ? (

                        <p className="text-xs text-foreground/40">{t("workflow.noComments")}</p>

                      ) : (

                        letter.comments.map((c) => (

                          <div key={c.id} className="text-sm border border-border/50 rounded-xl p-3 bg-foreground/[0.02]">

                            <p className="font-semibold text-xs text-foreground/45">{c.authorName}</p>

                            <p className="mt-1">{c.body}</p>

                            <p className="text-[10px] text-foreground/35 mt-1">{new Date(c.createdAt).toLocaleString(locale)}</p>

                          </div>

                        ))

                      )}

                    </div>

                    <textarea className={cn(inputClass, "min-h-[72px]")} rows={2} placeholder={t("workflow.comment")} value={newComment} onChange={(e) => setNewComment(e.target.value)} />

                    <Button size="sm" variant="secondary" className="mt-2" disabled={acting || !newComment.trim()} onClick={() => act(async () => { await api.post(`/dcs/incoming-letters/${documentId}/comments`, { body: newComment }); setNewComment(""); })}>

                      {t("workflow.addComment")}

                    </Button>

                  </WorkflowPanel>

                )}

              </>

            )}



            {tab === "distribution" && (

              <ul className="space-y-2">

                {letter.resolutionManagerName && (

                  <li className="flex items-center justify-between gap-3 text-sm border border-violet-500/25 rounded-xl px-4 py-3 bg-violet-500/5">

                    <div className="flex items-center gap-2.5">

                      <Users size={16} className="text-violet-500/70" />

                      <span className="font-medium">{letter.resolutionManagerName}</span>

                    </div>

                    <span className="text-violet-600 text-xs font-semibold">{t("workflow.resolutionManager")}</span>

                  </li>

                )}

                {letter.recipients.map((r) => (

                  <li key={r.id} className="flex items-center justify-between gap-3 text-sm border border-border/50 rounded-xl px-4 py-3 bg-foreground/[0.02]">

                    <div className="flex items-center gap-2.5">

                      <Users size={16} className="text-violet-500/70" />

                      <span className="font-medium">{r.userName}</span>

                    </div>

                    <span className={cn(

                      "text-xs font-semibold",

                      r.forInformation ? "text-emerald-600" : "text-violet-600"

                    )}>

                      {r.forInformation ? t("workflow.forInformation") : t("workflow.resolutionManager")}

                    </span>

                  </li>

                ))}

                {!letter.resolutionManagerName && letter.recipients.length === 0 && (

                  <p className="text-center text-foreground/40 text-sm py-10">{t("tabs.distributionEmpty")}</p>

                )}

              </ul>

            )}



            {tab === "connected" && (

              <p className="text-center text-foreground/40 text-sm py-10">{t("tabs.connectedEmpty")}</p>

            )}

          </div>

        </DcsWorkflowCard>

    </DcsWorkflowShell>

  );

}



const inputClass = dcsInputClass("incoming");



function LetterFields({

  letter,

  locale,

  t,

}: {

  letter: IncomingLetter;

  locale: string;

  t: ReturnType<typeof useTranslations<"dcs.incoming">>;

}) {

  const field = (label: string, value?: string) => <DcsDetailField label={label} value={value || "—"} />;



  return (

    <div className="space-y-4">

      <div className="grid grid-cols-2 md:grid-cols-5 gap-3">

        {field(t("fields.inNum"), letter.incomingNumber)}

        {field(t("fields.inDate"), formatDate(letter.incomingDate, locale))}

        {field(t("fields.regNum"), letter.number)}

        {field(t("fields.regDate"), formatDate(letter.registeredAt, locale))}

        {field(t("fields.recordBook"), letter.recordBook)}

      </div>

      <div className="grid md:grid-cols-2 gap-3">

        {field(t("fields.subjectEn"), letter.title)}

        {field(t("fields.subjectRu"), letter.titleRu)}

      </div>

      <div className="grid md:grid-cols-2 gap-3">

        {field(t("fields.otherSenders"), letter.senderName)}

        {field(t("fields.receiver"), letter.receiverName)}

        {field(t("fields.registeredBy"), letter.authorName)}

        {field(t("workflow.assignee"), letter.assigneeName)}

      </div>

      {letter.assignmentTask && field(t("workflow.assignmentTask"), letter.assignmentTask)}

      {letter.dueDate && field(t("workflow.dueDate"), formatDate(letter.dueDate, locale))}

      {letter.sourceLanguage && field(t("workflow.sourceLanguage"), translationLanguageLabel(letter.sourceLanguage, locale))}

      {letter.translatingLanguages && letter.translatingLanguages.length > 0 && field(t("workflow.translatingLanguage"), translationLanguagesLabel(letter.translatingLanguages, locale))}

      {letter.helpDeskTicketNumber && (

        <div>

          <label className="block text-[10px] font-bold uppercase tracking-wider text-foreground/40 mb-1">{t("workflow.helpDeskTicket")}</label>

          <a

            href={`/${locale}/helpdesk/tickets/${letter.helpDeskTicketId}`}

            className="inline-flex h-10 items-center px-3 rounded-xl border border-violet-500/25 bg-violet-500/5 text-sm text-violet-700 dark:text-violet-300 hover:underline"

          >

            {letter.helpDeskTicketNumber}

          </a>

        </div>

      )}

      {letter.routedToDepartmentName && (

        <div className="grid md:grid-cols-2 gap-3">

          {field(t("fields.routedDept"), deptLabel(letter.routedToDepartmentName, letter.routedToDepartmentNameEn ?? "", locale))}

          {field(t("fields.routedBy"), letter.routedByName)}

        </div>

      )}

      {(letter.attachmentFileName || letter.translatedAttachmentFileName) && (

        <div className="space-y-2">

          {letter.attachmentFileName && (

            <div>

              <p className="text-[10px] font-bold uppercase tracking-wider text-foreground/40 mb-1.5">

                {t("sections.originalDocument")}

              </p>

              <div className="flex items-center gap-2 text-sm rounded-xl border border-sky-500/20 bg-sky-500/5 px-3 py-2.5">

                <Paperclip size={14} className="text-sky-600 shrink-0" />

                {letter.attachmentStorageKey ? (

                  <a href={fileDownloadUrl(letter.attachmentStorageKey, letter.attachmentFileName)} target="_blank" rel="noreferrer" className="text-sky-700 hover:underline dark:text-sky-300 truncate">

                    {letter.attachmentFileName}

                  </a>

                ) : (

                  <span className="truncate">{letter.attachmentFileName}</span>

                )}

              </div>

            </div>

          )}

          {letter.translatedAttachmentFileName && (

            <div>

              <p className="text-[10px] font-bold uppercase tracking-wider text-foreground/40 mb-1.5">

                {t("sections.translatedDocument")}

              </p>

              <div className="flex items-center gap-2 text-sm rounded-xl border border-emerald-500/20 bg-emerald-500/5 px-3 py-2.5">

                <Paperclip size={14} className="text-emerald-600 shrink-0" />

                {letter.translatedAttachmentStorageKey ? (

                  <a href={fileDownloadUrl(letter.translatedAttachmentStorageKey, letter.translatedAttachmentFileName)} target="_blank" rel="noreferrer" className="text-emerald-700 hover:underline dark:text-emerald-300 truncate">

                    {letter.translatedAttachmentFileName}

                  </a>

                ) : (

                  <span className="truncate">{letter.translatedAttachmentFileName}</span>

                )}

              </div>

            </div>

          )}

        </div>

      )}

    </div>

  );

}



function WorkflowPanel({

  title,

  children,

  accent,

}: {

  title: string;

  children: React.ReactNode;

  accent: "violet" | "amber" | "sky" | "emerald" | "slate";

}) {

  const borders: Record<typeof accent, string> = {

    violet: "border-violet-500/25 bg-violet-500/[0.03]",

    amber: "border-amber-500/25 bg-amber-500/[0.03]",

    sky: "border-sky-500/25 bg-sky-500/[0.03]",

    emerald: "border-emerald-500/25 bg-emerald-500/[0.03]",

    slate: "border-border/60 bg-foreground/[0.02]",

  };

  return (
    <DcsWorkflowCard kind="incoming" title={title}>
      <div className={cn("rounded-xl border p-4", borders[accent])}>{children}</div>
    </DcsWorkflowCard>
  );

}


