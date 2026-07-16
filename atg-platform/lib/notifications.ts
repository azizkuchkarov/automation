export type NotificationType =
  | "DcsApprovalRequired"
  | "MarketingPlanApprovalRequired"
  | "TaskAssigned"
  | "TicketAssigned"
  | "DcsApprovalRejected"
  | "DcsApprovalReminder"
  | "MarketingPlanApprovalReminder"
  | "ContractsRoutingRequired"
  | "ContractsSectionAssigned"
  | "ContractsEngineerAssigned"
  | "ProcurementPhaseMoved"
  | "HrBusinessTripCertificateAvailable"
  | "ItAssetExpiryWarning";

export interface NotificationItem {
  id: string;
  type: NotificationType;
  title: string;
  body?: string;
  entityType?: string;
  entityId?: string;
  actionUrl?: string;
  isRead: boolean;
  createdAt: string;
}

export interface NotificationUnreadCount {
  count: number;
}

export interface NotificationPage {
  items: NotificationItem[];
  total: number;
  page: number;
  pageSize: number;
}

export type NotificationVisual = {
  accent: string;
  iconBg: string;
  iconColor: string;
  badge?: string;
};

export function notificationVisual(type: NotificationType | string): NotificationVisual {
  switch (type) {
    case "ProcurementPhaseMoved":
      return {
        accent: "border-l-emerald-500",
        iconBg: "bg-emerald-500/12",
        iconColor: "text-emerald-600 dark:text-emerald-400",
        badge: "bg-emerald-500/12 text-emerald-700 dark:text-emerald-300",
      };
    case "HrBusinessTripCertificateAvailable":
      return {
        accent: "border-l-violet-500",
        iconBg: "bg-violet-500/12",
        iconColor: "text-violet-600 dark:text-violet-400",
        badge: "bg-violet-500/12 text-violet-700 dark:text-violet-300",
      };
    case "ItAssetExpiryWarning":
      return {
        accent: "border-l-cyan-500",
        iconBg: "bg-cyan-500/12",
        iconColor: "text-cyan-600 dark:text-cyan-400",
        badge: "bg-cyan-500/12 text-cyan-700 dark:text-cyan-300",
      };
    case "DcsApprovalRequired":
    case "MarketingPlanApprovalRequired":
    case "DcsApprovalReminder":
    case "MarketingPlanApprovalReminder":
      return {
        accent: "border-l-amber-500",
        iconBg: "bg-amber-500/12",
        iconColor: "text-amber-600 dark:text-amber-400",
        badge: "bg-amber-500/12 text-amber-700 dark:text-amber-300",
      };
    case "DcsApprovalRejected":
      return {
        accent: "border-l-rose-500",
        iconBg: "bg-rose-500/12",
        iconColor: "text-rose-600 dark:text-rose-400",
        badge: "bg-rose-500/12 text-rose-700 dark:text-rose-300",
      };
    case "ContractsRoutingRequired":
    case "ContractsSectionAssigned":
    case "ContractsEngineerAssigned":
      return {
        accent: "border-l-indigo-500",
        iconBg: "bg-indigo-500/12",
        iconColor: "text-indigo-600 dark:text-indigo-400",
        badge: "bg-indigo-500/12 text-indigo-700 dark:text-indigo-300",
      };
    case "TaskAssigned":
    case "TicketAssigned":
      return {
        accent: "border-l-sky-500",
        iconBg: "bg-sky-500/12",
        iconColor: "text-sky-600 dark:text-sky-400",
        badge: "bg-sky-500/12 text-sky-700 dark:text-sky-300",
      };
    default:
      return {
        accent: "border-l-slate-400",
        iconBg: "bg-slate-500/10",
        iconColor: "text-slate-600 dark:text-slate-300",
      };
  }
}
