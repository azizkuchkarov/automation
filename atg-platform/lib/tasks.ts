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

export interface TaskPrioritySlice {
  priority: TaskPriority;
  count: number;
  percent: number;
}

export interface TaskAgingBucket {
  key: string;
  count: number;
  percent: number;
  minDays: number;
  maxDays?: number;
}

export interface TaskVelocityPoint {
  label: string;
  completed: number;
  movingAverage: number;
}

export interface TaskInsight {
  code: string;
  severity: "good" | "warning" | "info";
  value?: number;
  context?: string;
}

export interface TaskHealthScore {
  score: number;
  grade: string;
  completionComponent: number;
  slaComponent: number;
  velocityComponent: number;
  balanceComponent: number;
  riskPenalty: number;
}

export interface TaskSlaMetrics {
  compliancePercent: number;
  withDueDate: number;
  onTime: number;
  late: number;
  atRisk: number;
}

export interface TaskCycleTime {
  p50Days: number;
  p75Days: number;
  p90Days: number;
  meanDays: number;
}

export interface TaskHeatmapCell {
  dayOfWeek: number;
  label: string;
  created: number;
  completed: number;
  intensity: number;
}

export interface TaskForecastPoint {
  label: string;
  actual: number;
  forecast?: number;
  isProjected: boolean;
}

export interface TaskBurndownPoint {
  label: string;
  remaining: number;
  ideal: number;
  completed: number;
}

export interface TaskRiskItem {
  id: string;
  number: string;
  title: string;
  assigneeName: string;
  priority: TaskPriority;
  riskScore: number;
  riskLevel: "low" | "medium" | "high" | "critical";
  ageDays: number;
  isOverdue: boolean;
}

export interface TaskWorkloadBalance {
  balanceScore: number;
  giniCoefficient: number;
  assigneeCount: number;
  avgLoad: number;
  maxLoad: number;
}

export interface TaskPriorityStatusCell {
  priority: TaskPriority;
  status: WorkTaskStatus;
  count: number;
}

export interface EmployeeTaskSummary {
  userId: string;
  fullName: string;
  employeeId?: string;
  newCount: number;
  inProgressCount: number;
  doneCount: number;
  total: number;
  completionRate: number;
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
  byPriority: TaskPrioritySlice[];
  agingBuckets: TaskAgingBucket[];
  weeklyTrend: TaskTrendPoint[];
  velocityTrend: TaskVelocityPoint[];
  overdueCount: number;
  avgResolutionDays: number;
  throughputChangePercent: number;
  insights: TaskInsight[];
  healthScore?: TaskHealthScore;
  slaMetrics?: TaskSlaMetrics;
  cycleTime?: TaskCycleTime;
  activityHeatmap?: TaskHeatmapCell[];
  completionForecast?: TaskForecastPoint[];
  burndown?: TaskBurndownPoint[];
  riskQueue?: TaskRiskItem[];
  workloadBalance?: TaskWorkloadBalance;
  priorityMatrix?: TaskPriorityStatusCell[];
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

export const PRIORITY_COLORS: Record<TaskPriority, string> = {
  Low: "#94a3b8",
  Medium: "#2563eb",
  High: "#f97316",
  Critical: "#dc2626",
};

export const RISK_COLORS: Record<TaskRiskItem["riskLevel"], string> = {
  low: "#94a3b8",
  medium: "#2563eb",
  high: "#f97316",
  critical: "#dc2626",
};

export const HEATMAP_SCALE = ["#f1f5f9", "#bfdbfe", "#60a5fa", "#2563eb", "#1e3a8a"];

export const AGING_COLORS: Record<string, string> = {
  "0_3": "#10b981",
  "4_7": "#2563eb",
  "8_14": "#f59e0b",
  "15_plus": "#ef4444",
};
export const SOURCE_COLORS: Record<TaskSource, string> = {
  Manual: "#d97706",
  HelpDesk: "#0d9488",
  DCS: "#7c3aed",
  HR: "#2563eb",
};

export function priorityLabel(priority: TaskPriority, t: (k: string) => string) {
  return t(`priority.${priority}`);
}

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
