"use client";

import { useState } from "react";
import { uploadFile } from "@/lib/files";
import { getApiErrorMessage } from "@/lib/api";
import { cn } from "@/lib/utils";

interface Props {
  folder: string;
  fileName?: string;
  storageKey?: string;
  disabled?: boolean;
  onUploaded: (fileName: string, storageKey: string) => void;
  onError?: (message: string) => void;
  className?: string;
  labels?: { uploading?: string; attached?: string; pick?: string };
}

export function DocumentFileUpload({
  folder,
  fileName,
  storageKey,
  disabled,
  onUploaded,
  onError,
  className,
  labels,
}: Props) {
  const [uploading, setUploading] = useState(false);

  const onPick = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setUploading(true);
    try {
      const result = await uploadFile(file, folder);
      onUploaded(file.name, result.key);
    } catch (err) {
      onError?.(getApiErrorMessage(err, "Upload failed"));
    } finally {
      setUploading(false);
      e.target.value = "";
    }
  };

  return (
    <div className={cn("flex flex-1 flex-col gap-1 min-w-0", className)}>
      <input
        type="file"
        disabled={disabled || uploading}
        onChange={onPick}
        className="text-sm w-full file:mr-2 file:rounded-lg file:border-0 file:bg-pink-500/10 file:px-3 file:py-1.5 file:text-xs file:font-semibold file:text-pink-700"
      />
      {uploading && <span className="text-xs text-foreground/45">{labels?.uploading ?? "Uploading…"}</span>}
      {!uploading && storageKey && (
        <span className="text-xs text-emerald-600 truncate" title={fileName}>
          {labels?.attached ?? "Attached"}: {fileName ?? storageKey}
        </span>
      )}
    </div>
  );
}
