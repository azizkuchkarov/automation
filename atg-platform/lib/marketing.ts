export type MarketingRequestCategory = 1 | 2 | 3 | 4;

export type ProcurementMethodType =
  | "DirectContract"
  | "LocalEshop"
  | "LocalAuction"
  | "LocalCooperation"
  | "LocalExchange"
  | "SmallValue"
  | "Rfp"
  | "BestOffer"
  | "Tender";

export type MarketingRecordStatus =
  | "WaitingExecutor"
  | "WaitingAccept"
  | "StudyingDocuments"
  | "TzIssue"
  | "RfqPreparation"
  | "RfqSent"
  | "KpAnalysis"
  | "KpNegotiation"
  | "PlanPreparation"
  | "PlanManagementReview"
  | "PlanPortalApproval"
  | "PlanMonitoring"
  | "CompletedToContract"
  | "Cancelled";

export type RfqDispatchType = "AtgSite" | "Tenderweek" | "Vendor" | "Distributor" | "OpenSource";
export type MarketingOfferSource = "Manual" | "AtgSite" | "Tenderweek" | "Vendor" | "Distributor" | "OpenSource";

export interface MarketingRecordListItem {
  id: string;
  documentId: string;
  portalNumber?: string;
  requestTitle?: string;
  status: MarketingRecordStatus;
  requestCategory?: MarketingRequestCategory;
  deadlineDate?: string;
  remainingWorkingDays?: number;
  deadlineColor?: string;
  marketingExecutorName?: string;
  initiatorDepartment?: string;
  budgetAmount?: number;
  budgetCurrency: string;
  offerCount: number;
  updatedAt: string;
}

export interface MarketingOffer {
  id: string;
  companyName: string;
  offerAmount?: number;
  currency: string;
  vatIncluded: boolean;
  deliveryIncluded: boolean;
  warrantyTerms?: string;
  offerDate?: string;
  offerValidityDate?: string;
  contactInfo?: string;
  meetsTzRequirements?: boolean;
  rejectionReason?: string;
  isAffiliated: boolean;
  affiliationNote?: string;
  source: MarketingOfferSource;
  attachmentKey?: string;
  createdAt: string;
}

export interface RfqDispatch {
  id: string;
  dispatchType: RfqDispatchType;
  recipientName?: string;
  recipientEmail?: string;
  recipientPhone?: string;
  sentAt: string;
  responseReceivedAt?: string;
  followupSentAt?: string;
  followupPhoneCalled: boolean;
  notes?: string;
}

export interface MarketingRecord extends MarketingRecordListItem {
  registeredDate?: string;
  initiatorFullName?: string;
  receivedDate?: string;
  deadlineBaseDate?: string;
  deadlineWorkingDays?: number;
  marketingExecutorId?: string;
  marketingCurrentStep: number;
  procurementMethod?: ProcurementMethodType;
  budgetAmount?: number;
  rfqPreparedAt?: string;
  rfqPublishedAtgSite: boolean;
  rfqPublishedTenderweek: boolean;
  rfqSentToVendor: boolean;
  rfqSentToDistributor: boolean;
  rfqOpenSearchDone: boolean;
  offers: MarketingOffer[];
  rfqDispatches: RfqDispatch[];
  offersSummary?: {
    compliantCount: number;
    averageCompliantAmount?: number;
    affiliatedCount: number;
  };
}

export interface MarketingBoardColumn {
  status: MarketingRecordStatus;
  labelRu: string;
  labelEn: string;
  items: MarketingRecordListItem[];
}

export interface MarketingStats {
  total: number;
  inProgress: number;
  overdue: number;
  completed: number;
  byCategory: { category: MarketingRequestCategory; count: number }[];
  byExecutor: { executorName: string; count: number; overdue: number }[];
  byMethod: { method: ProcurementMethodType; count: number }[];
}

export interface MarketingLeadershipItem {
  documentId: string;
  portalNumber?: string;
  requestTitle?: string;
  status: MarketingRecordStatus;
  marketingCurrentStep: number;
  remainingWorkingDays?: number;
  deadlineColor?: string;
  isOverdue: boolean;
}

export interface MarketingLeadershipRow {
  initiatorDepartment: string;
  initiatorFullName: string;
  items: MarketingLeadershipItem[];
}

export const CATEGORY_DEADLINE_DAYS: Record<MarketingRequestCategory, number> = {
  1: 10,
  2: 15,
  3: 20,
  4: 25,
};

export function categoryLabel(cat: MarketingRequestCategory, locale: string) {
  const ru: Record<MarketingRequestCategory, string> = {
    1: "1-категория (10 р.д.)",
    2: "2-категория (15 р.д.)",
    3: "3-категория (20 р.д.)",
    4: "4-категория (25 р.д.)",
  };
  const en: Record<MarketingRequestCategory, string> = {
    1: "Category 1 (10 wd)",
    2: "Category 2 (15 wd)",
    3: "Category 3 (20 wd)",
    4: "Category 4 (25 wd)",
  };
  return locale.startsWith("en") ? en[cat] : ru[cat];
}

export function procurementMethodLabel(method: ProcurementMethodType, locale: string) {
  const ru: Record<ProcurementMethodType, string> = {
    DirectContract: "Прямой договор",
    LocalEshop: "Локальная закупка — электронный магазин",
    LocalAuction: "Локальная закупка — аукцион",
    LocalCooperation: "Локальная закупка — кооперация",
    LocalExchange: "Локальная закупка — биржевые торги",
    SmallValue: "Закупка малой стоимости",
    Rfp: "Запрос предложений / цен",
    BestOffer: "Отбор наилучшего предложения",
    Tender: "Тендер",
  };
  const en: Record<ProcurementMethodType, string> = {
    DirectContract: "Direct contract",
    LocalEshop: "Local procurement — e-shop",
    LocalAuction: "Local procurement — auction",
    LocalCooperation: "Local procurement — cooperation",
    LocalExchange: "Local procurement — exchange",
    SmallValue: "Small-value procurement",
    Rfp: "Request for proposals / prices",
    BestOffer: "Best offer selection",
    Tender: "Tender",
  };
  return locale.startsWith("en") ? en[method] : ru[method];
}

export function rfqDispatchLabel(type: RfqDispatchType, locale: string) {
  const ru: Record<RfqDispatchType, string> = {
    Vendor: "Производитель",
    Distributor: "Дистрибьютор",
    OpenSource: "Открытый поиск",
    AtgSite: "Сайт ATG",
    Tenderweek: "Tenderweek",
  };
  const en: Record<RfqDispatchType, string> = {
    Vendor: "Vendor",
    Distributor: "Distributor",
    OpenSource: "Open search",
    AtgSite: "ATG website",
    Tenderweek: "Tenderweek",
  };
  return locale.startsWith("en") ? en[type] : ru[type];
}

export function deadlineColorClass(color?: string) {
  switch (color) {
    case "red": return "text-red-600 bg-red-500/10";
    case "orange": return "text-orange-600 bg-orange-500/10";
    case "yellow": return "text-amber-600 bg-amber-500/10";
    default: return "text-emerald-600 bg-emerald-500/10";
  }
}

export async function uploadMarketingFile(file: File, folder = "marketing") {
  const { uploadFile } = await import("@/lib/files");
  return uploadFile(file, folder);
}
