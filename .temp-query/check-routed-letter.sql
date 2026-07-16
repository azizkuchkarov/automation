SELECT d."Number", d."DepartmentId", dept."Code" AS dept_code, ild."Phase", ild."RoutedToDepartmentId"
FROM documents d
JOIN incoming_letter_details ild ON ild."DocumentId" = d."Id"
JOIN departments dept ON dept."Id" = d."DepartmentId"
WHERE d."Number" = 'OTHER-LI-2026-0006';

SELECT w."Number", w."Title", w."Status", u."Email", u."Role", dept."Code"
FROM work_tasks w
JOIN users u ON u."Id" = w."AssigneeId"
JOIN departments dept ON dept."Id" = w."DepartmentId"
WHERE w."ExternalId" = 'f551adcf-bbab-4e14-80fb-c626a738a41c'
ORDER BY w."CreatedAt" DESC;

SELECT "Email", "Role", "DepartmentId", d."Code"
FROM users u
JOIN departments d ON d."Id" = u."DepartmentId"
WHERE u."Email" IN ('a.lebedev@atg.uz', 'a.kuchkarov@atg.uz');
