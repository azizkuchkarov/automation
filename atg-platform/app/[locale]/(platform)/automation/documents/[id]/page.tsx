"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { useParams } from "next/navigation";
import {
  Building2,
  Calendar,
  CheckCircle2,
  ChevronRight,
  FileText,
  Hash,
  User,
} from "lucide-react";
import api from "@/lib/api";
import { Document, DocumentStatus, deptLabel } from "@/lib/dcs";
import { DocumentStatusBadge } from "@/components/dcs/DocumentBadges";
import { DcsPageHeader } from "@/components/dcs/DcsPageHeader";
import { IncomingLetterView } from "@/components/dcs/IncomingLetterView";
import { MemoView } from "@/components/dcs/MemoView";
import { OrderView } from "@/components/dcs/OrderView";
import { OutgoingLetterView } from "@/components/dcs/OutgoingLetterView";
import { ProcurementRequestView } from "@/components/dcs/ProcurementRequestView";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

const WORKFLOW: DocumentStatus[] = [
  "Draft",
  "Registered",
  "InReview",
  "Approved",
  "Archived",
];

const NEXT_STATUS: Partial<Record<DocumentStatus, DocumentStatus>> = {
  Draft: "Registered",
  Registered: "InReview",
  InReview: "Approved",
  Approved: "Archived",
};

export default function DocumentDetailPage() {
  const { id } = useParams<{ id: string }>();
  const t = useTranslations("dcs");
  const locale = useLocale();
  const [doc, setDoc] = useState<Document | null>(null);
  const [loading, setLoading] = useState(true);

  const load = () => {
    setLoading(true);
    api
      .get(`/dcs/documents/${id}`)
      .then((r) => setDoc(r.data))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    load();
  }, [id]);

  const advanceStatus = async () => {
    if (!doc) return;
    const next = NEXT_STATUS[doc.status];
    if (!next) return;
    await api.patch(`/dcs/documents/${doc.id}/status`, { status: next });
    load();
  };

  if (loading) {
    return (
      <div className="flex-1 flex items-center justify-center">
        <div className="flex flex-col items-center gap-3 text-foreground/40">
          <div className="w-8 h-8 rounded-full border-2 border-atg-blue/30 border-t-atg-blue animate-spin" />
          <span className="text-sm">{t("loading")}</span>
        </div>
      </div>
    );
  }

  if (!doc) {
    return (
      <div className="flex-1 flex items-center justify-center text-foreground/40">{t("notFound")}</div>
    );
  }

  if (doc.type === "Incoming") {
    return <IncomingLetterView documentId={doc.id} />;
  }

  if (doc.type === "Outgoing") {
    return <OutgoingLetterView documentId={doc.id} />;
  }

  if (doc.type === "Memo") {
    return <MemoView documentId={doc.id} />;
  }

  if (doc.type === "Order") {
    return <OrderView documentId={doc.id} />;
  }

  if (doc.type === "ProcurementRequest") {
    return <ProcurementRequestView documentId={doc.id} />;
  }

  const nextStatus = NEXT_STATUS[doc.status];
  const currentStep = WORKFLOW.indexOf(doc.status);

  return (
    <>
      <DcsPageHeader
        title={doc.number}
        subtitle={doc.title}
        breadcrumb={doc.title}
        icon={FileText}
        iconClassName="bg-atg-blue/10 text-atg-blue"
        actions={
          nextStatus ? (
            <Button size="sm" onClick={advanceStatus} className="shadow-sm font-semibold">
              <CheckCircle2 size={15} className="mr-1.5" />
              {t("actions.advance", { status: t(`status.${nextStatus}`) })}
            </Button>
          ) : undefined
        }
      />

      <div className="flex-1 overflow-auto px-6 py-6">
        <div className="max-w-5xl mx-auto space-y-6">
          <div className="rounded-2xl border border-border/70 bg-surface p-5 shadow-sm">
            <p className="text-[10px] font-bold uppercase tracking-wider text-foreground/40 mb-4">
              {t("detail.workflow")}
            </p>
            <div className="flex items-center gap-1 overflow-x-auto pb-1">
              {WORKFLOW.map((step, i) => {
                const done = i < currentStep;
                const active = step === doc.status;
                return (
                  <div key={step} className="flex items-center shrink-0">
                    <div
                      className={cn(
                        "flex items-center gap-2 px-3 py-2 rounded-xl text-xs font-semibold transition-colors",
                        active && "bg-atg-blue/12 text-atg-blue ring-1 ring-atg-blue/25",
                        done && !active && "text-emerald-600 dark:text-emerald-400",
                        !done && !active && "text-foreground/35"
                      )}
                    >
                      <span
                        className={cn(
                          "w-5 h-5 rounded-full flex items-center justify-center text-[10px] font-bold",
                          active && "bg-atg-blue text-white",
                          done && !active && "bg-emerald-500/15 text-emerald-600",
                          !done && !active && "bg-foreground/[0.06]"
                        )}
                      >
                        {done && !active ? "✓" : i + 1}
                      </span>
                      {t(`status.${step}`)}
                    </div>
                    {i < WORKFLOW.length - 1 && (
                      <ChevronRight size={14} className="text-foreground/20 mx-0.5 shrink-0" />
                    )}
                  </div>
                );
              })}
            </div>
          </div>

          <div className="grid lg:grid-cols-3 gap-6">
            <div className="lg:col-span-2 space-y-6">
              <div className="rounded-2xl border border-border/70 bg-surface p-6 shadow-sm">
                <div className="flex flex-wrap items-center gap-3 mb-5">
                  <DocumentStatusBadge status={doc.status} />
                  <span className="text-xs text-foreground/40 font-mono px-2 py-1 rounded-md bg-foreground/[0.04]">
                    {doc.type}
                  </span>
                </div>
                <h2 className="text-xl font-bold tracking-tight">{doc.title}</h2>
                {doc.description ? (
                  <p className="text-sm text-foreground/60 mt-4 leading-relaxed whitespace-pre-wrap">
                    {doc.description}
                  </p>
                ) : (
                  <p className="text-sm text-foreground/35 mt-4 italic">{t("detail.noDescription")}</p>
                )}
              </div>

              {doc.activities.length > 0 && (
                <div className="rounded-2xl border border-border/70 bg-surface overflow-hidden shadow-sm">
                  <div className="px-5 py-4 border-b border-border/60 font-semibold text-sm bg-foreground/[0.015]">
                    {t("detail.activity")}
                  </div>
                  <ul className="divide-y divide-border/30">
                    {doc.activities.map((a) => (
                      <li key={a.id} className="px-5 py-4 flex gap-3">
                        <div className="w-8 h-8 rounded-full bg-atg-blue/10 flex items-center justify-center text-xs font-bold text-atg-blue shrink-0 mt-0.5">
                          {a.actorName.charAt(0)}
                        </div>
                        <div>
                          <div className="font-medium text-sm">{a.actorName}</div>
                          <div className="text-foreground/50 text-xs mt-0.5">
                            {a.action}
                            {a.toStatus && (
                              <span className="text-atg-blue font-medium">
                                {" → "}
                                {t(`status.${a.toStatus}`)}
                              </span>
                            )}
                          </div>
                          <div className="text-foreground/35 text-[11px] mt-1 tabular-nums">
                            {new Date(a.createdAt).toLocaleString(locale)}
                          </div>
                        </div>
                      </li>
                    ))}
                  </ul>
                </div>
              )}
            </div>

            <div className="space-y-4">
              <MetaCard title={t("detail.details")}>
                <MetaRow icon={User} label={t("fields.author")} value={doc.authorName} />
                <MetaRow
                  icon={Building2}
                  label={t("fields.department")}
                  value={deptLabel(doc.departmentName, doc.departmentNameEn, locale)}
                />
                {doc.externalReference && (
                  <MetaRow icon={Hash} label={t("form.externalRef")} value={doc.externalReference} />
                )}
                {doc.registeredAt && (
                  <MetaRow
                    icon={Calendar}
                    label={t("fields.registered")}
                    value={new Date(doc.registeredAt).toLocaleDateString(locale)}
                  />
                )}
                <MetaRow
                  icon={Calendar}
                  label={t("fields.updated")}
                  value={new Date(doc.updatedAt).toLocaleDateString(locale)}
                />
              </MetaCard>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}

function MetaCard({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="rounded-2xl border border-border/70 bg-surface shadow-sm overflow-hidden">
      <div className="px-4 py-3 border-b border-border/50 text-xs font-bold uppercase tracking-wider text-foreground/40 bg-foreground/[0.015]">
        {title}
      </div>
      <dl className="p-4 space-y-4">{children}</dl>
    </div>
  );
}

function MetaRow({
  icon: Icon,
  label,
  value,
}: {
  icon: React.ComponentType<{ size?: number; className?: string }>;
  label: string;
  value: string;
}) {
  return (
    <div className="flex gap-3">
      <Icon size={15} className="text-foreground/30 mt-0.5 shrink-0" />
      <div className="min-w-0">
        <dt className="text-[10px] text-foreground/40 uppercase tracking-wider font-semibold">{label}</dt>
        <dd className="text-sm font-medium mt-0.5 break-words">{value}</dd>
      </div>
    </div>
  );
}
