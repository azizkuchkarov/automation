"use client";

import { useEffect, useState } from "react";
import { useTranslations, useLocale } from "next-intl";
import Link from "next/link";
import api from "@/lib/api";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Badge } from "@/components/ui/Badge";
import { formatRelativeTime } from "@/lib/utils";
import { Pencil } from "lucide-react";

interface User {
  id: string;
  employeeId?: string;
  fullName: string;
  email: string;
  organizationCode: string;
  departmentName?: string;
  positionName?: string;
  role: string;
  isActive: boolean;
  lastLoginAt?: string;
}

export default function UsersPage() {
  const t = useTranslations("users");
  const tAdmin = useTranslations("admin");
  const locale = useLocale();
  const [users, setUsers] = useState<User[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [orgId, setOrgId] = useState("");
  const [role, setRole] = useState("");
  const [isActive, setIsActive] = useState("");
  const [orgs, setOrgs] = useState<{ id: string; name: string; code: string; children?: unknown[] }[]>([]);

  const load = () => {
    api.get("/users", {
      params: { page, pageSize: 20, search: search || undefined, orgId: orgId || undefined, role: role || undefined, isActive: isActive || undefined },
    }).then((r) => {
      setUsers(r.data.items);
      setTotal(r.data.totalCount);
    });
  };

  useEffect(() => { load(); }, [page, orgId, role, isActive]);

  useEffect(() => {
    api.get("/organizations").then((r) => {
      const flat: { id: string; name: string; code: string }[] = [];
      const walk = (items: typeof r.data) => {
        items.forEach((o: { id: string; name: string; code: string; children?: unknown[] }) => {
          flat.push(o);
          if (o.children) walk(o.children as typeof r.data);
        });
      };
      walk(r.data);
      setOrgs(flat);
    });
  }, []);

  const toggleActive = async (id: string, active: boolean) => {
    await api.patch(`/users/${id}/${active ? "deactivate" : "activate"}`);
    load();
  };

  const exportCsv = () => {
    window.open("/api/users/export", "_blank");
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-2xl font-semibold">{t("title")}</h1>
        <div className="flex gap-2">
          <Button variant="secondary" size="sm" onClick={exportCsv}>{tAdmin("export")}</Button>
          <Link href={`/${locale}/admin/users/new`}><Button size="sm">{t("addUser")}</Button></Link>
        </div>
      </div>
      <div className="flex flex-wrap gap-2 mb-4">
        <Input placeholder={t("title") + "..."} value={search} onChange={(e) => setSearch(e.target.value)} className="max-w-xs" />
        <Button size="sm" variant="secondary" onClick={() => { setPage(1); load(); }}>Search</Button>
        <select value={orgId} onChange={(e) => setOrgId(e.target.value)} className="h-9 rounded-md border border-border bg-surface px-2 text-sm">
          <option value="">All orgs</option>
          {orgs.map((o) => <option key={o.id} value={o.id}>{o.code} — {o.name}</option>)}
        </select>
        <select value={role} onChange={(e) => setRole(e.target.value)} className="h-9 rounded-md border border-border bg-surface px-2 text-sm">
          <option value="">All roles</option>
          {["SuperAdmin", "HOTopManager", "HONachalnik", "HOEngineer", "BMGMCManager", "BMGMCNachalnikiOtdeli", "BMGMCEngineer", "StationEngineer"].map((r) => (
            <option key={r} value={r}>{r}</option>
          ))}
        </select>
        <select value={isActive} onChange={(e) => setIsActive(e.target.value)} className="h-9 rounded-md border border-border bg-surface px-2 text-sm">
          <option value="">All status</option>
          <option value="true">{t("active")}</option>
          <option value="false">{t("inactive")}</option>
        </select>
      </div>
      <div className="rounded-lg border border-border overflow-x-auto">
        <table className="w-full text-sm min-w-[800px]">
          <thead className="bg-surface border-b border-border">
            <tr>
              <th className="text-left p-3">Employee</th>
              <th className="text-left p-3">{t("organization")}</th>
              <th className="text-left p-3">{t("department")}</th>
              <th className="text-left p-3">{t("position")}</th>
              <th className="text-left p-3">{t("role")}</th>
              <th className="text-left p-3">{t("status")}</th>
              <th className="text-left p-3">{t("lastLogin")}</th>
              <th className="text-left p-3"></th>
            </tr>
          </thead>
          <tbody>
            {users.map((u) => (
              <tr key={u.id} className="border-b border-border/50 h-10 hover:bg-border/10">
                <td className="p-3">
                  <div className="font-medium">{u.fullName}</div>
                  <div className="text-xs text-foreground/50">{u.employeeId} · {u.email}</div>
                </td>
                <td className="p-3"><Badge className="bg-atg-blue/20 text-atg-blue">{u.organizationCode}</Badge></td>
                <td className="p-3">{u.departmentName || "—"}</td>
                <td className="p-3">{u.positionName || "—"}</td>
                <td className="p-3"><Badge className="bg-border/50">{u.role}</Badge></td>
                <td className="p-3">
                  <Badge className={u.isActive ? "bg-green-500/20 text-green-400" : "bg-red-500/20 text-red-400"}>
                    {u.isActive ? t("active") : t("inactive")}
                  </Badge>
                </td>
                <td className="p-3">{formatRelativeTime(u.lastLoginAt ?? null, locale)}</td>
                <td className="p-3">
                  <div className="flex gap-1">
                    <Link href={`/${locale}/admin/users/${u.id}`} className="p-1 hover:bg-border/30 rounded"><Pencil size={14} /></Link>
                    <button onClick={() => toggleActive(u.id, u.isActive)} className="text-xs text-foreground/60 hover:underline">
                      {u.isActive ? "Off" : "On"}
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <div className="flex justify-between items-center mt-4 text-sm">
        <span>{total} total</span>
        <div className="flex gap-2">
          <Button size="sm" variant="secondary" disabled={page <= 1} onClick={() => setPage(page - 1)}>Prev</Button>
          <span>Page {page}</span>
          <Button size="sm" variant="secondary" disabled={page * 20 >= total} onClick={() => setPage(page + 1)}>Next</Button>
        </div>
      </div>
    </div>
  );
}
