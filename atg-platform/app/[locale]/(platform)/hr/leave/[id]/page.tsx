"use client";

import { useCallback, useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { Loader2 } from "lucide-react";
import { useTranslations } from "next-intl";
import api from "@/lib/api";
import { HrLeaveRequest } from "@/lib/hrLeave";
import { HrLeaveRequestView } from "@/components/hr/HrLeaveRequestView";

export default function HrLeaveDetailPage() {
  const { id } = useParams<{ id: string }>();
  const t = useTranslations("hr.leave");
  const [request, setRequest] = useState<HrLeaveRequest | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);

  const reload = useCallback(() => {
    setLoading(true);
    api
      .get(`/hr/leave-requests/${id}`)
      .then((res) => setRequest(res.data))
      .catch(() => setNotFound(true))
      .finally(() => setLoading(false));
  }, [id]);

  useEffect(() => {
    reload();
  }, [reload]);

  if (loading) {
    return (
      <div className="flex-1 flex items-center justify-center gap-2 text-foreground/40">
        <Loader2 className="animate-spin" size={20} />
        <span className="text-sm">{t("loading")}</span>
      </div>
    );
  }

  if (notFound || !request) {
    return (
      <div className="flex-1 flex items-center justify-center text-red-500 text-sm">{t("notFound")}</div>
    );
  }

  return <HrLeaveRequestView request={request} onUpdated={reload} />;
}
