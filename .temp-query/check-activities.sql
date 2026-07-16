SELECT a."Action", a."CreatedAt", a."Comment"
FROM document_activities a
JOIN documents d ON d."Id" = a."DocumentId"
WHERE d."Number" = 'HBT-2026-028'
ORDER BY a."CreatedAt" DESC
LIMIT 10;
