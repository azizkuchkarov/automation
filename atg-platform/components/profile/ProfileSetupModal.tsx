"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Loader2, UserCircle2 } from "lucide-react";
import api, { getApiErrorMessage } from "@/lib/api";
import { HrPrimaryButton } from "@/components/hr/HrChrome";
import { hrInputClass } from "@/components/hr/hrTheme";
import type { AuthUser } from "@/store/authStore";

interface Props {
  user: AuthUser;
  onCompleted: (user: AuthUser) => void;
}

export function ProfileSetupModal({ user, onCompleted }: Props) {
  const t = useTranslations("profile.setup");
  const [pinpp, setPinpp] = useState(user.pinpp ?? "");
  const [passportSeries, setPassportSeries] = useState(user.passportSeries ?? "");
  const [passportNumber, setPassportNumber] = useState(user.passportNumber ?? "");
  const [phone, setPhone] = useState(user.phone ?? "");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  const inputClass = hrInputClass();

  const save = async () => {
    setError("");
    if (pinpp.trim().length !== 14) {
      setError(t("pinppInvalid"));
      return;
    }
    if (passportSeries.trim().length !== 2) {
      setError(t("passportSeriesInvalid"));
      return;
    }
    if (passportNumber.trim().length < 7) {
      setError(t("passportNumberInvalid"));
      return;
    }

    setSaving(true);
    try {
      const { data } = await api.patch<AuthUser>("/auth/me/profile", {
        pinpp: pinpp.trim(),
        passportSeries: passportSeries.trim().toUpperCase(),
        passportNumber: passportNumber.trim(),
        phone: phone.trim() || null,
      });
      onCompleted({
        ...data,
        requiresProfileSetup: false,
      });
    } catch (err: unknown) {
      setError(getApiErrorMessage(err) ?? t("saveError"));
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center bg-background/95 p-4">
      <div className="w-full max-w-lg rounded-2xl border border-border bg-surface shadow-2xl">
        <div className="flex items-center gap-3 border-b border-border px-6 py-5">
          <div className="flex h-10 w-10 items-center justify-center rounded-full bg-blue-100 text-blue-700">
            <UserCircle2 size={22} />
          </div>
          <div>
            <h2 className="text-lg font-semibold text-foreground">{t("title")}</h2>
            <p className="text-sm text-foreground/55">{t("subtitle")}</p>
          </div>
        </div>

        <div className="space-y-4 px-6 py-5">
          <div>
            <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
              {t("pinpp")}
            </label>
            <input
              type="text"
              inputMode="numeric"
              maxLength={14}
              value={pinpp}
              onChange={(e) => setPinpp(e.target.value.replace(/\D/g, ""))}
              placeholder={t("pinppPlaceholder")}
              className={inputClass}
              disabled={saving}
            />
            <p className="mt-1 text-xs text-foreground/45">{t("pinppHint")}</p>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
                {t("passportSeries")}
              </label>
              <input
                type="text"
                maxLength={2}
                value={passportSeries}
                onChange={(e) => setPassportSeries(e.target.value.replace(/[^a-zA-Z]/g, "").toUpperCase())}
                placeholder="AB"
                className={inputClass}
                disabled={saving}
              />
            </div>
            <div>
              <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
                {t("passportNumber")}
              </label>
              <input
                type="text"
                inputMode="numeric"
                maxLength={9}
                value={passportNumber}
                onChange={(e) => setPassportNumber(e.target.value.replace(/\D/g, ""))}
                placeholder="1234567"
                className={inputClass}
                disabled={saving}
              />
            </div>
          </div>
          <p className="text-xs text-foreground/45 -mt-1">{t("passportHint")}</p>

          <div>
            <label className="text-[11px] font-semibold uppercase tracking-wider text-foreground/45 mb-2 block">
              {t("phone")}
            </label>
            <input
              type="tel"
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              placeholder={t("phonePlaceholder")}
              className={inputClass}
              disabled={saving}
            />
          </div>

          {error && (
            <p className="text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-3 py-2">{error}</p>
          )}
        </div>

        <div className="border-t border-border px-6 py-4">
          <HrPrimaryButton className="w-full justify-center" disabled={saving} onClick={save}>
            {saving ? <Loader2 size={14} className="animate-spin mr-1.5" /> : null}
            {t("save")}
          </HrPrimaryButton>
        </div>
      </div>
    </div>
  );
}
