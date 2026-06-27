"use client";

import { useCallback, useEffect, useState } from "react";
import { useTranslations, useLocale } from "next-intl";
import api from "@/lib/api";
import { localizedDepartmentName } from "@/lib/utils";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Pencil, Plus, Trash2 } from "lucide-react";

interface Org { id: string; name: string; code: string; children?: Org[] }
interface Dept {
  id: string;
  organizationId: string;
  organizationName: string;
  name: string;
  nameEn: string;
  code: string;
  isActive: boolean;
}

export default function DepartmentsPage() {
  const t = useTranslations("admin");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const [orgs, setOrgs] = useState<Org[]>([]);
  const [orgId, setOrgId] = useState("");
  const [depts, setDepts] = useState<Dept[]>([]);
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<Dept | null>(null);
  const [name, setName] = useState("");
  const [nameEn, setNameEn] = useState("");
  const [code, setCode] = useState("");
  const [error, setError] = useState("");

  const loadOrgs = useCallback(() => {
    api.get("/organizations").then((r) => {
      const flat: Org[] = [];
      const walk = (items: Org[]) => items.forEach((o) => { flat.push(o); if (o.children) walk(o.children); });
      walk(r.data);
      setOrgs(flat);
      const ho = flat.find((o) => o.code === "HO");
      if (ho && !orgId) setOrgId(ho.id);
    });
  }, [orgId]);

  const loadDepts = useCallback(() => {
    if (!orgId) return;
    api.get("/departments", { params: { orgId } }).then((r) => setDepts(r.data));
  }, [orgId]);

  useEffect(() => { loadOrgs(); }, [loadOrgs]);
  useEffect(() => { loadDepts(); }, [loadDepts]);

  const openCreate = () => {
    setEditing(null);
    setName("");
    setNameEn("");
    const org = orgs.find((o) => o.id === orgId);
    setCode(org ? `${org.code}-` : "");
    setError("");
    setShowForm(true);
  };

  const openEdit = (d: Dept) => {
    setEditing(d);
    setName(d.name);
    setNameEn(d.nameEn);
    setCode(d.code);
    setError("");
    setShowForm(true);
  };

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    try {
      if (editing) {
        await api.put(`/departments/${editing.id}`, { name, nameEn, code });
      } else {
        await api.post("/departments", { organizationId: orgId, name, nameEn, code });
      }
      setShowForm(false);
      loadDepts();
    } catch (err: unknown) {
      setError((err as { response?: { data?: { error?: string } } })?.response?.data?.error || tCommon("error"));
    }
  };

  const remove = async (id: string) => {
    if (!confirm(t("confirmDeleteDept"))) return;
    await api.delete(`/departments/${id}`);
    loadDepts();
  };

  const selectedOrg = orgs.find((o) => o.id === orgId);

  return (
    <div>
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-2xl font-semibold">{t("departments")}</h1>
        <Button size="sm" onClick={openCreate} disabled={!orgId}>
          <Plus size={14} className="mr-1" />
          {t("addDepartment")}
        </Button>
      </div>

      <div className="flex flex-wrap gap-2 mb-4 items-center">
        <label className="text-sm text-foreground/60">{t("filterByOrg")}</label>
        <select
          value={orgId}
          onChange={(e) => setOrgId(e.target.value)}
          className="h-9 rounded-md border border-border bg-surface px-2 text-sm min-w-[240px]"
        >
          <option value="">—</option>
          {orgs.map((o) => (
            <option key={o.id} value={o.id}>{o.code} — {o.name}</option>
          ))}
        </select>
        {selectedOrg?.code === "HO" && (
          <span className="text-xs text-atg-blue bg-atg-blue/10 px-2 py-1 rounded">{t("hoPhase")}</span>
        )}
      </div>

      {showForm && (
        <form onSubmit={submit} className="mb-6 p-4 rounded-lg border border-border bg-surface/50 space-y-3 max-w-md">
          <h2 className="font-medium">{editing ? tCommon("edit") : t("addDepartment")}</h2>
          <div>
            <label className="text-sm">{t("deptName")} (RU) *</label>
            <Input value={name} onChange={(e) => setName(e.target.value)} required />
          </div>
          <div>
            <label className="text-sm">{t("deptName")} (EN) *</label>
            <Input value={nameEn} onChange={(e) => setNameEn(e.target.value)} required />
          </div>
          <div>
            <label className="text-sm">{t("deptCode")} *</label>
            <Input value={code} onChange={(e) => setCode(e.target.value)} required />
          </div>
          {error && <p className="text-sm text-red-400">{error}</p>}
          <div className="flex gap-2">
            <Button type="submit" size="sm">{tCommon("save")}</Button>
            <Button type="button" size="sm" variant="secondary" onClick={() => setShowForm(false)}>{tCommon("cancel")}</Button>
          </div>
        </form>
      )}

      <div className="rounded-lg border border-border overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-surface border-b border-border">
            <tr>
              <th className="text-left p-3">{t("deptCode")}</th>
              <th className="text-left p-3">{t("deptName")}</th>
              <th className="text-left p-3"></th>
            </tr>
          </thead>
          <tbody>
            {depts.map((d) => (
              <tr key={d.id} className="border-b border-border/50 h-10 hover:bg-border/10">
                <td className="p-3 font-mono text-xs">{d.code}</td>
                <td className="p-3">{localizedDepartmentName(d, locale)}</td>
                <td className="p-3 text-right">
                  <button onClick={() => openEdit(d)} className="p-1 hover:bg-border/30 rounded inline-flex"><Pencil size={14} /></button>
                  <button onClick={() => remove(d.id)} className="p-1 hover:bg-border/30 rounded inline-flex text-red-400 ml-1"><Trash2 size={14} /></button>
                </td>
              </tr>
            ))}
            {depts.length === 0 && (
              <tr><td colSpan={3} className="p-6 text-center text-foreground/50">{t("noDepartments")}</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
