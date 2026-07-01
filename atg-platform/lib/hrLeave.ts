export type HrLeaveItemType =
  | "RegularLeave"
  | "CompensationDays"
  | "UnpaidLeave"
  | "PartialPayLeave"
  | "FinancialAid";

export type HrLeaveRequestPhase =
  | "Draft"
  | "HrReview"
  | "AwaitingApproval"
  | "Approved"
  | "Rejected";

export type HrLeaveApprovalRole =
  | "HrSpecialist"
  | "DeputyDepartmentHead"
  | "DepartmentHead"
  | "SupervisingDeputyGd"
  | "GeneralDirector";

export type HrLeaveApproverStatus = "Pending" | "Approved" | "Rejected";

export interface HrLeaveItem {
  id: string;
  type: HrLeaveItemType;
  dateFrom?: string;
  dateTo?: string;
  daysCount?: number;
  noteRu?: string;
  noteEn?: string;
  sortOrder: number;
  textRu: string;
  textEn: string;
}

export interface HrLeaveApprover {
  id: string;
  userId: string;
  userName: string;
  role: HrLeaveApprovalRole;
  status: HrLeaveApproverStatus;
  sortOrder: number;
  approvalGroup: number;
  decidedAt?: string;
  comment?: string;
  departmentName?: string;
  departmentNameEn?: string;
}

export interface HrLeaveTimelineEvent {
  id: string;
  action: string;
  actorName: string;
  details?: string;
  createdAt: string;
}

export interface HrLeavePermissions {
  canCreate: boolean;
  canEdit: boolean;
  canSubmit: boolean;
  canHrReview: boolean;
  canApprove: boolean;
  canReject: boolean;
}

export interface HrLeaveRequest {
  id: string;
  number: string;
  status: string;
  phase: HrLeaveRequestPhase;
  track: string;
  periodLabel: string;
  requestDate: string;
  authorName: string;
  departmentName: string;
  departmentNameEn: string;
  organizationName: string;
  hrDepartmentName: string;
  hrDepartmentNameEn: string;
  hrTaskNumber?: string;
  createdAt: string;
  updatedAt: string;
  items: HrLeaveItem[];
  approvers: HrLeaveApprover[];
  timeline: HrLeaveTimelineEvent[];
  permissions: HrLeavePermissions;
}

export interface HrLeaveListItem {
  id: string;
  number: string;
  status: string;
  phase: HrLeaveRequestPhase;
  authorName: string;
  departmentName: string;
  departmentNameEn: string;
  requestDate: string;
  createdAt: string;
  itemCount: number;
}

export interface CreateHrLeaveItemPayload {
  type: HrLeaveItemType;
  dateFrom?: string | null;
  dateTo?: string | null;
  daysCount?: number | null;
  noteRu?: string | null;
  noteEn?: string | null;
}

export const HR_LEAVE_ITEM_TYPES: HrLeaveItemType[] = [
  "RegularLeave",
  "CompensationDays",
  "UnpaidLeave",
  "PartialPayLeave",
  "FinancialAid",
];

export function itemTypeNeedsDates(type: HrLeaveItemType) {
  return type === "RegularLeave" || type === "UnpaidLeave" || type === "PartialPayLeave";
}

export function itemTypeNeedsDaysCount(type: HrLeaveItemType) {
  return type === "CompensationDays";
}

export function phaseLabel(phase: HrLeaveRequestPhase, locale: string) {
  const ru: Record<HrLeaveRequestPhase, string> = {
    Draft: "Черновик",
    HrReview: "Проверка HR",
    AwaitingApproval: "На согласовании",
    Approved: "Утверждено",
    Rejected: "Отклонено",
  };
  const en: Record<HrLeaveRequestPhase, string> = {
    Draft: "Draft",
    HrReview: "HR review",
    AwaitingApproval: "Awaiting approval",
    Approved: "Approved",
    Rejected: "Rejected",
  };
  return locale.startsWith("en") ? en[phase] : ru[phase];
}

export function itemTypeLabel(type: HrLeaveItemType, locale: string) {
  const ru: Record<HrLeaveItemType, string> = {
    RegularLeave: "Трудовой отпуск",
    CompensationDays: "Компенсация дней отпуска",
    UnpaidLeave: "Отпуск без сохранения ЗП",
    PartialPayLeave: "Отпуск с частичной оплатой",
    FinancialAid: "Материальная помощь",
  };
  const en: Record<HrLeaveItemType, string> = {
    RegularLeave: "Regular leave",
    CompensationDays: "Vacation compensation",
    UnpaidLeave: "Unpaid leave",
    PartialPayLeave: "Partial pay leave",
    FinancialAid: "Financial aid",
  };
  return locale.startsWith("en") ? en[type] : ru[type];
}

export function approverRoleLabel(role: HrLeaveApprovalRole, locale: string) {
  const ru: Record<HrLeaveApprovalRole, string> = {
    HrSpecialist: "Специалист HR",
    DeputyDepartmentHead: "Зам. начальника отдела",
    DepartmentHead: "Начальник отдела",
    SupervisingDeputyGd: "Курирующий зам. ген. директора",
    GeneralDirector: "Генеральный директор",
  };
  const en: Record<HrLeaveApprovalRole, string> = {
    HrSpecialist: "HR specialist",
    DeputyDepartmentHead: "Deputy department head",
    DepartmentHead: "Department head",
    SupervisingDeputyGd: "Supervising deputy GD",
    GeneralDirector: "General director",
  };
  return locale.startsWith("en") ? en[role] : ru[role];
}

export function deptLabel(name: string, nameEn: string | undefined, locale: string) {
  return locale.startsWith("en") && nameEn ? nameEn : name;
}

export function approverStatusClass(status: HrLeaveApproverStatus) {
  if (status === "Approved") return "text-emerald-600 bg-emerald-50 border-emerald-200";
  if (status === "Rejected") return "text-red-600 bg-red-50 border-red-200";
  return "text-amber-700 bg-amber-50 border-amber-200";
}
