"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useLocale } from "next-intl";
import { useAuthStore } from "@/store/authStore";
import { fetchMe } from "@/lib/auth";
import { isAdminRole } from "@/lib/utils";

export function AuthGuard({ children, adminOnly = false }: { children: React.ReactNode; adminOnly?: boolean }) {
  const router = useRouter();
  const locale = useLocale();
  const { user, accessToken, setAuth } = useAuthStore();
  const [ready, setReady] = useState(false);
  const [hasSession, setHasSession] = useState(false);

  useEffect(() => {
    setHasSession(document.cookie.includes("hasSession"));
  }, []);

  useEffect(() => {
    const check = async () => {
      if (!hasSession && !accessToken) {
        router.replace(`/${locale}/login`);
        return;
      }
      if (!user && hasSession) {
        try {
          const me = await fetchMe();
          setAuth(useAuthStore.getState().accessToken || "", me);
        } catch {
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

  if (!ready) return null;
  return <>{children}</>;
}
