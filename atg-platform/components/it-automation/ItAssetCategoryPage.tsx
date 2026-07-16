"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { AlertTriangle, Loader2, Plus, Save, X } from "lucide-react";
import {
  createItAsset,
  deleteItAsset,
  fetchItAssets,
  formatDate,
  formatMoney,
  updateItAsset,
  type ItAsset,
  type ItAssetCategory,
} from "@/lib/itAutomation";
import { getApiErrorMessage } from "@/lib/api";
import { cn } from "@/lib/utils";
import { itAutomationTheme } from "@/components/it-automation/itAutomationTheme";

const STATUSES = ["Active", "InProcess", "Done", "Expired", "Suspended", "Cancelled"] as const;

interface Props {
  category: ItAssetCategory;
}

export function ItAssetCategoryPage({ category }: Props) {
  const t = useTranslations("itAutomation");
  const locale = useLocale();
  const [items, setItems] = useState<ItAsset[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [editorOpen, setEditorOpen] = useState(false);
  const [editing, setEditing] = useState<ItAsset | null>(null);
  const [saving, setSaving] = useState(false);

  const load = () => {
    setLoading(true);
    fetchItAssets(category)
      .then(setItems)
      .catch((err) => setError(getApiErrorMessage(err, t("loadError"))))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    load();
  }, [category]);

  const expiring = useMemo(() => items.filter((i) => i.expiryWarning).length, [items]);

  const openCreate = () => {
    setEditing(null);
    setEditorOpen(true);
  };

  const openEdit = (item: ItAsset) => {
    setEditing(item);
    setEditorOpen(true);
  };

  const onDelete = async (id: string) => {
    if (!confirm(t("confirmDelete"))) return;
    try {
      await deleteItAsset(id);
      load();
    } catch (err) {
      setError(getApiErrorMessage(err, t("saveError")));
    }
  };

  return (
    <div className={cn("relative flex-1 overflow-y-auto", itAutomationTheme.meshBg)}>
      <div className="mx-auto max-w-6xl px-6 py-8">
        <div className="mb-6 flex flex-wrap items-end justify-between gap-4">
          <div>
            <p className={itAutomationTheme.sectionLabel}>{t("title")}</p>
            <h1 className="mt-1 text-2xl font-semibold tracking-tight">
              {t(`categories.${category}` as "categories.License")}
            </h1>
            <p className="mt-1 text-sm text-foreground/50">
              {t(`categoryDesc.${category}` as "categoryDesc.License")}
            </p>
          </div>
          <button
            type="button"
            onClick={openCreate}
            className="inline-flex items-center gap-2 rounded-xl bg-gradient-to-r from-cyan-700 to-indigo-500 px-4 py-2.5 text-sm font-semibold text-white shadow-md shadow-cyan-500/20"
          >
            <Plus size={16} />
            {t("addItem")}
          </button>
        </div>

        {expiring > 0 && (
          <div className="mb-4 flex items-center gap-2 rounded-xl border border-amber-500/30 bg-amber-500/10 px-4 py-3 text-sm text-amber-800 dark:text-amber-300">
            <AlertTriangle size={16} />
            {t("expiringBanner", { count: expiring })}
          </div>
        )}

        {error && <p className="mb-3 text-sm text-red-600">{error}</p>}

        {loading ? (
          <p className="text-sm text-foreground/45">{t("loading")}</p>
        ) : items.length === 0 ? (
          <div className={cn("px-6 py-16 text-center", itAutomationTheme.card)}>
            <p className="text-sm text-foreground/50">{t("empty")}</p>
          </div>
        ) : (
          <div className={cn("overflow-hidden", itAutomationTheme.card)}>
            <div className="overflow-x-auto">
              <table className="w-full min-w-[900px] text-left text-sm">
                <thead className="border-b border-border/60 bg-foreground/[0.02] text-[11px] font-bold uppercase tracking-wider text-foreground/40">
                  <tr>
                    <th className="px-4 py-3">{t("columns.name")}</th>
                    <th className="px-4 py-3">{t("columns.qty")}</th>
                    <th className="px-4 py-3">{t("columns.term")}</th>
                    <th className="px-4 py-3">{t("columns.contract")}</th>
                    <th className="px-4 py-3">{t("columns.expires")}</th>
                    <th className="px-4 py-3">{t("columns.cost")}</th>
                    <th className="px-4 py-3">{t("columns.status")}</th>
                    <th className="px-4 py-3" />
                  </tr>
                </thead>
                <tbody>
                  {items.map((item) => (
                    <tr key={item.id} className="border-b border-border/40 last:border-0 hover:bg-foreground/[0.02]">
                      <td className="px-4 py-3">
                        <p className="font-medium text-foreground">
                          {locale.startsWith("en") ? item.nameEn || item.nameRu : item.nameRu || item.nameEn}
                        </p>
                        {item.responsibleUserName && (
                          <p className="mt-0.5 text-xs text-foreground/40">{item.responsibleUserName}</p>
                        )}
                      </td>
                      <td className="px-4 py-3 text-foreground/60">{item.quantity ?? "—"}</td>
                      <td className="px-4 py-3 text-foreground/60">{item.term ?? "—"}</td>
                      <td className="px-4 py-3 text-foreground/60">
                        <div>{item.contractNumber ?? "—"}</div>
                        <div className="text-xs text-foreground/35">{formatDate(item.contractDate, locale)}</div>
                      </td>
                      <td className="px-4 py-3">
                        <span className={cn(item.expiryWarning && "font-semibold text-amber-700 dark:text-amber-300")}>
                          {formatDate(item.expiresAt, locale)}
                        </span>
                        {item.daysUntilExpiry != null && item.daysUntilExpiry >= 0 && item.daysUntilExpiry <= 100 && (
                          <div className="text-[11px] text-foreground/40">
                            {t("daysLeft", { days: item.daysUntilExpiry })}
                          </div>
                        )}
                      </td>
                      <td className="px-4 py-3 text-foreground/60">{formatMoney(item.cost, item.currency)}</td>
                      <td className="px-4 py-3">
                        <StatusBadge status={item.status} label={t(`status.${item.status}` as "status.Active")} />
                      </td>
                      <td className="px-4 py-3 text-right whitespace-nowrap">
                        <button type="button" onClick={() => openEdit(item)} className="text-xs font-medium text-cyan-700 hover:underline dark:text-cyan-400">
                          {t("edit")}
                        </button>
                        <button type="button" onClick={() => onDelete(item.id)} className="ml-3 text-xs font-medium text-red-600 hover:underline">
                          {t("delete")}
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}
      </div>

      {editorOpen && (
        <AssetEditor
          category={category}
          item={editing}
          onClose={() => setEditorOpen(false)}
          onSaved={() => {
            setEditorOpen(false);
            load();
          }}
          saving={saving}
          setSaving={setSaving}
        />
      )}
    </div>
  );
}

function StatusBadge({ status, label }: { status: string; label: string }) {
  const cls =
    status === "Done" || status === "Active"
      ? "bg-emerald-50 text-emerald-700 border-emerald-200"
      : status === "InProcess"
        ? "bg-sky-50 text-sky-700 border-sky-200"
        : status === "Expired"
          ? "bg-red-50 text-red-700 border-red-200"
          : "bg-slate-100 text-slate-600 border-slate-200";
  return <span className={cn("inline-flex rounded-full border px-2.5 py-0.5 text-[11px] font-semibold", cls)}>{label}</span>;
}

function AssetEditor({
  category,
  item,
  onClose,
  onSaved,
  saving,
  setSaving,
}: {
  category: ItAssetCategory;
  item: ItAsset | null;
  onClose: () => void;
  onSaved: () => void;
  saving: boolean;
  setSaving: (v: boolean) => void;
}) {
  const t = useTranslations("itAutomation");
  const [error, setError] = useState("");
  const [form, setForm] = useState({
    nameRu: item?.nameRu ?? "",
    nameEn: item?.nameEn ?? "",
    quantity: item?.quantity ?? "",
    term: item?.term ?? "Annual",
    contractNumber: item?.contractNumber ?? "",
    contractDate: item?.contractDate?.slice(0, 10) ?? "",
    expiresAt: item?.expiresAt?.slice(0, 10) ?? "",
    cost: item?.cost?.toString() ?? "",
    currency: item?.currency ?? "UZS",
    status: item?.status ?? "Active",
    note: item?.note ?? "",
    planYear: (item?.planYear ?? new Date().getFullYear()).toString(),
  });

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError("");
    const body = {
      category,
      nameRu: form.nameRu.trim(),
      nameEn: form.nameEn.trim() || form.nameRu.trim(),
      quantity: form.quantity || null,
      term: form.term || null,
      contractNumber: form.contractNumber || null,
      contractDate: form.contractDate || null,
      expiresAt: form.expiresAt || null,
      cost: form.cost ? Number(form.cost) : null,
      currency: form.currency || null,
      status: form.status,
      note: form.note || null,
      planYear: Number(form.planYear) || new Date().getFullYear(),
      budgetCode: null,
      budgetAmount: null,
      responsibleUserId: item?.responsibleUserId ?? null,
      startsAt: form.contractDate || null,
    };
    try {
      if (item) await updateItAsset(item.id, body);
      else await createItAsset(body);
      onSaved();
    } catch (err) {
      setError(getApiErrorMessage(err, t("saveError")));
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4 backdrop-blur-sm">
      <form onSubmit={submit} className={cn("relative w-full max-w-xl max-h-[90vh] overflow-y-auto p-6", itAutomationTheme.card)}>
        <button type="button" onClick={onClose} className="absolute right-4 top-4 text-foreground/40 hover:text-foreground">
          <X size={18} />
        </button>
        <h2 className="text-lg font-semibold">{item ? t("editItem") : t("addItem")}</h2>
        <div className="mt-4 grid gap-3 sm:grid-cols-2">
          <Field label={t("fields.nameRu")} value={form.nameRu} onChange={(v) => setForm({ ...form, nameRu: v })} required />
          <Field label={t("fields.nameEn")} value={form.nameEn} onChange={(v) => setForm({ ...form, nameEn: v })} />
          <Field label={t("fields.quantity")} value={form.quantity} onChange={(v) => setForm({ ...form, quantity: v })} />
          <Field label={t("fields.term")} value={form.term} onChange={(v) => setForm({ ...form, term: v })} />
          <Field label={t("fields.contractNumber")} value={form.contractNumber} onChange={(v) => setForm({ ...form, contractNumber: v })} />
          <Field label={t("fields.contractDate")} value={form.contractDate} onChange={(v) => setForm({ ...form, contractDate: v })} type="date" />
          <Field label={t("fields.expiresAt")} value={form.expiresAt} onChange={(v) => setForm({ ...form, expiresAt: v })} type="date" />
          <Field label={t("fields.planYear")} value={form.planYear} onChange={(v) => setForm({ ...form, planYear: v })} />
          <Field label={t("fields.cost")} value={form.cost} onChange={(v) => setForm({ ...form, cost: v })} />
          <Field label={t("fields.currency")} value={form.currency} onChange={(v) => setForm({ ...form, currency: v })} />
          <div className="sm:col-span-2">
            <label className="mb-1.5 block text-[11px] font-semibold uppercase tracking-wider text-foreground/45">{t("fields.status")}</label>
            <select
              value={form.status}
              onChange={(e) => setForm({ ...form, status: e.target.value })}
              className="h-10 w-full rounded-xl border border-border bg-background px-3 text-sm"
            >
              {STATUSES.map((s) => (
                <option key={s} value={s}>{t(`status.${s}`)}</option>
              ))}
            </select>
          </div>
          <div className="sm:col-span-2">
            <label className="mb-1.5 block text-[11px] font-semibold uppercase tracking-wider text-foreground/45">{t("fields.note")}</label>
            <textarea
              value={form.note}
              onChange={(e) => setForm({ ...form, note: e.target.value })}
              rows={3}
              className="w-full rounded-xl border border-border bg-background px-3 py-2 text-sm"
            />
          </div>
        </div>
        {error && <p className="mt-3 text-sm text-red-600">{error}</p>}
        <div className="mt-5 flex justify-end gap-2">
          <button type="button" onClick={onClose} className="rounded-xl border border-border px-4 py-2 text-sm">{t("cancel")}</button>
          <button
            type="submit"
            disabled={saving}
            className="inline-flex items-center gap-2 rounded-xl bg-cyan-700 px-4 py-2 text-sm font-semibold text-white disabled:opacity-60"
          >
            {saving ? <Loader2 size={14} className="animate-spin" /> : <Save size={14} />}
            {t("save")}
          </button>
        </div>
      </form>
    </div>
  );
}

function Field({
  label,
  value,
  onChange,
  type = "text",
  required,
}: {
  label: string;
  value: string;
  onChange: (v: string) => void;
  type?: string;
  required?: boolean;
}) {
  return (
    <div>
      <label className="mb-1.5 block text-[11px] font-semibold uppercase tracking-wider text-foreground/45">{label}</label>
      <input
        required={required}
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="h-10 w-full rounded-xl border border-border bg-background px-3 text-sm"
      />
    </div>
  );
}
