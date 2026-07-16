ALTER TABLE marketing_offers ADD COLUMN IF NOT EXISTS "EngineerReviewComment" text;
ALTER TABLE marketing_offers ADD COLUMN IF NOT EXISTS "EngineerReviewStatus" integer NOT NULL DEFAULT 0;
ALTER TABLE marketing_offers ADD COLUMN IF NOT EXISTS "EngineerReviewedAt" timestamp without time zone;
ALTER TABLE marketing_offers ADD COLUMN IF NOT EXISTS "EngineerReviewedById" uuid;
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260702120000_AddMarketingOfferEngineerReview', '10.0.0')
ON CONFLICT DO NOTHING;
