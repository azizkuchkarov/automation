import api from "@/lib/api";

export interface HomeModuleCounts {
  admin: number;
  automation: number;
  itAutomation: number;
  helpDesk: number;
  hr: number;
  tasks: number;
}

export async function fetchHomeModuleCounts(): Promise<HomeModuleCounts> {
  const { data } = await api.get<{
    admin: number;
    automation: number;
    itAutomation?: number;
    helpDesk: number;
    hr: number;
    tasks: number;
  }>("/platform/module-counts");

  return {
    admin: data.admin ?? 0,
    automation: data.automation ?? 0,
    itAutomation: data.itAutomation ?? 0,
    helpDesk: data.helpDesk ?? 0,
    hr: data.hr ?? 0,
    tasks: data.tasks ?? 0,
  };
}
