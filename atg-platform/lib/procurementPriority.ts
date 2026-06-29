export type ProcurementPriority = "Low" | "Medium" | "High" | "Critical";

export type ProcurementRegion = "HeadOffice" | "Bmgmc" | "Station";

export const PROCUREMENT_PRIORITIES: ProcurementPriority[] = ["Low", "Medium", "High", "Critical"];

export function priorityDotClass(priority?: ProcurementPriority | null): string {
  switch (priority) {
    case "Low":
      return "bg-slate-400 shadow-slate-400/40";
    case "High":
      return "bg-amber-500 shadow-amber-500/40";
    case "Critical":
      return "bg-red-500 shadow-red-500/40";
    case "Medium":
    default:
      return "bg-sky-500 shadow-sky-500/40";
  }
}

export function priorityRingClass(priority?: ProcurementPriority | null): string {
  switch (priority) {
    case "Low":
      return "ring-slate-400/30 border-slate-300/50";
    case "High":
      return "ring-amber-500/30 border-amber-400/50";
    case "Critical":
      return "ring-red-500/30 border-red-400/50";
    case "Medium":
    default:
      return "ring-sky-500/30 border-sky-400/50";
  }
}

export function priorityLabel(priority: ProcurementPriority, locale: string): string {
  const ru: Record<ProcurementPriority, string> = {
    Low: "Низкий",
    Medium: "Средний",
    High: "Высокий",
    Critical: "Критический",
  };
  const en: Record<ProcurementPriority, string> = {
    Low: "Low",
    Medium: "Medium",
    High: "High",
    Critical: "Critical",
  };
  return locale.startsWith("en") ? en[priority] : ru[priority];
}

export function regionLabelFromDept(
  orgCode: string,
  orgName: string,
  isStation: boolean,
  locale: string
): string {
  if (orgCode === "HO") {
    return locale.startsWith("en") ? "Tashkent Head Office" : "Ташкент — головной офис";
  }
  if (!isStation && orgCode === "BMGMC") {
    return "BMGMC";
  }
  return orgName;
}
