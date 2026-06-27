"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { Input } from "@/components/ui/Input";
import { Button } from "@/components/ui/Button";
import { LanguageToggle } from "@/components/ui/LanguageToggle";
import { login } from "@/lib/auth";
import { Eye, EyeOff } from "lucide-react";

export default function LoginPage() {
  const t = useTranslations("auth");
  const locale = useLocale();
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);
    try {
      await login(email, password);
      router.push(`/${locale}/home`);
    } catch (err: unknown) {
      const message = (err as { response?: { data?: { error?: string } } })?.response?.data?.error;
      if (message === "User is not registered in the platform") {
        setError(t("userNotRegistered"));
      } else {
        setError(t("invalidCredentials"));
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center p-4 relative">
      <div className="absolute bottom-4 right-4">
        <LanguageToggle />
      </div>
      <div className="w-full max-w-[420px] rounded-xl border border-border bg-surface p-8 shadow-xl">
        <div className="text-center mb-8">
          <div className="w-14 h-14 rounded-xl bg-atg-blue text-white font-bold text-xl flex items-center justify-center mx-auto mb-4">ATG</div>
          <h1 className="text-xl font-semibold">{t("title")}</h1>
          <p className="text-sm text-foreground/60 mt-1">{t("subtitle")}</p>
        </div>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="text-sm mb-1 block">{t("email")}</label>
            <Input type="text" value={email} onChange={(e) => setEmail(e.target.value)} required autoComplete="username" />
          </div>
          <div>
            <label className="text-sm mb-1 block">{t("password")}</label>
            <div className="relative">
              <Input
                type={showPassword ? "text" : "password"}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                autoComplete="current-password"
              />
              <button type="button" onClick={() => setShowPassword(!showPassword)} className="absolute right-2 top-1/2 -translate-y-1/2 p-1 text-foreground/50">
                {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
              </button>
            </div>
          </div>
          {error && <p className="text-sm text-red-400">{error}</p>}
          <Button type="submit" className="w-full" disabled={loading}>
            {loading ? "..." : t("signIn")}
          </Button>
          <p className="text-xs text-center text-foreground/50">{t("ldapHint")}</p>
        </form>
      </div>
    </div>
  );
}
