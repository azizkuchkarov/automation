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
  Plus,
  User,
} from "lucide-react";
import api, { getApiErrorMessage } from "@/lib/api";
import {
  ProcurementAttachmentKind,
  ProcurementApproverRole,
  ProcurementCreateOptions,
  ProcurementInitiatorDepartment,
  ProcurementRequestUser,
  attachmentKindLabel,
  approverRoleLabel,
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
import {
  ApprovalChainStep,
  ChainDocument,
  RequestApprovalChainPreview,
} from "@/components/dcs/RequestApprovalChainPreview";
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
  const [tasRequisitionType, setTasRequisitionType] = useState<"MaterialRequest" | "ServiceRequest">("MaterialRequest");
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
  const attachments = isTas ? tasAttachments : expressAttachments;
  const setAttachments = isTas ? setTasAttachments : setExpressAttachments;

  const selectedDept = useMemo(
    () => initiatorDepartments.find((d) => d.id === initiatorDepartmentId),
    [initiatorDepartments, initiatorDepartmentId],
  );

  const regionDisplay = useMemo(() => {
    if (isTas && selectedDept) {
      return regionLabelFromDept(
        selectedDept.organizationCode,
        selectedDept.organizationName,
        selectedDept.isStation,
        locale,
      );
    }
    if (ctx) return locale.startsWith("en") ? ctx.regionLabelEn : ctx.regionLabelRu;
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

  const responsibleName = responsibleUsers.find((u) => u.id === responsibleId)?.fullName;

  const chainSteps: ApprovalChainStep[] = useMemo(() => {
    if (isTas) {
      const initiatorReady = Boolean(initiatorId);
      const responsibleReady = Boolean(responsibleId);
      return [
        {
          id: "initiator",
          levelLabel: t("chain.levelInitiator"),
          name: initiatingEmployeeDisplay !== "—" ? initiatingEmployeeDisplay : t("chain.selectPerson"),
          statusLabel: initiatorReady ? t("chain.ready") : t("chain.waiting"),
          status: initiatorReady ? "done" : "active",
        },
        {
          id: "tas",
          levelLabel: t("chain.levelTas"),
          name: responsibleName ?? t("chain.selectPerson"),
          statusLabel: responsibleReady ? t("chain.ready") : t("chain.waiting"),
          status: responsibleReady ? "active" : initiatorReady ? "active" : "waiting",
        },
        {
          id: "approval",
          levelLabel: t("chain.levelApproval"),
          name: t("chain.approversLater"),
          statusLabel: t("chain.afterStep6"),
          status: "waiting",
        },
        {
          id: "marketing",
          levelLabel: t("chain.levelMarketing"),
          name: t("chain.hoMarketing"),
          statusLabel: t("chain.waiting"),
          status: "waiting",
        },
      ];
    }

    return approvers.map((a, i) => {
      const user = assignable.find((u) => u.id === a.userId);
      const prevFilled = approvers.slice(0, i).every((x) => x.userId);
      const filled = Boolean(a.userId);
      let status: ApprovalChainStep["status"] = "waiting";
      if (filled && prevFilled) status = i === 0 ? "active" : "waiting";
      if (filled && i === 0) status = "active";
      if (!filled && (i === 0 || prevFilled)) status = "active";

      return {
        id: `appr-${i}`,
        levelLabel: t("chain.levelN", {
          n: i + 1,
          role: approverRoleLabel(a.role, locale),
        }),
        name: user?.fullName ?? t("chain.selectPerson"),
        statusLabel: filled ? t("chain.pendingReview") : t("chain.waiting"),
        status,
        isYou: false,
      };
    });
  }, [
    isTas,
    initiatorId,
    responsibleId,
    initiatingEmployeeDisplay,
    responsibleName,
    approvers,
    assignable,
    locale,
    t,
  ]);

  const chainDocuments: ChainDocument[] = useMemo(
    () =>
      attachments
        .filter((a) => a.fileName && a.storageKey)
        .map((a, i) => ({
          id: `${a.storageKey}-${i}`,
          fileName: a.fileName,
          kindLabel: attachmentKindLabel(a.kind, locale),
        })),
    [attachments, locale],
  );

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
    "w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm",
    "text-slate-800 placeholder:text-slate-400",
    "focus:outline-none focus:ring-2 focus:ring-sky-500/20 focus:border-sky-500",
    "dark:border-white/10 dark:bg-white/[0.04] dark:text-slate-100",
  );

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setSubmitting(true);
    try {
      if (isTas) {
        if (!subjectEn.trim() || !subjectRu.trim()) {
          setError(t("bilingualRequired"));
          setSubmitting(false);
          return;
        }
        const r = await api.post("/dcs/procurement-requests/tas", {
          eamNumber,
          initiatorId,
          subjectEn: subjectEn.trim(),
          subjectRu: subjectRu.trim(),
          tasRequisitionType,
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
          setSubmitting(false);
          return;
        }
        const r = await api.post("/dcs/procurement-requests/express", {
          subjectEn: subjectEn.trim(),
          subjectRu: subjectRu.trim(),
          approvers: approvers.filter((a) => a.userId),
          attachments: expressAttachments
            .filter((a) => a.storageKey)
            .map((a) => ({
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
      <div className="flex flex-1 items-center justify-center text-sm text-foreground/40">
        {t("loading")}
      </div>
    );
  }

  if (!options.canCreateTas && !options.canCreateExpress) {
    return (
      <div className="flex flex-1 items-center justify-center px-6 text-center text-sm text-foreground/50">
        {t("noAccess")}
      </div>
    );
  }

  return (
    <div className="flex min-h-0 flex-1 flex-col bg-[#f4f6f8] dark:bg-slate-950">
      <DcsPageHeader
        title={t("newTitle")}
        subtitle={t("newSubtitleModern")}
        breadcrumb={t("newTitle")}
        icon={FilePlus}
        iconClassName="bg-sky-500/10 text-sky-600"
      />

      <div className="flex-1 overflow-auto">
        <form onSubmit={submit} className="mx-auto w-full max-w-[1600px] px-4 py-4 sm:px-6 lg:px-8">
          {options.canCreateTas && options.canCreateExpress && (
            <div className="mb-4 inline-flex rounded-lg border border-slate-200 bg-white p-1 shadow-sm dark:border-white/10 dark:bg-slate-900">
              {(["TechnicalAffairs", "Express"] as const).map((f) => (
                <button
                  key={f}
                  type="button"
                  onClick={() => setFlow(f)}
                  className={cn(
                    "rounded-md px-4 py-2 text-sm font-semibold transition-all",
                    flow === f
                      ? "bg-sky-600 text-white shadow-sm"
                      : "text-slate-500 hover:text-slate-800 dark:hover:text-slate-200",
                  )}
                >
                  {f === "TechnicalAffairs" ? t("flowTas") : t("flowExpress")}
                </button>
              ))}
            </div>
          )}

          <div className="grid items-start gap-5 xl:grid-cols-12">
            {/* Main form */}
            <div className="space-y-4 xl:col-span-8">
              <section className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm dark:border-white/10 dark:bg-slate-900/50">
                <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 px-5 py-3.5 dark:border-white/[0.06]">
                  <div>
                    <p className="text-[10px] font-bold uppercase tracking-[0.16em] text-slate-400">
                      {t("createRequest")}
                    </p>
                    <h2 className="text-base font-semibold text-slate-800 dark:text-slate-100">
                      {t("createRequestHint")}
                    </h2>
                  </div>
                  <div className="flex items-center gap-2 rounded-full border border-slate-200 bg-slate-50 px-3 py-1 dark:border-white/10 dark:bg-white/[0.04]">
                    <span className={cn("h-2.5 w-2.5 rounded-full", priorityDotClass(priority))} />
                    <span className="text-xs font-semibold text-slate-600 dark:text-slate-300">
                      {priorityLabel(priority, locale)}
                    </span>
                  </div>
                </div>

                <div className="space-y-5 p-5">
                  {/* Compact auto meta strip */}
                  <div>
                    <p className="mb-2 text-[10px] font-bold uppercase tracking-[0.14em] text-slate-400">
                      {t("autoFields")}
                    </p>
                    <div className="grid gap-2 sm:grid-cols-2 xl:grid-cols-4">
                      <MetaChip icon={MapPin} label={t("region")} value={regionDisplay} />
                      <MetaChip icon={Calendar} label={t("regDate")} value={regDateDisplay} />
                      <MetaChip icon={Building2} label={t("initiatorDepartment")} value={initiatingDepartmentDisplay} />
                      <MetaChip icon={User} label={t("initiatingEmployee")} value={initiatingEmployeeDisplay} />
                    </div>
                  </div>

                  {/* Priority */}
                  <div>
                    <p className="mb-2 flex items-center gap-1.5 text-[10px] font-bold uppercase tracking-[0.14em] text-slate-400">
                      <Flag size={11} /> {t("priority")}
                    </p>
                    <div className="flex flex-wrap gap-2">
                      {PROCUREMENT_PRIORITIES.map((p) => (
                        <button
                          key={p}
                          type="button"
                          onClick={() => setPriority(p)}
                          className={cn(
                            "inline-flex items-center gap-2 rounded-lg border px-3 py-2 text-sm font-medium transition-all",
                            priority === p
                              ? cn("bg-white shadow-sm dark:bg-white/[0.06]", priorityRingClass(p))
                              : "border-slate-200 bg-slate-50 text-slate-500 hover:bg-white dark:border-white/10 dark:bg-white/[0.03]",
                          )}
                        >
                          <span className={cn("h-2 w-2 rounded-full", priorityDotClass(p))} />
                          {priorityLabel(p, locale)}
                        </button>
                      ))}
                    </div>
                  </div>

                  <div className="h-px bg-slate-100 dark:bg-white/[0.06]" />

                  <p className="text-[10px] font-bold uppercase tracking-[0.14em] text-slate-400">
                    {t("requestDetails")}
                  </p>

                  {isTas ? (
                    <div className="space-y-4">
                      <div className="grid gap-3 sm:grid-cols-2">
                        <Field label={t("initiatorDepartment")}>
                          <select
                            className={inputClass}
                            value={initiatorDepartmentId}
                            onChange={(e) => setInitiatorDepartmentId(e.target.value)}
                            required
                          >
                            <option value="">{t("selectDepartment")}</option>
                            {Object.entries(
                              initiatorDepartments.reduce<Record<string, ProcurementInitiatorDepartment[]>>(
                                (acc, d) => {
                                  (acc[d.organizationName] ??= []).push(d);
                                  return acc;
                                },
                                {},
                              ),
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
                            <option value="">
                              {initiatorDepartmentId ? t("selectUser") : t("selectDepartmentFirst")}
                            </option>
                            {initiators.map((u) => (
                              <option key={u.id} value={u.id}>
                                {u.fullName}
                              </option>
                            ))}
                          </select>
                        </Field>
                      </div>

                      <Field label={t("requestType")}>
                        <div className="grid grid-cols-2 gap-2">
                          {(["MaterialRequest", "ServiceRequest"] as const).map((type) => (
                            <button
                              key={type}
                              type="button"
                              onClick={() => setTasRequisitionType(type)}
                              className={cn(
                                "rounded-lg border px-3 py-2.5 text-left text-sm font-medium transition-all",
                                tasRequisitionType === type
                                  ? "border-sky-500 bg-sky-50 text-sky-900 dark:bg-sky-500/10 dark:text-sky-100"
                                  : "border-slate-200 bg-white text-slate-500 hover:bg-slate-50 dark:border-white/10",
                              )}
                            >
                              {type === "MaterialRequest" ? t("requestTypeMaterial") : t("requestTypeService")}
                            </button>
                          ))}
                        </div>
                      </Field>

                      <div className="grid gap-3 sm:grid-cols-2">
                        <Field label={t("subjectEn")}>
                          <textarea
                            className={cn(inputClass, "min-h-[64px] resize-y")}
                            value={subjectEn}
                            onChange={(e) => setSubjectEn(e.target.value)}
                            required
                            maxLength={500}
                            placeholder={t("subjectPlaceholder")}
                          />
                        </Field>
                        <Field label={t("subjectRu")}>
                          <textarea
                            className={cn(inputClass, "min-h-[64px] resize-y")}
                            value={subjectRu}
                            onChange={(e) => setSubjectRu(e.target.value)}
                            required
                            maxLength={500}
                            placeholder={t("subjectPlaceholder")}
                          />
                        </Field>
                      </div>

                      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
                        <Field label={t("eamNumber")}>
                          <input
                            className={inputClass}
                            value={eamNumber}
                            onChange={(e) => setEamNumber(e.target.value)}
                            required
                          />
                        </Field>
                        <Field label={t("responsible")}>
                          <select
                            className={inputClass}
                            value={responsibleId}
                            onChange={(e) => setResponsibleId(e.target.value)}
                            required
                          >
                            <option value="">{t("selectUser")}</option>
                            {responsibleUsers.map((u) => (
                              <option key={u.id} value={u.id}>
                                {u.fullName}
                              </option>
                            ))}
                          </select>
                        </Field>
                        <Field label={t("deadline")}>
                          <input
                            type="date"
                            className={inputClass}
                            value={deadline}
                            onChange={(e) => setDeadline(e.target.value)}
                            required
                          />
                        </Field>
                        <Field label={t("eamDate")}>
                          <input
                            type="date"
                            className={inputClass}
                            value={eamDate}
                            onChange={(e) => setEamDate(e.target.value)}
                            required
                          />
                        </Field>
                      </div>
                    </div>
                  ) : (
                    <div className="space-y-4">
                      <div className="grid gap-3 sm:grid-cols-2">
                        <Field label={t("subjectEn")}>
                          <input
                            className={inputClass}
                            value={subjectEn}
                            onChange={(e) => setSubjectEn(e.target.value)}
                            required
                            placeholder={t("subjectPlaceholder")}
                          />
                        </Field>
                        <Field label={t("subjectRu")}>
                          <input
                            className={inputClass}
                            value={subjectRu}
                            onChange={(e) => setSubjectRu(e.target.value)}
                            required
                            placeholder={t("subjectPlaceholder")}
                          />
                        </Field>
                      </div>

                      <div>
                        <p className="mb-2 text-[11px] font-semibold uppercase tracking-wider text-slate-400">
                          {t("approvers")}
                        </p>
                        <div className="space-y-2">
                          {approvers.map((a, i) => (
                            <div
                              key={i}
                              className="grid gap-2 rounded-lg border border-slate-100 bg-slate-50/80 p-2.5 sm:grid-cols-[140px_1fr] dark:border-white/[0.06] dark:bg-white/[0.02]"
                            >
                              <select
                                className={inputClass}
                                value={a.role}
                                onChange={(e) => {
                                  const next = [...approvers];
                                  next[i] = {
                                    ...next[i],
                                    role: e.target.value as ProcurementApproverRole,
                                  };
                                  setApprovers(next);
                                }}
                              >
                                <option value="SectionHead">{t("roleSectionHead")}</option>
                                <option value="TopManager">{t("roleTopManager")}</option>
                              </select>
                              <select
                                className={inputClass}
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
                                  <option key={u.id} value={u.id}>
                                    {u.fullName}
                                  </option>
                                ))}
                              </select>
                            </div>
                          ))}
                        </div>
                        <button
                          type="button"
                          className="mt-2 inline-flex items-center gap-1 text-xs font-semibold text-sky-700 hover:text-sky-800"
                          onClick={() =>
                            setApprovers([...approvers, { userId: "", role: "SectionHead" }])
                          }
                        >
                          <Plus size={14} /> {t("addApprover")}
                        </button>
                      </div>
                    </div>
                  )}

                  <div className="h-px bg-slate-100 dark:bg-white/[0.06]" />

                  <AttachmentsBlock
                    attachments={attachments}
                    setAttachments={setAttachments}
                    submitting={submitting}
                    inputClass={inputClass}
                    t={t}
                    setError={setError}
                  />
                </div>

                <div className="flex flex-wrap items-center gap-3 border-t border-slate-100 bg-slate-50/80 px-5 py-3.5 dark:border-white/[0.06] dark:bg-white/[0.02]">
                  {error && (
                    <p className="w-full text-sm text-red-600 dark:text-red-400">{error}</p>
                  )}
                  <Button
                    type="submit"
                    disabled={submitting}
                    className="h-10 rounded-lg bg-sky-600 px-6 font-semibold text-white hover:bg-sky-500"
                  >
                    {submitting ? t("submitting") : t("submit")}
                  </Button>
                  <span className="text-xs text-slate-400">{t("submitHint")}</span>
                </div>
              </section>
            </div>

            {/* Right: Approval Chain */}
            <div className="xl:col-span-4">
              <div className="xl:sticky xl:top-4">
                <RequestApprovalChainPreview
                  title={t("chain.title")}
                  steps={chainSteps}
                  documentsTitle={t("chain.sharedDocuments")}
                  documents={chainDocuments}
                  emptyDocsHint={t("chain.emptyDocuments")}
                  finalLabel={t("chain.finalOutcome")}
                  finalName={isTas ? t("chain.finalTas") : t("chain.finalExpress")}
                />
              </div>
            </div>
          </div>
        </form>
      </div>
    </div>
  );
}

function MetaChip({
  icon: Icon,
  label,
  value,
}: {
  icon: typeof MapPin;
  label: string;
  value: string;
}) {
  return (
    <div className="rounded-lg border border-slate-200 bg-slate-50/80 px-3 py-2.5 dark:border-white/10 dark:bg-white/[0.03]">
      <p className="mb-0.5 flex items-center gap-1 text-[10px] font-bold uppercase tracking-wider text-slate-400">
        <Icon size={10} /> {label}
      </p>
      <p className="truncate text-sm font-semibold text-slate-800 dark:text-slate-100" title={value}>
        {value}
      </p>
    </div>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="mb-1.5 block text-[11px] font-semibold uppercase tracking-wider text-slate-400">
        {label}
      </label>
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
      <p className="mb-2 flex items-center gap-1.5 text-[11px] font-semibold uppercase tracking-wider text-slate-400">
        <Paperclip size={12} /> {t("attachments")}
      </p>
      <div className="space-y-2">
        {attachments.map((a, i) => (
          <div
            key={i}
            className="flex flex-wrap items-start gap-2 rounded-lg border border-slate-100 bg-slate-50/60 p-2.5 dark:border-white/[0.06] dark:bg-white/[0.02]"
          >
            <select
              className={cn(inputClass, "w-28 shrink-0")}
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
            <div className="min-w-0 flex-1">
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
          </div>
        ))}
      </div>
      <button
        type="button"
        className="mt-2 inline-flex items-center gap-1 text-xs font-semibold text-sky-700 hover:text-sky-800"
        onClick={() =>
          setAttachments([...attachments, { kind: "TechnicalAssignment", fileName: "" }])
        }
      >
        <Plus size={14} /> {t("addAttachment")}
      </button>
    </div>
  );
}
