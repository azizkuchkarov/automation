"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { ChevronDown, ChevronRight, Loader2, Paperclip, X } from "lucide-react";
import api from "@/lib/api";
import {
  IncomingLetter,
  IncomingLetterDepartment,
  IncomingLetterPermissions,
  IncomingLetterUser,
  phaseLabel,
} from "@/lib/incomingLetter";
import { deptLabel } from "@/lib/dcs";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

type TabKey = "letter" | "distribution" | "connected";

interface Props {
  documentId: string;
}

function formatDate(value?: string, locale?: string) {
  if (!value) return "";
  return new Date(value).toLocaleDateString(locale?.startsWith("en") ? "en-GB" : "ru-RU", {
    day: "2-digit", month: "2-digit", year: "numeric",
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

  const [topManagers, setTopManagers] = useState<IncomingLetterUser[]>([]);
  const [selectedManagers, setSelectedManagers] = useState<string[]>([]);
  const [departments, setDepartments] = useState<IncomingLetterDepartment[]>([]);
  const [targetDept, setTargetDept] = useState("");
  const [workers, setWorkers] = useState<IncomingLetterUser[]>([]);
  const [assigneeId, setAssigneeId] = useState("");
  const [comment, setComment] = useState("");

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

  useEffect(() => { load(); }, [load]);

  useEffect(() => {
    if (perms?.canInform) api.get("/dcs/incoming-letters/top-managers").then((r) => setTopManagers(r.data));
    if (perms?.canRoute) api.get("/dcs/incoming-letters/departments").then((r) => setDepartments(r.data));
    if (perms?.canAssign) api.get(`/dcs/incoming-letters/${documentId}/workers`).then((r) => setWorkers(r.data));
  }, [perms, documentId]);

  const act = async (fn: () => Promise<void>) => {
    setActing(true);
    try { await fn(); load(); } finally { setActing(false); }
  };

  if (loading || !letter || !perms) {
    return (
      <div className="flex-1 flex items-center justify-center gap-2 text-foreground/40">
        <Loader2 className="animate-spin" size={20} />
        <span className="text-sm">{t("loading")}</span>
      </div>
    );
  }

  const tabs: { key: TabKey; label: string }[] = [
    { key: "letter", label: t("tabs.letter") },
    { key: "distribution", label: t("tabs.distribution") },
    { key: "connected", label: t("tabs.connected", { count: 0 }) },
  ];

  return (
    <div className="flex-1 overflow-auto bg-[#eef1f5] dark:bg-[#0c1117] p-4 md:p-6">
      <div className="max-w-5xl mx-auto bg-white dark:bg-surface rounded-lg shadow-lg border border-slate-200/80 dark:border-border overflow-hidden flex flex-col min-h-[calc(100vh-8rem)]">
        <div className="flex items-center justify-between px-5 py-4 border-b border-slate-200 dark:border-border">
          <h1 className="text-lg font-normal text-foreground/90">
            {t("viewTitle")} <span className="font-semibold text-atg-blue">{letter.number}</span>
          </h1>
          <button type="button" onClick={() => router.push(`/${locale}/automation/office/incoming`)} className="p-1.5 rounded-md text-foreground/40 hover:text-foreground hover:bg-foreground/5">
            <X size={20} />
          </button>
        </div>

        <div className="px-5 py-2 bg-slate-50/80 dark:bg-foreground/[0.02] border-b border-slate-200 dark:border-border text-sm">
          {t("workflow.phase")}: <span className="font-semibold">{phaseLabel(letter.phase, locale)}</span>
        </div>

        <div className="flex border-b border-slate-200 dark:border-border px-5 gap-6">
          {tabs.map(({ key, label }) => (
            <button key={key} type="button" onClick={() => setTab(key)} className={cn("relative py-3 text-sm font-medium", tab === key ? "text-atg-blue" : "text-foreground/50")}>
              {label}
              {tab === key && <span className="absolute bottom-0 left-0 right-0 h-0.5 bg-atg-blue" />}
            </button>
          ))}
        </div>

        <div className="flex-1 overflow-auto px-5 py-5 space-y-5">
          {tab === "letter" && (
            <>
              <LetterFields letter={letter} locale={locale} t={t} />
              {perms.canInform && (
                <WorkflowPanel title={t("workflow.informTitle")}>
                  <p className="text-xs text-foreground/50 mb-3">{t("workflow.informHint")}</p>
                  <div className="space-y-2 max-h-48 overflow-auto mb-3">
                    {topManagers.map((m) => (
                      <label key={m.id} className="flex items-center gap-2 text-sm">
                        <input type="checkbox" checked={selectedManagers.includes(m.id)} onChange={(e) => {
                          setSelectedManagers(e.target.checked ? [...selectedManagers, m.id] : selectedManagers.filter((id) => id !== m.id));
                        }} />
                        {m.fullName}
                      </label>
                    ))}
                  </div>
                  <Button size="sm" disabled={acting || selectedManagers.length === 0} onClick={() => act(async () => {
                    await api.post(`/dcs/incoming-letters/${documentId}/inform`, { topManagerIds: selectedManagers });
                  })}>{t("workflow.inform")}</Button>
                </WorkflowPanel>
              )}
              {perms.canRoute && (
                <WorkflowPanel title={t("workflow.routeTitle")}>
                  <select className="w-full rounded-lg border border-border px-3 py-2 text-sm mb-2" value={targetDept} onChange={(e) => setTargetDept(e.target.value)}>
                    <option value="">{t("workflow.selectDept")}</option>
                    {departments.map((d) => (
                      <option key={d.id} value={d.id}>{deptLabel(d.name, d.nameEn, locale)}</option>
                    ))}
                  </select>
                  <textarea className="w-full rounded-lg border border-border px-3 py-2 text-sm mb-2" rows={2} placeholder={t("workflow.comment")} value={comment} onChange={(e) => setComment(e.target.value)} />
                  <Button size="sm" disabled={acting || !targetDept} onClick={() => act(async () => {
                    await api.post(`/dcs/incoming-letters/${documentId}/route`, { targetDepartmentId: targetDept, comment: comment || null });
                    setComment("");
                  })}>{t("workflow.route")}</Button>
                </WorkflowPanel>
              )}
              {perms.canAssign && (
                <WorkflowPanel title={t("workflow.assignTitle")}>
                  <select className="w-full rounded-lg border border-border px-3 py-2 text-sm mb-2" value={assigneeId} onChange={(e) => setAssigneeId(e.target.value)}>
                    <option value="">{t("workflow.selectWorker")}</option>
                    {workers.map((w) => <option key={w.id} value={w.id}>{w.fullName}</option>)}
                  </select>
                  <Button size="sm" disabled={acting || !assigneeId} onClick={() => act(async () => {
                    await api.post(`/dcs/incoming-letters/${documentId}/assign`, { assigneeId, comment: null });
                  })}>{t("workflow.assign")}</Button>
                </WorkflowPanel>
              )}
              {perms.canComplete && (
                <WorkflowPanel title={t("workflow.completeTitle")}>
                  <Button size="sm" disabled={acting} onClick={() => act(async () => {
                    await api.post(`/dcs/incoming-letters/${documentId}/complete`);
                  })}>{t("workflow.complete")}</Button>
                </WorkflowPanel>
              )}
              {perms.canComment && (
                <WorkflowPanel title={t("workflow.comments")}>
                  <div className="space-y-2 mb-3">
                    {letter.comments.map((c) => (
                      <div key={c.id} className="text-sm border border-border/60 rounded-lg p-2">
                        <p className="font-medium text-xs text-foreground/50">{c.authorName}</p>
                        <p>{c.body}</p>
                      </div>
                    ))}
                  </div>
                  <textarea className="w-full rounded-lg border border-border px-3 py-2 text-sm mb-2" rows={2} value={comment} onChange={(e) => setComment(e.target.value)} />
                  <Button size="sm" variant="secondary" disabled={acting || !comment.trim()} onClick={() => act(async () => {
                    await api.post(`/dcs/incoming-letters/${documentId}/comments`, { body: comment });
                    setComment("");
                  })}>{t("workflow.addComment")}</Button>
                </WorkflowPanel>
              )}
            </>
          )}
          {tab === "distribution" && (
            letter.recipients.length > 0 ? (
              <ul className="space-y-2">
                {letter.recipients.map((r) => (
                  <li key={r.id} className="flex justify-between text-sm border border-border/60 rounded-lg px-3 py-2">
                    <span>{r.userName}</span>
                    <span className="text-emerald-600 text-xs">{t("workflow.informed")}</span>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="text-center text-foreground/40 text-sm py-8">{t("tabs.distributionEmpty")}</p>
            )
          )}
          {tab === "connected" && <p className="text-center text-foreground/40 text-sm py-8">{t("tabs.connectedEmpty")}</p>}
        </div>

        <div className="flex items-center justify-between px-5 py-3 border-t border-slate-200 dark:border-border bg-slate-50/50">
          <p className="text-sm text-foreground/60">{t("statusLabel")}: <span className="font-semibold">{phaseLabel(letter.phase, locale)}</span></p>
          <Button variant="secondary" size="sm" onClick={() => router.push(`/${locale}/automation/office/incoming`)}>{t("close")}</Button>
        </div>
      </div>
    </div>
  );
}

function LetterFields({ letter, locale, t }: { letter: IncomingLetter; locale: string; t: ReturnType<typeof useTranslations> }) {
  const field = (label: string, value?: string) => (
    <div>
      <label className="block text-[11px] text-foreground/55 mb-1">{label}</label>
      <div className="h-9 px-3 flex items-center rounded-md border border-slate-200 dark:border-border bg-slate-50/80 text-sm truncate">{value || "\u00a0"}</div>
    </div>
  );
  return (
    <div className="space-y-3">
      <div className="grid grid-cols-2 md:grid-cols-5 gap-3">
        {field(t("fields.inNum"), letter.incomingNumber ?? "№ NA")}
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
      {letter.attachmentFileName && (
        <div className="flex items-center gap-2 text-sm text-atg-blue"><Paperclip size={14} />{letter.attachmentFileName}</div>
      )}
    </div>
  );
}

function WorkflowPanel({ title, children }: { title: string; children: React.ReactNode }) {
  const [open, setOpen] = useState(true);
  return (
    <div className="border border-atg-blue/20 rounded-lg overflow-hidden">
      <button type="button" onClick={() => setOpen(!open)} className="flex items-center gap-2 w-full px-4 py-2.5 bg-atg-blue/5 text-sm font-semibold text-left">
        {open ? <ChevronDown size={16} /> : <ChevronRight size={16} />}
        {title}
      </button>
      {open && <div className="px-4 py-3">{children}</div>}
    </div>
  );
}
