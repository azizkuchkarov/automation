export type IncomingLetterPhase =
  | "Registered"
  | "Informed"
  | "RoutedToDepartment"
  | "InExecution"
  | "Completed";

export interface IncomingLetterRecipient {
  id: string;
  userId: string;
  userName: string;
  informed: boolean;
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
  translationRequestCount: number;
  departmentName: string;
  departmentNameEn: string;
  assigneeName?: string;
  routedToDepartmentName?: string;
  routedToDepartmentNameEn?: string;
  routedByName?: string;
  registeredAt?: string;
  informedAt?: string;
  routedAt?: string;
  completedAt?: string;
  recipients: IncomingLetterRecipient[];
  comments: IncomingLetterComment[];
  createdAt: string;
  updatedAt: string;
}

export interface IncomingLetterPermissions {
  isRegistrar: boolean;
  canInform: boolean;
  canRoute: boolean;
  canAssign: boolean;
  canComplete: boolean;
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

export function phaseLabel(phase: IncomingLetterPhase, locale: string) {
  const ru: Record<IncomingLetterPhase, string> = {
    Registered: "Зарегистрировано",
    Informed: "Направлено руководству",
    RoutedToDepartment: "Направлено в подразделение",
    InExecution: "Исполнение",
    Completed: "Завершено",
  };
  const en: Record<IncomingLetterPhase, string> = {
    Registered: "Registered",
    Informed: "Informed to top management",
    RoutedToDepartment: "Routed to department",
    InExecution: "In execution",
    Completed: "Completed",
  };
  return locale.startsWith("en") ? en[phase] : ru[phase];
}
