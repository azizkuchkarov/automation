"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { FormEvent, useState } from "react";
import { FilePlus2 } from "lucide-react";
import api from "@/lib/api";
import { slugToType, typeToSlug, ALL_TYPE_SLUGS } from "@/lib/dcs";
import { DcsPageHeader } from "@/components/dcs/DcsPageHeader";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/utils";

const inputClass =
  "w-full rounded-xl border border-border/80 bg-background px-4 text-sm shadow-sm transition-shadow focus:outline-none focus:ring-2 focus:ring-atg-blue/25 focus:border-atg-blue/40";

export default function NewDocumentPage() {
  const t = useTranslations("dcs");
  const locale = useLocale();
  const router = useRouter();
  const searchParams = useSearchParams();
  const initialSlug = searchParams.get("type") ?? "technical-assignments";
  const [typeSlug, setTypeSlug] = useState(initialSlug);
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [externalRef, setExternalRef] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault();
    const docType = slugToType(typeSlug);
    if (!docType) return;

    setSaving(true);
    setError("");
    try {
      const r = await api.post("/dcs/documents", {
        title,
        description,
        type: docType,
        externalReference: externalRef || null,
      });
      router.push(`/${locale}/automation/documents/${r.data.id}`);
    } catch {
      setError(t("form.error"));
    } finally {
      setSaving(false);
    }
  };

  return (
    <>
      <DcsPageHeader
        title={t("form.newTitle")}
        subtitle={t("form.newSubtitle")}
        breadcrumb={t("nav.new")}
        icon={FilePlus2}
        iconClassName="bg-emerald-500/10 text-emerald-600 dark:text-emerald-400"
      />

      <div className="flex-1 overflow-auto px-6 py-6">
        <div className="max-w-2xl mx-auto">
          <form
            onSubmit={onSubmit}
            className="rounded-2xl border border-border/70 bg-surface shadow-sm overflow-hidden"
          >
            <div className="px-6 py-5 border-b border-border/50 bg-foreground/[0.015]">
              <h2 className="font-semibold text-sm">{t("form.sectionMain")}</h2>
              <p className="text-xs text-foreground/45 mt-0.5">{t("form.sectionMainHint")}</p>
            </div>

            <div className="p-6 space-y-5">
              <Field label={t("form.type")}>
                <select
                  value={typeSlug}
                  onChange={(e) => setTypeSlug(e.target.value)}
                  className={cn(inputClass, "h-11")}
                >
                  {ALL_TYPE_SLUGS.map(({ slug }) => (
                    <option key={slug} value={slug}>
                      {t(`types.${slug}`)}
                    </option>
                  ))}
                </select>
              </Field>

              <Field label={t("fields.title")} required>
                <input
                  required
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  placeholder={t("form.titlePlaceholder")}
                  className={cn(inputClass, "h-11")}
                />
              </Field>

              <Field label={t("form.description")}>
                <textarea
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  rows={5}
                  placeholder={t("form.descPlaceholder")}
                  className={cn(inputClass, "py-3 resize-none")}
                />
              </Field>

              <Field label={t("form.externalRef")}>
                <input
                  value={externalRef}
                  onChange={(e) => setExternalRef(e.target.value)}
                  placeholder={t("form.externalRefPlaceholder")}
                  className={cn(inputClass, "h-11")}
                />
              </Field>

              {error && (
                <p className="text-sm text-red-500 bg-red-500/8 px-4 py-3 rounded-xl border border-red-500/20">
                  {error}
                </p>
              )}
            </div>

            <div className="px-6 py-4 border-t border-border/50 bg-foreground/[0.015] flex gap-2 justify-end">
              <Button
                type="button"
                variant="ghost"
                onClick={() => {
                  const slug = typeToSlug(slugToType(typeSlug)!);
                  const section = ["incoming", "outgoing", "memo", "minutes", "orders"].includes(slug!)
                    ? "office"
                    : "procurement";
                  router.push(`/${locale}/automation/${section}/${slug}`);
                }}
              >
                {t("form.cancel")}
              </Button>
              <Button type="submit" disabled={saving} className="min-w-[140px] shadow-sm font-semibold">
                {saving ? t("loading") : t("form.submit")}
              </Button>
            </div>
          </form>
        </div>
      </div>
    </>
  );
}

function Field({
  label,
  required,
  children,
}: {
  label: string;
  required?: boolean;
  children: React.ReactNode;
}) {
  return (
    <div>
      <label className="block text-xs font-bold text-foreground/50 uppercase tracking-wider mb-2">
        {label}
        {required && <span className="text-red-400 ml-0.5">*</span>}
      </label>
      {children}
    </div>
  );
}
