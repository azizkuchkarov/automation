export type WorkTaskStatus = "New" | "InProgress" | "Done" | "Cancelled";
export type TaskPriority = "Low" | "Medium" | "High" | "Critical";
export type TaskSource = "Manual" | "HelpDesk" | "DCS" | "HR";

export interface TaskListItem {
  id: string;
  number: string;
  title: string;
  status: WorkTaskStatus;
  priority: TaskPriority;
  source: TaskSource;
  isEditable: boolean;
  externalId?: string;
  assigneeName: string;
  assigneeId: string;
  departmentName: string;
  departmentNameEn: string;
  createdByName: string;
  dueDate?: string;
  createdAt: string;
  updatedAt: string;
}

export interface TaskStatusSlice {
  status: WorkTaskStatus;
  count: number;
  percent: number;
}

export interface TaskSourceSlice {
  source: TaskSource;
  count: number;
  percent: number;
}

export interface TaskTrendPoint {
  label: string;
  new: number;
  inProgress: number;
  done: number;
}

export interface EmployeeTaskSummary {
  userId: string;
  fullName: string;
  employeeId?: string;
  newCount: number;
  inProgressCount: number;
  doneCount: number;
  total: number;
}

export interface TaskAnalytics {
  scope: "personal" | "department" | "organization";
  scopeLabel: string;
  organizationId?: string;
  departmentId?: string;
  totalNew: number;
  totalInProgress: number;
  totalDone: number;
  totalCancelled: number;
  totalActive: number;
  completionRate: number;
  statusDistribution: TaskStatusSlice[];
  bySource: TaskSourceSlice[];
  weeklyTrend: TaskTrendPoint[];
  recentTasks: TaskListItem[];
  byEmployee?: EmployeeTaskSummary[];
}

export interface TaskNavigationUnit {
  id: string;
  name: string;
  nameEn: string;
  code: string;
  unitType: "department" | "station";
  organizationId: string;
  taskCount: number;
  children: TaskNavigationUnit[];
}

export interface TaskNavigationOrg {
  id: string;
  name: string;
  code: string;
  orgType: string;
  taskCount: number;
  units: TaskNavigationUnit[];
}

export interface TaskNavigationDto {
  organizations: TaskNavigationOrg[];
}

export const STATUS_COLORS: Record<WorkTaskStatus, string> = {
  New: "#64748b",
  InProgress: "#2563eb",
  Done: "#059669",
  Cancelled: "#94a3b8",
};

export const SOURCE_COLORS: Record<TaskSource, string> = {
  Manual: "#d97706",
  HelpDesk: "#0d9488",
  DCS: "#7c3aed",
  HR: "#2563eb",
};

export function statusLabel(status: WorkTaskStatus, t: (k: string) => string) {
  if (status === "InProgress") return t("status.InProgress");
  return t(`status.${status}`);
}

export function sourceLabel(source: TaskSource, t: (k: string) => string) {
  return t(`sources.${source}`);
}

export function isDeptManager(role: string) {
  return ["HONachalnik", "BMGMCNachalnikiOtdeli", "BMGMCManager", "SuperAdmin", "HOTopManager"].includes(role);
}

export function canUseOrgNav(role: string) {
  return role === "SuperAdmin" || role === "HOTopManager" || role === "BMGMCManager";
}

export function statusBadgeClass(status: WorkTaskStatus) {
  switch (status) {
    case "New": return "bg-slate-500/12 text-slate-600 dark:text-slate-300 border-slate-500/25";
    case "InProgress": return "bg-atg-blue/12 text-blue-700 dark:text-blue-300 border-atg-blue/25";
    case "Done": return "bg-emerald-500/12 text-emerald-700 dark:text-emerald-300 border-emerald-500/25";
    default: return "bg-border/50 text-foreground/50 border-border";
  }
}

export function sourceBadgeClass(source: TaskSource) {
  switch (source) {
    case "HelpDesk": return "bg-atg-teal/12 text-atg-teal border-atg-teal/25";
    case "DCS": return "bg-atg-purple/12 text-violet-600 dark:text-violet-300 border-violet-500/25";
    case "HR": return "bg-atg-blue/12 text-blue-700 dark:text-blue-300 border-atg-blue/25";
    default: return "bg-atg-amber/12 text-atg-amber border-atg-amber/25";
  }
}

export function priorityBadgeClass(p: TaskPriority) {
  switch (p) {
    case "Critical": return "text-red-600 dark:text-red-400 bg-red-500/12";
    case "High": return "text-orange-600 dark:text-orange-400 bg-orange-500/12";
    case "Medium": return "text-atg-blue bg-atg-blue/12";
    default: return "text-foreground/50 bg-border/40";
  }
}

export function buildAnalyticsParams(scope: {
  organizationId?: string;
  departmentId?: string;
}) {
  const params = new URLSearchParams();
  if (scope.departmentId) params.set("departmentId", scope.departmentId);
  else if (scope.organizationId) params.set("organizationId", scope.organizationId);
  return params;
}
