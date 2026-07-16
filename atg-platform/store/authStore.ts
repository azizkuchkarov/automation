import { create } from "zustand";

export interface AuthUser {
  id: string;
  employeeId?: string;
  firstName: string;
  lastName: string;
  middleName?: string;
  fullName: string;
  firstNameEn?: string;
  lastNameEn?: string;
  middleNameEn?: string;
  fullNameEn?: string;
  jobTitleRu?: string;
  jobTitleEn?: string;
  email: string;
  phone?: string;
  organizationId: string;
  organizationName: string;
  organizationCode: string;
  departmentId?: string;
  departmentName?: string;
  departmentNameEn?: string;
  positionId?: string;
  positionName?: string;
  role: string;
  isActive: boolean;
  language: string;
  lastLoginAt?: string;
  pinpp?: string;
  passportSeries?: string;
  passportNumber?: string;
  requiresProfileSetup?: boolean;
}

interface AuthState {
  accessToken: string | null;
  user: AuthUser | null;
  setAuth: (token: string, user: AuthUser) => void;
  setToken: (token: string) => void;
  clearAuth: () => void;
  hydrate: () => void;
}

const STORAGE_KEY = "atg-auth";

function loadStored(): { accessToken: string | null; user: AuthUser | null } {
  if (typeof window === "undefined") return { accessToken: null, user: null };
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (!raw) return { accessToken: null, user: null };
    return JSON.parse(raw) as { accessToken: string | null; user: AuthUser | null };
  } catch {
    return { accessToken: null, user: null };
  }
}

function persist(token: string | null, user: AuthUser | null) {
  if (typeof window === "undefined") return;
  if (!token && !user) {
    sessionStorage.removeItem(STORAGE_KEY);
    return;
  }
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify({ accessToken: token, user }));
}

export const useAuthStore = create<AuthState>((set) => ({
  accessToken: null,
  user: null,
  hydrate: () => {
    const stored = loadStored();
    if (stored.accessToken || stored.user) {
      set({ accessToken: stored.accessToken, user: stored.user });
    }
  },
  setAuth: (accessToken, user) => {
    persist(accessToken, user);
    set({ accessToken, user });
  },
  setToken: (accessToken) => {
    const user = useAuthStore.getState().user;
    persist(accessToken, user);
    set({ accessToken });
  },
  clearAuth: () => {
    persist(null, null);
    set({ accessToken: null, user: null });
  },
}));
