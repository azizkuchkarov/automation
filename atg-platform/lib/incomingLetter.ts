export type IncomingLetterPhase =

  | "Received"

  | "TranslationPending"

  | "ReadyForRegistration"

  | "Registered"

  | "AwaitingResolution"

  | "RoutedToDepartment"

  | "AwaitingAcceptance"

  | "InExecution"

  | "AwaitingReview"

  | "NeedsRevision"

  | "AwaitingArchive"

  | "Completed";



export interface IncomingLetterRecipient {

  id: string;

  userId: string;

  userName: string;

  informed: boolean;

  forInformation: boolean;

  informedAt?: string;

  taskId?: string;

}



export interface IncomingLetterComment {

  id: string;

  authorId: string;

  authorName: string;

  body: string;

  createdAt: string;

}



export interface IncomingLetter {

  id: string;

  number: string;

  title: string;

  titleRu?: string;

  status: string;

  phase: IncomingLetterPhase;

  authorId: string;

  authorName: string;

  incomingNumber?: string;

  incomingDate?: string;

  recordBook?: string;

  senderName?: string;

  receiverName?: string;

  attachmentFileName?: string;

  attachmentStorageKey?: string;

  translationRequestCount: number;

  departmentName: string;

  departmentNameEn: string;

  assigneeName?: string;

  requiresTranslation: boolean;
  sourceLanguage?: string;
  translatingLanguages?: string[];
  helpDeskTicketId?: string;
  helpDeskTicketNumber?: string;
  translatedAttachmentFileName?: string;
  translatedAttachmentStorageKey?: string;

  resolutionManagerId?: string;

  resolutionManagerName?: string;

  routedToDepartmentName?: string;

  routedToDepartmentNameEn?: string;

  routedByName?: string;

  assignmentTask?: string;

  dueDate?: string;

  requiresResponse: boolean;

  registeredAt?: string;

  sentToTranslationAt?: string;

  translationReturnedAt?: string;

  sentForResolutionAt?: string;

  informedAt?: string;

  routedAt?: string;

  executorAcceptedAt?: string;

  reportedAt?: string;

  reviewedAt?: string;

  archivedAt?: string;

  completedAt?: string;

  recipients: IncomingLetterRecipient[];

  comments: IncomingLetterComment[];

  createdAt: string;

  updatedAt: string;

}



export interface IncomingLetterPermissions {

  isRegistrar: boolean;

  isTranslationDept: boolean;

  isResolutionManager: boolean;

  canSendToTranslation: boolean;

  canCompleteTranslation: boolean;

  canRegisterInEds: boolean;

  canSendForResolution: boolean;

  canInformAdditional: boolean;

  canRoute: boolean;

  canAssign: boolean;

  canAccept: boolean;

  canReport: boolean;

  canRequestRevision: boolean;

  canAcceptCompletion: boolean;

  canArchive: boolean;

  canComment: boolean;

}



export interface IncomingLetterUser {

  id: string;

  fullName: string;

  email: string;

  departmentName: string;

  departmentNameEn: string;

}



export interface IncomingLetterDepartment {

  id: string;

  code: string;

  name: string;

  nameEn: string;

}



export const INCOMING_LETTER_PHASES: IncomingLetterPhase[] = [

  "Received",

  "TranslationPending",

  "ReadyForRegistration",

  "Registered",

  "AwaitingResolution",

  "RoutedToDepartment",

  "AwaitingAcceptance",

  "InExecution",

  "AwaitingReview",

  "NeedsRevision",

  "AwaitingArchive",

  "Completed",

];



export const INCOMING_LETTER_STEPS: { id: string; phases: IncomingLetterPhase[] }[] = [

  { id: "intake", phases: ["Received", "TranslationPending", "ReadyForRegistration"] },

  { id: "registered", phases: ["Registered"] },

  { id: "resolution", phases: ["AwaitingResolution"] },

  { id: "department", phases: ["RoutedToDepartment"] },

  { id: "assignment", phases: ["AwaitingAcceptance"] },

  { id: "execution", phases: ["InExecution", "NeedsRevision"] },

  { id: "review", phases: ["AwaitingReview"] },

  { id: "archive", phases: ["AwaitingArchive"] },

  { id: "completed", phases: ["Completed"] },

];



export function currentStepIndex(phase: IncomingLetterPhase): number {

  const idx = INCOMING_LETTER_STEPS.findIndex((s) => s.phases.includes(phase));

  return idx >= 0 ? idx : 0;

}



export function phaseLabel(phase: IncomingLetterPhase, locale: string) {

  const ru: Record<IncomingLetterPhase, string> = {

    Received: "Поступило",

    TranslationPending: "На переводе",

    ReadyForRegistration: "Готово к регистрации",

    Registered: "Зарегистрировано",

    AwaitingResolution: "На резолюции",

    RoutedToDepartment: "В подразделении",

    AwaitingAcceptance: "Ожидает принятия",

    InExecution: "Исполнение",

    AwaitingReview: "На проверке",

    NeedsRevision: "Доработка",

    AwaitingArchive: "К архивации",

    Completed: "Завершено",

  };

  const en: Record<IncomingLetterPhase, string> = {

    Received: "Received",

    TranslationPending: "Translation",

    ReadyForRegistration: "Ready for registration",

    Registered: "Registered in EDS",

    AwaitingResolution: "Awaiting resolution",

    RoutedToDepartment: "Routed to department",

    AwaitingAcceptance: "Awaiting acceptance",

    InExecution: "In execution",

    AwaitingReview: "Awaiting review",

    NeedsRevision: "Needs revision",

    AwaitingArchive: "Awaiting archive",

    Completed: "Completed",

  };

  return locale.startsWith("en") ? en[phase] : ru[phase];

}



export function stepLabel(stepId: string, locale: string) {

  const ru: Record<string, string> = {

    intake: "Поступление",

    registered: "Регистрация в ЭДО",

    resolution: "Резолюция",

    department: "Подразделение",

    assignment: "Назначение",

    execution: "Исполнение",

    review: "Проверка",

    archive: "Архив",

    completed: "Завершено",

  };

  const en: Record<string, string> = {

    intake: "Intake",

    registered: "EDS registration",

    resolution: "Resolution",

    department: "Department",

    assignment: "Assignment",

    execution: "Execution",

    review: "Review",

    archive: "Archive",

    completed: "Completed",

  };

  return locale.startsWith("en") ? en[stepId] : ru[stepId];

}



export function phaseHint(phase: IncomingLetterPhase, locale: string) {

  const ru: Record<IncomingLetterPhase, string> = {

    Received: "Письмо поступило, решите вопрос о переводе",

    TranslationPending: "Документ в отделе технических переводов",

    ReadyForRegistration: "Загрузите документ в ЭДО и присвойте номер",

    Registered: "Направьте ответственному члену высшего руководства",

    AwaitingResolution: "Руководитель распределяет задачи по подразделениям",

    RoutedToDepartment: "Руководитель подразделения назначает исполнителя",

    AwaitingAcceptance: "Исполнитель должен принять документ",

    InExecution: "Исполнитель выполняет поручение",

    AwaitingReview: "Руководитель проверяет выполнение",

    NeedsRevision: "Исполнитель дорабатывает по комментариям",

    AwaitingArchive: "Делопроизводитель архивирует в ЭДО",

    Completed: "Процесс завершён",

  };

  const en: Record<IncomingLetterPhase, string> = {

    Received: "Letter received — decide if translation is needed",

    TranslationPending: "Document is at technical translations",

    ReadyForRegistration: "Upload to EDS and assign internal number",

    Registered: "Send to responsible top manager for resolution",

    AwaitingResolution: "Manager routes tasks to departments",

    RoutedToDepartment: "Department head assigns executor",

    AwaitingAcceptance: "Executor must accept the document",

    InExecution: "Executor is working on the task",

    AwaitingReview: "Manager reviews completion",

    NeedsRevision: "Executor revises per comments",

    AwaitingArchive: "Registrar archives in EDS",

    Completed: "Process completed",

  };

  return locale.startsWith("en") ? en[phase] : ru[phase];

}



export function incomingPhaseLabel(phase: IncomingLetterPhase, locale: string) {

  return phaseLabel(phase, locale);

}



export const TRANSLATION_LANGUAGE_CODES = [

  "en", "ru", "uz", "zh", "ko", "fr", "de", "ar", "tr", "other",

] as const;



export function translationLanguageLabel(code: string, locale: string) {

  const ru: Record<string, string> = {

    en: "Английский", ru: "Русский", uz: "Узбекский", zh: "Китайский", ko: "Корейский",

    fr: "Французский", de: "Немецкий", ar: "Арабский", tr: "Турецкий", other: "Другой",

  };

  const en: Record<string, string> = {

    en: "English", ru: "Russian", uz: "Uzbek", zh: "Chinese", ko: "Korean",

    fr: "French", de: "German", ar: "Arabic", tr: "Turkish", other: "Other",

  };

  const map = locale.startsWith("en") ? en : ru;

  return map[code] ?? code;

}



export function translationLanguagesLabel(codes: string[] | undefined, locale: string) {

  if (!codes?.length) return "—";

  return codes.map((code) => translationLanguageLabel(code, locale)).join(", ");

}


