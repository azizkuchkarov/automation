import api from "./api";
import { normalizeRole } from "@/lib/utils";
import { useAuthStore, type AuthUser } from "@/store/authStore";

function normalizeUser(user: AuthUser): AuthUser {
  return { ...user, role: normalizeRole(user.role) };
}

export async function login(email: string, password: string) {
  const { data } = await api.post<{ accessToken: string; user: AuthUser }>("/auth/login", { email, password });
  useAuthStore.getState().setAuth(data.accessToken, normalizeUser(data.user));
  document.cookie = "hasSession=1; path=/; max-age=604800";
  return data;
}

export async function logout() {
  try {
    await api.post("/auth/logout");
  } finally {
    useAuthStore.getState().clearAuth();
    document.cookie = "hasSession=; path=/; max-age=0";
  }
}

export async function fetchMe() {
  if (!useAuthStore.getState().accessToken) {
    try {
      const { data } = await api.post<{ accessToken: string }>("/auth/refresh");
      useAuthStore.getState().setToken(data.accessToken);
    } catch {
      /* refresh cookie missing */
    }
  }
  const { data } = await api.get<AuthUser>("/auth/me");
  return normalizeUser(data);
}
