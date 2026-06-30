export type NotificationType =
  | "DcsApprovalRequired"
  | "MarketingPlanApprovalRequired"
  | "TaskAssigned"
  | "TicketAssigned"
  | "DcsApprovalRejected"
  | "DcsApprovalReminder"
  | "MarketingPlanApprovalReminder";

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
