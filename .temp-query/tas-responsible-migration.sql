ALTER TABLE procurement_request_details ADD COLUMN IF NOT EXISTS "TasResponsibleId" uuid;

CREATE INDEX IF NOT EXISTS "IX_procurement_request_details_TasResponsibleId"
    ON procurement_request_details ("TasResponsibleId");

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'FK_procurement_request_details_users_TasResponsibleId'
    ) THEN
        ALTER TABLE procurement_request_details
            ADD CONSTRAINT "FK_procurement_request_details_users_TasResponsibleId"
            FOREIGN KEY ("TasResponsibleId") REFERENCES users ("Id") ON DELETE SET NULL;
    END IF;
END $$;

UPDATE procurement_request_details d
SET "TasResponsibleId" = t."AssigneeId"
FROM work_tasks t
WHERE d."ResponsibleTaskId" = t."Id"
  AND d."Flow" = 'TechnicalAffairs'
  AND d."TasResponsibleId" IS NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260702143000_AddTasResponsibleId', '10.0.0')
ON CONFLICT DO NOTHING;
