"use client";

import { useQuery, useQueryClient } from "@tanstack/react-query";
import api from "@/lib/api";
import type { MarketingRecord } from "@/lib/marketing";

export function marketingRecordKey(documentId: string) {
  return ["marketing-record", documentId] as const;
}

export function useMarketingRecord(documentId: string, enabled = true) {
  return useQuery({
    queryKey: marketingRecordKey(documentId),
    enabled: Boolean(documentId) && enabled,
    queryFn: async () => {
      const r = await api.get<MarketingRecord>(`/marketing/records/by-document/${documentId}`);
      return r.data;
    },
  });
}

export function useInvalidateMarketingRecord(documentId: string) {
  const qc = useQueryClient();
  return () => qc.invalidateQueries({ queryKey: marketingRecordKey(documentId) });
}
