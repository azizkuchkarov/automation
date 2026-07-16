"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import api from "@/lib/api";
import {
  HelpDeskCategory,
  TicketCategory,
  TicketPriority,
  categoryLabel,
  categoryPath,
} from "@/lib/helpdesk";
import { fetchHelpDeskCategories } from "@/lib/helpdeskApi";
import { HelpdeskPageHeader } from "@/components/helpdesk/HelpdeskPageHeader";
import { Button } from "@/components/ui/Button";
import { Monitor, Building2, Truck, Plane, Languages, Calculator } from "lucide-react";
import { cn } from "@/lib/utils";

const ICONS: Record<string, typeof Monitor> = {
  Monitor,
  Building2,
  Calculator,
  Truck,
  Plane,
  Languages,
};

export function CreateTicketForm({ fixedCategory }: { fixedCategory?: TicketCategory }) {
  const t = useTranslations("helpdesk");
  const locale = useLocale();
  const router = useRouter();
  const [categories, setCategories] = useState<HelpDeskCategory[]>([]);
  const [category, setCategory] = useState<TicketCategory>(fixedCategory ?? "IT");
  const [priority, setPriority] = useState<TicketPriority>("Medium");
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (fixedCategory) setCategory(fixedCategory);
  }, [fixedCategory]);

  useEffect(() => {
    fetchHelpDeskCategories().then(setCategories).catch(() => setCategories([]));
  }, []);

  const meta = categories.find((c) => c.category === category);
  const categoryTitle = meta ? categoryLabel(meta, locale) : category;

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setSubmitting(true);
    try {
      const r = await api.post("/helpdesk/tickets", { title, description, category, priority });
      router.push(`/${locale}/helpdesk/tickets/${r.data.id}`);
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error;
      setError(msg ?? t("create.error"));
    } finally {
      setSubmitting(false);
    }
  };

  const inputClass =
    "w-full rounded-lg border border-border/80 bg-background px-3 text-sm text-foreground placeholder:text-foreground/35 focus:outline-none focus:ring-2 focus:ring-atg-teal/30 focus:border-atg-teal/50 transition-shadow";

  return (
    <>
      <HelpdeskPageHeader
        title={fixedCategory ? t("create.categoryTitle", { category: categoryTitle }) : t("create.title")}
        subtitle={fixedCategory ? t("create.categorySubtitle") : t("create.subtitle")}
        breadcrumb={t("nav.create")}
      />

      <div className="flex-1 overflow-auto px-6 py-5">
        <form
          onSubmit={submit}
          className="max-w-2xl space-y-6 rounded-xl border border-border/80 bg-surface p-6 shadow-sm"
        >
          {!fixedCategory && (
            <div>
              <label className="mb-3 block text-[11px] font-semibold uppercase tracking-wider text-foreground/45">
                {t("fields.category")}
              </label>
              <div className="grid grid-cols-1 gap-2.5 sm:grid-cols-2">
                {categories.map((c) => {
                  const Icon = ICONS[c.icon] ?? Monitor;
                  const selected = category === c.category;
                  return (
                    <button
                      key={c.category}
                      type="button"
                      onClick={() => setCategory(c.category)}
                      className={cn(
                        "flex items-center gap-3 rounded-xl border p-3.5 text-left transition-all",
                        selected
                          ? "border-atg-teal/60 bg-atg-teal/8 ring-2 ring-atg-teal/20 shadow-sm"
                          : "border-border/70 bg-background/50 hover:border-border hover:shadow-sm",
                      )}
                    >
                      <div
                        className={cn(
                          "flex h-10 w-10 items-center justify-center rounded-lg transition-colors",
                          selected ? "bg-atg-teal/15 text-atg-teal" : "bg-foreground/[0.04] text-foreground/45",
                        )}
                      >
                        <Icon size={18} />
                      </div>
                      <span className="text-sm font-medium">{categoryLabel(c, locale)}</span>
                    </button>
                  );
                })}
              </div>
            </div>
          )}

          {fixedCategory && meta && (
            <div className="flex items-center gap-3 rounded-xl border border-atg-teal/20 bg-atg-teal/5 px-4 py-3">
              {(() => {
                const Icon = ICONS[meta.icon] ?? Monitor;
                return (
                  <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-atg-teal/15 text-atg-teal">
                    <Icon size={18} />
                  </div>
                );
              })()}
              <div>
                <p className="text-[11px] font-semibold uppercase tracking-wider text-foreground/40">
                  {t("fields.category")}
                </p>
                <p className="text-sm font-medium text-foreground">{categoryTitle}</p>
              </div>
            </div>
          )}

          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <label className="mb-2 block text-[11px] font-semibold uppercase tracking-wider text-foreground/45">
                {t("fields.priority")}
              </label>
              <select
                value={priority}
                onChange={(e) => setPriority(e.target.value as TicketPriority)}
                className={cn(inputClass, "h-10")}
              >
                {(["Low", "Medium", "High", "Critical"] as TicketPriority[]).map((p) => (
                  <option key={p} value={p}>
                    {p}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div>
            <label className="mb-2 block text-[11px] font-semibold uppercase tracking-wider text-foreground/45">
              {t("fields.summary")}
            </label>
            <input
              required
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              maxLength={200}
              placeholder={t("create.summaryPlaceholder")}
              className={cn(inputClass, "h-10")}
            />
          </div>

          <div>
            <label className="mb-2 block text-[11px] font-semibold uppercase tracking-wider text-foreground/45">
              {t("fields.description")}
            </label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              rows={6}
              placeholder={t("create.descPlaceholder")}
              className={cn(inputClass, "min-h-[140px] resize-y py-2.5")}
            />
          </div>

          {error && (
            <p className="rounded-lg border border-red-500/20 bg-red-500/8 px-3 py-2 text-sm text-red-500">
              {error}
            </p>
          )}

          <div className="flex gap-3 border-t border-border/60 pt-2">
            <Button type="submit" disabled={submitting}>
              {t("create.submit")}
            </Button>
            <Button
              type="button"
              variant="secondary"
              onClick={() =>
                router.push(
                  fixedCategory ? categoryPath(locale, fixedCategory, "board") : `/${locale}/helpdesk`,
                )
              }
            >
              {t("create.cancel")}
            </Button>
          </div>
        </form>
      </div>
    </>
  );
}
