export type HrBusinessTripPhase =
  | "Draft"
  | "HrReview"
  | "AwaitingApproval"
  | "Approved"
  | "OrderPending"
  | "AwaitingOrderEimzo"
  | "CertificatePending"
  | "Completed"
  | "Rejected";

export type HrBusinessTripApprovalRole =
  | "DeputyDepartmentHead"
  | "DepartmentHead"
  | "HrManager"
  | "FirstDeputyGeneralDirector"
  | "GeneralDirector";

export type HrBusinessTripWorkflowStepKey =
  | "draft"
  | "departmentHead"
  | "hrManager"
  | "firstDeputyGd"
  | "order"
  | "gdOrderEimzo"
  | "certificate"
  | "completed";

export type WorkflowStepStatus = "pending" | "active" | "completed" | "rejected" | "skipped";

export type HrBusinessTripApproverStatus = "Pending" | "Approved" | "Rejected";

export interface HrBusinessTripTraveler {
  id: string;
  fullNameRu: string;
  fullNameEn?: string;
  positionRu: string;
  positionEn?: string;
  sortOrder: number;
  displayRu: string;
  displayEn: string;
  certificateNumber?: string | null;
  hasCertificate?: boolean;
  certificateDeliveredAt?: string | null;
  userId?: string | null;
}

export interface HrBusinessTripApprover {
  id: string;
  userId: string;
  userName: string;
  positionRu?: string;
  positionEn?: string;
  role: HrBusinessTripApprovalRole;
  status: HrBusinessTripApproverStatus;
  sortOrder: number;
  decidedAt?: string;
  comment?: string;
}

export interface HrBusinessTripTimelineEvent {
  id: string;
  action: string;
  actorName: string;
  details?: string;
  createdAt: string;
}

export interface HrBusinessTripPermissions {
  canCreate: boolean;
  canEdit: boolean;
  canSubmit: boolean;
  canHrReview: boolean;
  canApprove: boolean;
  canEimzoApprove: boolean;
  canIssueOrder: boolean;
  canGenerateCertificates: boolean;
  canDeliverCertificates: boolean;
  canReject: boolean;
}

export interface HrBusinessTripSignature {
  id: string;
  kind: string;
  signerName: string;
  signerPinpp?: string;
  signedAt: string;
  certificateSerial?: string;
}

export interface HrBusinessTripSigningPackage {
  jsonBase64: string;
  pdfBase64: string;
  payloadSha256: string;
  number: string;
}

export interface HrBusinessTripRequest {
  id: string;
  number: string;
  status: string;
  phase: HrBusinessTripPhase;
  requestDate: string;
  purposeRu: string;
  purposeEn?: string;
  dateFrom: string;
  dateTo: string;
  daysCount: number;
  placeRu: string;
  placeEn?: string;
  authorName: string;
  departmentName: string;
  departmentNameEn: string;
  organizationName: string;
  createdAt: string;
  updatedAt: string;
  orderNumber?: string | null;
  orderIssuedAt?: string | null;
  hasMemoPdf?: boolean;
  hasOrderPdf?: boolean;
  hasOrderSigned?: boolean;
  hasCertificates?: boolean;
  allCertificatesDelivered?: boolean;
  isTravelerView?: boolean;
  myTravelerId?: string | null;
  travelers: HrBusinessTripTraveler[];
  approvers: HrBusinessTripApprover[];
  timeline: HrBusinessTripTimelineEvent[];
  signatures: HrBusinessTripSignature[];
  permissions: HrBusinessTripPermissions;
}

export interface HrBusinessTripListItem {
  id: string;
  number: string;
  phase: HrBusinessTripPhase;
  departmentName: string;
  departmentNameEn: string;
  requestDate: string;
  dateFrom: string;
  dateTo: string;
  placeRu: string;
  travelerCount: number;
  createdAt: string;
  hasMyCertificate?: boolean;
}

export interface HrBusinessTripColleague {
  id: string;
  fullNameRu: string;
  fullNameEn?: string | null;
  positionRu: string;
  positionEn?: string | null;
}

export interface CreateHrBusinessTripTravelerPayload {
  fullNameRu: string;
  fullNameEn?: string | null;
  positionRu: string;
  positionEn?: string | null;
}

export const BUSINESS_TRIP_PLACE_OTHER = "__other__";

export const BUSINESS_TRIP_PLACE_OPTIONS: ReadonlyArray<{ ru: string; en: string }> = [
  {
    ru: "Бухарская и Кашкадарьинская области",
    en: "Bukhara and Kashkadarya regions",
  },
  {
    ru: "Бухарская область",
    en: "Bukhara region",
  },
  {
    ru: "Бухарская, Навоийская и Кашкадарьинская области",
    en: "Bukhara, Navoi and Kashkadarya regions",
  },
  {
    ru: "Бухарская, Навоийская области",
    en: "Bukhara and Navoi regions",
  },
  {
    ru: "г. Бухара, Бухарская область",
    en: "Bukhara city, Bukhara region",
  },
  {
    ru: "Кашкадарьинская область",
    en: "Kashkadarya region",
  },
];

export function computeDaysInclusive(from: string, to: string) {
  if (!from || !to) return 0;
  const start = new Date(from);
  const end = new Date(to);
  if (end < start) return 0;
  return Math.floor((end.getTime() - start.getTime()) / 86400000) + 1;
}

export function deptLabel(name: string, nameEn: string, locale: string) {
  return locale.startsWith("en") && nameEn ? nameEn : name;
}

const PHASE_LABELS: Record<HrBusinessTripPhase, { ru: string; en: string }> = {
  Draft: { ru: "Черновик", en: "Draft" },
  HrReview: { ru: "Проверка HR", en: "HR review" },
  AwaitingApproval: { ru: "На согласовании", en: "Awaiting approval" },
  Approved: { ru: "Утверждено", en: "Approved" },
  OrderPending: { ru: "Приказ — подготовка", en: "Order — preparation" },
  AwaitingOrderEimzo: { ru: "Приказ — подпись ГД", en: "Order — GD signature" },
  CertificatePending: { ru: "Удостоверение", en: "Travel certificate" },
  Completed: { ru: "Завершено", en: "Completed" },
  Rejected: { ru: "Отклонено", en: "Rejected" },
};

export function phaseLabel(phase: HrBusinessTripPhase, locale: string) {
  const entry = PHASE_LABELS[phase];
  return locale.startsWith("en") ? entry.en : entry.ru;
}

const ROLE_LABELS: Record<HrBusinessTripApprovalRole, { ru: string; en: string }> = {
  DeputyDepartmentHead: { ru: "Заместитель начальника", en: "Deputy department head" },
  DepartmentHead: { ru: "Начальник отдела", en: "Department head" },
  HrManager: { ru: "HR менеджер", en: "HR Manager" },
  FirstDeputyGeneralDirector: { ru: "Первый зам. ген. директора", en: "First Deputy GD" },
  GeneralDirector: { ru: "Генеральный директор (E-IMZO)", en: "General Director (E-IMZO)" },
};

export function approverRoleLabel(role: HrBusinessTripApprovalRole, locale: string) {
  const entry = ROLE_LABELS[role];
  return locale.startsWith("en") ? entry.en : entry.ru;
}

const STATUS_LABELS: Record<HrBusinessTripApproverStatus, { ru: string; en: string }> = {
  Pending: { ru: "Ожидает", en: "Pending" },
  Approved: { ru: "Согласовано", en: "Approved" },
  Rejected: { ru: "Отклонено", en: "Rejected" },
};

export function approverStatusLabel(status: HrBusinessTripApproverStatus, locale: string) {
  const entry = STATUS_LABELS[status];
  return locale.startsWith("en") ? entry.en : entry.ru;
}

export function approverStatusClass(status: HrBusinessTripApproverStatus) {
  if (status === "Approved") return "bg-emerald-50 text-emerald-700 border-emerald-200";
  if (status === "Rejected") return "bg-red-50 text-red-700 border-red-200";
  return "bg-amber-50 text-amber-700 border-amber-200";
}

export function travelerPayloadFromColleague(
  colleague: HrBusinessTripColleague,
): CreateHrBusinessTripTravelerPayload {
  return {
    fullNameRu: colleague.fullNameRu,
    fullNameEn: colleague.fullNameEn ?? null,
    positionRu: colleague.positionRu,
    positionEn: colleague.positionEn ?? null,
  };
}

export function travelerPayloadFromAuthUser(user: {
  id: string;
  fullName: string;
  fullNameEn?: string;
  jobTitleRu?: string;
  jobTitleEn?: string;
  positionName?: string;
}): CreateHrBusinessTripTravelerPayload {
  return {
    fullNameRu: user.fullName,
    fullNameEn: user.fullNameEn ?? null,
    positionRu: user.jobTitleRu ?? user.positionName ?? "",
    positionEn: user.jobTitleEn ?? null,
  };
}

export async function downloadHrBusinessTripPdf(requestId: string, signed = false) {
  const api = (await import("@/lib/api")).default;
  const path = signed
    ? `/hr/business-trips/${requestId}/download/signed-pdf`
    : `/hr/business-trips/${requestId}/download/pdf`;
  const response = await api.get(path, { responseType: "blob" });
  const blob = new Blob([response.data], { type: "application/pdf" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = `business-trip-${requestId}${signed ? "-signed" : ""}.pdf`;
  link.click();
  URL.revokeObjectURL(url);
}

export async function downloadHrBusinessTripOrderPdf(
  requestId: string,
  orderNumber?: string | null,
  signed = false,
) {
  const api = (await import("@/lib/api")).default;
  const path = signed
    ? `/hr/business-trips/${requestId}/download/order-signed-pdf`
    : `/hr/business-trips/${requestId}/download/order-pdf`;
  const response = await api.get(path, { responseType: "blob" });
  const blob = new Blob([response.data], { type: "application/pdf" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = `order-${orderNumber ?? requestId}${signed ? "-signed" : ""}.pdf`;
  link.click();
  URL.revokeObjectURL(url);
}

export async function downloadHrBusinessTripCertificate(
  requestId: string,
  travelerId: string,
  certificateNumber?: string | null,
) {
  const api = (await import("@/lib/api")).default;
  const response = await api.get(
    `/hr/business-trips/${requestId}/travelers/${travelerId}/download/certificate`,
    { responseType: "blob" },
  );
  const blob = new Blob([response.data], {
    type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
  });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = `certificate-${certificateNumber ?? travelerId}.xlsx`;
  link.click();
  URL.revokeObjectURL(url);
}

const WORKFLOW_STEP_KEYS: HrBusinessTripWorkflowStepKey[] = [
  "draft",
  "departmentHead",
  "hrManager",
  "firstDeputyGd",
  "order",
  "gdOrderEimzo",
  "certificate",
  "completed",
];

const WORKFLOW_STEP_LABELS: Record<HrBusinessTripWorkflowStepKey, { ru: string; en: string }> = {
  draft: { ru: "Черновик", en: "Draft" },
  departmentHead: { ru: "Начальник отдела", en: "Department head" },
  hrManager: { ru: "HR менеджер", en: "HR Manager" },
  firstDeputyGd: { ru: "Первый зам. ГД", en: "First Deputy GD" },
  order: { ru: "Приказ", en: "Order" },
  gdOrderEimzo: { ru: "Ген. директор (E-IMZO)", en: "GD (E-IMZO)" },
  certificate: { ru: "Командировочное удостоверение", en: "Travel certificate" },
  completed: { ru: "Завершено", en: "Completed" },
};

const STEP_TO_ROLE: Partial<Record<HrBusinessTripWorkflowStepKey, HrBusinessTripApprovalRole>> = {
  departmentHead: "DepartmentHead",
  hrManager: "HrManager",
  firstDeputyGd: "FirstDeputyGeneralDirector",
};

export function workflowStepItems(locale: string) {
  return WORKFLOW_STEP_KEYS.map((key) => ({
    key,
    label: locale.startsWith("en") ? WORKFLOW_STEP_LABELS[key].en : WORKFLOW_STEP_LABELS[key].ru,
  }));
}

function findApprover(request: HrBusinessTripRequest, step: HrBusinessTripWorkflowStepKey) {
  const role = STEP_TO_ROLE[step];
  if (!role) return undefined;
  return request.approvers.find((a) => a.role === role)
    ?? (step === "departmentHead"
      ? request.approvers.find((a) => a.role === "DeputyDepartmentHead")
      : undefined);
}

export function workflowStepStatus(
  request: HrBusinessTripRequest,
  step: HrBusinessTripWorkflowStepKey,
): WorkflowStepStatus {
  if (step === "draft") {
    if (request.phase === "Draft") return "active";
    return "completed";
  }

  if (step === "order") {
    if (
      request.phase === "AwaitingOrderEimzo"
      || request.phase === "CertificatePending"
      || request.phase === "Completed"
    )
      return "completed";
    if (request.phase === "OrderPending") return "active";
    if (request.phase === "Rejected") return "rejected";
    return "pending";
  }

  if (step === "gdOrderEimzo") {
    if (request.phase === "CertificatePending" || request.phase === "Completed") return "completed";
    if (request.phase === "AwaitingOrderEimzo") return "active";
    if (request.phase === "Rejected") return "rejected";
    return "pending";
  }

  if (step === "certificate") {
    if (request.phase === "Completed") return "completed";
    if (request.phase === "CertificatePending") return "active";
    if (request.phase === "Rejected") return "rejected";
    return "pending";
  }

  if (step === "completed") {
    if (request.phase === "Completed") return "completed";
    if (request.phase === "Rejected") return "rejected";
    return "pending";
  }

  const approver = findApprover(request, step);
  if (!approver) {
    if (request.phase === "Draft" || request.phase === "HrReview") return "pending";
    return "skipped";
  }

  if (approver.status === "Approved") return "completed";
  if (approver.status === "Rejected") return "rejected";

  const pending = request.approvers
    .filter((a) => a.status === "Pending")
    .sort((a, b) => a.sortOrder - b.sortOrder)[0];

  if (pending?.id === approver.id && request.phase === "AwaitingApproval") return "active";
  return "pending";
}

export function workflowActiveIndex(request: HrBusinessTripRequest) {
  if (request.phase === "Draft") return 0;
  if (request.phase === "HrReview") return 1;
  if (request.phase === "OrderPending") return WORKFLOW_STEP_KEYS.indexOf("order");
  if (request.phase === "AwaitingOrderEimzo") return WORKFLOW_STEP_KEYS.indexOf("gdOrderEimzo");
  if (request.phase === "CertificatePending") return WORKFLOW_STEP_KEYS.indexOf("certificate");
  if (request.phase === "Completed") return WORKFLOW_STEP_KEYS.length - 1;
  if (request.phase === "Approved")
    return WORKFLOW_STEP_KEYS.indexOf("order");
  if (request.phase === "Rejected") {
    const rejected = request.approvers.find((a) => a.status === "Rejected");
    if (rejected) {
      const stepKey = Object.entries(STEP_TO_ROLE).find(([, role]) => role === rejected.role)?.[0]
        ?? (rejected.role === "DeputyDepartmentHead" ? "departmentHead" : undefined);
      if (stepKey) return WORKFLOW_STEP_KEYS.indexOf(stepKey as HrBusinessTripWorkflowStepKey);
    }
    return 1;
  }

  const pending = request.approvers
    .filter((a) => a.status === "Pending")
    .sort((a, b) => a.sortOrder - b.sortOrder)[0];

  if (!pending) return WORKFLOW_STEP_KEYS.length - 1;

  const stepKey =
    Object.entries(STEP_TO_ROLE).find(([, role]) => role === pending.role)?.[0]
    ?? (pending.role === "DeputyDepartmentHead" ? "departmentHead" : "departmentHead");

  const idx = WORKFLOW_STEP_KEYS.indexOf(stepKey as HrBusinessTripWorkflowStepKey);
  return idx >= 0 ? idx : 1;
}

export function workflowCurrentHint(request: HrBusinessTripRequest, locale: string) {
  if (request.phase === "Draft") {
    return locale.startsWith("en") ? "Draft — not yet submitted" : "Черновик — ещё не отправлено";
  }
  if (request.phase === "HrReview") {
    return locale.startsWith("en") ? "HR preliminary review" : "Предварительная проверка HR";
  }
  if (request.phase === "OrderPending") {
    return locale.startsWith("en")
      ? "Memorandum approved — HR prepares the order"
      : "Записка утверждена — HR готовит приказ";
  }
  if (request.phase === "AwaitingOrderEimzo") {
    return locale.startsWith("en")
      ? `Order ${request.orderNumber ?? ""} — awaiting GD E-IMZO signature`
      : `Приказ ${request.orderNumber ?? ""} — ожидает подписи ГД (E-IMZO)`;
  }
  if (request.phase === "CertificatePending") {
    return locale.startsWith("en")
      ? "Order signed — HR prepares travel certificates"
      : "Приказ подписан — HR готовит командировочные удостоверения";
  }
  if (request.phase === "Completed") {
    return locale.startsWith("en")
      ? `Completed — order ${request.orderNumber ?? ""}`.trim()
      : `Завершено — приказ ${request.orderNumber ?? ""}`.trim();
  }
  if (request.phase === "Approved") {
    return locale.startsWith("en") ? "Fully approved" : "Полностью утверждено";
  }
  if (request.phase === "Rejected") {
    return locale.startsWith("en") ? "Rejected" : "Отклонено";
  }

  const pending = request.approvers
    .filter((a) => a.status === "Pending")
    .sort((a, b) => a.sortOrder - b.sortOrder)[0];

  if (!pending) {
    return locale.startsWith("en") ? "Awaiting approval" : "На согласовании";
  }

  const roleLabel = approverRoleLabel(pending.role, locale);
  const waiting = locale.startsWith("en") ? "Waiting for" : "Ожидает";
  return `${waiting}: ${pending.userName} (${roleLabel})`;
}
