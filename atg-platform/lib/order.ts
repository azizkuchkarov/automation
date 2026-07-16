export type OrderPhase =
  | "Draft"
  | "AwaitingDeptHeadApproval"
  | "NeedsRevision"
  | "SpecialistCoordination"
  | "DepartmentCoordination"
  | "AwaitingLegalApproval"
  | "AwaitingSupervisingDeputyApproval"
  | "AwaitingFirstDeputyApproval"
  | "AwaitingGeneralDirectorApproval"
  | "EdsFinalized"
  | "AwaitingRegistration"
  | "AwaitingPaperSignature"
  | "AwaitingScanUpload"
  | "AwaitingDistribution"
  | "AwaitingArchive"
  | "Completed";

export const ORDER_PHASES: OrderPhase[] = [
  "Draft",
  "AwaitingDeptHeadApproval",
  "NeedsRevision",
  "SpecialistCoordination",
  "DepartmentCoordination",
  "AwaitingLegalApproval",
  "AwaitingSupervisingDeputyApproval",
  "AwaitingFirstDeputyApproval",
  "AwaitingGeneralDirectorApproval",
  "EdsFinalized",
  "AwaitingRegistration",
  "AwaitingPaperSignature",
  "AwaitingScanUpload",
  "AwaitingDistribution",
  "AwaitingArchive",
  "Completed",
];

export const ORDER_STEP_GROUPS: { key: string; phases: OrderPhase[] }[] = [
  { key: "draft", phases: ["Draft", "NeedsRevision"] },
  { key: "deptApproval", phases: ["AwaitingDeptHeadApproval"] },
  { key: "coordination", phases: ["SpecialistCoordination", "DepartmentCoordination"] },
  { key: "legal", phases: ["AwaitingLegalApproval"] },
  {
    key: "leadership",
    phases: [
      "AwaitingSupervisingDeputyApproval",
      "AwaitingFirstDeputyApproval",
      "AwaitingGeneralDirectorApproval",
      "EdsFinalized",
    ],
  },
  {
    key: "registration",
    phases: [
      "AwaitingRegistration",
      "AwaitingPaperSignature",
      "AwaitingScanUpload",
      "AwaitingDistribution",
      "AwaitingArchive",
    ],
  },
  { key: "completion", phases: ["Completed"] },
];

export interface OrderUser {
  id: string;
  fullName: string;
  email: string;
  employeeId?: string;
  departmentName: string;
  departmentNameEn: string;
}

export interface OrderCoordinator {
  id: string;
  userId: string;
  userName: string;
  forDepartment: boolean;
  coordinatedAt: string;
}

export interface OrderRecipient {
  id: string;
  userId: string;
  userName: string;
  notifiedAt?: string;
}

export interface OrderComment {
  id: string;
  authorId: string;
  authorName: string;
  body: string;
  createdAt: string;
}

export interface Order {
  id: string;
  number: string;
  title: string;
  titleRu?: string;
  status: string;
  phase: OrderPhase;
  authorId: string;
  authorName: string;
  attachmentFileName?: string;
  attachmentStorageKey?: string;
  scanAttachmentFileName?: string;
  scanAttachmentStorageKey?: string;
  organizationId: string;
  departmentId: string;
  departmentName: string;
  departmentNameEn: string;
  deptHeadId?: string;
  deptHeadName?: string;
  legalHeadId?: string;
  legalHeadName?: string;
  supervisingDeputyId?: string;
  supervisingDeputyName?: string;
  firstDeputyId?: string;
  firstDeputyName?: string;
  generalDirectorId?: string;
  generalDirectorName?: string;
  revisionNotes?: string;
  coordinators: OrderCoordinator[];
  recipients: OrderRecipient[];
  comments: OrderComment[];
  createdAt: string;
  updatedAt: string;
}

export interface OrderPermissions {
  isInitiator: boolean;
  isRegistrar: boolean;
  isDeptHead: boolean;
  isLegalHead: boolean;
  isSupervisingDeputy: boolean;
  isFirstDeputy: boolean;
  isGeneralDirector: boolean;
  canCreate: boolean;
  canEditDraft: boolean;
  canSubmitForApproval: boolean;
  canApproveDeptHead: boolean;
  canRejectDeptHead: boolean;
  canManageSpecialistCoordination: boolean;
  canManageDepartmentCoordination: boolean;
  canApproveLegal: boolean;
  canRejectLegal: boolean;
  canApproveSupervisingDeputy: boolean;
  canApproveFirstDeputy: boolean;
  canApproveGeneralDirector: boolean;
  canRejectApproval: boolean;
  canFinalizeEds: boolean;
  canSendToRegistrar: boolean;
  canRegister: boolean;
  canConfirmPaperSignature: boolean;
  canUploadScan: boolean;
  canDistribute: boolean;
  canArchive: boolean;
  canView: boolean;
}

export function orderPhaseLabel(phase: OrderPhase, locale: string) {
  const en: Record<OrderPhase, string> = {
    Draft: "Draft",
    AwaitingDeptHeadApproval: "Dept head approval",
    NeedsRevision: "Needs revision",
    SpecialistCoordination: "Specialist coordination",
    DepartmentCoordination: "Department coordination",
    AwaitingLegalApproval: "Legal approval",
    AwaitingSupervisingDeputyApproval: "Supervising deputy",
    AwaitingFirstDeputyApproval: "First deputy",
    AwaitingGeneralDirectorApproval: "General director",
    EdsFinalized: "EDS finalized",
    AwaitingRegistration: "Registration",
    AwaitingPaperSignature: "Paper signature",
    AwaitingScanUpload: "Scan upload",
    AwaitingDistribution: "Distribution",
    AwaitingArchive: "Archive",
    Completed: "Completed",
  };
  const ru: Record<OrderPhase, string> = {
    Draft: "Проект",
    AwaitingDeptHeadApproval: "Согласование начальника",
    NeedsRevision: "Доработка",
    SpecialistCoordination: "Согласование специалистов",
    DepartmentCoordination: "Согласование департаментов",
    AwaitingLegalApproval: "Согласование юристов",
    AwaitingSupervisingDeputyApproval: "Курирующий зам.",
    AwaitingFirstDeputyApproval: "Первый зам.",
    AwaitingGeneralDirectorApproval: "Генеральный директор",
    EdsFinalized: "ЭДО завершено",
    AwaitingRegistration: "Регистрация",
    AwaitingPaperSignature: "Подпись на бумаге",
    AwaitingScanUpload: "Загрузка скана",
    AwaitingDistribution: "Рассылка",
    AwaitingArchive: "Архив",
    Completed: "Завершено",
  };
  return (locale.startsWith("en") ? en : ru)[phase] ?? phase;
}

export function orderStepLabel(key: string, locale: string) {
  const en: Record<string, string> = {
    draft: "Draft",
    deptApproval: "Dept head",
    coordination: "Coordination",
    legal: "Legal",
    leadership: "Leadership approval",
    registration: "Registration & distribution",
    completion: "Completion",
  };
  const ru: Record<string, string> = {
    draft: "Проект",
    deptApproval: "Начальник отдела",
    coordination: "Согласование",
    legal: "Юридический отдел",
    leadership: "Руководство",
    registration: "Регистрация и рассылка",
    completion: "Завершение",
  };
  return (locale.startsWith("en") ? en : ru)[key] ?? key;
}

export function currentOrderStepIndex(phase: OrderPhase) {
  const idx = ORDER_STEP_GROUPS.findIndex((g) => g.phases.includes(phase));
  return idx >= 0 ? idx : 0;
}
