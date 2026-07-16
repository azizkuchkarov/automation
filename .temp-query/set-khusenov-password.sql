UPDATE users
SET "PasswordHash" = (
    SELECT "PasswordHash" FROM users WHERE "Email" = 'a.khamroev@atg.uz' LIMIT 1
),
"UpdatedAt" = NOW()
WHERE "Email" = 'v.khusenov@atg.uz'
RETURNING "Email";
