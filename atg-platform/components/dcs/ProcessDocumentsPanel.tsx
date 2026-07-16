"use client";

import { FileSpreadsheet, FileText, Sparkles, Upload } from "lucide-react";
import {
  ProcurementProcessDocument,
  ProcurementRequest,
} from "@/lib/procurementRequest";
import { deptLabel } from "@/lib/dcs";
import { fileDownloadUrl } from "@/lib/files";
import { cn } from "@/lib/utils";

interface Props {
  req: ProcurementRequest;
  locale: string;
  t: (key: string) => string;
  /** When true, omit outer card chrome (used inside a parent accordion). */
  embedded?: boolean;
}

function categoryLabel(category: string, t: (key: string) => string): string {
  const map: Record<string, string> = {
    TechnicalAssignment: t("processDocs.catTa"),
    MaterialRequisition: t("processDocs.catMr"),
    ServiceRequisition: t("processDocs.catSr"),
    Other: t("processDocs.catOther"),
    RfqDocument: t("processDocs.catRfq"),
    CommercialOffer: t("processDocs.catOffer"),
    PlanTemplate: t("processDocs.catPlanTemplate"),
    PlanDocument: t("processDocs.catPlanDoc"),
  };
  return map[category] ?? category;
}

function phaseLabel(phase: string, t: (key: string) => string): string {
  const map: Record<string, string> = {
    Initiation: t("processDocs.phaseInitiation"),
    Marketing: t("processDocs.phaseMarketing"),
    Contracts: t("processDocs.phaseContracts"),
  };
  return map[phase] ?? phase;
}

function phaseSortKey(phase: string): number {
  switch (phase) {
    case "Initiation":
      return 0;
    case "Marketing":
      return 1;
    case "Contracts":
      return 2;
    default:
      return 9;
  }
}

const PHASE_ORDER = ["Initiation", "Marketing", "Contracts"] as const;

function fallbackDocs(req: ProcurementRequest): ProcurementProcessDocument[] {
  return (req.attachments ?? []).map((a) => ({
    id: a.id,
    fileName: a.fileName,
    storageKey: a.storageKey,
    source: "Uploaded" as const,
    phase: "Initiation",
    category: a.kind,
    userName: a.uploadedByName,
    at: a.uploadedAt,
  }));
}

export function ProcessDocumentsPanel({ req, locale, t, embedded }: Props) {
  const docs = (
    req.processDocuments && req.processDocuments.length > 0
      ? [...req.processDocuments]
      : fallbackDocs(req)
  ).sort((a, b) => {
    const phase = phaseSortKey(a.phase) - phaseSortKey(b.phase);
    if (phase !== 0) return phase;
    return new Date(a.at ?? 0).getTime() - new Date(b.at ?? 0).getTime();
  });

  if (docs.length === 0) {
    if (embedded) {
      return <p className="text-xs text-slate-400">{t("processDocs.empty")}</p>;
    }
    return (
      <section className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm dark:border-white/10 dark:bg-white/[0.03]">
        <h3 className="mb-2 text-[10px] font-bold uppercase tracking-[0.14em] text-slate-400">
          {t("attachments")}
        </h3>
        <p className="text-xs text-slate-400">{t("processDocs.empty")}</p>
      </section>
    );
  }

  // Group: Phase → Department → User → files (Request → Marketing → Contract)
  type UserGroup = { userName: string; files: ProcurementProcessDocument[] };
  type DeptGroup = { deptKey: string; deptLabel: string; users: Map<string, UserGroup> };
  type PhaseGroup = { phase: string; departments: Map<string, DeptGroup> };

  const phases = new Map<string, PhaseGroup>();

  for (const phase of PHASE_ORDER) {
    phases.set(phase, { phase, departments: new Map() });
  }

  for (const doc of docs) {
    const phaseKey = PHASE_ORDER.includes(doc.phase as (typeof PHASE_ORDER)[number])
      ? doc.phase
      : "Initiation";
    if (!phases.has(phaseKey)) {
      phases.set(phaseKey, { phase: phaseKey, departments: new Map() });
    }
    const phaseGroup = phases.get(phaseKey)!;

    const deptName = deptLabel(doc.departmentName ?? "", doc.departmentNameEn ?? "", locale);
    const deptKey = deptName || t("processDocs.unknownDept");
    const userName = doc.userName?.trim() || t("processDocs.systemUser");

    if (!phaseGroup.departments.has(deptKey)) {
      phaseGroup.departments.set(deptKey, {
        deptKey,
        deptLabel: deptKey,
        users: new Map(),
      });
    }
    const dept = phaseGroup.departments.get(deptKey)!;
    if (!dept.users.has(userName)) {
      dept.users.set(userName, { userName, files: [] });
    }
    dept.users.get(userName)!.files.push(doc);
  }

  const visiblePhases = [...phases.values()].filter((p) => p.departments.size > 0);

  const body = (
      <div className="space-y-5">
        {visiblePhases.map((phaseGroup) => (
          <div key={phaseGroup.phase}>
            <p className="mb-2 text-[11px] font-bold uppercase tracking-[0.12em] text-sky-700 dark:text-sky-300">
              {phaseLabel(phaseGroup.phase, t)}
            </p>
            <div className="space-y-3 border-l-2 border-sky-100 pl-3 dark:border-sky-500/20">
              {[...phaseGroup.departments.values()].map((dept) => (
                <div key={dept.deptKey}>
                  <p className="mb-1.5 text-[11px] font-bold uppercase tracking-wide text-slate-500">
                    {dept.deptLabel}
                  </p>
                  <div className="space-y-2.5 pl-2">
                    {[...dept.users.values()].map((user) => (
                      <div key={user.userName}>
                        <p className="mb-1 text-xs font-semibold text-slate-700 dark:text-slate-200">
                          {user.userName}
                        </p>
                        <ul className="space-y-1.5">
                          {user.files.map((doc) => {
                            const isPdf = doc.fileName.toLowerCase().endsWith(".pdf");
                            const isGenerated = doc.source === "Generated";
                            return (
                              <li
                                key={doc.id}
                                className="flex items-start gap-2 rounded-lg border border-slate-100 bg-slate-50/80 px-2.5 py-2 dark:border-white/[0.06] dark:bg-white/[0.02]"
                              >
                                <span
                                  className={cn(
                                    "mt-0.5 flex h-7 w-7 shrink-0 items-center justify-center rounded-md",
                                    isPdf ? "bg-red-50 text-red-600" : "bg-sky-50 text-sky-600",
                                  )}
                                >
                                  {isPdf ? <FileText size={14} /> : <FileSpreadsheet size={14} />}
                                </span>
                                <div className="min-w-0 flex-1">
                                  {doc.storageKey ? (
                                    <a
                                      href={fileDownloadUrl(doc.storageKey, doc.fileName)}
                                      target="_blank"
                                      rel="noreferrer"
                                      className="block truncate text-sm font-medium text-sky-700 hover:underline dark:text-sky-300"
                                    >
                                      {doc.fileName}
                                    </a>
                                  ) : (
                                    <span className="block truncate text-sm font-medium text-slate-800 dark:text-slate-100">
                                      {doc.fileName}
                                    </span>
                                  )}
                                  <div className="mt-0.5 flex flex-wrap items-center gap-1.5 text-[10px] text-slate-400">
                                    <span className="inline-flex items-center gap-0.5 rounded bg-white px-1.5 py-0.5 font-semibold uppercase tracking-wide text-slate-500 ring-1 ring-slate-200 dark:bg-white/[0.04] dark:ring-white/10">
                                      {isGenerated ? (
                                        <Sparkles size={9} className="text-violet-500" />
                                      ) : (
                                        <Upload size={9} className="text-sky-500" />
                                      )}
                                      {isGenerated
                                        ? t("processDocs.generated")
                                        : t("processDocs.uploaded")}
                                    </span>
                                    <span>{categoryLabel(doc.category, t)}</span>
                                    {doc.at && (
                                      <>
                                        <span>·</span>
                                        <span className="tabular-nums">
                                          {new Date(doc.at).toLocaleString(locale, {
                                            day: "2-digit",
                                            month: "2-digit",
                                            year: "numeric",
                                            hour: "2-digit",
                                            minute: "2-digit",
                                          })}
                                        </span>
                                      </>
                                    )}
                                  </div>
                                </div>
                              </li>
                            );
                          })}
                        </ul>
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>
  );

  if (embedded) return body;

  return (
    <section className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm dark:border-white/10 dark:bg-white/[0.03]">
      <h3 className="mb-3 text-[10px] font-bold uppercase tracking-[0.14em] text-slate-400">
        {t("attachments")} ({docs.length})
      </h3>
      {body}
    </section>
  );
}
