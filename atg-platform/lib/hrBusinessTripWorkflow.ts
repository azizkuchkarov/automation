import api from "@/lib/api";

export interface HrBusinessTripWorkflowPerson {
  userId: string;
  fullName: string;
  email: string;
}

export interface HrBusinessTripWorkflowStep {
  id: string;
  sortOrder: number;
  approverUserId: string;
  approverName: string;
  approverEmail: string;
  role: string;
  labelRu?: string | null;
  labelEn?: string | null;
}

export interface HrBusinessTripWorkflowTier {
  id: string;
  tierKey: string;
  titleRu: string;
  titleEn: string;
  matchPriority: number;
  catchAllStaff: boolean;
  prependsSectionManager: boolean;
  initiators: HrBusinessTripWorkflowPerson[];
  steps: HrBusinessTripWorkflowStep[];
}

export interface HrBusinessTripDeptWorkflow {
  id: string;
  departmentCode: string;
  titleRu: string;
  titleEn: string;
  tiers: HrBusinessTripWorkflowTier[];
}

export interface HrBusinessTripWorkflowAdmin {
  departments: HrBusinessTripDeptWorkflow[];
  organizationName?: string | null;
}

export async function fetchHrBusinessTripWorkflowAdmin(): Promise<HrBusinessTripWorkflowAdmin> {
  const { data } = await api.get<HrBusinessTripWorkflowAdmin>("/hr/business-trips/admin/workflow");
  return data;
}
