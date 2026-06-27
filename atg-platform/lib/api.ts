import axios from "axios";
import { useAuthStore } from "@/store/authStore";

const api = axios.create({
  baseURL: "/api",
  withCredentials: true,
});

let isRefreshing = false;
let refreshQueue: Array<(token: string) => void> = [];

function redirectToLogin() {
  if (typeof window === "undefined") return;
  const locale = window.location.pathname.split("/")[1] || "ru";
  if (!window.location.pathname.includes("/login")) {
    window.location.href = `/${locale}/login`;
  }
}

api.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  if (config.data instanceof FormData) {
    delete config.headers["Content-Type"];
  }
  return config;
});

api.interceptors.response.use(
  (res) => res,
  async (error) => {
    const original = error.config;
    const isAuthEndpoint = original?.url?.includes("/auth/login") || original?.url?.includes("/auth/refresh");

    if (error.response?.status === 401 && original && !original._retry && !isAuthEndpoint) {
      if (isRefreshing) {
        return new Promise((resolve) => {
          refreshQueue.push((token) => {
            original.headers.Authorization = `Bearer ${token}`;
            resolve(api(original));
          });
        });
      }
      original._retry = true;
      isRefreshing = true;
      try {
        const { data } = await axios.post("/api/auth/refresh", {}, { withCredentials: true });
        useAuthStore.getState().setToken(data.accessToken);
        refreshQueue.forEach((cb) => cb(data.accessToken));
        refreshQueue = [];
        original.headers.Authorization = `Bearer ${data.accessToken}`;
        return api(original);
      } catch {
        useAuthStore.getState().clearAuth();
        if (typeof document !== "undefined") {
          document.cookie = "hasSession=; path=/; max-age=0";
        }
        redirectToLogin();
        return Promise.reject(error);
      } finally {
        isRefreshing = false;
      }
    }

    if (error.response?.status === 401 && !isAuthEndpoint) {
      useAuthStore.getState().clearAuth();
      redirectToLogin();
    }

    return Promise.reject(error);
  }
);

export default api;

export function getApiErrorMessage(err: unknown, fallback = "Request failed"): string {
  const data = (err as { response?: { data?: unknown } })?.response?.data;
  if (!data || typeof data !== "object") return fallback;
  if ("error" in data && typeof (data as { error: unknown }).error === "string") {
    return (data as { error: string }).error;
  }
  if ("errors" in data) {
    const errors = (data as { errors: Record<string, string[]> }).errors;
    const messages = Object.values(errors).flat();
    if (messages.length > 0) return messages.join(". ");
  }
  if ("title" in data && typeof (data as { title: unknown }).title === "string") {
    return (data as { title: string }).title;
  }
  return fallback;
}
