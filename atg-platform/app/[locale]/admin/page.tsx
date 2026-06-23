"use client";

import { useEffect, useState } from "react";
import { useTranslations, useLocale } from "next-intl";
import Link from "next/link";
import api from "@/lib/api";
import { Button } from "@/components/ui/Button";

interface Stats {
  totalUsers: number;
  activeUsers: number;
  organizations: number;
  departments: number;
}

interface AuditItem {
  id: string;
  userName?: string;
  action: string;
  entityType?: string;
  createdAt: string;
}

export default function AdminDashboard() {
  const t = useTranslations("admin");
  const locale = useLocale();
  const [stats, setStats] = useState<Stats | null>(null);
  const [audit, setAudit] = useState<AuditItem[]>([]);

  useEffect(() => {
    api.get("/audit-logs/dashboard").then((r) => setStats(r.data));
    api.get("/audit-logs", { params: { page: 1, pageSize: 10 } }).then((r) => setAudit(r.data.items));
  }, []);

  const cards = stats
    ? [
        { label: t("totalUsers"), value: stats.totalUsers },
        { label: t("activeUsers"), value: stats.activeUsers },
        { label: t("orgs"), value: stats.organizations },
        { label: t("depts"), value: stats.departments },
      ]
    : [];

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-semibold">{t("dashboard")}</h1>
        <Link href={`/${locale}/admin/users/new`}>
          <Button size="sm">{t("addUser")}</Button>
        </Link>
      </div>
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        {cards.map((c) => (
          <div key={c.label} className="rounded-lg border border-border bg-surface p-4">
            <p className="text-sm text-foreground/60">{c.label}</p>
            <p className="text-2xl font-semibold mt-1">{c.value}</p>
          </div>
        ))}
      </div>
      <h2 className="text-lg font-medium mb-3">{t("recentActivity")}</h2>
      <div className="rounded-lg border border-border overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-surface border-b border-border">
            <tr>
              <th className="text-left p-3 font-medium">User</th>
              <th className="text-left p-3 font-medium">Action</th>
              <th className="text-left p-3 font-medium">Entity</th>
              <th className="text-left p-3 font-medium">Time</th>
            </tr>
          </thead>
          <tbody>
            {audit.map((a) => (
              <tr key={a.id} className="border-b border-border/50 h-10">
                <td className="p-3">{a.userName || "—"}</td>
                <td className="p-3">{a.action}</td>
                <td className="p-3">{a.entityType || "—"}</td>
                <td className="p-3">{new Date(a.createdAt).toLocaleString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
