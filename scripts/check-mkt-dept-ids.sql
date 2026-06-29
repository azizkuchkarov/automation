SELECT d."Id", d."Code", COUNT(u."Id") AS users
FROM departments d
LEFT JOIN users u ON u."DepartmentId" = d."Id" AND u."IsActive" = true AND u."Role" != 'HONachalnik'
WHERE d."Code" = 'HO-MKT-MKT'
GROUP BY d."Id", d."Code";

SELECT u."Email", u."DepartmentId" = d."Id" AS dept_match
FROM users u
CROSS JOIN departments d
WHERE d."Code" = 'HO-MKT-MKT'
  AND u."Email" = 'gaowentai@atg.uz';
