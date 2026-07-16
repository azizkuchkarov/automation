SELECT w."DepartmentCode", t."TierKey", s."SortOrder", u."Email"
FROM hr_business_trip_workflow_steps s
JOIN hr_business_trip_workflow_tiers t ON t."Id" = s."TierId"
JOIN hr_business_trip_dept_workflows w ON w."Id" = t."WorkflowId"
JOIN users u ON u."Id" = s."ApproverUserId"
ORDER BY w."DepartmentCode", t."MatchPriority" DESC, s."SortOrder";
