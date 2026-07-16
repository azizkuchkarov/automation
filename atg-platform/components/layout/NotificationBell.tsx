"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import {
  AlertTriangle,
  ArrowRightLeft,
  Bell,
  Briefcase,
  CheckCheck,
  CheckCircle2,
  ClipboardList,
  FileWarning,
  Wifi,
  X,
} from "lucide-react";
import api from "@/lib/api";
import { startNotificationHub } from "@/lib/notificationHub";
import { NotificationItem, notificationVisual } from "@/lib/notifications";
import { useAuthStore } from "@/store/authStore";
import { cn } from "@/lib/utils";

const POLL_MS = 120_000;
const TOAST_MS = 8_000;

type ToastItem = NotificationItem & { expiresAt: number };

function NotificationIcon({ type, className }: { type: string; className?: string }) {
  const size = 16;
  if (type === "ProcurementPhaseMoved") return <CheckCircle2 size={size} className={className} />;
  if (type === "HrBusinessTripCertificateAvailable") return <Briefcase size={size} className={className} />;
  if (type === "ItAssetExpiryWarning") return <AlertTriangle size={size} className={className} />;
  if (type === "DcsApprovalRejected") return <FileWarning size={size} className={className} />;
  if (
    type === "DcsApprovalRequired" ||
    type === "MarketingPlanApprovalRequired" ||
    type === "DcsApprovalReminder" ||
    type === "MarketingPlanApprovalReminder"
  ) {
    return <ClipboardList size={size} className={className} />;
  }
  if (
    type === "ContractsRoutingRequired" ||
    type === "ContractsSectionAssigned" ||
    type === "ContractsEngineerAssigned"
  ) {
    return <ArrowRightLeft size={size} className={className} />;
  }
  return <Bell size={size} className={className} />;
}

export function NotificationBell() {
  const t = useTranslations("common.notifications");
  const locale = useLocale();
  const router = useRouter();
  const accessToken = useAuthStore((s) => s.accessToken);
  const [open, setOpen] = useState(false);
  const [unread, setUnread] = useState(0);
  const [items, setItems] = useState<NotificationItem[]>([]);
  const [toasts, setToasts] = useState<ToastItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [live, setLive] = useState(false);
  const panelRef = useRef<HTMLDivElement>(null);

  const loadCount = useCallback(async () => {
    try {
      const r = await api.get("/notifications/unread-count");
      setUnread(r.data.count ?? 0);
    } catch {
      /* ignore */
    }
  }, []);

  const loadInbox = useCallback(async () => {
    setLoading(true);
    try {
      const r = await api.get("/notifications?pageSize=20");
      setItems(r.data.items ?? []);
    } catch {
      setItems([]);
    } finally {
      setLoading(false);
    }
  }, []);

  const pushToast = useCallback((notification: NotificationItem) => {
    if (notification.type !== "ProcurementPhaseMoved") return;
    const toast: ToastItem = { ...notification, expiresAt: Date.now() + TOAST_MS };
    setToasts((prev) => [toast, ...prev.filter((x) => x.id !== toast.id)].slice(0, 3));
  }, []);

  useEffect(() => {
    if (toasts.length === 0) return;
    const id = window.setInterval(() => {
      const now = Date.now();
      setToasts((prev) => prev.filter((t) => t.expiresAt > now));
    }, 500);
    return () => clearInterval(id);
  }, [toasts.length]);

  useEffect(() => {
    if (!accessToken) return;

    loadCount();
    const pollId = setInterval(loadCount, POLL_MS);

    const cleanup = startNotificationHub(
      (notification, count) => {
        setLive(true);
        setUnread(count);
        setItems((prev) => {
          if (prev.some((n) => n.id === notification.id)) return prev;
          return [notification, ...prev].slice(0, 20);
        });
        pushToast(notification);
      },
      (count) => {
        setLive(true);
        setUnread(count);
      }
    );

    return () => {
      clearInterval(pollId);
      cleanup();
    };
  }, [accessToken, loadCount, pushToast]);

  useEffect(() => {
    if (!open) return;
    loadInbox();
  }, [open, loadInbox]);

  useEffect(() => {
    if (!open) return;
    const onDocClick = (e: MouseEvent) => {
      if (panelRef.current && !panelRef.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", onDocClick);
    return () => document.removeEventListener("mousedown", onDocClick);
  }, [open]);

  const openItem = async (item: NotificationItem) => {
    try {
      if (!item.isRead) {
        await api.post(`/notifications/${item.id}/read`);
        setUnread((c) => Math.max(0, c - 1));
        setItems((prev) => prev.map((n) => (n.id === item.id ? { ...n, isRead: true } : n)));
      }
    } catch {
      /* ignore */
    }
    setOpen(false);
    setToasts((prev) => prev.filter((x) => x.id !== item.id));
    if (item.actionUrl) router.push(`/${locale}${item.actionUrl}`);
  };

  const markAllRead = async () => {
    try {
      await api.post("/notifications/read-all");
      setUnread(0);
      setItems((prev) => prev.map((n) => ({ ...n, isRead: true })));
    } catch {
      /* ignore */
    }
  };

  const dismissToast = (id: string) => setToasts((prev) => prev.filter((x) => x.id !== id));

  return (
    <>
      <div className="relative" ref={panelRef}>
        <button
          type="button"
          onClick={() => setOpen((v) => !v)}
          className="relative p-2 rounded-lg hover:bg-foreground/5 text-foreground/70 hover:text-foreground transition-colors"
          aria-label={t("title")}
        >
          <Bell size={18} />
          {live && (
            <span className="absolute top-1 left-1 w-2 h-2 rounded-full bg-emerald-500 ring-2 ring-surface" title={t("live")} />
          )}
          {unread > 0 && (
            <span className="absolute top-1 right-1 min-w-[16px] h-4 px-1 rounded-full bg-red-500 text-white text-[10px] font-bold flex items-center justify-center">
              {unread > 99 ? "99+" : unread}
            </span>
          )}
        </button>

        {open && (
          <div className="absolute right-0 top-full mt-2 w-[min(100vw-2rem,400px)] rounded-2xl border border-border/80 bg-surface shadow-2xl z-50 overflow-hidden">
            <div className="flex items-center justify-between px-4 py-3 border-b border-border/60 bg-gradient-to-r from-sky-500/[0.06] to-transparent">
              <div className="flex items-center gap-2">
                <span className="text-sm font-semibold">{t("title")}</span>
                {live && (
                  <span className="inline-flex items-center gap-1 text-[10px] text-emerald-600 dark:text-emerald-400">
                    <Wifi size={12} />
                    {t("live")}
                  </span>
                )}
              </div>
              {unread > 0 && (
                <button
                  type="button"
                  onClick={markAllRead}
                  className="inline-flex items-center gap-1 text-xs text-atg-blue hover:underline"
                >
                  <CheckCheck size={14} />
                  {t("markAllRead")}
                </button>
              )}
            </div>

            <div className="max-h-[420px] overflow-y-auto">
              {loading ? (
                <p className="px-4 py-8 text-center text-sm text-foreground/40">{t("loading")}</p>
              ) : items.length === 0 ? (
                <p className="px-4 py-8 text-center text-sm text-foreground/40">{t("empty")}</p>
              ) : (
                <ul className="p-2 space-y-1.5">
                  {items.map((item) => {
                    const visual = notificationVisual(item.type);
                    return (
                      <li key={item.id}>
                        <button
                          type="button"
                          onClick={() => openItem(item)}
                          className={cn(
                            "w-full text-left rounded-xl border border-border/50 px-3 py-3 transition-all",
                            "hover:border-border hover:bg-foreground/[0.03] hover:shadow-sm",
                            "border-l-[3px]",
                            visual.accent,
                            !item.isRead && "bg-sky-500/[0.04] ring-1 ring-sky-500/10"
                          )}
                        >
                          <div className="flex gap-3">
                            <span
                              className={cn(
                                "mt-0.5 flex h-9 w-9 shrink-0 items-center justify-center rounded-xl",
                                visual.iconBg,
                                visual.iconColor
                              )}
                            >
                              <NotificationIcon type={item.type} />
                            </span>
                            <div className="min-w-0 flex-1">
                              <div className="flex items-start justify-between gap-2">
                                <p className="text-sm font-semibold leading-snug text-foreground">
                                  {item.title}
                                </p>
                                {!item.isRead && (
                                  <span className="mt-1 h-2 w-2 shrink-0 rounded-full bg-sky-500" />
                                )}
                              </div>
                              {item.body && (
                                <p className="mt-1 text-xs leading-relaxed text-foreground/55 line-clamp-3">
                                  {item.body}
                                </p>
                              )}
                              <div className="mt-2 flex flex-wrap items-center gap-2">
                                {item.type === "ProcurementPhaseMoved" && visual.badge && (
                                  <span
                                    className={cn(
                                      "rounded-full px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide",
                                      visual.badge
                                    )}
                                  >
                                    {t("phaseUpdate")}
                                  </span>
                                )}
                                <span className="text-[10px] text-foreground/35 tabular-nums">
                                  {new Date(item.createdAt).toLocaleString(locale)}
                                </span>
                              </div>
                            </div>
                          </div>
                        </button>
                      </li>
                    );
                  })}
                </ul>
              )}
            </div>
          </div>
        )}
      </div>

      {toasts.length > 0 && (
        <div className="pointer-events-none fixed right-4 top-16 z-[80] flex w-[min(100vw-2rem,380px)] flex-col gap-2">
          {toasts.map((toast) => {
            const visual = notificationVisual(toast.type);
            return (
              <div
                key={toast.id}
                className="pointer-events-auto overflow-hidden rounded-2xl border border-emerald-500/25 bg-surface shadow-2xl"
              >
                <div className="border-l-[4px] border-l-emerald-500 p-4">
                  <div className="flex gap-3">
                    <span
                      className={cn(
                        "flex h-10 w-10 shrink-0 items-center justify-center rounded-xl",
                        visual.iconBg,
                        visual.iconColor
                      )}
                    >
                      <CheckCircle2 size={20} />
                    </span>
                    <div className="min-w-0 flex-1">
                      <div className="flex items-start justify-between gap-2">
                        <div>
                          <p className="text-[10px] font-bold uppercase tracking-[0.14em] text-emerald-700 dark:text-emerald-300">
                            {t("phaseUpdate")}
                          </p>
                          <p className="mt-0.5 text-sm font-bold leading-snug text-foreground">
                            {toast.title}
                          </p>
                        </div>
                        <button
                          type="button"
                          onClick={() => dismissToast(toast.id)}
                          className="rounded-lg p-1 text-foreground/40 hover:bg-foreground/5 hover:text-foreground"
                          aria-label={t("dismiss")}
                        >
                          <X size={14} />
                        </button>
                      </div>
                      {toast.body && (
                        <p className="mt-1.5 text-xs leading-relaxed text-foreground/60">
                          {toast.body}
                        </p>
                      )}
                      <button
                        type="button"
                        onClick={() => openItem(toast)}
                        className="mt-3 inline-flex items-center gap-1 text-xs font-semibold text-emerald-700 hover:underline dark:text-emerald-300"
                      >
                        {t("openRequest")}
                        <ArrowRightLeft size={12} />
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </>
  );
}
