"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import api from "@/lib/api";

interface Dept {
  id: string;
  organizationName: string;
  name: string;
  code: string;
  isActive: boolean;
}

export default function DepartmentsPage() {
  const t = useTranslations("admin");
  const [depts, setDepts] = useState<Dept[]>([]);

  useEffect(() => {
    api.get("/departments").then((r) => setDepts(r.data));
  }, []);

  return (
    <div>
      <h1 className="text-2xl font-semibold mb-6">{t("departments")}</h1>
      <div className="rounded-lg border border-border overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-surface border-b border-border">
            <tr>
              <th className="text-left p-3">Organization</th>
              <th className="text-left p-3">Name</th>
              <th className="text-left p-3">Code</th>
            </tr>
          </thead>
          <tbody>
            {depts.map((d) => (
              <tr key={d.id} className="border-b border-border/50 h-10">
                <td className="p-3">{d.organizationName}</td>
                <td className="p-3">{d.name}</td>
                <td className="p-3">{d.code}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
