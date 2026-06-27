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

export function fileDownloadUrl(storageKey: string) {
  return `/api/marketing/files/${storageKey.split("/").map(encodeURIComponent).join("/")}`;
}
