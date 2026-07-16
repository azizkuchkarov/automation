import api from "@/lib/api";

export type ItAssetCategory =
  | "License"
  | "Service"
  | "MobileService"
  | "GovernmentService"
  | "Equipment";

export type ItAssetStatus = "Active" | "InProcess" | "Done" | "Expired" | "Suspended" | "Cancelled";

export interface ItAsset {
  id: string;
  category: ItAssetCategory | string;
  nameRu: string;
  nameEn: string;
  quantity?: string | null;
  term?: string | null;
  budgetCode?: string | null;
  budgetAmount?: number | null;
  currency?: string | null;
  responsibleUserId?: string | null;
  responsibleUserName?: string | null;
  startsAt?: string | null;
  expiresAt?: string | null;
  contractNumber?: string | null;
  contractDate?: string | null;
  cost?: number | null;
  status: ItAssetStatus | string;
  note?: string | null;
  planYear: number;
  daysUntilExpiry?: number | null;
  expiryWarning: boolean;
}

export interface ItAssetCategorySummary {
  category: ItAssetCategory | string;
  total: number;
  expiringSoon: number;
  expired: number;
  responsibleUserName?: string | null;
}

export interface ItAutomationHub {
  categories: ItAssetCategorySummary[];
  expiringSoonTotal: number;
}

export interface ItAutomationRole {
  category: string;
  titleRu: string;
  titleEn: string;
  descriptionRu: string;
  descriptionEn: string;
  responsibleUserId?: string | null;
  responsibleUserName?: string | null;
  responsibleUserEmail?: string | null;
}

export interface ItAutomationRolesAdmin {
  roles: ItAutomationRole[];
  candidates: { id: string; fullName: string; email: string; departmentName?: string }[];
}

export const CATEGORY_SLUGS: Record<ItAssetCategory, string> = {
  License: "licenses",
  Service: "services",
  MobileService: "mobile-services",
  GovernmentService: "government-services",
  Equipment: "equipment",
};

export const SLUG_TO_CATEGORY: Record<string, ItAssetCategory> = {
  licenses: "License",
  services: "Service",
  "mobile-services": "MobileService",
  "government-services": "GovernmentService",
  equipment: "Equipment",
};

export async function fetchItAutomationHub() {
  const { data } = await api.get<ItAutomationHub>("/it-automation/hub");
  return data;
}

export async function fetchItAssets(category?: string, planYear?: number) {
  const { data } = await api.get<ItAsset[]>("/it-automation/assets", {
    params: { category, planYear },
  });
  return data;
}

export async function createItAsset(body: Record<string, unknown>) {
  const { data } = await api.post<ItAsset>("/it-automation/assets", body);
  return data;
}

export async function updateItAsset(id: string, body: Record<string, unknown>) {
  const { data } = await api.put<ItAsset>(`/it-automation/assets/${id}`, body);
  return data;
}

export async function deleteItAsset(id: string) {
  await api.delete(`/it-automation/assets/${id}`);
}

export async function fetchItAutomationRoles() {
  const { data } = await api.get<ItAutomationRolesAdmin>("/it-automation/admin/roles");
  return data;
}

export async function updateItAutomationRole(category: string, responsibleUserId: string | null) {
  const { data } = await api.put<ItAutomationRole>(`/it-automation/admin/roles/${category}`, {
    responsibleUserId,
  });
  return data;
}

export function formatMoney(value?: number | null, currency?: string | null) {
  if (value == null) return "—";
  const formatted = new Intl.NumberFormat("ru-RU", { maximumFractionDigits: 2 }).format(value);
  return currency ? `${formatted} ${currency}` : formatted;
}

export function formatDate(value?: string | null, locale = "ru") {
  if (!value) return "—";
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return "—";
  return d.toLocaleDateString(locale.startsWith("en") ? "en-GB" : "ru-RU");
}
