"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { Plus, Trash2 } from "lucide-react";
import api, { getApiErrorMessage } from "@/lib/api";
import {
  CreateHrLeaveItemPayload,
  HR_LEAVE_ITEM_TYPES,
  HrLeaveItemType,
  itemTypeLabel,
  itemTypeNeedsDates,
  itemTypeNeedsDaysCount,
} from "@/lib/hrLeave";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

type ItemRow = CreateHrLeaveItemPayload & { key: string };

function emptyItem(): ItemRow {
  return {
    key: crypto.randomUUID(),
    type: "RegularLeave",
    dateFrom: null,
    dateTo: null,
    daysCount: null,
    noteRu: null,
    noteEn: null,
  };
}

export default function NewHrLeavePage() {
  const t = useTranslations("hr.leave");
  const locale = useLocale();
  const router = useRouter();
  const [periodLabel, setPeriodLabel] = useState(new Date().getFullYear().toString());
  const [requestDate, setRequestDate] = useState(new Date().toISOString().slice(0, 10));
  const [items, setItems] = useState<ItemRow[]>([emptyItem()]);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  const inputClass =
    "w-full rounded-lg border border-border/80 bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-violet-500/30 focus:border-violet-500/50";

  const updateItem = (key: string, patch: Partial<ItemRow>) => {
    setItems((prev) => prev.map((row) => (row.key === key ? { ...row, ...patch } : row)));
  };

  const buildPayload = () =>
    items.map(({ key: _key, ...item }) => ({
      type: item.type,
      dateFrom: itemTypeNeedsDates(item.type) ? item.dateFrom : null,
      dateTo: itemTypeNeedsDates(item.type) ? item.dateTo : null,
      daysCount: itemTypeNeedsDaysCount(item.type) ? item.daysCount : null,
      noteRu: item.noteRu || null,
      noteEn: item.noteEn || null,
    }));

  const save = async (submit: boolean) => {
    setError("");
    setSubmitting(true);
    try {
      const body = {
        periodLabel: periodLabel.trim(),
        requestDate,
        items: buildPayload(),
      };
      const res = await api.post("/hr/leave-requests", body);
      if (submit) {
        await api.post(`/hr/leave-requests/${res.data.id}/submit`);
      }
      router.push(`/${locale}/hr/leave/${res.data.id}`);
    } catch (err: unknown) {
      setError(getApiErrorMessage(err) ?? t("createError"));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="flex flex-col flex-1 min-h-0">
      <header className="shrink-0 border-b border-border/80 bg-surface px-6 py-5">
        <h1 className="text-xl font-semibold text-foreground">{t("newTitle")}</h1>
        <p className="text-sm text-foreground/50 mt-1">{t("newSubtitle")}</p>
      </header>

      <div className="flex-1 overflow-y-auto px-6 py-5">
        <form
          className="max-w-3xl space-y-6"
          onSubmit={(e) => {
            e.preventDefault();
            save(true);
          }}
        >
          <div className="grid sm:grid-cols-2 gap-4">
            <div>
              <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
                {t("fields.periodLabel")}
              </label>
              <input
                required
                value={periodLabel}
                onChange={(e) => setPeriodLabel(e.target.value)}
                placeholder={t("fields.periodPlaceholder")}
                className={cn(inputClass, "h-10")}
              />
            </div>
            <div>
              <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
                {t("fields.requestDate")}
              </label>
              <input
                required
                type="date"
                value={requestDate}
                onChange={(e) => setRequestDate(e.target.value)}
                className={cn(inputClass, "h-10")}
              />
            </div>
          </div>

          <section className="space-y-4">
            <div className="flex items-center justify-between">
              <h2 className="text-sm font-semibold text-foreground">{t("itemsTitle")}</h2>
              <Button
                type="button"
                variant="secondary"
                size="sm"
                onClick={() => setItems((prev) => [...prev, emptyItem()])}
              >
                <Plus size={14} className="mr-1" />
                {t("addItem")}
              </Button>
            </div>

            {items.map((row, index) => (
              <div key={row.key} className="rounded-xl border border-border/80 bg-surface p-4 space-y-3 shadow-sm">
                <div className="flex items-center justify-between gap-2">
                  <span className="text-xs font-semibold uppercase tracking-wider text-foreground/40">
                    {t("itemNumber", { n: index + 1 })}
                  </span>
                  {items.length > 1 && (
                    <button
                      type="button"
                      onClick={() => setItems((prev) => prev.filter((i) => i.key !== row.key))}
                      className="text-foreground/40 hover:text-red-500 p-1"
                    >
                      <Trash2 size={14} />
                    </button>
                  )}
                </div>

                <div>
                  <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
                    {t("fields.type")}
                  </label>
                  <select
                    value={row.type}
                    onChange={(e) => updateItem(row.key, { type: e.target.value as HrLeaveItemType })}
                    className={cn(inputClass, "h-10")}
                  >
                    {HR_LEAVE_ITEM_TYPES.map((type) => (
                      <option key={type} value={type}>
                        {itemTypeLabel(type, locale)}
                      </option>
                    ))}
                  </select>
                </div>

                {itemTypeNeedsDates(row.type) && (
                  <div className="grid sm:grid-cols-2 gap-3">
                    <div>
                      <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
                        {t("fields.dateFrom")}
                      </label>
                      <input
                        required
                        type="date"
                        value={row.dateFrom ?? ""}
                        onChange={(e) => updateItem(row.key, { dateFrom: e.target.value || null })}
                        className={cn(inputClass, "h-10")}
                      />
                    </div>
                    <div>
                      <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
                        {t("fields.dateTo")}
                      </label>
                      <input
                        required
                        type="date"
                        value={row.dateTo ?? ""}
                        onChange={(e) => updateItem(row.key, { dateTo: e.target.value || null })}
                        className={cn(inputClass, "h-10")}
                      />
                    </div>
                  </div>
                )}

                {itemTypeNeedsDaysCount(row.type) && (
                  <div>
                    <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
                      {t("fields.daysCount")}
                    </label>
                    <input
                      required
                      type="number"
                      min={1}
                      value={row.daysCount ?? ""}
                      onChange={(e) =>
                        updateItem(row.key, { daysCount: e.target.value ? Number(e.target.value) : null })
                      }
                      className={cn(inputClass, "h-10")}
                    />
                  </div>
                )}
              </div>
            ))}
          </section>

          {error && (
            <p className="text-sm text-red-500 bg-red-500/8 border border-red-500/20 rounded-lg px-3 py-2">
              {error}
            </p>
          )}

          <div className="flex flex-wrap gap-3 pt-2 border-t border-border/60">
            <Button type="submit" disabled={submitting} className="bg-violet-600 hover:bg-violet-700">
              {t("submit")}
            </Button>
            <Button
              type="button"
              variant="secondary"
              disabled={submitting}
              onClick={() => save(false)}
            >
              {t("saveDraft")}
            </Button>
            <Button type="button" variant="ghost" onClick={() => router.back()}>
              {t("cancel")}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
