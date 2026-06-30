"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { Bell, CheckCheck, Wifi } from "lucide-react";
import api from "@/lib/api";
import { startNotificationHub } from "@/lib/notificationHub";
import { NotificationItem } from "@/lib/notifications";
import { useAuthStore } from "@/store/authStore";
import { cn } from "@/lib/utils";

const POLL_MS = 120_000;

export function NotificationBell() {
  const t = useTranslations("common.notifications");
  const locale = useLocale();
  const router = useRouter();
  const accessToken = useAuthStore((s) => s.accessToken);
  const [open, setOpen] = useState(false);
  const [unread, setUnread] = useState(0);
  const [items, setItems] = useState<NotificationItem[]>([]);
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
  }, [accessToken, loadCount]);

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

  return (
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
        <div className="absolute right-0 top-full mt-2 w-[min(100vw-2rem,380px)] rounded-xl border border-border bg-surface shadow-xl z-50 overflow-hidden">
          <div className="flex items-center justify-between px-4 py-3 border-b border-border/60">
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

          <div className="max-h-[360px] overflow-y-auto">
            {loading ? (
              <p className="px-4 py-8 text-center text-sm text-foreground/40">{t("loading")}</p>
            ) : items.length === 0 ? (
              <p className="px-4 py-8 text-center text-sm text-foreground/40">{t("empty")}</p>
            ) : (
              <ul>
                {items.map((item) => (
                  <li key={item.id}>
                    <button
                      type="button"
                      onClick={() => openItem(item)}
                      className={cn(
                        "w-full text-left px-4 py-3 border-b border-border/40 hover:bg-foreground/[0.03] transition-colors",
                        !item.isRead && "bg-sky-500/[0.06]"
                      )}
                    >
                      <p className="text-sm font-medium leading-snug">{item.title}</p>
                      {item.body && (
                        <p className="text-xs text-foreground/50 mt-1 line-clamp-2">{item.body}</p>
                      )}
                      <p className="text-[10px] text-foreground/35 mt-1.5 tabular-nums">
                        {new Date(item.createdAt).toLocaleString(locale)}
                      </p>
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
