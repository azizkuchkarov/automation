"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import {
  FileSignature,
  Globe2,
  Loader2,
  MapPin,
  Megaphone,
  Save,
  Users,
} from "lucide-react";
import api, { getApiErrorMessage } from "@/lib/api";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

interface WorkflowUser {
  id: string;
  fullName: string;
  email: string;
  departmentName?: string;
}

interface WorkflowDepartment {
  id: string;
  code: string;
  name: string;
  nameEn: string;
}

interface WorkflowRole {
  roleKey: string;
  titleRu: string;
  titleEn: string;
  descriptionRu: string;
  descriptionEn: string;
  managerUserId?: string;
  managerUserName?: string;
  managerUserEmail?: string;
  engineerDepartmentId?: string;
  engineerDepartmentName?: string;
  engineerDepartmentNameEn?: string;
  engineerDepartmentCode?: string;
  engineers: WorkflowUser[];
}

interface AdminPayload {
  roles: WorkflowRole[];
  candidateManagers: WorkflowUser[];
  departments: WorkflowDepartment[];
}

const ROLE_ICONS: Record<string, typeof Megaphone> = {
  MarketingSectionHead: Megaphone,
  ContractsDepartmentHead: FileSignature,
  ContractsIntSectionHead: Globe2,
  ContractsDomSectionHead: MapPin,
};

export default function AdminProcurementRolesPage() {
  const t = useTranslations("admin.procurementRoles");
  const locale = useLocale();
  const [data, setData] = useState<AdminPayload | null>(null);
  const [loading, setLoading] = useState(true);
  const [savingKey, setSavingKey] = useState<string | null>(null);
  const [error, setError] = useState("");
  const [ok, setOk] = useState("");
  const [drafts, setDrafts] = useState<
    Record<string, { managerUserId: string; engineerDepartmentId: string }>
  >({});

  const load = () => {
    setLoading(true);
    setError("");
    api
      .get<AdminPayload>("/dcs/procurement-requests/admin/workflow-roles")
      .then((r) => {
        setData(r.data);
        const next: Record<string, { managerUserId: string; engineerDepartmentId: string }> = {};
        for (const role of r.data.roles) {
          next[role.roleKey] = {
            managerUserId: role.managerUserId ?? "",
            engineerDepartmentId: role.engineerDepartmentId ?? "",
          };
        }
        setDrafts(next);
      })
      .catch((err) => setError(getApiErrorMessage(err, t("loadError"))))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    load();
  }, []);

  const save = async (roleKey: string) => {
    const draft = drafts[roleKey];
    if (!draft) return;
    setSavingKey(roleKey);
    setError("");
    setOk("");
    try {
      const r = await api.put<WorkflowRole>(
        `/dcs/procurement-requests/admin/workflow-roles/${roleKey}`,
        {
          managerUserId: draft.managerUserId || null,
          engineerDepartmentId: draft.engineerDepartmentId || null,
        },
      );
      setData((prev) =>
        prev
          ? {
              ...prev,
              roles: prev.roles.map((role) => (role.roleKey === roleKey ? r.data : role)),
            }
          : prev,
      );
      setOk(t("saved"));
    } catch (err) {
      setError(getApiErrorMessage(err, t("saveError")));
    } finally {
      setSavingKey(null);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center gap-2 p-8 text-sm text-foreground/40">
        <Loader2 size={16} className="animate-spin" />
        {t("loading")}
      </div>
    );
  }

  if (!data) {
    return <div className="p-8 text-sm text-red-600">{error || t("loadError")}</div>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">{t("title")}</h1>
        <p className="mt-1 text-sm text-foreground/50">{t("subtitle")}</p>
      </div>

      {error && (
        <div className="rounded-xl border border-red-500/30 bg-red-500/5 px-4 py-3 text-sm text-red-700">
          {error}
        </div>
      )}
      {ok && (
        <div className="rounded-xl border border-emerald-500/30 bg-emerald-500/5 px-4 py-3 text-sm text-emerald-700">
          {ok}
        </div>
      )}

      <div className="grid gap-4 xl:grid-cols-2">
        {data.roles.map((role) => {
          const Icon = ROLE_ICONS[role.roleKey] ?? Users;
          const draft = drafts[role.roleKey] ?? {
            managerUserId: "",
            engineerDepartmentId: "",
          };
          const title = locale.startsWith("en") ? role.titleEn : role.titleRu;
          const description = locale.startsWith("en")
            ? role.descriptionEn
            : role.descriptionRu;

          return (
            <section
              key={role.roleKey}
              className="rounded-2xl border border-border bg-surface p-5 shadow-sm"
            >
              <div className="mb-4 flex items-start gap-3">
                <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-sky-500/10 text-sky-600">
                  <Icon size={18} />
                </div>
                <div className="min-w-0">
                  <h2 className="text-base font-semibold">{title}</h2>
                  <p className="mt-0.5 text-xs text-foreground/50">{description}</p>
                </div>
              </div>

              <div className="space-y-3">
                <div>
                  <label className="mb-1.5 block text-[11px] font-bold uppercase tracking-wider text-foreground/40">
                    {t("manager")}
                  </label>
                  <select
                    className="w-full rounded-lg border border-border bg-background px-3 py-2.5 text-sm"
                    value={draft.managerUserId}
                    onChange={(e) =>
                      setDrafts((prev) => ({
                        ...prev,
                        [role.roleKey]: {
                          ...draft,
                          managerUserId: e.target.value,
                        },
                      }))
                    }
                  >
                    <option value="">{t("selectManager")}</option>
                    {data.candidateManagers.map((u) => (
                      <option key={u.id} value={u.id}>
                        {u.fullName} ({u.email})
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="mb-1.5 block text-[11px] font-bold uppercase tracking-wider text-foreground/40">
                    {t("engineerDepartment")}
                  </label>
                  <select
                    className="w-full rounded-lg border border-border bg-background px-3 py-2.5 text-sm"
                    value={draft.engineerDepartmentId}
                    onChange={(e) =>
                      setDrafts((prev) => ({
                        ...prev,
                        [role.roleKey]: {
                          ...draft,
                          engineerDepartmentId: e.target.value,
                        },
                      }))
                    }
                  >
                    <option value="">{t("selectDepartment")}</option>
                    {data.departments.map((d) => (
                      <option key={d.id} value={d.id}>
                        {d.code} — {locale.startsWith("en") && d.nameEn ? d.nameEn : d.name}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="rounded-xl border border-border/70 bg-background/60 p-3">
                  <p className="mb-2 text-[11px] font-bold uppercase tracking-wider text-foreground/40">
                    {t("engineers")} ({role.engineers.length})
                  </p>
                  {role.engineers.length === 0 ? (
                    <p className="text-xs text-foreground/40">{t("noEngineers")}</p>
                  ) : (
                    <ul className="max-h-36 space-y-1 overflow-y-auto">
                      {role.engineers.map((eng) => (
                        <li
                          key={eng.id}
                          className="flex items-center justify-between gap-2 rounded-lg px-2 py-1.5 text-sm hover:bg-foreground/[0.03]"
                        >
                          <span className="font-medium">{eng.fullName}</span>
                          <span className="truncate text-xs text-foreground/40">{eng.email}</span>
                        </li>
                      ))}
                    </ul>
                  )}
                </div>

                <Button
                  size="sm"
                  disabled={savingKey === role.roleKey}
                  onClick={() => save(role.roleKey)}
                  className={cn("bg-sky-600 text-white hover:bg-sky-500")}
                >
                  {savingKey === role.roleKey ? (
                    <Loader2 size={14} className="mr-1.5 animate-spin" />
                  ) : (
                    <Save size={14} className="mr-1.5" />
                  )}
                  {t("save")}
                </Button>
              </div>
            </section>
          );
        })}
      </div>
    </div>
  );
}
