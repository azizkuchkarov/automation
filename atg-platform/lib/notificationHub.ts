import * as signalR from "@microsoft/signalr";
import { useAuthStore } from "@/store/authStore";
import type { NotificationItem } from "@/lib/notifications";

export type NotificationHandler = (notification: NotificationItem, unreadCount: number) => void;
export type UnreadCountHandler = (count: number) => void;

let connection: signalR.HubConnection | null = null;
let startPromise: Promise<void> | null = null;

/** Browser must reach the API hub directly — Next.js standalone does not proxy WebSockets. */
function getHubUrl(): string {
  const api = process.env.NEXT_PUBLIC_API_URL?.replace(/\/$/, "");
  if (api) return `${api}/hubs/notifications`;
  return "/hubs/notifications";
}

export function startNotificationHub(
  onNotification: NotificationHandler,
  onUnreadCount: UnreadCountHandler
): () => void {
  if (typeof window === "undefined") return () => {};

  if (!connection) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl(getHubUrl(), {
        accessTokenFactory: () => useAuthStore.getState().accessToken ?? "",
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    connection.on("notification", (notification: NotificationItem, unreadCount: number) => {
      onNotification(notification, unreadCount);
    });
    connection.on("unreadCount", (unreadCount: number) => {
      onUnreadCount(unreadCount);
    });
  }

  if (!startPromise) {
    startPromise = connection.start().catch((err) => {
      startPromise = null;
      console.warn("Notification hub connection failed", err);
    });
  }

  return () => {
    /* keep singleton connection alive for app session */
  };
}

export async function stopNotificationHub() {
  if (connection) {
    await connection.stop();
    connection = null;
    startPromise = null;
  }
}
