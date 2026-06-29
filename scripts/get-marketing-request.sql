SELECT d."Id", d."Number", prd."Phase", prd."MarketingSubPhase", prd."MarketingCurrentStep"
FROM procurement_request_details prd
JOIN documents d ON d."Id" = prd."DocumentId"
WHERE prd."Phase" = 'Marketing'
ORDER BY d."UpdatedAt" DESC
LIMIT 3;
