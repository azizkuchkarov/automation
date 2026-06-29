export type DocumentType =
  | "Incoming"
  | "Outgoing"
  | "Memo"
  | "MinutesOfMeeting"
  | "Order"
  | "TechnicalAssignment"
  | "MaterialServiceRequisition"
  | "Marketing"
  | "Contract"
  | "Payment"
  | "Accounting"
  | "SupplySection"
  | "ProcurementRequest";

export type DocumentStatus =
  | "Draft"
  | "Registered"
  | "InReview"
  | "Approved"
  | "Rejected"
  | "Archived";

export interface DocumentListItem {
  id: string;
  number: string;
  title: string;
  type: DocumentType;
  status: DocumentStatus;
  authorName: string;
  assigneeName?: string;
  departmentName: string;
  departmentNameEn: string;
  createdAt: string;
  updatedAt: string;
  procurementFlow?: "TechnicalAffairs" | "Express";
  procurementPhase?: "InProgress" | "AwaitingApproval" | "Marketing" | "Contracts" | "Completed";
  procurementCurrentStep?: number;
  initiatorName?: string;
  priority?: "Low" | "Medium" | "High" | "Critical";
}

export interface DocumentActivity {
  id: string;
  actorId: string;
  actorName: string;
  action: string;
  fromStatus?: DocumentStatus;
  toStatus?: DocumentStatus;
  details?: string;
  createdAt: string;
}

export interface Document extends DocumentListItem {
  description: string;
  authorId: string;
  authorNameEn?: string;
  authorEmail: string;
  organizationId: string;
  organizationName: string;
  departmentId: string;
  externalReference?: string;
  registeredAt?: string;
  dueDate?: string;
  titleRu?: string;
  incomingNumber?: string;
  incomingDate?: string;
  recordBook?: string;
  senderName?: string;
  receiverName?: string;
  receiverNameEn?: string;
  attachmentFileName?: string;
  translationRequestCount: number;
  activities: DocumentActivity[];
}

export interface DcsTypeInfo {
  type: DocumentType;
  nameEn: string;
  nameRu: string;
  section: "office" | "procurement";
  icon: string;
}

export const OFFICE_TYPES: { slug: string; type: DocumentType }[] = [
  { slug: "incoming", type: "Incoming" },
  { slug: "outgoing", type: "Outgoing" },
  { slug: "memo", type: "Memo" },
  { slug: "minutes", type: "MinutesOfMeeting" },
  { slug: "orders", type: "Order" },
];

export const PROCUREMENT_TYPES: { slug: string; type: DocumentType }[] = [
  { slug: "requests", type: "ProcurementRequest" },
  { slug: "marketing", type: "Marketing" },
  { slug: "contracts", type: "Contract" },
  { slug: "payment", type: "Payment" },
  { slug: "accounting", type: "Accounting" },
  { slug: "supply-section", type: "SupplySection" },
];

export const ALL_TYPE_SLUGS = [...OFFICE_TYPES, ...PROCUREMENT_TYPES];

export function slugToType(slug: string): DocumentType | undefined {
  return ALL_TYPE_SLUGS.find((t) => t.slug === slug)?.type;
}

export function typeToSlug(type: DocumentType): string | undefined {
  return ALL_TYPE_SLUGS.find((t) => t.type === type)?.slug;
}

export function typeLabel(info: Pick<DcsTypeInfo, "nameEn" | "nameRu">, locale: string) {
  return locale.startsWith("en") ? info.nameEn : info.nameRu;
}

export function deptLabel(name: string, nameEn: string, locale: string) {
  return locale.startsWith("en") && nameEn ? nameEn : name;
}

export const DOCUMENT_STATUSES: DocumentStatus[] = [
  "Draft", "Registered", "InReview", "Approved", "Rejected", "Archived",
];

export function typeSlugIcon(slug: string) {
  switch (slug) {
    case "incoming": return "inbox";
    case "outgoing": return "send";
    case "memo": return "file-text";
    case "minutes": return "users";
    case "orders": return "scroll";
    case "technical-assignments": return "clipboard";
    case "requests": return "file-plus";
    case "mr-sr": return "package";
    case "marketing": return "megaphone";
    case "contracts": return "signature";
    case "payment": return "credit-card";
    case "accounting": return "calculator";
    case "supply-section": return "truck";
    default: return "file";
  }
}

export function incomingStatusLabel(status: DocumentStatus, locale: string) {
  if (status === "InReview") {
    return locale.startsWith("en") ? "Execution" : "Исполнение";
  }
  return null;
}

export interface DcsStaff {
  id: string;
  employeeId?: string;
  fullName: string;
  email: string;
  role: string;
  jobTitleEn?: string;
  jobTitleRu?: string;
}

export interface DcsOrgRouting {
  organizationCode: string;
  organizationName: string;
  departmentId: string;
  departmentCode: string;
  departmentName: string;
  departmentNameEn: string;
  draftCount: number;
  activeCount: number;
  assigners: DcsStaff[];
  handlers: DcsStaff[];
  designatedRegistrar?: DcsStaff;
}

export interface DcsCategoryRouting {
  type: DocumentType;
  nameEn: string;
  nameRu: string;
  section: string;
  icon: string;
  color: string;
  routes: DcsOrgRouting[];
}

export interface DcsDashboard {
  totalDraft: number;
  totalInReview: number;
  totalApproved: number;
  totalArchived: number;
  recentDocuments: DocumentListItem[];
}

export interface DcsAdminControl {
  dashboard: DcsDashboard;
  categories: DcsCategoryRouting[];
}

export function statusColor(s: DocumentStatus) {
  switch (s) {
    case "Draft": return "bg-slate-500/12 text-slate-600 dark:text-slate-300 border-slate-500/25";
    case "Registered": return "bg-sky-500/12 text-sky-700 dark:text-sky-300 border-sky-500/25";
    case "InReview": return "bg-atg-blue/12 text-blue-700 dark:text-blue-300 border-atg-blue/25";
    case "Approved": return "bg-emerald-500/12 text-emerald-700 dark:text-emerald-300 border-emerald-500/25";
    case "Rejected": return "bg-red-500/12 text-red-700 dark:text-red-300 border-red-500/25";
    case "Archived": return "bg-border/50 text-foreground/50 border-border";
    default: return "bg-border/40 text-foreground/50";
  }
}
