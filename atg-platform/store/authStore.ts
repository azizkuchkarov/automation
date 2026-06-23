import { create } from "zustand";

export interface AuthUser {
  id: string;
  employeeId?: string;
  firstName: string;
  lastName: string;
  middleName?: string;
  fullName: string;
  email: string;
  phone?: string;
  organizationId: string;
  organizationName: string;
  organizationCode: string;
  departmentId?: string;
  departmentName?: string;
  positionId?: string;
  positionName?: string;
  role: string;
  isActive: boolean;
  language: string;
  lastLoginAt?: string;
}

interface AuthState {
  accessToken: string | null;
  user: AuthUser | null;
  setAuth: (token: string, user: AuthUser) => void;
  setToken: (token: string) => void;
  clearAuth: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  accessToken: null,
  user: null,
  setAuth: (accessToken, user) => set({ accessToken, user }),
  setToken: (accessToken) => set({ accessToken }),
  clearAuth: () => set({ accessToken: null, user: null }),
}));
