SELECT d."Number", d."Id", h."EimzoCompletedAt", h."PdfPresentationStorageKey"
FROM documents d
JOIN hr_leave_request_details h ON h."DocumentId" = d."Id"
WHERE h."EimzoCompletedAt" IS NOT NULL
ORDER BY d."Number" DESC
LIMIT 5;
