import api from "@/lib/api";

export interface FileUploadResult {
  key: string;
  publicUrl?: string;
}

export async function uploadFile(file: File, folder = "documents"): Promise<FileUploadResult> {
  const form = new FormData();
  form.append("file", file);
  const r = await api.post(`/marketing/files/upload?folder=${encodeURIComponent(folder)}`, form);
  return r.data as FileUploadResult;
}

export function fileDownloadUrl(storageKey: string, fileName?: string) {
  const path = storageKey.split("/").map(encodeURIComponent).join("/");
  const qs = fileName ? `?fileName=${encodeURIComponent(fileName)}` : "";
  return `/api/marketing/files/${path}${qs}`;
}

/** Strip storage-key GUID prefix (`{32hex}_originalName`) when original name is unknown. */
export function originalFileNameFromKey(storageKey: string, fileName?: string) {
  if (fileName?.trim()) return fileName.trim().split(/[/\\]/).pop() ?? fileName.trim();
  const name = storageKey.split("/").pop() ?? "download";
  if (name.length > 33 && name[32] === "_" && /^[0-9a-fA-F]{32}$/.test(name.slice(0, 32))) {
    return name.slice(33);
  }
  return name;
}

function parseDownloadFileName(contentDisposition: string | undefined, fallback: string) {
  if (!contentDisposition) return fallback;
  const match = /filename\*?=(?:UTF-8''|")?([^";]+)/i.exec(contentDisposition);
  return match?.[1] ? decodeURIComponent(match[1].replace(/"/g, "")) : fallback;
}

export async function downloadMarketingFile(storageKey: string, fileName?: string) {
  const path = storageKey.split("/").map(encodeURIComponent).join("/");
  const preferredName = originalFileNameFromKey(storageKey, fileName);
  const qs = preferredName ? `?fileName=${encodeURIComponent(preferredName)}` : "";
  const response = await api.get(`/marketing/files/${path}${qs}`, { responseType: "blob" });

  const blob = response.data as Blob;
  const contentType = String(response.headers["content-type"] ?? blob.type);
  if (contentType.includes("application/json") || contentType.includes("text/plain")) {
    const text = await blob.text();
    try {
      const json = JSON.parse(text) as { error?: string };
      throw new Error(json.error ?? "Download failed");
    } catch {
      throw new Error(text || "Download failed");
    }
  }

  const resolvedName = parseDownloadFileName(
    response.headers["content-disposition"],
    preferredName
  );
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = resolvedName;
  link.style.display = "none";
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  window.setTimeout(() => URL.revokeObjectURL(url), 1000);
}
