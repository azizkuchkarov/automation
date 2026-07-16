"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useLocale } from "next-intl";
import { useAuthStore } from "@/store/authStore";
import { fetchMe } from "@/lib/auth";
import { isAdminRole } from "@/lib/utils";
import { ProfileSetupModal } from "@/components/profile/ProfileSetupModal";

export function AuthGuard({ children, adminOnly = false }: { children: React.ReactNode; adminOnly?: boolean }) {
  const router = useRouter();
  const locale = useLocale();
  const { user, accessToken, setAuth, hydrate } = useAuthStore();
  const [ready, setReady] = useState(false);
  const [hasSession, setHasSession] = useState(false);

  useEffect(() => {
    hydrate();
    setHasSession(document.cookie.includes("hasSession"));
  }, [hydrate]);

  useEffect(() => {
    const check = async () => {
      const token = useAuthStore.getState().accessToken;
      const hasCookie = document.cookie.includes("hasSession");

      if (!hasCookie && !token) {
        router.replace(`/${locale}/login`);
        return;
      }

      if (!useAuthStore.getState().user) {
        try {
          const me = await fetchMe();
          setAuth(useAuthStore.getState().accessToken || "", me);
        } catch {
          document.cookie = "hasSession=; path=/; max-age=0";
          router.replace(`/${locale}/login`);
          return;
        }
      }

      const currentUser = useAuthStore.getState().user;
      if (adminOnly && currentUser && !isAdminRole(currentUser.role)) {
        router.replace(`/${locale}/home`);
        return;
      }
      setReady(true);
    };

    if (hasSession || accessToken) check();
    else router.replace(`/${locale}/login`);
  }, [hasSession, accessToken, user, router, locale, adminOnly, setAuth]);

  if (!ready) {
    return (
      <div className="min-h-screen flex items-center justify-center text-foreground/40 text-sm">
        …
      </div>
    );
  }

  const currentUser = user;
  if (currentUser?.requiresProfileSetup) {
    return (
      <ProfileSetupModal
        user={currentUser}
        onCompleted={(updated) => {
          setAuth(useAuthStore.getState().accessToken || "", updated);
        }}
      />
    );
  }

  return <>{children}</>;
}
