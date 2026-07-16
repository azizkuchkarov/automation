SELECT d."Number", t."FullNameRu", t."PositionRu", d2."PlaceRu", d2."OrderNumber", d2."OrderIssuedAt", d2."DateFrom", d2."DateTo", d2."DaysCount", (t."CertificateStorageKey" IS NOT NULL) as has_cert
FROM hr_business_trip_travelers t
JOIN hr_business_trip_request_details d2 ON d2."DocumentId" = t."DocumentId"
JOIN documents d ON d."Id" = d2."DocumentId"
WHERE d2."Phase" IN ('CertificatePending', 'Completed')
ORDER BY d."Number" DESC
LIMIT 5;
