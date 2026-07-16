export type OutgoingLetterPhase =
  | "Draft"
  | "TranslationPending"
  | "ReadyForEds"
  | "AwaitingDeptHeadApproval"
  | "NeedsRevision"
  | "SpecialistCoordination"
  | "DepartmentCoordination"
  | "AwaitingSupervisingDeputyApproval"
  | "AwaitingFirstDeputyApproval"
  | "AwaitingGeneralDirectorApproval"
  | "EdsFinalized"
  | "AwaitingRegistration"
  | "AwaitingPaperSignature"
  | "AwaitingDispatch"
  | "AwaitingArchive"
  | "Completed";

export const OUTGOING_LETTER_PHASES: OutgoingLetterPhase[] = [
  "Draft",
  "TranslationPending",
  "ReadyForEds",
  "AwaitingDeptHeadApproval",
  "NeedsRevision",
  "SpecialistCoordination",
  "DepartmentCoordination",
  "AwaitingSupervisingDeputyApproval",
  "AwaitingFirstDeputyApproval",
  "AwaitingGeneralDirectorApproval",
  "EdsFinalized",
  "AwaitingRegistration",
  "AwaitingPaperSignature",
  "AwaitingDispatch",
  "AwaitingArchive",
  "Completed",
];

export const OUTGOING_STEP_GROUPS: { key: string; phases: OutgoingLetterPhase[] }[] = [
  { key: "draft", phases: ["Draft", "TranslationPending", "ReadyForEds", "NeedsRevision"] },
  { key: "deptApproval", phases: ["AwaitingDeptHeadApproval"] },
  { key: "coordination", phases: ["SpecialistCoordination", "DepartmentCoordination"] },
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
      "AwaitingDispatch",
      "AwaitingArchive",
      "Completed",
    ],
  },
];

export interface OutgoingLetterUser {
  id: string;
  fullName: string;
  email: string;
  employeeId?: string;
  departmentName: string;
  departmentNameEn: string;
}

export interface OutgoingLetterCoordinator {
  id: string;
  userId: string;
  userName: string;
  forDepartment: boolean;
  coordinatedAt: string;
}

export interface OutgoingLetter {
  id: string;
  number: string;
  title: string;
  titleRu?: string;
  status: string;
  phase: OutgoingLetterPhase;
  authorId: string;
  authorName: string;
  addresseeName?: string;
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
  supervisingDeputyId?: string;
  supervisingDeputyName?: string;
  firstDeputyId?: string;
  firstDeputyName?: string;
  generalDirectorId?: string;
  generalDirectorName?: string;
  revisionNotes?: string;
  coordinators: OutgoingLetterCoordinator[];
  createdAt: string;
  updatedAt: string;
}

export interface OutgoingLetterPermissions {
  isInitiator: boolean;
  isRegistrar: boolean;
  isDeptHead: boolean;
  canCreate: boolean;
  canEditDraft: boolean;
  canSendToTranslation: boolean;
  canSubmitToEds: boolean;
  canApproveDeptHead: boolean;
  canRejectDeptHead: boolean;
  canManageSpecialistCoordination: boolean;
  canManageDepartmentCoordination: boolean;
  canSkipCoordination: boolean;
  canApproveSupervisingDeputy: boolean;
  canApproveFirstDeputy: boolean;
  canApproveGeneralDirector: boolean;
  canFinalizeEds: boolean;
  canSendToRegistrar: boolean;
  canRegister: boolean;
  canConfirmPaperSignature: boolean;
  canConfirmDispatch: boolean;
  canArchive: boolean;
  canView: boolean;
}

export function outgoingPhaseLabel(phase: OutgoingLetterPhase, locale: string) {
  const en: Record<OutgoingLetterPhase, string> = {
    Draft: "Draft",
    TranslationPending: "Translation",
    ReadyForEds: "Ready for EDS",
    AwaitingDeptHeadApproval: "Dept head approval",
    NeedsRevision: "Needs revision",
    SpecialistCoordination: "Specialist coordination",
    DepartmentCoordination: "Department coordination",
    AwaitingSupervisingDeputyApproval: "Supervising deputy",
    AwaitingFirstDeputyApproval: "First deputy",
    AwaitingGeneralDirectorApproval: "General director",
    EdsFinalized: "EDS finalized",
    AwaitingRegistration: "Registration",
    AwaitingPaperSignature: "Paper signature",
    AwaitingDispatch: "Dispatch",
    AwaitingArchive: "Archive",
    Completed: "Completed",
  };
  const ru: Record<OutgoingLetterPhase, string> = {
    Draft: "Проект",
    TranslationPending: "Перевод",
    ReadyForEds: "Готово к ЭДО",
    AwaitingDeptHeadApproval: "Согласование начальника",
    NeedsRevision: "Доработка",
    SpecialistCoordination: "Согласование специалистов",
    DepartmentCoordination: "Согласование департаментов",
    AwaitingSupervisingDeputyApproval: "Курирующий зам.",
    AwaitingFirstDeputyApproval: "Первый зам.",
    AwaitingGeneralDirectorApproval: "Генеральный директор",
    EdsFinalized: "ЭДО завершено",
    AwaitingRegistration: "Регистрация",
    AwaitingPaperSignature: "Подпись на бумаге",
    AwaitingDispatch: "Отправка",
    AwaitingArchive: "Архив",
    Completed: "Завершено",
  };
  return (locale.startsWith("en") ? en : ru)[phase] ?? phase;
}

export function outgoingStepLabel(key: string, locale: string) {
  const en: Record<string, string> = {
    draft: "Draft & translation",
    deptApproval: "Dept head",
    coordination: "Coordination",
    leadership: "Leadership approval",
    registration: "Registration & dispatch",
  };
  const ru: Record<string, string> = {
    draft: "Проект и перевод",
    deptApproval: "Начальник отдела",
    coordination: "Согласование",
    leadership: "Руководство",
    registration: "Регистрация и отправка",
  };
  return (locale.startsWith("en") ? en : ru)[key] ?? key;
}

export function currentOutgoingStepIndex(phase: OutgoingLetterPhase) {
  const idx = OUTGOING_STEP_GROUPS.findIndex((g) => g.phases.includes(phase));
  return idx >= 0 ? idx : 0;
}

export {
  TRANSLATION_LANGUAGE_CODES,
  translationLanguageLabel,
  translationLanguagesLabel,
} from "@/lib/incomingLetter";
