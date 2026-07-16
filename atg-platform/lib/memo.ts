export type MemoPhase =
  | "Draft"
  | "TranslationPending"
  | "ReadyForSubmit"
  | "SpecialistCoordination"
  | "AwaitingDeptHeadApproval"
  | "NeedsRevision"
  | "Registered"
  | "AwaitingTopManagement"
  | "RoutedToDepartment"
  | "AwaitingAcceptance"
  | "InExecution"
  | "AwaitingReview"
  | "ExecutionNeedsRevision"
  | "AwaitingArchive"
  | "Completed";

export const MEMO_PHASES: MemoPhase[] = [
  "Draft",
  "TranslationPending",
  "ReadyForSubmit",
  "SpecialistCoordination",
  "AwaitingDeptHeadApproval",
  "NeedsRevision",
  "Registered",
  "AwaitingTopManagement",
  "RoutedToDepartment",
  "AwaitingAcceptance",
  "InExecution",
  "AwaitingReview",
  "ExecutionNeedsRevision",
  "AwaitingArchive",
  "Completed",
];

export const MEMO_STEP_GROUPS: { key: string; phases: MemoPhase[] }[] = [
  { key: "draft", phases: ["Draft", "TranslationPending", "ReadyForSubmit", "NeedsRevision"] },
  { key: "approval", phases: ["SpecialistCoordination", "AwaitingDeptHeadApproval"] },
  { key: "distribution", phases: ["Registered", "AwaitingTopManagement"] },
  {
    key: "execution",
    phases: [
      "RoutedToDepartment",
      "AwaitingAcceptance",
      "InExecution",
      "AwaitingReview",
      "ExecutionNeedsRevision",
    ],
  },
  { key: "completion", phases: ["AwaitingArchive", "Completed"] },
];

export interface MemoUser {
  id: string;
  fullName: string;
  email: string;
  employeeId?: string;
  departmentName: string;
  departmentNameEn: string;
}

export interface MemoDepartment {
  id: string;
  code: string;
  name: string;
  nameEn: string;
}

export interface MemoRecipient {
  id: string;
  userId?: string;
  userName?: string;
  departmentId?: string;
  departmentName?: string;
  departmentNameEn?: string;
  forInformation: boolean;
  notifiedAt?: string;
}

export interface MemoCoordinator {
  id: string;
  userId: string;
  userName: string;
  coordinatedAt?: string;
}

export interface MemoComment {
  id: string;
  authorId: string;
  authorName: string;
  body: string;
  createdAt: string;
}

export interface Memo {
  id: string;
  number: string;
  title: string;
  titleRu?: string;
  status: string;
  phase: MemoPhase;
  authorId: string;
  authorName: string;
  attachmentFileName?: string;
  attachmentStorageKey?: string;
  translatedAttachmentFileName?: string;
  translatedAttachmentStorageKey?: string;
  requiresTranslation: boolean;
  sourceLanguage?: string;
  translatingLanguages?: string[];
  helpDeskTicketId?: string;
  helpDeskTicketNumber?: string;
  deptHeadId?: string;
  deptHeadName?: string;
  requiresTopManagementResolution: boolean;
  resolutionManagerId?: string;
  resolutionManagerName?: string;
  routedToDepartmentId?: string;
  routedDepartmentName?: string;
  assigneeId?: string;
  assigneeName?: string;
  assignmentTask?: string;
  dueDate?: string;
  requiresResponse: boolean;
  revisionNotes?: string;
  recipients: MemoRecipient[];
  coordinators: MemoCoordinator[];
  comments: MemoComment[];
  createdAt: string;
  updatedAt: string;
}

export interface MemoPermissions {
  isInitiator: boolean;
  isDeptHead: boolean;
  isResolutionManager: boolean;
  isRecipient: boolean;
  isAssignee: boolean;
  isRoutedDeptManager: boolean;
  canCreate: boolean;
  canEditDraft: boolean;
  canSendToTranslation: boolean;
  canSubmitForApproval: boolean;
  canManageSpecialistCoordination: boolean;
  canApproveDeptHead: boolean;
  canRejectDeptHead: boolean;
  canRegisterAndDistribute: boolean;
  canActAsTopManagement: boolean;
  canInformRecipients: boolean;
  canRouteToDepartment: boolean;
  canAssignWorker: boolean;
  canAcceptExecution: boolean;
  canReportCompletion: boolean;
  canRequestRevision: boolean;
  canAcceptCompletion: boolean;
  canArchive: boolean;
  canView: boolean;
}

export function memoPhaseLabel(phase: MemoPhase, locale: string) {
  const en: Record<MemoPhase, string> = {
    Draft: "Draft",
    TranslationPending: "Translation",
    ReadyForSubmit: "Ready to submit",
    SpecialistCoordination: "Specialist coordination",
    AwaitingDeptHeadApproval: "Dept head approval",
    NeedsRevision: "Needs revision",
    Registered: "Approved — register",
    AwaitingTopManagement: "Top management",
    RoutedToDepartment: "Routed to department",
    AwaitingAcceptance: "Awaiting acceptance",
    InExecution: "In execution",
    AwaitingReview: "Awaiting review",
    ExecutionNeedsRevision: "Revision required",
    AwaitingArchive: "Archive",
    Completed: "Completed",
  };
  const ru: Record<MemoPhase, string> = {
    Draft: "Проект",
    TranslationPending: "Перевод",
    ReadyForSubmit: "Готово к отправке",
    SpecialistCoordination: "Согласование специалистов",
    AwaitingDeptHeadApproval: "Согласование начальника",
    NeedsRevision: "Доработка",
    Registered: "Согласовано — регистрация",
    AwaitingTopManagement: "Высшее руководство",
    RoutedToDepartment: "В подразделении",
    AwaitingAcceptance: "Ожидает принятия",
    InExecution: "На исполнении",
    AwaitingReview: "На проверке",
    ExecutionNeedsRevision: "Доработка",
    AwaitingArchive: "Архив",
    Completed: "Завершено",
  };
  return (locale.startsWith("en") ? en : ru)[phase] ?? phase;
}

export function memoStepLabel(key: string, locale: string) {
  const en: Record<string, string> = {
    draft: "Draft & translation",
    approval: "Approval",
    distribution: "Registration",
    execution: "Execution",
    completion: "Completion",
  };
  const ru: Record<string, string> = {
    draft: "Проект и перевод",
    approval: "Согласование",
    distribution: "Регистрация",
    execution: "Исполнение",
    completion: "Завершение",
  };
  return (locale.startsWith("en") ? en : ru)[key] ?? key;
}

export function currentMemoStepIndex(phase: MemoPhase) {
  const idx = MEMO_STEP_GROUPS.findIndex((g) => g.phases.includes(phase));
  return idx >= 0 ? idx : 0;
}

export {
  TRANSLATION_LANGUAGE_CODES,
  translationLanguageLabel,
  translationLanguagesLabel,
} from "@/lib/incomingLetter";
