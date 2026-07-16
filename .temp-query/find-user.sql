SELECT "Email", "PasswordHash" IS NOT NULL as has_pw FROM users WHERE "Email" LIKE '%khamroev%' LIMIT 3;
