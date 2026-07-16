SELECT d."Id", d."Number", ild."Phase", ild."ResolutionManagerId", u."Email"
FROM documents d
JOIN incoming_letter_details ild ON ild."DocumentId" = d."Id"
LEFT JOIN users u ON u."Id" = ild."ResolutionManagerId"
WHERE ild."Phase" = 'AwaitingResolution'
ORDER BY d."UpdatedAt" DESC
LIMIT 5;

SELECT "Id", "Code", "Name" FROM departments WHERE "Code" = 'HO-ITDIG' LIMIT 1;
