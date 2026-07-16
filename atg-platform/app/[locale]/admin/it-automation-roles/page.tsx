"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { Building2, FileKey2, HardDrive, Loader2, Phone, Save, Server, Shield } from "lucide-react";
import {
  fetchItAutomationRoles,
  updateItAutomationRole,
  type ItAutomationRolesAdmin,
} from "@/lib/itAutomation";
import { getApiErrorMessage } from "@/lib/api";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

const ICONS: Record<string, typeof FileKey2> = {
  License: FileKey2,
  Service: Server,
  MobileService: Phone,
  GovernmentService: Building2,
  Equipment: HardDrive,
};

export default function AdminItAutomationRolesPage() {
  const t = useTranslations("admin.itAutomationRoles");
  const locale = useLocale();
  const [data, setData] = useState<ItAutomationRolesAdmin | null>(null);
  const [loading, setLoading] = useState(true);
  const [savingKey, setSavingKey] = useState<string | null>(null);
  const [error, setError] = useState("");
  const [ok, setOk] = useState("");
  const [drafts, setDrafts] = useState<Record<string, string>>({});

  const load = () => {
    setLoading(true);
    setError("");
    fetchItAutomationRoles()
      .then((payload) => {
        setData(payload);
        const next: Record<string, string> = {};
        for (const role of payload.roles) next[role.category] = role.responsibleUserId ?? "";
        setDrafts(next);
      })
      .catch((err) => setError(getApiErrorMessage(err, t("loadError"))))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    load();
  }, []);

  const save = async (category: string) => {
    setSavingKey(category);
    setError("");
    setOk("");
    try {
      const updated = await updateItAutomationRole(category, drafts[category] || null);
      setData((prev) =>
        prev
          ? { ...prev, roles: prev.roles.map((r) => (r.category === category ? updated : r)) }
          : prev,
      );
      setOk(t("saved"));
    } catch (err) {
      setError(getApiErrorMessage(err, t("saveError")));
    } finally {
      setSavingKey(null);
    }
  };

  return (
    <div className="mx-auto max-w-5xl px-6 py-8">
      <div className="mb-6 flex items-start gap-3">
        <div className="rounded-xl bg-cyan-500/10 p-3 text-cyan-700">
          <Shield size={22} />
        </div>
        <div>
          <h1 className="text-xl font-semibold tracking-tight">{t("title")}</h1>
          <p className="mt-1 text-sm text-foreground/55">{t("subtitle")}</p>
        </div>
      </div>

      {loading ? (
        <div className="flex items-center gap-2 text-sm text-foreground/50">
          <Loader2 className="animate-spin" size={16} />
          {t("loading")}
        </div>
      ) : (
        <>
          {error && <p className="mb-3 text-sm text-red-600">{error}</p>}
          {ok && <p className="mb-3 text-sm text-emerald-600">{ok}</p>}
          <div className="grid gap-4 md:grid-cols-2">
            {(data?.roles ?? []).map((role) => {
              const Icon = ICONS[role.category] ?? Shield;
              const title = locale.startsWith("en") ? role.titleEn : role.titleRu;
              const desc = locale.startsWith("en") ? role.descriptionEn : role.descriptionRu;
              return (
                <div key={role.category} className="rounded-2xl border border-border bg-surface p-5 shadow-sm">
                  <div className="flex items-start gap-3">
                    <div className="rounded-xl bg-cyan-500/10 p-2.5 text-cyan-700">
                      <Icon size={18} />
                    </div>
                    <div>
                      <h2 className="font-semibold text-foreground">{title}</h2>
                      <p className="mt-1 text-xs leading-relaxed text-foreground/50">{desc}</p>
                    </div>
                  </div>
                  <label className="mt-4 mb-1.5 block text-[11px] font-semibold uppercase tracking-wider text-foreground/45">
                    {t("responsible")}
                  </label>
                  <select
                    value={drafts[role.category] ?? ""}
                    onChange={(e) => setDrafts((d) => ({ ...d, [role.category]: e.target.value }))}
                    className="h-10 w-full rounded-xl border border-border bg-background px-3 text-sm"
                  >
                    <option value="">{t("unassigned")}</option>
                    {(data?.candidates ?? []).map((u) => (
                      <option key={u.id} value={u.id}>
                        {u.fullName} ({u.email})
                      </option>
                    ))}
                  </select>
                  <div className="mt-4 flex justify-end">
                    <Button
                      type="button"
                      onClick={() => save(role.category)}
                      disabled={savingKey === role.category}
                      className="gap-2"
                    >
                      {savingKey === role.category ? (
                        <Loader2 size={14} className="animate-spin" />
                      ) : (
                        <Save size={14} />
                      )}
                      {t("save")}
                    </Button>
                  </div>
                </div>
              );
            })}
          </div>
        </>
      )}
    </div>
  );
}
