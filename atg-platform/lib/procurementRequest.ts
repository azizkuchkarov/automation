export type ProcurementRequestFlow = "TechnicalAffairs" | "Express";

export type ProcurementRequestPhase =

  | "InProgress"

  | "AwaitingApproval"

  | "Marketing"

  | "Contracts"

  | "Payment"

  | "Completed";

export type ProcurementPaymentSubPhase = "Pending" | "WaitingAccept" | "InProgress" | "Completed";

export type ProcurementMarketingSubPhase = "Pending" | "WaitingAccept" | "InProgress" | "Completed";

export type ProcurementContractsSubPhase = "Pending" | "SectionPending" | "WaitingAccept" | "InProgress" | "Completed";

export type ContractsProcurementSectionType = "International" | "Domestic";

export type ContractsIntProcurementVariant = "Sbp" | "Tender" | "DirectForeignContract";

export type ContractsDomProcurementVariant =
  | "EShop"
  | "ElectronicAuction"
  | "QuotationRequest"
  | "DirectContract"
  | "SmallValue";

export type ProcurementWorkflowPhase = "TechnicalAffairs" | "Approval" | "Marketing" | "Contracts";

export type ProcurementStepCommentKind =
  | "Note"
  | "StepCompletion"
  | "Branch"
  | "Assignment"
  | "Acceptance";

export type MarketingBranchType =
  | "TzEscalation"
  | "ResponseFollowUp"
  | "KpNegotiation"
  | "ManagementRevision"
  | "PortalExpedite";

export type ProcurementApproverRole =

  | "Initiator"

  | "TasManager"

  | "BmgmcTopManager"

  | "SectionHead"

  | "TopManager";

export type ProcurementApproverStatus = "Pending" | "Approved" | "Rejected";

export type ProcurementMarketingPlanApproverRole =
  | "PlanDeputyCeo"
  | "PlanCeo"
  | "PlanCommissionMember";

export type ProcurementAttachmentKind =

  | "TechnicalAssignment"

  | "MaterialRequisition"

  | "ServiceRequisition"

  | "Other";

export type ProcurementTopologyNodeStatus = "Pending" | "Active" | "Completed" | "Skipped";



export interface ProcurementStep {

  number: number;

  titleRu: string;

  titleEn: string;

}



export interface ProcurementInitiatorDepartment {
  id: string;
  name: string;
  nameEn: string;
  organizationName: string;
  organizationCode: string;
  isStation: boolean;
}



export interface ProcurementRequestUser {

  id: string;

  fullName: string;

  email: string;

  employeeId?: string;

  departmentName: string;

  departmentNameEn: string;

  organizationName: string;

}



export interface ProcurementApprover {

  id: string;

  userId: string;

  userName: string;

  role: ProcurementApproverRole;

  status: ProcurementApproverStatus;

  sortOrder: number;

  decidedAt?: string;

  comment?: string;

  departmentName?: string;

  departmentNameEn?: string;

  organizationName?: string;

  organizationNameEn?: string;

  jobTitleRu?: string;

  jobTitleEn?: string;

  userEmail?: string;

  employeeId?: string;

}



export interface ProcurementMarketingPlanApprover {
  id: string;
  userId: string;
  userName: string;
  role: ProcurementMarketingPlanApproverRole;
  status: ProcurementApproverStatus;
  sortOrder: number;
  decidedAt?: string;
  comment?: string;
  departmentName?: string;
  departmentNameEn?: string;
  userEmail?: string;
}



export interface ProcurementAttachment {

  id: string;

  kind: ProcurementAttachmentKind;

  fileName: string;
  storageKey?: string;
  uploadedByName: string;

  uploadedAt: string;

}

export type ProcessDocumentSource = "Uploaded" | "Generated";

export interface ProcurementProcessDocument {
  id: string;
  fileName: string;
  storageKey?: string;
  source: ProcessDocumentSource;
  phase: string;
  category: string;
  departmentName?: string;
  departmentNameEn?: string;
  userName?: string;
  at?: string;
}



export interface ProcurementTimelineEvent {

  id: string;

  action: string;

  actorName: string;

  details?: string;

  createdAt: string;

}



export interface ProcurementTopologyNode {

  key: string;

  labelRu: string;

  labelEn: string;

  departmentCode?: string;

  departmentNameRu?: string;

  departmentNameEn?: string;

  status: ProcurementTopologyNodeStatus;

  assigneeName?: string;

  completedAt?: string;

}



export interface ProcurementRequest {

  id: string;

  number: string;

  title: string;

  titleRu?: string;

  status: string;

  isRegistered: boolean;

  flow: ProcurementRequestFlow;

  phase: ProcurementRequestPhase;

  currentStep: number;

  authorId: string;

  authorName: string;

  assigneeId?: string;

  assigneeName?: string;

  initiatorId?: string;

  initiatorName?: string;

  initiatorDepartmentId?: string;

  initiatorDepartmentName?: string;

  initiatorDepartmentNameEn?: string;

  region?: "HeadOffice" | "Bmgmc" | "Station";

  regionLabelRu?: string;

  regionLabelEn?: string;

  priority?: "Low" | "Medium" | "High" | "Critical";

  eamNumber?: string;

  eamFormationDate?: string;

  tasRequisitionType?: "MaterialRequest" | "ServiceRequest";

  dueDate?: string;

  organizationId: string;

  organizationName: string;

  departmentId: string;

  departmentName: string;

  departmentNameEn: string;

  responsibleTaskId?: string;

  tasResponsibleId?: string;

  tasResponsibleName?: string;

  marketingTaskId?: string;

  marketingTaskNumber?: string;

  contractsTaskId?: string;

  contractsTaskNumber?: string;

  marketingSubPhase: ProcurementMarketingSubPhase;

  marketingSpecialistId?: string;

  marketingSpecialistName?: string;

  marketingAcceptedAt?: string;

  marketingAssignedAt?: string;

  marketingCompletedAt?: string;

  contractsSubPhase: ProcurementContractsSubPhase;

  contractsProcurementSection?: ContractsProcurementSectionType;

  contractsSectionRoutedAt?: string;

  contractsSpecialistId?: string;

  contractsSpecialistName?: string;

  contractsAssignedAt?: string;

  contractsAcceptedAt?: string;

  contractsIntVariant?: ContractsIntProcurementVariant;

  contractsIntCurrentStep: number;

  contractsIntVariantSelectedAt?: string;

  contractsIntCompletedAt?: string;

  contractsIntContractRegistrationNumber?: string;

  contractsIntContractRegisteredAt?: string;

  contractsIntSecretariatPending?: boolean;

  contractsIntSecretariatUserId?: string;

  contractsIntSecretariatUserName?: string;

  contractsDomVariant?: ContractsDomProcurementVariant;

  contractsDomCurrentStep: number;

  contractsDomVariantSelectedAt?: string;

  contractsDomCompletedAt?: string;

  contractsDomContractRegistrationNumber?: string;

  contractsDomContractRegisteredAt?: string;

  contractsDomContractsAdminPending?: boolean;

  contractsDomContractsAdminUserId?: string;

  contractsDomContractsAdminUserName?: string;

  contractsDomPriceRequestDate?: string;

  contractsDomPriceResponseDueDate?: string;

  contractsDomDeliveryDueDate?: string;

  contractsDomActualDeliveryDate?: string;

  contractsDomLastTerminationAt?: string;

  paymentSubPhase?: ProcurementPaymentSubPhase;

  paymentTaskId?: string;

  paymentSpecialistId?: string;

  paymentSpecialistName?: string;

  paymentAssignedAt?: string;

  paymentAcceptedAt?: string;

  contractsIntSteps?: ProcurementContractsIntStep[];

  contractsDomSteps?: ProcurementContractsDomStep[];

  marketingPermissions?: ProcurementMarketingPermissions;

  contractsPermissions?: ProcurementContractsPermissions;

  paymentPermissions?: ProcurementPaymentPermissions;

  marketingPlanApprovalSubmittedAt?: string;

  marketingPlanRegistrationNumber?: string;

  marketingPlanRegisteredAt?: string;

  marketingRfqRegistrationNumber?: string;

  marketingRfqRegisteredAt?: string;

  marketingProcurementPlanRegistrationNumber?: string;

  marketingProcurementPlanRegisteredAt?: string;

  marketingProcurementPlanRegistrationMethod?: string;

  marketingPlanPermissions?: ProcurementMarketingPlanPermissions;

  marketingPlanApprovers?: ProcurementMarketingPlanApprover[];

  marketingCurrentStep: number;

  marketingActiveBranch?: MarketingBranchType;

  marketingSteps: ProcurementMarketingStep[];

  steps: ProcurementStep[];

  approvers: ProcurementApprover[];

  attachments: ProcurementAttachment[];

  processDocuments?: ProcurementProcessDocument[];

  registeredAt?: string;

  timeline: ProcurementTimelineEvent[];

  stepComments: ProcurementStepComment[];

  topology: ProcurementTopologyNode[];

  createdAt: string;

  updatedAt: string;

}



export interface ProcurementCreateOptions {

  canCreateTas: boolean;

  canCreateExpress: boolean;

  defaultFlow?: ProcurementRequestFlow;

  formContext?: ProcurementRequestFormContext;

}



export interface ProcurementRequestFormContext {

  region: "HeadOffice" | "Bmgmc" | "Station";

  regionLabelRu: string;

  regionLabelEn: string;

  regDate: string;

  initiatingDepartmentId?: string;

  initiatingDepartmentName?: string;

  initiatingDepartmentNameEn?: string;

  initiatingEmployeeId: string;

  initiatingEmployeeName: string;

  requiresEamNumber: boolean;

  isTasStaff: boolean;

}



export interface ProcurementStepComment {
  id: string;
  phase: ProcurementWorkflowPhase;
  stepNumber: number;
  authorId: string;
  authorName: string;
  body: string;
  kind: ProcurementStepCommentKind;
  createdAt: string;
}

export interface ProcurementMarketingPermissions {
  canAccept: boolean;
  canAssign: boolean;
  canReturnToInitiator: boolean;
  canComplete: boolean;
  canForwardToContracts: boolean;
  canCompleteCurrentStep: boolean;
  canRecordBranch: boolean;
  canResolveBranch: boolean;
  canReviewProposals: boolean;
  canReviewProposalsAsEngineer: boolean;
  currentStep: number;
}



export interface ProcurementMarketingPlanPermissions {
  canSubmit: boolean;
  canApprove: boolean;
  canConfirmRegistration: boolean;
}



export interface ProcurementContractsPermissions {

  canAccept: boolean;

  canAssign: boolean;

  canRouteSection: boolean;

  canSelectIntVariant: boolean;

  canCompleteIntStep: boolean;

  canUploadIntStepFile: boolean;

  canSubmitIntStepApprovers: boolean;

  canDecideIntStepApproval: boolean;

  canSendToSecretariat: boolean;

  canCompleteAsSecretariat: boolean;

  contractsIntCurrentStep: number;

  canSelectDomVariant: boolean;

  canCompleteDomStep: boolean;

  canUploadDomStepFile: boolean;

  canSubmitDomStepApprovers: boolean;

  canDecideDomStepApproval: boolean;

  canSendToContractsAdmin: boolean;

  canCompleteAsContractsAdmin: boolean;

  canScheduleDomStep: boolean;

  canReturnDomStepToMarketing: boolean;

  canRollbackDomStep: boolean;

  contractsDomCurrentStep: number;

}

export interface ProcurementPaymentPermissions {
  canAssign: boolean;
  canAccept: boolean;
}

export interface ProcurementContractsIntStepFile {
  id: string;
  stepNumber: number;
  fileName: string;
  storageKey?: string;
  uploadedByName: string;
  uploadedAt: string;
}

export interface ProcurementContractsIntStepApprover {
  id: string;
  stepNumber: number;
  userId: string;
  fullName: string;
  email: string;
  status: ProcurementApproverStatus;
  sortOrder: number;
  decidedAt?: string;
  comment?: string;
}

export interface ProcurementContractsIntStep {

  number: number;

  titleRu: string;

  titleEn: string;

  hintRu: string;

  hintEn: string;

  hasBranch: boolean;

  branchHintRu?: string;

  branchHintEn?: string;

  requiresUpload: boolean;

  requiresApprovers: boolean;

  requiresSecretariat: boolean;

  requiresRegistration: boolean;

  files: ProcurementContractsIntStepFile[];

  approvers: ProcurementContractsIntStepApprover[];

  approversSubmitted: boolean;

  allApproversApproved: boolean;

  secretariatPending: boolean;

}

export interface ProcurementContractsDomStepFile {
  id: string;
  stepNumber: number;
  fileName: string;
  storageKey?: string;
  uploadedByName: string;
  uploadedAt: string;
}

export interface ProcurementContractsDomStepApprover {
  id: string;
  stepNumber: number;
  userId: string;
  fullName: string;
  email: string;
  status: ProcurementApproverStatus;
  sortOrder: number;
  decidedAt?: string;
  comment?: string;
}

export interface ProcurementContractsDomStep {
  number: number;
  titleRu: string;
  titleEn: string;
  hintRu: string;
  hintEn: string;
  hasBranch: boolean;
  branchHintRu?: string;
  branchHintEn?: string;
  requiresUpload: boolean;
  requiresApprovers: boolean;
  requiresContractsAdmin: boolean;
  requiresRegistration: boolean;
  requiresScheduleDate: boolean;
  scheduleLabelRu?: string;
  scheduleLabelEn?: string;
  scheduleHintRu?: string;
  scheduleHintEn?: string;
  allowsReturnToMarketing: boolean;
  allowsTerminationRollback: boolean;
  rollbackStepNumber?: number;
  files: ProcurementContractsDomStepFile[];
  approvers: ProcurementContractsDomStepApprover[];
  approversSubmitted: boolean;
  allApproversApproved: boolean;
  contractsAdminPending: boolean;
}

export interface ProcurementMarketingStep {

  number: number;

  titleRu: string;

  titleEn: string;

  hintRu: string;

  hintEn: string;

  hasBranch: boolean;

  branchHintRu?: string;

  branchHintEn?: string;

}



export interface ProcurementMarketingQueueItem {

  id: string;

  number: string;

  title: string;

  titleRu?: string;

  marketingSubPhase: ProcurementMarketingSubPhase;

  marketingCurrentStep: number;

  marketingStepTitleRu: string;

  marketingStepTitleEn: string;

  assigneeName?: string;

  marketingSpecialistName?: string;

  registeredAt: string;

  updatedAt: string;

}

export interface ProcurementContractsQueueItem {
  id: string;
  number: string;
  title: string;
  titleRu?: string;
  section?: ContractsProcurementSectionType | null;
  contractsSubPhase: ProcurementContractsSubPhase;
  assigneeName?: string;
  contractsSpecialistName?: string;
  updatedAt: string;
}

export interface ProcurementContractsBoardItem {
  id: string;
  number: string;
  title: string;
  titleRu?: string;
  section?: ContractsProcurementSectionType | null;
  contractsSubPhase: ProcurementContractsSubPhase;
  assigneeName?: string;
  contractsSpecialistName?: string;
  domVariant?: string | null;
  intVariant?: string | null;
  domCurrentStep: number;
  intCurrentStep: number;
  updatedAt: string;
}

export interface ProcurementContractsBoardColumn {
  subPhase: ProcurementContractsSubPhase;
  labelRu: string;
  labelEn: string;
  items: ProcurementContractsBoardItem[];
}

export function stepTitle(step: ProcurementStep, locale: string) {

  return locale.startsWith("en") ? step.titleEn : step.titleRu;

}



export function topologyLabel(node: ProcurementTopologyNode, locale: string) {

  return locale.startsWith("en") ? node.labelEn : node.labelRu;

}



export function topologyDept(node: ProcurementTopologyNode, locale: string) {

  if (!node.departmentNameRu && !node.departmentNameEn) return null;

  return locale.startsWith("en")

    ? node.departmentNameEn ?? node.departmentNameRu

    : node.departmentNameRu ?? node.departmentNameEn;

}



export function approverRoleLabel(role: ProcurementApproverRole, locale: string) {

  const ru: Record<ProcurementApproverRole, string> = {

    Initiator: "Инициатор",

    TasManager: "Руководитель TAS",

    BmgmcTopManager: "Топ-менеджер BMGMC",

    SectionHead: "Начальник подразделения",

    TopManager: "Топ-менеджер",

  };

  const en: Record<ProcurementApproverRole, string> = {

    Initiator: "Initiator",

    TasManager: "TAS Manager",

    BmgmcTopManager: "BMGMC Top Manager",

    SectionHead: "Section Head",

    TopManager: "Top Manager",

  };

  return locale.startsWith("en") ? en[role] : ru[role];

}



export const APPROVER_ROLE_ORDER: ProcurementApproverRole[] = [

  "Initiator",

  "TasManager",

  "BmgmcTopManager",

  "SectionHead",

  "TopManager",

];



export function getNextPendingApprover(approvers: ProcurementApprover[]) {

  return [...approvers]

    .filter((a) => a.status === "Pending")

    .sort((a, b) => APPROVER_ROLE_ORDER.indexOf(a.role) - APPROVER_ROLE_ORDER.indexOf(b.role))[0];

}



export function getNextPendingPlanApprover(approvers: ProcurementMarketingPlanApprover[]) {
  return [...approvers]
    .filter((a) => a.status === "Pending")
    .sort((a, b) => a.sortOrder - b.sortOrder)[0];
}



export function planApproverRoleLabel(role: ProcurementMarketingPlanApproverRole, locale: string) {
  const ru: Record<ProcurementMarketingPlanApproverRole, string> = {
    PlanDeputyCeo: "Первый зам. ген. директора",
    PlanCeo: "Генеральный директор",
    PlanCommissionMember: "Член закупочной комиссии",
  };
  const en: Record<ProcurementMarketingPlanApproverRole, string> = {
    PlanDeputyCeo: "First Deputy CEO",
    PlanCeo: "CEO",
    PlanCommissionMember: "Procurement commission member",
  };
  return locale.startsWith("en") ? en[role] : ru[role];
}



export function attachmentKindLabel(kind: ProcurementAttachmentKind, locale: string) {

  const ru: Record<ProcurementAttachmentKind, string> = {

    TechnicalAssignment: "ТЗ (TA)",

    MaterialRequisition: "ЛЗМ (MR)",

    ServiceRequisition: "ЛЗУ (SR)",

    Other: "Другое",

  };

  const en: Record<ProcurementAttachmentKind, string> = {

    TechnicalAssignment: "TA",

    MaterialRequisition: "MR",

    ServiceRequisition: "SR",

    Other: "Other",

  };

  return locale.startsWith("en") ? en[kind] : ru[kind];

}



export function phaseLabel(phase: ProcurementRequestPhase, locale: string) {

  const ru: Record<ProcurementRequestPhase, string> = {

    InProgress: "В работе",

    AwaitingApproval: "На согласовании",

    Marketing: "Маркетинг",

    Contracts: "Контракты",

    Payment: "Payment",

    Completed: "Завершено",

  };

  const en: Record<ProcurementRequestPhase, string> = {

    InProgress: "In progress",

    AwaitingApproval: "Awaiting approval",

    Marketing: "Marketing",

    Contracts: "Contracts",

    Payment: "Payment",

    Completed: "Completed",

  };

  return locale.startsWith("en") ? en[phase] : ru[phase];

}



export function marketingStepTitle(step: ProcurementMarketingStep, locale: string) {

  return locale.startsWith("en") ? step.titleEn : step.titleRu;

}



export function marketingStepHint(step: ProcurementMarketingStep, locale: string) {

  return locale.startsWith("en") ? step.hintEn : step.hintRu;

}



export function marketingStepBranchHint(step: ProcurementMarketingStep, locale: string) {

  if (!step.hasBranch) return null;

  return locale.startsWith("en") ? step.branchHintEn ?? step.branchHintRu : step.branchHintRu ?? step.branchHintEn;

}



export function branchForMarketingStep(stepNumber: number): MarketingBranchType | null {

  const map: Record<number, MarketingBranchType> = {
    2: "TzEscalation",
    6: "ManagementRevision",
  };

  return map[stepNumber] ?? null;

}



export function marketingSubPhaseLabel(subPhase: ProcurementMarketingSubPhase, locale: string) {

  const ru: Record<ProcurementMarketingSubPhase, string> = {

    Pending: "Ожидает назначения",

    WaitingAccept: "Ожидает принятия инженером",

    InProgress: "В работе",

    Completed: "Завершено",

  };

  const en: Record<ProcurementMarketingSubPhase, string> = {

    Pending: "Awaiting assignment",

    WaitingAccept: "Awaiting engineer acceptance",

    InProgress: "In progress",

    Completed: "Completed",

  };

  return locale.startsWith("en") ? en[subPhase] : ru[subPhase];

}



export function contractsSubPhaseLabel(subPhase: ProcurementContractsSubPhase, locale: string) {

  const ru: Record<ProcurementContractsSubPhase, string> = {

    Pending: "Ожидает маршрутизации",

    SectionPending: "Ожидает назначения инженера",

    WaitingAccept: "Ожидает принятия инженером",

    InProgress: "В работе",

    Completed: "Завершено",

  };

  const en: Record<ProcurementContractsSubPhase, string> = {

    Pending: "Awaiting section routing",

    SectionPending: "Awaiting engineer assignment",

    WaitingAccept: "Awaiting engineer acceptance",

    InProgress: "In progress",

    Completed: "Completed",

  };

  return locale.startsWith("en") ? en[subPhase] : ru[subPhase];

}



export function contractsSectionLabel(section: ContractsProcurementSectionType, locale: string) {

  const ru: Record<ContractsProcurementSectionType, string> = {

    International: "Отдел международных закупок",

    Domestic: "Отдел локальных закупок",

  };

  const en: Record<ContractsProcurementSectionType, string> = {

    International: "International Procurement Section",

    Domestic: "Domestic Procurement Section",

  };

  return locale.startsWith("en") ? en[section] : ru[section];

}



export function contractsIntVariantLabel(variant: ContractsIntProcurementVariant, locale: string) {

  const ru: Record<ContractsIntProcurementVariant, string> = {

    Sbp: "Отбор наилучших предложений (SBP)",

    Tender: "Тендер (TP)",

    DirectForeignContract: "Прямой контракт с иностранными поставщиками",

  };

  const en: Record<ContractsIntProcurementVariant, string> = {

    Sbp: "Selection of Best Proposals (SBP)",

    Tender: "Tender (TP)",

    DirectForeignContract: "Direct contract with foreign suppliers",

  };

  return locale.startsWith("en") ? en[variant] : ru[variant];

}

/** Example auto registration number, e.g. ATG-CD-INT-SBP-26-001 */
export function contractsIntRegistrationExample(variant?: ContractsIntProcurementVariant | null) {
  const code =
    variant === "Tender" ? "TP" : variant === "DirectForeignContract" ? "DFC" : "SBP";
  const yy = String(new Date().getFullYear() % 100).padStart(2, "0");
  return `ATG-CD-INT-${code}-${yy}-001`;
}

export function contractsIntWorkflowTitle(
  variant: ContractsIntProcurementVariant,
  locale: string,
  stepCount: number
) {
  const ru: Record<ContractsIntProcurementVariant, string> = {
    Sbp: `Отбор наилучших предложений (SBP) — ${stepCount} шагов`,
    Tender: `Тендер (TP) — ${stepCount} шагов`,
    DirectForeignContract: `Прямой контракт с иностранными поставщиками — ${stepCount} шагов`,
  };
  const en: Record<ContractsIntProcurementVariant, string> = {
    Sbp: `Selection of Best Proposals (SBP) — ${stepCount} steps`,
    Tender: `Tender (TP) — ${stepCount} steps`,
    DirectForeignContract: `Direct contract with foreign suppliers — ${stepCount} steps`,
  };
  return locale.startsWith("en") ? en[variant] : ru[variant];
}



export function contractsIntStepTitle(step: ProcurementContractsIntStep, locale: string) {

  return locale.startsWith("en") ? step.titleEn : step.titleRu;

}



export function contractsIntStepHint(step: ProcurementContractsIntStep, locale: string) {

  return locale.startsWith("en") ? step.hintEn : step.hintRu;

}

export function contractsDomVariantLabel(variant: ContractsDomProcurementVariant, locale: string) {
  const ru: Record<ContractsDomProcurementVariant, string> = {
    EShop: "Закупка методом E-shop",
    ElectronicAuction: "Электронный аукцион",
    QuotationRequest: "Запрос предложений",
    DirectContract: "Прямой договор",
    SmallValue: "Упрощённая процедура (малая стоимость)",
  };
  const en: Record<ContractsDomProcurementVariant, string> = {
    EShop: "E-shop procurement",
    ElectronicAuction: "Electronic auction",
    QuotationRequest: "Quotation request",
    DirectContract: "Direct contract",
    SmallValue: "Simplified small-value procedure",
  };
  return locale.startsWith("en") ? en[variant] : ru[variant];
}

export function contractsDomRegistrationExample(variant?: ContractsDomProcurementVariant | null) {
  const code =
    variant === "ElectronicAuction" ? "AUC"
      : variant === "QuotationRequest" ? "QRS"
        : variant === "DirectContract" ? "DPS"
          : variant === "SmallValue" ? "SPS"
            : "ESHOP";
  const yy = String(new Date().getFullYear() % 100).padStart(2, "0");
  return `ATG-CD-DOM-${code}-${yy}-001`;
}

export function contractsDomWorkflowTitle(
  variant: ContractsDomProcurementVariant,
  locale: string,
  stepCount: number
) {
  const label = contractsDomVariantLabel(variant, locale);
  return locale.startsWith("en") ? `${label} — ${stepCount} steps` : `${label} — ${stepCount} шагов`;
}

export function contractsDomStepTitle(step: ProcurementContractsDomStep, locale: string) {
  return locale.startsWith("en") ? step.titleEn : step.titleRu;
}

export function contractsDomStepHint(step: ProcurementContractsDomStep, locale: string) {
  return locale.startsWith("en") ? step.hintEn : step.hintRu;
}

export function topologyStatusLabel(status: ProcurementTopologyNodeStatus, locale: string) {

  const ru: Record<ProcurementTopologyNodeStatus, string> = {

    Pending: "Ожидание",

    Active: "Активно",

    Completed: "Завершено",

    Skipped: "Пропущено",

  };

  const en: Record<ProcurementTopologyNodeStatus, string> = {

    Pending: "Pending",

    Active: "Active",

    Completed: "Completed",

    Skipped: "Skipped",

  };

  return locale.startsWith("en") ? en[status] : ru[status];

}



export function timelineActionLabel(action: string, locale: string) {

  const ru: Record<string, string> = {

    created: "Создано",

    step_completed: "Шаг выполнен",

    tas_rejected: "Заявка отклонена (TAS)",

    submitted_for_approval: "Отправлено на согласование",

    approved: "Согласовано",

    rejected: "Отклонено",

    registered: "Зарегистрировано",

    handoff_marketing: "Передано в маркетинг",

    handoff_contracts: "Передано в контракты",

    contracts_section_routed: "Направлено в отдел закупок",

    contracts_int_completed: "Международный процесс завершён",

    handed_off_to_payment: "Передано в Payment",

    payment_assigned: "Назначен специалист Payment",

    payment_accepted: "Payment принял в работу",

    marketing_accepted: "Маркетинг принял в работу",

    marketing_assigned: "Назначен специалист",

    marketing_completed: "Маркетинг завершён",

    marketing_step_1_completed: "Шаг 1: принятие и назначение",

    marketing_step_2_completed: "Шаг 2: изучение документов",

    marketing_step_3_completed: "Шаг 3: подготовка RFQ",

    marketing_step_4_completed: "Шаг 4: рассылка RFQ",

    marketing_step_5_completed: "Шаг 5: ожидание ответов",

    marketing_step_6_completed: "Шаг 6: проверка КП",

    marketing_step_7_completed: "Шаг 7: план закупки",

    marketing_step_8_completed: "Шаг 8: согласование плана закупки",

    marketing_step_9_completed: "Шаг 9: регистрация маркетинга",

    marketing_plan_submitted: "План закупки направлен на согласование",

    marketing_plan_approved: "Согласование плана закупки",

    marketing_plan_rejected: "План закупки отклонён",

    marketing_plan_registered: "Маркетинговый процесс зарегистрирован",

    marketing_branch_recorded: "Зафиксировано отклонение",

    marketing_branch_resolved: "Отклонение устранено",

  };

  const en: Record<string, string> = {

    created: "Created",

    step_completed: "Step completed",

    tas_rejected: "Request rejected (TAS)",

    submitted_for_approval: "Submitted for approval",

    approved: "Approved",

    rejected: "Rejected",

    registered: "Registered",

    handoff_marketing: "Handed off to Marketing",

    handoff_contracts: "Handed off to Contracts",

    contracts_section_routed: "Routed to procurement section",

    contracts_int_completed: "International workflow completed",

    handed_off_to_payment: "Handed off to Payment",

    payment_assigned: "Payment specialist assigned",

    payment_accepted: "Payment accepted",

    marketing_accepted: "Marketing accepted",

    marketing_assigned: "Specialist assigned",

    marketing_completed: "Marketing completed",

    marketing_step_1_completed: "Step 1: accept & assign",

    marketing_step_2_completed: "Step 2: document review",

    marketing_step_3_completed: "Step 3: RFQ preparation",

    marketing_step_4_completed: "Step 4: RFQ distribution",

    marketing_step_5_completed: "Step 5: await responses",

    marketing_step_6_completed: "Step 6: evaluate proposals",

    marketing_step_7_completed: "Step 7: procurement plan",

    marketing_step_8_completed: "Step 8: procurement plan approval",

    marketing_step_9_completed: "Step 9: marketing registration",

    marketing_plan_submitted: "Procurement plan submitted for approval",

    marketing_plan_approved: "Procurement plan approved",

    marketing_plan_rejected: "Procurement plan rejected",

    marketing_plan_registered: "Marketing process registered",

    marketing_branch_recorded: "Branch deviation recorded",

    marketing_branch_resolved: "Branch deviation resolved",

  };

  const map = locale.startsWith("en") ? en : ru;

  return map[action] ?? action;

}

export function stepCommentKindLabel(kind: ProcurementStepCommentKind, locale: string) {
  const ru: Record<ProcurementStepCommentKind, string> = {
    Note: "Комментарий",
    StepCompletion: "Завершение этапа",
    Branch: "Отклонение",
    Assignment: "Назначение",
    Acceptance: "Принятие в работу",
  };
  const en: Record<ProcurementStepCommentKind, string> = {
    Note: "Comment",
    StepCompletion: "Step completion",
    Branch: "Deviation",
    Assignment: "Assignment",
    Acceptance: "Acceptance",
  };
  return locale.startsWith("en") ? en[kind] : ru[kind];
}

