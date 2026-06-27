"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import api, { getApiErrorMessage } from "@/lib/api";
import {
  ProcurementAttachmentKind,
  ProcurementApproverRole,
  ProcurementCreateOptions,
  ProcurementInitiatorDepartment,
  ProcurementRequestUser,
} from "@/lib/procurementRequest";
import { DocumentFileUpload } from "@/components/dcs/DocumentFileUpload";
import { DcsPageHeader } from "@/components/dcs/DcsPageHeader";
import { dcsTheme } from "@/components/dcs/dcsTheme";
import { Button } from "@/components/ui/Button";
import { FilePlus } from "lucide-react";
import { cn } from "@/lib/utils";

interface AssignableUser {
  id: string;
  fullName: string;
  email: string;
  role: string;
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

  // TAS fields
  const [eamNumber, setEamNumber] = useState("");
  const [initiatorDepartmentId, setInitiatorDepartmentId] = useState("");
  const [initiatorId, setInitiatorId] = useState("");
  const [procurementName, setProcurementName] = useState("");
  const [responsibleId, setResponsibleId] = useState("");
  const [eamDate, setEamDate] = useState("");
  const [deadline, setDeadline] = useState("");

  // Express fields
  const [subjectEn, setSubjectEn] = useState("");
  const [subjectRu, setSubjectRu] = useState("");
  const [approvers, setApprovers] = useState<ApproverRow[]>([
    { userId: "", role: "SectionHead" },
    { userId: "", role: "TopManager" },
  ]);
  const [attachments, setAttachments] = useState<AttachmentRow[]>([
    { kind: "TechnicalAssignment", fileName: "" },
  ]);

  useEffect(() => {
    api.get("/dcs/procurement-requests/create-options").then((r) => {
      setOptions(r.data);
      if (r.data.defaultFlow) setFlow(r.data.defaultFlow);
    });
    api.get("/tasks/assignees").then((r) => setAssignable(r.data));
  }, []);

  useEffect(() => {
    if (flow === "TechnicalAffairs") {
      api.get("/dcs/procurement-requests/initiator-departments").then((r) => setInitiatorDepartments(r.data));
      api.get("/dcs/procurement-requests/responsible-users").then((r) => setResponsibleUsers(r.data));
    }
  }, [flow]);

  useEffect(() => {
    if (flow !== "TechnicalAffairs" || !initiatorDepartmentId) {
      setInitiators([]);
      setInitiatorId("");
      return;
    }
    api
      .get(`/dcs/procurement-requests/initiators?departmentId=${initiatorDepartmentId}`)
      .then((r) => setInitiators(r.data))
      .catch(() => setInitiators([]));
    setInitiatorId("");
  }, [flow, initiatorDepartmentId]);

  const inputClass =
    "w-full rounded-lg border border-border/80 bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-atg-teal/30";

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setSubmitting(true);
    try {
      if (flow === "TechnicalAffairs") {
        const r = await api.post("/dcs/procurement-requests/tas", {
          eamNumber,
          initiatorId,
          procurementName,
          responsibleId,
          eamFormationDate: eamDate,
          deadline,
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
          attachments: attachments.filter((a) => a.storageKey).map((a) => ({
            kind: a.kind,
            fileName: a.fileName,
            storageKey: a.storageKey,
          })),
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
      <div className="flex-1 flex items-center justify-center text-foreground/40 text-sm">
        {t("loading")}
      </div>
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
        subtitle={t("newSubtitle")}
        breadcrumb={t("newTitle")}
        icon={FilePlus}
        iconClassName="bg-sky-500/10 text-sky-600"
      />

      <div className="flex-1 overflow-auto px-6 py-6">
        <form onSubmit={submit} className="max-w-2xl space-y-6">
          {options.canCreateTas && options.canCreateExpress && (
            <div className={cn("flex gap-2 p-1.5", dcsTheme.premiumCard)}>
              {(["TechnicalAffairs", "Express"] as const).map((f) => (
                <button
                  key={f}
                  type="button"
                  onClick={() => setFlow(f)}
                  className={cn(
                    "flex-1 py-2 rounded-lg text-sm font-medium transition-colors",
                    flow === f ? "bg-surface shadow-sm text-foreground" : "text-foreground/50"
                  )}
                >
                  {f === "TechnicalAffairs" ? t("flowTas") : t("flowExpress")}
                </button>
              ))}
            </div>
          )}

          {flow === "TechnicalAffairs" ? (
            <div className={cn("p-6 space-y-4", dcsTheme.premiumCard)}>
              <Field label={t("eamNumber")}>
                <input className={inputClass} value={eamNumber} onChange={(e) => setEamNumber(e.target.value)} required />
              </Field>
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
              <Field label={t("initiator")}>
                <select
                  className={inputClass}
                  value={initiatorId}
                  onChange={(e) => setInitiatorId(e.target.value)}
                  required
                  disabled={!initiatorDepartmentId}
                >
                  <option value="">{initiatorDepartmentId ? t("selectUser") : t("selectDepartmentFirst")}</option>
                  {initiators.map((u) => (
                    <option key={u.id} value={u.id}>
                      {u.fullName}
                    </option>
                  ))}
                </select>
              </Field>
              <Field label={t("procurementName")}>
                <input className={inputClass} value={procurementName} onChange={(e) => setProcurementName(e.target.value)} required />
              </Field>
              <Field label={t("responsible")}>
                <select className={inputClass} value={responsibleId} onChange={(e) => setResponsibleId(e.target.value)} required>
                  <option value="">{t("selectUser")}</option>
                  {responsibleUsers.map((u) => (
                    <option key={u.id} value={u.id}>{u.fullName}</option>
                  ))}
                </select>
              </Field>
              <Field label={t("eamDate")}>
                <input type="date" className={inputClass} value={eamDate} onChange={(e) => setEamDate(e.target.value)} required />
              </Field>
              <Field label={t("deadline")}>
                <input type="date" className={inputClass} value={deadline} onChange={(e) => setDeadline(e.target.value)} required />
              </Field>
            </div>
          ) : (
            <div className={cn("p-6 space-y-4", dcsTheme.premiumCard)}>
              <div className="rounded-lg bg-sky-500/5 border border-sky-500/20 px-4 py-3 text-sm text-foreground/65">
                {t("bilingualHint")}
              </div>
              <Field label={t("subjectEn")}>
                <input className={inputClass} value={subjectEn} onChange={(e) => setSubjectEn(e.target.value)} required placeholder="Procurement request subject…" />
              </Field>
              <Field label={t("subjectRu")}>
                <input className={inputClass} value={subjectRu} onChange={(e) => setSubjectRu(e.target.value)} required placeholder="Тема заявки на закупку…" />
              </Field>

              <div>
                <p className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2">{t("attachments")}</p>
                {attachments.map((a, i) => (
                  <div key={i} className="flex gap-2 mb-2 items-start">
                    <select
                      className={cn(inputClass, "w-36")}
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
                    </select>
                    <DocumentFileUpload
                      folder="procurement/express"
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
                <Button type="button" variant="ghost" size="sm" onClick={() => setAttachments([...attachments, { kind: "TechnicalAssignment", fileName: "" }])}>
                  + {t("addAttachment")}
                </Button>
              </div>

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
                      <option value="Initiator">{t("roleInitiator")}</option>
                      <option value="TasManager">{t("roleTasManager")}</option>
                      <option value="BmgmcTopManager">{t("roleBmgmcTop")}</option>
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
            </div>
          )}

          {error && <p className="text-sm text-red-600">{error}</p>}
          <Button type="submit" disabled={submitting} className={cn("font-semibold h-11 px-6 rounded-xl", dcsTheme.primaryBtn)}>
            {submitting ? t("submitting") : t("submit")}
          </Button>
        </form>
      </div>
    </>
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
