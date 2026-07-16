SELECT d."Id", d."Number", ild."Phase"
FROM documents d
JOIN incoming_letter_details ild ON ild."DocumentId" = d."Id"
WHERE ild."Phase" = 'Registered'
ORDER BY d."UpdatedAt" DESC
LIMIT 5;
