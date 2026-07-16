SELECT "Phase", "ResolutionManagerId", "SentForResolutionAt"
FROM incoming_letter_details
WHERE "DocumentId" = 'f551adcf-bbab-4e14-80fb-c626a738a41c';

SELECT "Id", "UserId", "Informed", "ForInformation", "TaskId"
FROM incoming_letter_recipients
WHERE "DocumentId" = 'f551adcf-bbab-4e14-80fb-c626a738a41c';
