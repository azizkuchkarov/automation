"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useLocale } from "next-intl";

/** Queue view redirects to tickets list with queue filter */
export default function QueuePage() {
  const router = useRouter();
  const locale = useLocale();
  useEffect(() => {
    router.replace(`/${locale}/helpdesk/tickets?view=queue`);
  }, [router, locale]);
  return null;
}
