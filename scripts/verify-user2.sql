SELECT u."Email", u."Role", d."Code"
FROM users u
JOIN departments d ON d."Id" = u."DepartmentId"
WHERE u."Email" = 'user2@atg.uz';

SELECT d."Number", u."Email" AS assignee
FROM documents d
JOIN procurement_request_details prd ON prd."DocumentId" = d."Id"
LEFT JOIN users u ON u."Id" = d."AssigneeId"
WHERE prd."Phase" = 'Marketing'
LIMIT 5;
