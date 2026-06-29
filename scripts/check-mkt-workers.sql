SELECT u."Email", u."Role", u."IsActive", d."Code"
FROM users u
JOIN departments d ON d."Id" = u."DepartmentId"
WHERE d."Code" IN ('HO-MKT-MKT', 'HO-MKT', 'HO-MKT-TND')
ORDER BY d."Code", u."Role", u."LastName";
