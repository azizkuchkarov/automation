"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { Loader2, Plus, Trash2, UserRound } from "lucide-react";
import api, { getApiErrorMessage } from "@/lib/api";
import { fetchMe } from "@/lib/auth";
import {
  BUSINESS_TRIP_PLACE_OPTIONS,
  BUSINESS_TRIP_PLACE_OTHER,
  computeDaysInclusive,
  CreateHrBusinessTripTravelerPayload,
  HrBusinessTripColleague,
  travelerPayloadFromAuthUser,
  travelerPayloadFromColleague,
} from "@/lib/hrBusinessTrip";
import { Button } from "@/components/ui/Button";
import { HrLoadingState, HrPageHeader, HrPageShell, HrPrimaryButton } from "@/components/hr/HrChrome";
import { hrInputClass, hrTheme } from "@/components/hr/hrTheme";
import { cn } from "@/lib/utils";
import type { AuthUser } from "@/store/authStore";

type TravelerRow = CreateHrBusinessTripTravelerPayload & { key: string; userId?: string };

function travelerRow(userId: string | undefined, payload: CreateHrBusinessTripTravelerPayload): TravelerRow {
  return { key: crypto.randomUUID(), userId, ...payload };
}

export default function NewHrBusinessTripPage() {
  const t = useTranslations("hr.businessTrip");
  const locale = useLocale();
  const router = useRouter();
  const [requestDate, setRequestDate] = useState(new Date().toISOString().slice(0, 10));
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const [purposeRu, setPurposeRu] = useState("");
  const [purposeEn, setPurposeEn] = useState("");
  const [placePreset, setPlacePreset] = useState("");
  const [placeRuCustom, setPlaceRuCustom] = useState("");
  const [placeEnCustom, setPlaceEnCustom] = useState("");
  const [travelers, setTravelers] = useState<TravelerRow[]>([]);
  const [colleagues, setColleagues] = useState<HrBusinessTripColleague[]>([]);
  const [currentUser, setCurrentUser] = useState<AuthUser | null>(null);
  const [loadingProfile, setLoadingProfile] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");
  const [selectedColleagueId, setSelectedColleagueId] = useState("");

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const [user, colleaguesRes] = await Promise.all([
          fetchMe(),
          api.get<HrBusinessTripColleague[]>("/hr/business-trips/colleagues"),
        ]);
        if (cancelled) return;
        setCurrentUser(user);
        setColleagues(colleaguesRes.data);
        setTravelers([travelerRow(user.id, travelerPayloadFromAuthUser(user))]);
      } catch {
        if (!cancelled) setError(t("profileLoadError"));
      } finally {
        if (!cancelled) setLoadingProfile(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [t]);

  const availableColleagues = useMemo(() => {
    const usedIds = new Set(travelers.map((row) => row.userId).filter(Boolean));
    return colleagues.filter((c) => !usedIds.has(c.id));
  }, [colleagues, travelers]);

  const daysCount = computeDaysInclusive(dateFrom, dateTo);
  const isPlaceOther = placePreset === BUSINESS_TRIP_PLACE_OTHER;
  const selectedPlace = BUSINESS_TRIP_PLACE_OPTIONS.find((p) => p.ru === placePreset);
  const placeRu = isPlaceOther ? placeRuCustom : (selectedPlace?.ru ?? "");
  const placeEn = isPlaceOther ? placeEnCustom : (selectedPlace?.en ?? "");
  const inputClass = hrInputClass("h-10");
  const readOnlyClass = "bg-slate-50 text-slate-600 cursor-default";

  const updateTraveler = (key: string, patch: Partial<TravelerRow>) => {
    setTravelers((prev) => prev.map((row) => (row.key === key ? { ...row, ...patch } : row)));
  };

  const addColleague = () => {
    const colleague = colleagues.find((c) => c.id === selectedColleagueId);
    if (!colleague) return;
    setTravelers((prev) => [...prev, travelerRow(colleague.id, travelerPayloadFromColleague(colleague))]);
    setSelectedColleagueId("");
  };

  const buildPayload = () => ({
    requestDate,
    purposeRu: purposeRu.trim(),
    purposeEn: purposeEn.trim() || null,
    dateFrom,
    dateTo,
    placeRu: placeRu.trim(),
    placeEn: placeEn.trim() || null,
    travelers: travelers.map(({ key: _key, userId, ...row }) => ({
      fullNameRu: row.fullNameRu.trim(),
      fullNameEn: row.fullNameEn?.trim() || null,
      positionRu: row.positionRu.trim(),
      positionEn: row.positionEn?.trim() || null,
      userId: userId ?? null,
    })),
  });

  const save = async (submit: boolean) => {
    setError("");
    setSubmitting(true);
    try {
      const res = await api.post("/hr/business-trips", buildPayload());
      if (submit) {
        await api.post(`/hr/business-trips/${res.data.id}/submit`);
      }
      router.push(`/${locale}/hr/business-trip/${res.data.id}`);
    } catch (err: unknown) {
      setError(getApiErrorMessage(err) ?? t("createError"));
    } finally {
      setSubmitting(false);
    }
  };

  if (loadingProfile) {
    return (
      <HrPageShell>
        <HrLoadingState label={t("loading")} />
      </HrPageShell>
    );
  }

  return (
    <HrPageShell>
      <HrPageHeader title={t("newTitle")} subtitle={t("newSubtitle")} />

      <div className="flex-1 overflow-y-auto px-6 py-6">
        <form
          className={cn("max-w-3xl space-y-6 p-5 md:p-6", hrTheme.card)}
          onSubmit={(e) => {
            e.preventDefault();
            save(true);
          }}
        >
          <div className="grid sm:grid-cols-2 gap-4">
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
            <div />
            <div>
              <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
                {t("fields.dateFrom")}
              </label>
              <input
                required
                type="date"
                value={dateFrom}
                onChange={(e) => setDateFrom(e.target.value)}
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
                value={dateTo}
                onChange={(e) => setDateTo(e.target.value)}
                className={cn(inputClass, "h-10")}
              />
            </div>
            {daysCount > 0 && (
              <div className="sm:col-span-2 text-sm text-foreground/50">
                {t("fields.daysPreview", { count: daysCount })}
              </div>
            )}
          </div>

          <div className="grid sm:grid-cols-2 gap-4">
            <div>
              <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
                {t("fields.purposeRu")}
              </label>
              <textarea
                required
                rows={3}
                value={purposeRu}
                onChange={(e) => setPurposeRu(e.target.value)}
                className={inputClass}
              />
            </div>
            <div>
              <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
                {t("fields.purposeEn")}
              </label>
              <textarea
                rows={3}
                value={purposeEn}
                onChange={(e) => setPurposeEn(e.target.value)}
                className={inputClass}
              />
            </div>
          </div>

          <div className="space-y-4">
            <div>
              <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
                {t("fields.place")}
              </label>
              <select
                required
                value={placePreset}
                onChange={(e) => setPlacePreset(e.target.value)}
                className={cn(inputClass, "h-10")}
              >
                <option value="">{t("fields.placeSelect")}</option>
                {BUSINESS_TRIP_PLACE_OPTIONS.map((option) => (
                  <option key={option.ru} value={option.ru}>
                    {locale.startsWith("en") ? option.en : option.ru}
                  </option>
                ))}
                <option value={BUSINESS_TRIP_PLACE_OTHER}>{t("fields.placeOther")}</option>
              </select>
            </div>

            {isPlaceOther && (
              <div className="grid sm:grid-cols-2 gap-4">
                <div>
                  <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
                    {t("fields.placeOtherRu")}
                  </label>
                  <input
                    required
                    value={placeRuCustom}
                    onChange={(e) => setPlaceRuCustom(e.target.value)}
                    className={cn(inputClass, "h-10")}
                  />
                </div>
                <div>
                  <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
                    {t("fields.placeOtherEn")}
                  </label>
                  <input
                    value={placeEnCustom}
                    onChange={(e) => setPlaceEnCustom(e.target.value)}
                    className={cn(inputClass, "h-10")}
                  />
                </div>
              </div>
            )}
          </div>

          <section className="space-y-4">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div>
                <h2 className="text-sm font-semibold text-foreground">{t("travelersTitle")}</h2>
                {currentUser?.departmentName && (
                  <p className="text-xs text-foreground/45 mt-1">{t("travelersDepartmentHint")}</p>
                )}
              </div>
              <div className="flex flex-wrap items-center gap-2">
                <select
                  value={selectedColleagueId}
                  onChange={(e) => setSelectedColleagueId(e.target.value)}
                  className={cn(inputClass, "h-9 min-w-[220px]")}
                  disabled={availableColleagues.length === 0}
                >
                  <option value="">
                    {availableColleagues.length === 0 ? t("noColleaguesAvailable") : t("selectColleague")}
                  </option>
                  {availableColleagues.map((colleague) => (
                    <option key={colleague.id} value={colleague.id}>
                      {colleague.fullNameRu}
                      {colleague.positionRu ? ` — ${colleague.positionRu}` : ""}
                    </option>
                  ))}
                </select>
                <Button
                  type="button"
                  variant="secondary"
                  size="sm"
                  disabled={!selectedColleagueId}
                  onClick={addColleague}
                >
                  <Plus size={14} className="mr-1" />
                  {t("addTraveler")}
                </Button>
              </div>
            </div>

            {travelers.map((row, index) => {
              const isSelf = row.userId === currentUser?.id;
              const isFromDirectory = Boolean(row.userId);
              return (
                <div key={row.key} className="rounded-xl border border-border/80 bg-surface p-4 space-y-3 shadow-sm">
                  <div className="flex items-center justify-between gap-2">
                    <div className="flex items-center gap-2">
                      <span className="text-xs font-semibold uppercase tracking-wider text-foreground/40">
                        {t("travelerNumber", { n: index + 1 })}
                      </span>
                      {isSelf && (
                        <span className="inline-flex items-center gap-1 rounded-full bg-blue-500/10 px-2 py-0.5 text-[10px] font-semibold text-blue-700">
                          <UserRound size={11} />
                          {t("you")}
                        </span>
                      )}
                    </div>
                    {!isSelf && (
                      <button
                        type="button"
                        onClick={() => setTravelers((prev) => prev.filter((i) => i.key !== row.key))}
                        className="text-foreground/40 hover:text-red-500 p-1"
                      >
                        <Trash2 size={14} />
                      </button>
                    )}
                  </div>
                  <div className="grid sm:grid-cols-2 gap-3">
                    <div>
                      <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
                        {t("fields.fullNameRu")}
                      </label>
                      <input
                        required
                        readOnly={isFromDirectory}
                        value={row.fullNameRu}
                        onChange={(e) => updateTraveler(row.key, { fullNameRu: e.target.value })}
                        className={cn(inputClass, "h-10", isFromDirectory && readOnlyClass)}
                      />
                    </div>
                    <div>
                      <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
                        {t("fields.fullNameEn")}
                      </label>
                      <input
                        readOnly={isFromDirectory}
                        value={row.fullNameEn ?? ""}
                        onChange={(e) => updateTraveler(row.key, { fullNameEn: e.target.value || null })}
                        className={cn(inputClass, "h-10", isFromDirectory && readOnlyClass)}
                      />
                    </div>
                    <div>
                      <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
                        {t("fields.positionRu")}
                      </label>
                      <input
                        required
                        readOnly={isFromDirectory}
                        value={row.positionRu}
                        onChange={(e) => updateTraveler(row.key, { positionRu: e.target.value })}
                        className={cn(inputClass, "h-10", isFromDirectory && readOnlyClass)}
                      />
                    </div>
                    <div>
                      <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
                        {t("fields.positionEn")}
                      </label>
                      <input
                        readOnly={isFromDirectory}
                        value={row.positionEn ?? ""}
                        onChange={(e) => updateTraveler(row.key, { positionEn: e.target.value || null })}
                        className={cn(inputClass, "h-10", isFromDirectory && readOnlyClass)}
                      />
                    </div>
                  </div>
                </div>
              );
            })}
          </section>

          {error && (
            <p className="text-sm text-red-500 bg-red-500/8 border border-red-500/20 rounded-lg px-3 py-2">
              {error}
            </p>
          )}

          <div className="flex flex-wrap gap-3 pt-2 border-t border-slate-200/70">
            <HrPrimaryButton type="submit" disabled={submitting || travelers.length === 0}>
              {t("submit")}
            </HrPrimaryButton>
            <Button type="button" variant="secondary" className="rounded-xl" disabled={submitting} onClick={() => save(false)}>
              {t("saveDraft")}
            </Button>
            <Button type="button" variant="ghost" className="rounded-xl" onClick={() => router.back()}>
              {t("cancel")}
            </Button>
          </div>
        </form>
      </div>
    </HrPageShell>
  );
}
