"use client";

import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import api from "@/lib/api";
import { localizedDepartmentName } from "@/lib/utils";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";

interface Org { id: string; name: string; code: string; children?: Org[] }
interface Dept { id: string; name: string; nameEn?: string; organizationId: string }
interface Position { id: string; name: string }

const HO_ROLES = ["SuperAdmin", "HOTopManager", "HONachalnik", "HOEngineer"];
const ALL_ROLES = ["SuperAdmin", "HOTopManager", "HONachalnik", "HOEngineer", "BMGMCManager", "BMGMCNachalnikiOtdeli", "BMGMCEngineer", "StationEngineer"];

export default function UserFormPage({ params }: { params: Promise<{ id?: string }> }) {
  const t = useTranslations("users");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const router = useRouter();
  const [userId, setUserId] = useState<string | null>(null);
  const [orgs, setOrgs] = useState<Org[]>([]);
  const [depts, setDepts] = useState<Dept[]>([]);
  const [positions, setPositions] = useState<Position[]>([]);
  const [useLdap, setUseLdap] = useState(true);
  const [form, setForm] = useState({
    employeeId: "", firstName: "", lastName: "", middleName: "", email: "", phone: "",
    organizationId: "", departmentId: "", positionId: "", role: "HOEngineer", language: "ru",
    password: "", confirmPassword: "",
  });
  const [error, setError] = useState("");

  useEffect(() => {
    params.then((p) => {
      const id = (p as { id?: string }).id;
      if (id && id !== "new") setUserId(id);
    });
  }, [params]);

  useEffect(() => {
    api.get("/organizations").then((r) => {
      const flat: Org[] = [];
      const walk = (items: Org[]) => items.forEach((o) => { flat.push(o); if (o.children) walk(o.children); });
      walk(r.data);
      setOrgs(flat);
      const ho = flat.find((o) => o.code === "HO");
      if (ho && !userId) setForm((f) => f.organizationId ? f : { ...f, organizationId: ho.id });
    });
    api.get("/positions").then((r) => setPositions(r.data));
  }, [userId]);

  useEffect(() => {
    if (!userId) {
      api.get("/users/next-employee-id").then((r) => {
        setForm((f) => f.employeeId ? f : { ...f, employeeId: r.data.employeeId });
      });
    }
  }, [userId]);

  useEffect(() => {
    if (form.organizationId) {
      api.get("/departments", { params: { orgId: form.organizationId } }).then((r) => setDepts(r.data));
    } else setDepts([]);
  }, [form.organizationId]);

  useEffect(() => {
    if (!userId) return;
    api.get(`/users/${userId}`).then((r) => {
      const u = r.data;
      setForm({
        employeeId: u.employeeId || "", firstName: u.firstName, lastName: u.lastName, middleName: u.middleName || "",
        email: u.email, phone: u.phone || "", organizationId: u.organizationId, departmentId: u.departmentId || "",
        positionId: u.positionId || "", role: u.role, language: u.language, password: "", confirmPassword: "",
      });
    });
  }, [userId]);

  const selectedOrg = orgs.find((o) => o.id === form.organizationId);
  const roleOptions = selectedOrg?.code === "HO" ? HO_ROLES : ALL_ROLES;

  const set = (k: string, v: string) => setForm((f) => ({ ...f, [k]: v }));

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    if (!userId && !useLdap) {
      if (form.password !== form.confirmPassword) { setError(t("passwordMismatch")); return; }
      if (form.password.length < 8) { setError(t("passwordTooShort")); return; }
    }
    try {
      const body = {
        employeeId: form.employeeId, firstName: form.firstName, lastName: form.lastName, middleName: form.middleName || null,
        email: form.email, phone: form.phone || null, organizationId: form.organizationId,
        departmentId: form.departmentId || null, positionId: form.positionId || null,
        role: form.role, language: form.language,
        useLdap,
        ...(userId || useLdap ? {} : { password: form.password }),
      };
      if (userId) await api.put(`/users/${userId}`, body);
      else await api.post("/users", body);
      router.push(`/${locale}/admin/users`);
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error || tCommon("error");
      setError(msg);
    }
  };

  return (
    <div className="max-w-2xl">
      <h1 className="text-2xl font-semibold mb-6">{userId ? tCommon("edit") : t("addUser")}</h1>
      {selectedOrg?.code === "HO" && !userId && (
        <p className="text-sm text-atg-blue mb-4">{t("hoUserHint")}</p>
      )}
      <form onSubmit={submit} className="space-y-6">
        <section>
          <h2 className="text-sm font-medium text-foreground/60 mb-3">{t("personalInfo")}</h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div><label className="text-sm">{t("firstName")} *</label><Input value={form.firstName} onChange={(e) => set("firstName", e.target.value)} required /></div>
            <div><label className="text-sm">{t("lastName")} *</label><Input value={form.lastName} onChange={(e) => set("lastName", e.target.value)} required /></div>
            <div><label className="text-sm">{t("middleName")}</label><Input value={form.middleName} onChange={(e) => set("middleName", e.target.value)} /></div>
            <div><label className="text-sm">{t("employeeId")} *</label><Input value={form.employeeId} onChange={(e) => set("employeeId", e.target.value)} required /></div>
            <div><label className="text-sm">{t("email")} *</label><Input type="email" value={form.email} onChange={(e) => set("email", e.target.value)} required placeholder="user@atg.uz" /></div>
            <div><label className="text-sm">{t("phone")}</label><Input value={form.phone} onChange={(e) => set("phone", e.target.value)} /></div>
          </div>
        </section>
        <section>
          <h2 className="text-sm font-medium text-foreground/60 mb-3">{t("orgInfo")}</h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label className="text-sm">{t("organization")} *</label>
              <select value={form.organizationId} onChange={(e) => set("organizationId", e.target.value)} required className="h-9 w-full rounded-md border border-border bg-surface px-2 text-sm">
                <option value="">—</option>
                {orgs.map((o) => <option key={o.id} value={o.id}>{o.code} — {o.name}</option>)}
              </select>
            </div>
            <div>
              <label className="text-sm">{t("department")} *</label>
              <select value={form.departmentId} onChange={(e) => set("departmentId", e.target.value)} required className="h-9 w-full rounded-md border border-border bg-surface px-2 text-sm">
                <option value="">—</option>
                {depts.map((d) => <option key={d.id} value={d.id}>{localizedDepartmentName({ name: d.name, nameEn: (d as { nameEn?: string }).nameEn }, locale)}</option>)}
              </select>
            </div>
            <div>
              <label className="text-sm">{t("position")} *</label>
              <select value={form.positionId} onChange={(e) => set("positionId", e.target.value)} required className="h-9 w-full rounded-md border border-border bg-surface px-2 text-sm">
                <option value="">—</option>
                {positions.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
              </select>
            </div>
            <div>
              <label className="text-sm">{t("role")} *</label>
              <select value={form.role} onChange={(e) => set("role", e.target.value)} required className="h-9 w-full rounded-md border border-border bg-surface px-2 text-sm">
                {roleOptions.map((r) => <option key={r} value={r}>{r}</option>)}
              </select>
            </div>
          </div>
        </section>
        {!userId && (
          <section>
            <h2 className="text-sm font-medium text-foreground/60 mb-3">{t("accessInfo")}</h2>
            <div className="flex gap-4 mb-3">
              <label className="flex items-center gap-2 text-sm cursor-pointer">
                <input type="radio" checked={useLdap} onChange={() => setUseLdap(true)} />
                {t("authLdap")}
              </label>
              <label className="flex items-center gap-2 text-sm cursor-pointer">
                <input type="radio" checked={!useLdap} onChange={() => setUseLdap(false)} />
                {t("authLocal")}
              </label>
            </div>
            {!useLdap && (
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div><label className="text-sm">{t("password")} *</label><Input type="password" value={form.password} onChange={(e) => set("password", e.target.value)} required={!useLdap} /></div>
                <div><label className="text-sm">{t("confirmPassword")} *</label><Input type="password" value={form.confirmPassword} onChange={(e) => set("confirmPassword", e.target.value)} required={!useLdap} /></div>
              </div>
            )}
            {useLdap && <p className="text-xs text-foreground/50">{t("ldapAuthHint")}</p>}
            <div className="mt-3">
              <label className="text-sm">{t("language")}</label>
              <select value={form.language} onChange={(e) => set("language", e.target.value)} className="h-9 w-full rounded-md border border-border bg-surface px-2 text-sm max-w-[120px]">
                <option value="ru">RU</option>
                <option value="en">EN</option>
              </select>
            </div>
          </section>
        )}
        {error && <p className="text-sm text-red-400">{error}</p>}
        <div className="flex gap-2">
          <Button type="submit">{tCommon("save")}</Button>
          <Button type="button" variant="secondary" onClick={() => router.back()}>{tCommon("cancel")}</Button>
        </div>
      </form>
    </div>
  );
}
