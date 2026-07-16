import api from "./api";
import { normalizeRole } from "@/lib/utils";
import { useAuthStore, type AuthUser } from "@/store/authStore";

function normalizeUser(user: AuthUser): AuthUser {
  return { ...user, role: normalizeRole(user.role) };
}

export async function login(email: string, password: string) {
  const { data } = await api.post<{ accessToken: string; user: AuthUser }>("/auth/login", { email, password });
  useAuthStore.getState().setAuth(data.accessToken, normalizeUser(data.user));
  document.cookie = "hasSession=1; path=/; max-age=604800; SameSite=Lax";
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
      const { data } = await api.post<{ accessToken: string }>("/auth/refresh", {}, { withCredentials: true });
      useAuthStore.getState().setToken(data.accessToken);
    } catch {
      throw new Error("Session expired");
    }
  }

  if (!useAuthStore.getState().accessToken) {
    throw new Error("Not authenticated");
  }

  const { data } = await api.get<AuthUser>("/auth/me");
  const user = normalizeUser(data);
  useAuthStore.getState().setAuth(useAuthStore.getState().accessToken!, user);
  return user;
}
