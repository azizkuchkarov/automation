import api from "./api";

export interface ParsedImportRow {
  employeeId: string;
  firstName: string;
  lastName: string;
  middleName?: string;
  email: string;
  phone?: string;
  departmentCode: string;
  positionCode: string;
  role: string;
  language: string;
}

function parseCsvLine(line: string): string[] {
  const result: string[] = [];
  let current = "";
  let inQuotes = false;
  for (let i = 0; i < line.length; i++) {
    const c = line[i];
    if (c === '"') {
      inQuotes = !inQuotes;
    } else if (c === "," && !inQuotes) {
      result.push(current.trim());
      current = "";
    } else {
      current += c;
    }
  }
  result.push(current.trim());
  return result;
}

export function parseUsersCsv(text: string): ParsedImportRow[] {
  const lines = text.split(/\r?\n/).map((l) => l.trim()).filter(Boolean);
  if (lines.length < 2) return [];

  const headers = parseCsvLine(lines[0]).map((h) => h.toLowerCase());
  const idx = (name: string) => headers.indexOf(name.toLowerCase());

  const rows: ParsedImportRow[] = [];
  for (let i = 1; i < lines.length; i++) {
    const cols = parseCsvLine(lines[i]);
    const get = (name: string) => cols[idx(name)] ?? "";

    rows.push({
      employeeId: get("employeeid"),
      firstName: get("firstname"),
      lastName: get("lastname"),
      middleName: get("middlename") || undefined,
      email: get("email"),
      phone: get("phone") || undefined,
      departmentCode: get("departmentcode"),
      positionCode: get("positioncode"),
      role: get("role"),
      language: get("language") || "ru",
    });
  }
  return rows;
}

export async function importUsersCsv(organizationId: string, file: File) {
  const text = await file.text();
  const users = parseUsersCsv(text);
  const { data } = await api.post("/users/import", { organizationId, users });
  return data as { created: number; failed: number; errors: string[] };
}
