"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import api from "@/lib/api";
import { HelpDeskCategory, TicketCategory, TicketPriority, categoryLabel } from "@/lib/helpdesk";
import { HelpdeskPageHeader } from "@/components/helpdesk/HelpdeskPageHeader";
import { Button } from "@/components/ui/Button";
import { Monitor, Building2, Truck, Plane, Languages, Calculator } from "lucide-react";
import { cn } from "@/lib/utils";

const ICONS: Record<string, typeof Monitor> = {
  Monitor, Building2, Calculator, Truck, Plane, Languages,
};

export default function CreateTicketPage() {
  const t = useTranslations("helpdesk");
  const locale = useLocale();
  const router = useRouter();
  const [categories, setCategories] = useState<HelpDeskCategory[]>([]);
  const [category, setCategory] = useState<TicketCategory>("IT");
  const [priority, setPriority] = useState<TicketPriority>("Medium");
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    api.get("/helpdesk/categories").then((r) => setCategories(r.data));
  }, []);

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
        title={t("create.title")}
        subtitle={t("create.subtitle")}
        breadcrumb={t("nav.create")}
      />

      <div className="flex-1 overflow-auto px-6 py-5">
        <form
          onSubmit={submit}
          className="max-w-2xl rounded-xl border border-border/80 bg-surface shadow-sm p-6 space-y-6"
        >
          <div>
            <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-3 block">
              {t("fields.category")}
            </label>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-2.5">
              {categories.map((c) => {
                const Icon = ICONS[c.icon] ?? Monitor;
                const selected = category === c.category;
                return (
                  <button
                    key={c.category}
                    type="button"
                    onClick={() => setCategory(c.category)}
                    className={cn(
                      "flex items-center gap-3 p-3.5 rounded-xl border text-left transition-all",
                      selected
                        ? "border-atg-teal/60 bg-atg-teal/8 ring-2 ring-atg-teal/20 shadow-sm"
                        : "border-border/70 hover:border-border bg-background/50 hover:shadow-sm"
                    )}
                  >
                    <div
                      className={cn(
                        "w-10 h-10 rounded-lg flex items-center justify-center transition-colors",
                        selected ? "bg-atg-teal/15 text-atg-teal" : "bg-foreground/[0.04] text-foreground/45"
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

          <div className="grid sm:grid-cols-2 gap-4">
            <div>
              <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
                {t("fields.priority")}
              </label>
              <select
                value={priority}
                onChange={(e) => setPriority(e.target.value as TicketPriority)}
                className={cn(inputClass, "h-10")}
              >
                {(["Low", "Medium", "High", "Critical"] as TicketPriority[]).map((p) => (
                  <option key={p} value={p}>{p}</option>
                ))}
              </select>
            </div>
          </div>

          <div>
            <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
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
            <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
              {t("fields.description")}
            </label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              rows={6}
              placeholder={t("create.descPlaceholder")}
              className={cn(inputClass, "py-2.5 resize-y min-h-[140px]")}
            />
          </div>

          {error && (
            <p className="text-sm text-red-500 dark:text-red-400 bg-red-500/8 border border-red-500/20 rounded-lg px-3 py-2">
              {error}
            </p>
          )}

          <div className="flex gap-3 pt-2 border-t border-border/60">
            <Button type="submit" disabled={submitting}>{t("create.submit")}</Button>
            <Button type="button" variant="secondary" onClick={() => router.back()}>
              {t("create.cancel")}
            </Button>
          </div>
        </form>
      </div>
    </>
  );
}
