-- Assign user2@atg.uz as HO Marketing Section head and route in-flight marketing work to them.

DO $$
DECLARE
    user2_id uuid;
    mkt_section_id uuid;
    madrakhimov_id uuid;
BEGIN
    SELECT "Id" INTO user2_id FROM users WHERE lower("Email") = 'user2@atg.uz' LIMIT 1;
    SELECT "Id" INTO mkt_section_id FROM departments WHERE "Code" = 'HO-MKT-MKT' LIMIT 1;
    SELECT "Id" INTO madrakhimov_id FROM users WHERE lower("Email") = 'a.madrakhimov@atg.uz' LIMIT 1;

    IF user2_id IS NULL THEN
        RAISE NOTICE 'user2@atg.uz not found — run HO seed first';
        RETURN;
    END IF;

    UPDATE users
    SET "Role" = 'HONachalnik',
        "DepartmentId" = mkt_section_id,
        "JobTitleRu" = 'Начальник отдела маркетинга (тест)',
        "JobTitleEn" = 'Marketing Section Manager (test)',
        "UpdatedAt" = NOW()
    WHERE "Id" = user2_id;

    IF madrakhimov_id IS NOT NULL THEN
        UPDATE users
        SET "Role" = 'HOEngineer',
            "JobTitleRu" = 'Ведущий специалист по маркетингу',
            "JobTitleEn" = 'Marketing Leading specialist',
            "UpdatedAt" = NOW()
        WHERE "Id" = madrakhimov_id;
    END IF;

    UPDATE documents d
    SET "AssigneeId" = user2_id,
        "UpdatedAt" = NOW()
    FROM procurement_request_details prd
    WHERE prd."DocumentId" = d."Id"
      AND prd."Phase" = 'Marketing';

    UPDATE work_tasks wt
    SET "AssigneeId" = user2_id,
        "UpdatedAt" = NOW()
    WHERE wt."Title" LIKE 'Marketing review%'
       OR wt."Title" LIKE 'Marketing review —%';

    UPDATE procurement_request_details prd
    SET "MarketingSpecialistId" = NULL,
        "MarketingAssignedAt" = NULL,
        "MarketingAcceptedAt" = NULL,
        "MarketingSubPhase" = 'Pending',
        "MarketingCurrentStep" = 1
    WHERE prd."Phase" = 'Marketing'
      AND prd."MarketingSubPhase" IN ('WaitingAccept', 'InProgress')
      AND prd."MarketingSpecialistId" IS DISTINCT FROM user2_id
      AND prd."MarketingSpecialistId" IS NOT NULL;

END $$;
