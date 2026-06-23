"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import api from "@/lib/api";

interface AuditItem {
  id: string;
  userName?: string;
  action: string;
  entityType?: string;
  entityId?: string;
  details?: string;
  ipAddress?: string;
  createdAt: string;
}

export default function AuditPage() {
  const t = useTranslations("admin");
  const [logs, setLogs] = useState<AuditItem[]>([]);
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);

  useEffect(() => {
    api.get("/audit-logs", { params: { page, pageSize: 20 } }).then((r) => {
      setLogs(r.data.items);
      setTotal(r.data.totalCount);
    });
  }, [page]);

  return (
    <div>
      <h1 className="text-2xl font-semibold mb-6">{t("audit")}</h1>
      <div className="rounded-lg border border-border overflow-x-auto">
        <table className="w-full text-sm min-w-[700px]">
          <thead className="bg-surface border-b border-border">
            <tr>
              <th className="text-left p-3">User</th>
              <th className="text-left p-3">Action</th>
              <th className="text-left p-3">Entity</th>
              <th className="text-left p-3">IP</th>
              <th className="text-left p-3">Time</th>
            </tr>
          </thead>
          <tbody>
            {logs.map((l) => (
              <tr key={l.id} className="border-b border-border/50 h-10">
                <td className="p-3">{l.userName || "—"}</td>
                <td className="p-3">{l.action}</td>
                <td className="p-3">{l.entityType || "—"}</td>
                <td className="p-3">{l.ipAddress || "—"}</td>
                <td className="p-3">{new Date(l.createdAt).toLocaleString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <p className="text-sm text-foreground/60 mt-2">{total} entries</p>
    </div>
  );
}
