"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import Link from "next/link";
import api from "@/lib/api";
import { DcsAdminControl } from "@/lib/dcs";
import { DocumentStatusBadge } from "@/components/dcs/DocumentBadges";
import { DcsCategoryControl } from "@/components/dcs/DcsCategoryControl";
import { Briefcase, FileCheck, FileClock, FileText, Archive } from "lucide-react";

export default function AdminDcsPage() {
  const t = useTranslations("dcs");
  const locale = useLocale();
  const [control, setControl] = useState<DcsAdminControl | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api
      .get("/dcs/admin/control")
      .then((r) => setControl(r.data))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <div className="p-8 text-foreground/40">{t("loading")}</div>;
  if (!control) return null;

  const dash = control.dashboard;

  const stats = [
    { label: t("admin.draft"), value: dash.totalDraft, icon: FileText, color: "text-slate-400" },
    { label: t("admin.inReview"), value: dash.totalInReview, icon: FileClock, color: "text-atg-blue" },
    { label: t("admin.approved"), value: dash.totalApproved, icon: FileCheck, color: "text-emerald-400" },
    { label: t("admin.archived"), value: dash.totalArchived, icon: Archive, color: "text-foreground/40" },
  ];

  return (
    <div className="space-y-8">
      <div className="flex items-center gap-3">
        <div className="w-10 h-10 rounded-xl bg-atg-blue/20 flex items-center justify-center">
          <Briefcase size={20} className="text-atg-blue" />
        </div>
        <div>
          <h1 className="text-2xl font-bold">{t("admin.title")}</h1>
          <p className="text-sm text-foreground/50">{t("admin.subtitle")}</p>
        </div>
      </div>

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-3">
        {stats.map(({ label, value, icon: Icon, color }) => (
          <div key={label} className="rounded-xl border border-border bg-surface p-4">
            <div className="flex items-center gap-2 mb-2">
              <Icon size={16} className={color} />
              <span className="text-xs text-foreground/50 uppercase tracking-wider">{label}</span>
            </div>
            <div className={`text-3xl font-bold tabular-nums ${color}`}>{value}</div>
          </div>
        ))}
      </div>

      <DcsCategoryControl categories={control.categories} />

      <div className="rounded-xl border border-border overflow-hidden">
        <div className="px-4 py-3 border-b border-border bg-surface">
          <h2 className="font-semibold text-sm">{t("admin.recentDocuments")}</h2>
        </div>
        <table className="w-full text-sm">
          <thead className="bg-surface/50 border-b border-border">
            <tr className="text-left text-xs text-foreground/50 uppercase tracking-wider">
              <th className="px-4 py-2.5">{t("fields.regNum")}</th>
              <th className="px-4 py-2.5">{t("fields.title")}</th>
              <th className="px-4 py-2.5">{t("form.type")}</th>
              <th className="px-4 py-2.5">{t("fields.status")}</th>
              <th className="px-4 py-2.5">{t("fields.author")}</th>
            </tr>
          </thead>
          <tbody>
            {dash.recentDocuments.map((doc) => (
              <tr key={doc.id} className="border-b border-border/30 hover:bg-surface/30">
                <td className="px-4 py-3">
                  <Link
                    href={`/${locale}/automation/documents/${doc.id}`}
                    className="font-mono text-atg-blue font-semibold hover:underline"
                  >
                    {doc.number}
                  </Link>
                </td>
                <td className="px-4 py-3 font-medium max-w-[200px] truncate">{doc.title}</td>
                <td className="px-4 py-3 text-foreground/60 text-xs font-mono">{doc.type}</td>
                <td className="px-4 py-3">
                  <DocumentStatusBadge status={doc.status} />
                </td>
                <td className="px-4 py-3 text-foreground/60">{doc.authorName}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
