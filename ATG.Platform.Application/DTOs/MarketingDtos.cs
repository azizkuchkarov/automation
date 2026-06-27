using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Application.DTOs;

public record MarketingRecordDto(
    Guid Id,
    Guid DocumentId,
    string? PortalNumber,
    DateOnly? RegisteredDate,
    string? InitiatorDepartment,
    string? InitiatorFullName,
    DateOnly? ReceivedDate,
    DateOnly? DeadlineBaseDate,
    MarketingRequestCategory? RequestCategory,
    int? DeadlineWorkingDays,
    DateOnly? DeadlineDate,
    int? RemainingWorkingDays,
    string? DeadlineColor,
    Guid? MarketingExecutorId,
    string? MarketingExecutorName,
    Guid? AssignedByManagerId,
    string? AssignedByManagerName,
    DateOnly? HandoverDate,
    DateTime? AcceptedAt,
    string? RequestTitle,
    ProcurementMethodType? ProcurementMethod,
    string? StrategyNumber,
    string? StrategyNumberManual,
    decimal? BudgetAmount,
    string BudgetCurrency,
    string? LegalBasis,
    DateTime? RfqPreparedAt,
    bool RfqPublishedAtgSite,
    bool RfqPublishedTenderweek,
    bool RfqSentToVendor,
    bool RfqSentToDistributor,
    bool RfqOpenSearchDone,
    bool TzIssueFound,
    string? TzIssueDescription,
    DateTime? TzIssueResolvedAt,
    MarketingRecordStatus Status,
    int MarketingCurrentStep,
    string? Notes,
    IReadOnlyList<MarketingOfferDto> Offers,
    IReadOnlyList<RfqDispatchDto> RfqDispatches,
    IReadOnlyList<MarketingProcurementPlanDto> Plans,
    IReadOnlyList<MarketingPortalApprovalDto> PortalApprovals,
    MarketingOffersSummaryDto? OffersSummary,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record MarketingRecordListItemDto(
    Guid Id,
    Guid DocumentId,
    string? PortalNumber,
    string? RequestTitle,
    MarketingRecordStatus Status,
    MarketingRequestCategory? RequestCategory,
    DateOnly? DeadlineDate,
    int? RemainingWorkingDays,
    string? DeadlineColor,
    string? MarketingExecutorName,
    string? InitiatorDepartment,
    decimal? BudgetAmount,
    string BudgetCurrency,
    int OfferCount,
    DateTime UpdatedAt);

public record MarketingOfferDto(
    Guid Id,
    string CompanyName,
    decimal? OfferAmount,
    string Currency,
    bool VatIncluded,
    bool DeliveryIncluded,
    string? WarrantyTerms,
    DateOnly? OfferDate,
    DateOnly? OfferValidityDate,
    string? ContactInfo,
    bool? MeetsTzRequirements,
    string? RejectionReason,
    bool IsAffiliated,
    string? AffiliationNote,
    MarketingOfferSource Source,
    string? AttachmentKey,
    DateTime CreatedAt);

public record MarketingOffersSummaryDto(
    int CompliantCount,
    decimal? AverageCompliantAmount,
    int AffiliatedCount);

public record RfqDispatchDto(
    Guid Id,
    RfqDispatchType DispatchType,
    string? RecipientName,
    string? RecipientEmail,
    string? RecipientPhone,
    DateTime SentAt,
    DateTime? ResponseReceivedAt,
    DateTime? FollowupSentAt,
    bool FollowupPhoneCalled,
    string? Notes);

public record MarketingProcurementPlanDto(
    Guid Id,
    int Version,
    ProcurementMethodType ProcurementMethod,
    decimal? StartPrice,
    string? StartPriceCurrency,
    bool VatConsidered,
    string? Incoterms,
    string? CompetitionCriteria,
    string? EvaluationGroupMembers,
    string? NdsNote,
    MarketingPlanStatus Status,
    string? RejectionNotes,
    DateTime? SubmittedAt,
    DateTime? ApprovedAt,
    string? AttachmentKey,
    DateTime CreatedAt);

public record MarketingPortalApprovalDto(
    Guid Id,
    PortalApprovalType ApprovalType,
    DateTime SubmittedAt,
    DateTime? ApprovedAt,
    string? BudgetNumber,
    DateTime? ReminderSentAt,
    string? Notes);

public record SetMarketingCategoryRequest(
    MarketingRequestCategory Category,
    DateOnly? DeadlineBaseDate);

public record AssignMarketingExecutorRequest(Guid ExecutorId);

public record MarketingTzIssueRequest(string IssueDescription);

public record AddRfqDispatchRequest(
    RfqDispatchType DispatchType,
    string? RecipientName,
    string? RecipientEmail,
    string? RecipientPhone,
    string? Notes);

public record MarkRfqFollowupRequest(bool PhoneCallMade, string? Notes);

public record AddMarketingOfferRequest(
    string CompanyName,
    decimal? OfferAmount,
    string? Currency,
    bool VatIncluded,
    bool DeliveryIncluded,
    string? WarrantyTerms,
    DateOnly? OfferDate,
    DateOnly? OfferValidityDate,
    string? ContactInfo,
    MarketingOfferSource Source,
    string? AttachmentKey);

public record UpdateOfferComplianceRequest(bool MeetsTz, string? RejectionReason);

public record UpdateOfferAffiliationRequest(bool IsAffiliated, string? Note);

public record CreateMarketingPlanRequest(
    ProcurementMethodType ProcurementMethod,
    decimal? StartPrice,
    string? StartPriceCurrency,
    bool VatConsidered,
    string? Incoterms,
    string? CompetitionCriteria,
    string? EvaluationGroupMembers,
    string? NdsNote,
    string? AttachmentKey);

public record SubmitPortalRequest(Guid PlanId, PortalApprovalType ApprovalType, string? Notes);

public record CompletePortalApprovalRequest(string BudgetNumber, string? Notes);

public record UpdateMarketingRecordRequest(
    ProcurementMethodType? ProcurementMethod,
    string? StrategyNumberManual,
    decimal? BudgetAmount,
    string? BudgetCurrency,
    string? LegalBasis,
    string? Notes);

public record MarketingCancelRequest(string Reason);

public record MarketingBoardColumnDto(
    MarketingRecordStatus Status,
    string LabelRu,
    string LabelEn,
    IReadOnlyList<MarketingRecordListItemDto> Items);

public record MarketingStatsDto(
    int Total,
    int InProgress,
    int Overdue,
    int Completed,
    IReadOnlyList<MarketingCategoryStatDto> ByCategory,
    IReadOnlyList<MarketingExecutorStatDto> ByExecutor,
    IReadOnlyList<MarketingMethodStatDto> ByMethod);

public record MarketingCategoryStatDto(MarketingRequestCategory Category, int Count);

public record MarketingExecutorStatDto(string ExecutorName, int Count, int Overdue);

public record MarketingMethodStatDto(ProcurementMethodType Method, int Count);

public record MarketingLeadershipItemDto(
    Guid DocumentId,
    string? PortalNumber,
    string? RequestTitle,
    MarketingRecordStatus Status,
    int MarketingCurrentStep,
    int? RemainingWorkingDays,
    string? DeadlineColor,
    bool IsOverdue);

public record MarketingLeadershipRowDto(
    string InitiatorDepartment,
    string InitiatorFullName,
    IReadOnlyList<MarketingLeadershipItemDto> Items);

public record FileUploadResultDto(string Key, string? PublicUrl);
