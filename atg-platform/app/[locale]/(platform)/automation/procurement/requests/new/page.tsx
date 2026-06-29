"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import {
  Building2,
  Calendar,
  FilePlus,
  Flag,
  MapPin,
  Paperclip,
  User,
} from "lucide-react";
import api, { getApiErrorMessage } from "@/lib/api";
import {
  ProcurementAttachmentKind,
  ProcurementApproverRole,
  ProcurementCreateOptions,
  ProcurementInitiatorDepartment,
  ProcurementRequestUser,
} from "@/lib/procurementRequest";
import {
  PROCUREMENT_PRIORITIES,
  ProcurementPriority,
  priorityDotClass,
  priorityLabel,
  priorityRingClass,
  regionLabelFromDept,
} from "@/lib/procurementPriority";
import { DocumentFileUpload } from "@/components/dcs/DocumentFileUpload";
import { DcsPageHeader } from "@/components/dcs/DcsPageHeader";
import { dcsTheme } from "@/components/dcs/dcsTheme";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

interface AssignableUser {
  id: string;
  fullName: string;
}

type ApproverRow = { userId: string; role: ProcurementApproverRole };
type AttachmentRow = { kind: ProcurementAttachmentKind; fileName: string; storageKey?: string };

export default function NewProcurementRequestPage() {
  const t = useTranslations("dcs.request");
  const locale = useLocale();
  const router = useRouter();
  const [options, setOptions] = useState<ProcurementCreateOptions | null>(null);
  const [initiatorDepartments, setInitiatorDepartments] = useState<ProcurementInitiatorDepartment[]>([]);
  const [initiators, setInitiators] = useState<ProcurementRequestUser[]>([]);
  const [responsibleUsers, setResponsibleUsers] = useState<ProcurementRequestUser[]>([]);
  const [assignable, setAssignable] = useState<AssignableUser[]>([]);
  const [flow, setFlow] = useState<"TechnicalAffairs" | "Express">("TechnicalAffairs");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  const [priority, setPriority] = useState<ProcurementPriority>("Medium");
  const [eamNumber, setEamNumber] = useState("");
  const [initiatorDepartmentId, setInitiatorDepartmentId] = useState("");
  const [initiatorId, setInitiatorId] = useState("");
  const [subject, setSubject] = useState("");
  const [responsibleId, setResponsibleId] = useState("");
  const [eamDate, setEamDate] = useState(new Date().toISOString().slice(0, 10));
  const [deadline, setDeadline] = useState("");
  const [tasAttachments, setTasAttachments] = useState<AttachmentRow[]>([
    { kind: "TechnicalAssignment", fileName: "" },
  ]);

  const [subjectEn, setSubjectEn] = useState("");
  const [subjectRu, setSubjectRu] = useState("");
  const [approvers, setApprovers] = useState<ApproverRow[]>([
    { userId: "", role: "SectionHead" },
    { userId: "", role: "TopManager" },
  ]);
  const [expressAttachments, setExpressAttachments] = useState<AttachmentRow[]>([
    { kind: "TechnicalAssignment", fileName: "" },
  ]);

  const ctx = options?.formContext;
  const isTas = flow === "TechnicalAffairs";

  const selectedDept = useMemo(
    () => initiatorDepartments.find((d) => d.id === initiatorDepartmentId),
    [initiatorDepartments, initiatorDepartmentId]
  );

  const regionDisplay = useMemo(() => {
    if (isTas && selectedDept) {
      return regionLabelFromDept(selectedDept.organizationCode, selectedDept.organizationName, selectedDept.isStation, locale);
    }
    if (ctx) {
      return locale.startsWith("en") ? ctx.regionLabelEn : ctx.regionLabelRu;
    }
    return "—";
  }, [isTas, selectedDept, ctx, locale]);

  const initiatingDepartmentDisplay = useMemo(() => {
    if (isTas && selectedDept) {
      return locale.startsWith("en") && selectedDept.nameEn ? selectedDept.nameEn : selectedDept.name;
    }
    if (ctx?.initiatingDepartmentName) {
      return locale.startsWith("en") && ctx.initiatingDepartmentNameEn
        ? ctx.initiatingDepartmentNameEn
        : ctx.initiatingDepartmentName;
    }
    return "—";
  }, [isTas, selectedDept, ctx, locale]);

  const initiatingEmployeeDisplay = useMemo(() => {
    if (isTas) {
      const u = initiators.find((i) => i.id === initiatorId);
      return u?.fullName ?? "—";
    }
    return ctx?.initiatingEmployeeName ?? "—";
  }, [isTas, initiators, initiatorId, ctx]);

  const regDateDisplay = ctx?.regDate
    ? new Date(ctx.regDate).toLocaleDateString(locale)
    : new Date().toLocaleDateString(locale);

  useEffect(() => {
    api.get("/dcs/procurement-requests/create-options").then((r) => {
      setOptions(r.data);
      if (r.data.defaultFlow) setFlow(r.data.defaultFlow);
    });
    api.get("/tasks/assignees").then((r) => setAssignable(r.data));
  }, []);

  useEffect(() => {
    if (isTas) {
      api.get("/dcs/procurement-requests/initiator-departments").then((r) => setInitiatorDepartments(r.data));
      api.get("/dcs/procurement-requests/responsible-users").then((r) => setResponsibleUsers(r.data));
    }
  }, [isTas]);

  useEffect(() => {
    if (!isTas || !initiatorDepartmentId) {
      setInitiators([]);
      setInitiatorId("");
      return;
    }
    api
      .get(`/dcs/procurement-requests/initiators?departmentId=${initiatorDepartmentId}`)
      .then((r) => setInitiators(r.data))
      .catch(() => setInitiators([]));
    setInitiatorId("");
  }, [isTas, initiatorDepartmentId]);

  const inputClass = cn(
    "w-full rounded-xl border border-slate-200/80 dark:border-white/10 bg-white/70 dark:bg-white/[0.04]",
    "px-3.5 py-2.5 text-sm shadow-inner focus:outline-none focus:ring-2 focus:ring-sky-500/25 focus:border-sky-500/40"
  );

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setSubmitting(true);
    try {
      if (isTas) {
        const r = await api.post("/dcs/procurement-requests/tas", {
          eamNumber,
          initiatorId,
          procurementName: subject,
          responsibleId,
          eamFormationDate: eamDate,
          deadline,
          priority,
          attachments: tasAttachments
            .filter((a) => a.storageKey)
            .map((a) => ({ kind: a.kind, fileName: a.fileName, storageKey: a.storageKey })),
        });
        router.push(`/${locale}/automation/documents/${r.data.id}`);
      } else {
        if (!subjectEn.trim() || !subjectRu.trim()) {
          setError(t("bilingualRequired"));
          return;
        }
        const r = await api.post("/dcs/procurement-requests/express", {
          subjectEn: subjectEn.trim(),
          subjectRu: subjectRu.trim(),
          approvers: approvers.filter((a) => a.userId),
          attachments: expressAttachments.filter((a) => a.storageKey).map((a) => ({
            kind: a.kind,
            fileName: a.fileName,
            storageKey: a.storageKey,
          })),
          priority,
        });
        router.push(`/${locale}/automation/documents/${r.data.id}`);
      }
    } catch (err: unknown) {
      setError(getApiErrorMessage(err, t("error")));
    } finally {
      setSubmitting(false);
    }
  };

  if (!options) {
    return (
      <div className="flex-1 flex items-center justify-center text-foreground/40 text-sm">{t("loading")}</div>
    );
  }

  if (!options.canCreateTas && !options.canCreateExpress) {
    return (
      <div className="flex-1 flex items-center justify-center text-foreground/50 text-sm px-6 text-center">
        {t("noAccess")}
      </div>
    );
  }

  return (
    <>
      <DcsPageHeader
        title={t("newTitle")}
        subtitle={t("newSubtitleModern")}
        breadcrumb={t("newTitle")}
        icon={FilePlus}
        iconClassName="bg-sky-500/10 text-sky-600"
      />

      <div className="flex-1 overflow-auto px-6 py-6">
        <form onSubmit={submit} className="max-w-5xl mx-auto space-y-6">
          {options.canCreateTas && options.canCreateExpress && (
            <div className={cn("flex gap-2 p-1.5", dcsTheme.premiumCard)}>
              {(["TechnicalAffairs", "Express"] as const).map((f) => (
                <button
                  key={f}
                  type="button"
                  onClick={() => setFlow(f)}
                  className={cn(
                    "flex-1 py-2.5 rounded-xl text-sm font-semibold transition-all",
                    flow === f ? "bg-sky-600 text-white shadow-md shadow-sky-600/25" : "text-foreground/50 hover:text-foreground"
                  )}
                >
                  {f === "TechnicalAffairs" ? t("flowTas") : t("flowExpress")}
                </button>
              ))}
            </div>
          )}

          <div className={cn("overflow-hidden", dcsTheme.premiumCard, priorityRingClass(priority))}>
            <div className="px-6 py-4 border-b border-slate-200/60 dark:border-white/[0.06] bg-gradient-to-r from-slate-50/80 to-transparent dark:from-white/[0.03] flex flex-wrap items-center justify-between gap-3">
              <div>
                <p className="text-[10px] font-bold uppercase tracking-[0.2em] text-foreground/40">{t("createRequest")}</p>
                <h2 className="text-base font-bold mt-0.5">{t("createRequestHint")}</h2>
              </div>
              <div className="flex items-center gap-2">
                <span className={cn("w-3 h-3 rounded-full shadow-sm", priorityDotClass(priority))} />
                <span className="text-sm font-semibold text-foreground/70">{priorityLabel(priority, locale)}</span>
              </div>
            </div>

            <div className="p-6 grid lg:grid-cols-5 gap-8">
              {/* Auto / metadata column */}
              <div className="lg:col-span-2 space-y-4">
                <p className="text-[10px] font-bold uppercase tracking-wider text-foreground/40">{t("autoFields")}</p>
                <ReadOnlyField icon={MapPin} label={t("region")} value={regionDisplay} />
                <ReadOnlyField icon={Calendar} label={t("regDate")} value={regDateDisplay} />
                <ReadOnlyField icon={Building2} label={t("initiatorDepartment")} value={initiatingDepartmentDisplay} />
                <ReadOnlyField icon={User} label={t("initiatingEmployee")} value={initiatingEmployeeDisplay} />

                <div className="pt-2">
                  <p className="text-[10px] font-bold uppercase tracking-wider text-foreground/40 mb-2 flex items-center gap-1.5">
                    <Flag size={12} /> {t("priority")}
                  </p>
                  <div className="grid grid-cols-2 gap-2">
                    {PROCUREMENT_PRIORITIES.map((p) => (
                      <button
                        key={p}
                        type="button"
                        onClick={() => setPriority(p)}
                        className={cn(
                          "flex items-center gap-2 rounded-xl border px-3 py-2.5 text-left text-sm font-medium transition-all",
                          priority === p
                            ? cn("bg-white dark:bg-white/[0.06] shadow-sm", priorityRingClass(p))
                            : "border-transparent bg-foreground/[0.03] text-foreground/55 hover:bg-foreground/[0.05]"
                        )}
                      >
                        <span className={cn("w-2.5 h-2.5 rounded-full shrink-0", priorityDotClass(p))} />
                        {priorityLabel(p, locale)}
                      </button>
                    ))}
                  </div>
                </div>
              </div>

              {/* User input column */}
              <div className="lg:col-span-3 space-y-5">
                <p className="text-[10px] font-bold uppercase tracking-wider text-foreground/40">{t("requestDetails")}</p>

                {isTas ? (
                  <>
                    {isTas && (
                      <div className="grid sm:grid-cols-2 gap-4">
                        <Field label={t("initiatorDepartment")}>
                          <select
                            className={inputClass}
                            value={initiatorDepartmentId}
                            onChange={(e) => setInitiatorDepartmentId(e.target.value)}
                            required
                          >
                            <option value="">{t("selectDepartment")}</option>
                            {Object.entries(
                              initiatorDepartments.reduce<Record<string, ProcurementInitiatorDepartment[]>>((acc, d) => {
                                (acc[d.organizationName] ??= []).push(d);
                                return acc;
                              }, {})
                            ).map(([orgName, depts]) => (
                              <optgroup key={orgName} label={orgName}>
                                {depts.map((d) => (
                                  <option key={d.id} value={d.id}>
                                    {locale === "en" && d.nameEn ? d.nameEn : d.name}
                                  </option>
                                ))}
                              </optgroup>
                            ))}
                          </select>
                        </Field>
                        <Field label={t("initiatingEmployee")}>
                          <select
                            className={inputClass}
                            value={initiatorId}
                            onChange={(e) => setInitiatorId(e.target.value)}
                            required
                            disabled={!initiatorDepartmentId}
                          >
                            <option value="">{initiatorDepartmentId ? t("selectUser") : t("selectDepartmentFirst")}</option>
                            {initiators.map((u) => (
                              <option key={u.id} value={u.id}>{u.fullName}</option>
                            ))}
                          </select>
                        </Field>
                      </div>
                    )}

                    <Field label={t("subject")}>
                      <textarea
                        className={cn(inputClass, "min-h-[88px] resize-y")}
                        value={subject}
                        onChange={(e) => setSubject(e.target.value)}
                        required
                        maxLength={500}
                        placeholder={t("subjectPlaceholder")}
                      />
                      <p className="text-[11px] text-foreground/40 mt-1 text-right">{subject.length}/500</p>
                    </Field>

                    <Field label={t("eamNumber")}>
                      <input className={inputClass} value={eamNumber} onChange={(e) => setEamNumber(e.target.value)} required />
                    </Field>

                    <div className="grid sm:grid-cols-2 gap-4">
                      <Field label={t("responsible")}>
                        <select className={inputClass} value={responsibleId} onChange={(e) => setResponsibleId(e.target.value)} required>
                          <option value="">{t("selectUser")}</option>
                          {responsibleUsers.map((u) => (
                            <option key={u.id} value={u.id}>{u.fullName}</option>
                          ))}
                        </select>
                      </Field>
                      <Field label={t("deadline")}>
                        <input type="date" className={inputClass} value={deadline} onChange={(e) => setDeadline(e.target.value)} required />
                      </Field>
                    </div>

                    <Field label={t("eamDate")}>
                      <input type="date" className={inputClass} value={eamDate} onChange={(e) => setEamDate(e.target.value)} required />
                    </Field>

                    <AttachmentsBlock
                      attachments={tasAttachments}
                      setAttachments={setTasAttachments}
                      submitting={submitting}
                      inputClass={inputClass}
                      t={t}
                      setError={setError}
                    />
                  </>
                ) : (
                  <>
                    <div className="rounded-xl bg-sky-500/5 border border-sky-500/15 px-4 py-3 text-sm text-foreground/65">
                      {t("bilingualHint")}
                    </div>
                    <Field label={t("subjectEn")}>
                      <input className={inputClass} value={subjectEn} onChange={(e) => setSubjectEn(e.target.value)} required />
                    </Field>
                    <Field label={t("subjectRu")}>
                      <input className={inputClass} value={subjectRu} onChange={(e) => setSubjectRu(e.target.value)} required />
                    </Field>
                    <AttachmentsBlock
                      attachments={expressAttachments}
                      setAttachments={setExpressAttachments}
                      submitting={submitting}
                      inputClass={inputClass}
                      t={t}
                      setError={setError}
                    />
                    <ApproversBlock approvers={approvers} setApprovers={setApprovers} assignable={assignable} inputClass={inputClass} t={t} />
                  </>
                )}
              </div>
            </div>
          </div>

          {error && (
            <div className="rounded-xl border border-red-500/30 bg-red-500/5 px-4 py-3 text-sm text-red-700 dark:text-red-400">
              {error}
            </div>
          )}

          <div className="flex items-center gap-3">
            <Button type="submit" disabled={submitting} className={cn("font-semibold h-11 px-8 rounded-xl", dcsTheme.primaryBtn)}>
              {submitting ? t("submitting") : t("submit")}
            </Button>
            <span className="text-xs text-foreground/40">{t("submitHint")}</span>
          </div>
        </form>
      </div>
    </>
  );
}

function ReadOnlyField({
  icon: Icon,
  label,
  value,
}: {
  icon: typeof MapPin;
  label: string;
  value: string;
}) {
  return (
    <div className="rounded-xl border border-slate-200/60 dark:border-white/[0.06] bg-slate-50/50 dark:bg-white/[0.02] px-3.5 py-3">
      <p className="text-[10px] font-bold uppercase tracking-wider text-foreground/40 flex items-center gap-1.5 mb-1">
        <Icon size={11} /> {label}
      </p>
      <p className="text-sm font-semibold text-foreground/85">{value}</p>
    </div>
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

function AttachmentsBlock({
  attachments,
  setAttachments,
  submitting,
  inputClass,
  t,
  setError,
}: {
  attachments: AttachmentRow[];
  setAttachments: (v: AttachmentRow[]) => void;
  submitting: boolean;
  inputClass: string;
  t: ReturnType<typeof useTranslations>;
  setError: (v: string) => void;
}) {
  return (
    <div>
      <p className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 flex items-center gap-1.5">
        <Paperclip size={12} /> {t("attachments")}
      </p>
      <div className="space-y-2">
        {attachments.map((a, i) => (
          <div key={i} className="flex gap-2 items-start">
            <select
              className={cn(inputClass, "w-32 shrink-0")}
              value={a.kind}
              onChange={(e) => {
                const next = [...attachments];
                next[i] = { ...next[i], kind: e.target.value as ProcurementAttachmentKind };
                setAttachments(next);
              }}
            >
              <option value="TechnicalAssignment">TA</option>
              <option value="MaterialRequisition">MR</option>
              <option value="ServiceRequisition">SR</option>
              <option value="Other">{t("attachmentOther")}</option>
            </select>
            <DocumentFileUpload
              folder="procurement/requests"
              fileName={a.fileName}
              storageKey={a.storageKey}
              disabled={submitting}
              labels={{ uploading: t("uploading"), attached: t("fileAttached") }}
              onUploaded={(fileName, storageKey) => {
                const next = [...attachments];
                next[i] = { ...next[i], fileName, storageKey };
                setAttachments(next);
              }}
              onError={setError}
            />
          </div>
        ))}
      </div>
      <Button
        type="button"
        variant="ghost"
        size="sm"
        className="mt-2"
        onClick={() => setAttachments([...attachments, { kind: "TechnicalAssignment", fileName: "" }])}
      >
        + {t("addAttachment")}
      </Button>
    </div>
  );
}

function ApproversBlock({
  approvers,
  setApprovers,
  assignable,
  inputClass,
  t,
}: {
  approvers: ApproverRow[];
  setApprovers: (v: ApproverRow[]) => void;
  assignable: AssignableUser[];
  inputClass: string;
  t: ReturnType<typeof useTranslations>;
}) {
  return (
    <div>
      <p className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2">{t("approvers")}</p>
      {approvers.map((a, i) => (
        <div key={i} className="flex gap-2 mb-2">
          <select
            className={cn(inputClass, "w-40")}
            value={a.role}
            onChange={(e) => {
              const next = [...approvers];
              next[i] = { ...next[i], role: e.target.value as ProcurementApproverRole };
              setApprovers(next);
            }}
          >
            <option value="SectionHead">{t("roleSectionHead")}</option>
            <option value="TopManager">{t("roleTopManager")}</option>
          </select>
          <select
            className={cn(inputClass, "flex-1")}
            value={a.userId}
            onChange={(e) => {
              const next = [...approvers];
              next[i] = { ...next[i], userId: e.target.value };
              setApprovers(next);
            }}
            required
          >
            <option value="">{t("selectUser")}</option>
            {assignable.map((u) => (
              <option key={u.id} value={u.id}>{u.fullName}</option>
            ))}
          </select>
        </div>
      ))}
      <Button type="button" variant="ghost" size="sm" onClick={() => setApprovers([...approvers, { userId: "", role: "SectionHead" }])}>
        + {t("addApprover")}
      </Button>
    </div>
  );
}
