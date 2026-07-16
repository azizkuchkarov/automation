SELECT t."CertificateStorageKey"
FROM hr_business_trip_travelers t
JOIN documents d ON d."Id" = t."DocumentId"
WHERE d."Number" = 'HBT-2026-028'
LIMIT 1;
