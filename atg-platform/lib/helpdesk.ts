export type TicketCategory = "IT" | "Administration" | "Accountant" | "Transport" | "TravelTickets" | "Translator";
export type TicketStatus = "Open" | "Assigned" | "Accepted" | "InProgress" | "Done" | "Closed" | "Cancelled";
export type TicketPriority = "Low" | "Medium" | "High" | "Critical";

export const TICKET_CATEGORIES: TicketCategory[] = [
  "IT",
  "Administration",
  "Accountant",
  "Transport",
  "TravelTickets",
  "Translator",
];

export const CATEGORY_SLUG: Record<TicketCategory, string> = {
  IT: "it",
  Administration: "administration",
  Accountant: "accountant",
  Transport: "transport",
  TravelTickets: "travel",
  Translator: "translator",
};

export function categoryFromSlug(slug: string): TicketCategory | null {
  const match = Object.entries(CATEGORY_SLUG).find(([, value]) => value === slug);
  return match ? (match[0] as TicketCategory) : null;
}

export function categorySlug(category: TicketCategory): string {
  return CATEGORY_SLUG[category];
}

export function categoryPath(locale: string, category: TicketCategory, section: "board" | "tickets" | "queue" | "new") {
  const slug = categorySlug(category);
  if (section === "queue") return `/${locale}/helpdesk/${slug}/tickets?view=queue`;
  return `/${locale}/helpdesk/${slug}/${section}`;
}

export function countActiveTickets(board: TicketBoard): number {
  return board.open.length + board.assigned.length + board.accepted.length + board.inProgress.length;
}

export interface TicketListItem {
  id: string;
  number: string;
  title: string;
  category: TicketCategory;
  status: TicketStatus;
  priority: TicketPriority;
  requesterName: string;
  assigneeName?: string;
  targetDepartmentName: string;
  targetDepartmentNameEn: string;
  createdAt: string;
  updatedAt: string;
}

export interface TicketComment {
  id: string;
  authorId: string;
  authorName: string;
  body: string;
  isInternal: boolean;
  createdAt: string;
}

export interface TicketActivity {
  id: string;
  actorId: string;
  actorName: string;
  action: string;
  fromStatus?: TicketStatus;
  toStatus?: TicketStatus;
  details?: string;
  createdAt: string;
}

export interface Ticket extends TicketListItem {
  description: string;
  requesterId: string;
  requesterEmail: string;
  organizationId: string;
  organizationName: string;
  targetDepartmentId: string;
  assigneeId?: string;
  assignedById?: string;
  assignedByName?: string;
  assignedAt?: string;
  acceptedAt?: string;
  startedAt?: string;
  completedAt?: string;
  closedAt?: string;
  sourceLanguage?: string;
  translatingLanguages?: string[];
  linkedDocumentId?: string;
  linkedOriginalFileName?: string;
  linkedOriginalStorageKey?: string;
  linkedTranslatedFileName?: string;
  linkedTranslatedStorageKey?: string;
  comments: TicketComment[];
  activities: TicketActivity[];
}

export interface TicketBoard {
  open: TicketListItem[];
  assigned: TicketListItem[];
  accepted: TicketListItem[];
  inProgress: TicketListItem[];
  done: TicketListItem[];
  closed: TicketListItem[];
}

export interface HelpDeskCategory {
  category: TicketCategory;
  nameEn: string;
  nameRu: string;
  icon: string;
  color: string;
}

export const STATUS_ORDER: TicketStatus[] = [
  "Open", "Assigned", "Accepted", "InProgress", "Done", "Closed",
];

export const BOARD_COLUMNS: { key: keyof TicketBoard; status: TicketStatus }[] = [
  { key: "open", status: "Open" },
  { key: "assigned", status: "Assigned" },
  { key: "accepted", status: "Accepted" },
  { key: "inProgress", status: "InProgress" },
  { key: "done", status: "Done" },
];

export function categoryLabel(c: HelpDeskCategory, locale: string) {
  return locale.startsWith("en") ? c.nameEn : c.nameRu;
}

export function deptLabel(name: string, nameEn: string, locale: string) {
  return locale.startsWith("en") && nameEn ? nameEn : name;
}

export function priorityColor(p: TicketPriority) {
  switch (p) {
    case "Critical": return "text-red-600 dark:text-red-400 bg-red-500/12";
    case "High": return "text-orange-600 dark:text-orange-400 bg-orange-500/12";
    case "Medium": return "text-atg-blue bg-atg-blue/12";
    default: return "text-foreground/50 bg-border/40";
  }
}

export function statusColor(s: TicketStatus) {
  switch (s) {
    case "Open": return "bg-slate-500/12 text-slate-600 dark:text-slate-300 border-slate-500/25";
    case "Assigned": return "bg-violet-500/12 text-violet-700 dark:text-violet-300 border-violet-500/25";
    case "Accepted": return "bg-indigo-500/12 text-indigo-700 dark:text-indigo-300 border-indigo-500/25";
    case "InProgress": return "bg-atg-blue/12 text-blue-700 dark:text-blue-300 border-atg-blue/25";
    case "Done": return "bg-emerald-500/12 text-emerald-700 dark:text-emerald-300 border-emerald-500/25";
    case "Closed": return "bg-border/50 text-foreground/50 border-border";
    default: return "bg-border/40 text-foreground/50";
  }
}

export function categoryIconColor(c: TicketCategory) {
  switch (c) {
    case "IT": return "text-atg-teal bg-atg-teal/10";
    case "Administration": return "text-violet-600 dark:text-violet-400 bg-violet-500/10";
    case "Accountant": return "text-emerald-600 dark:text-emerald-400 bg-emerald-500/10";
    case "Transport": return "text-orange-600 dark:text-orange-400 bg-orange-500/10";
    case "TravelTickets": return "text-sky-600 dark:text-sky-400 bg-sky-500/10";
    case "Translator": return "text-atg-purple bg-atg-purple/10";
    default: return "text-foreground/50 bg-border/40";
  }
}

export interface HelpDeskAssignee {
  id: string;
  fullName: string;
  email: string;
  role: string;
}

export interface HelpDeskStaff {
  id: string;
  employeeId?: string;
  fullName: string;
  email: string;
  role: string;
  jobTitleEn?: string;
  jobTitleRu?: string;
}

export interface HelpDeskOrgRouting {
  organizationCode: string;
  organizationName: string;
  departmentId: string;
  departmentCode: string;
  departmentName: string;
  departmentNameEn: string;
  openTickets: number;
  activeTickets: number;
  assigners: HelpDeskStaff[];
  engineers: HelpDeskStaff[];
}

export interface HelpDeskCategoryRouting {
  category: TicketCategory;
  nameEn: string;
  nameRu: string;
  icon: string;
  color: string;
  routes: HelpDeskOrgRouting[];
}

export interface HelpDeskDashboard {
  totalOpen: number;
  totalInProgress: number;
  totalDone: number;
  totalClosed: number;
  recentTickets: TicketListItem[];
}

export interface HelpDeskAdminControl {
  dashboard: HelpDeskDashboard;
  categories: HelpDeskCategoryRouting[];
}

export function isPlatformAdmin(role: string) {
  return role === "SuperAdmin" || role === "HOTopManager";
}

export function isDeptManager(role: string) {
  return ["HONachalnik", "BMGMCNachalnikiOtdeli", "BMGMCManager", "SuperAdmin", "HOTopManager"].includes(role);
}

export function canAssignTicket(
  user: { role: string; departmentId?: string },
  ticket: { targetDepartmentId: string; status: TicketStatus }
) {
  if (!user) return false;
  if (isPlatformAdmin(user.role)) return ticket.status === "Open" || ticket.status === "Assigned";
  return (
    isDeptManager(user.role) &&
    user.departmentId === ticket.targetDepartmentId &&
    (ticket.status === "Open" || ticket.status === "Assigned")
  );
}

export function canManageWorkflow(
  user: { id: string; role: string } | null | undefined,
  ticket: { assigneeId?: string; status: TicketStatus }
) {
  if (!user) return false;
  if (isPlatformAdmin(user.role)) {
    return ["Assigned", "Accepted", "InProgress"].includes(ticket.status);
  }
  return user.id === ticket.assigneeId;
}
