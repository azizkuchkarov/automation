using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

public class MarketingRecord
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }

    public string? PortalNumber { get; set; }
    public DateOnly? RegisteredDate { get; set; }
    public string? InitiatorDepartment { get; set; }
    public string? InitiatorFullName { get; set; }
    public DateOnly? ReceivedDate { get; set; }
    public DateOnly? DeadlineBaseDate { get; set; }
    public MarketingRequestCategory? RequestCategory { get; set; }
    public int? DeadlineWorkingDays { get; set; }
    public DateOnly? DeadlineDate { get; set; }

    public Guid? MarketingExecutorId { get; set; }
    public Guid? AssignedByManagerId { get; set; }
    public DateOnly? HandoverDate { get; set; }
    public DateTime? AcceptedAt { get; set; }

    public string? RequestTitle { get; set; }
    public ProcurementMethodType? ProcurementMethod { get; set; }
    public string? StrategyNumber { get; set; }
    public string? StrategyNumberManual { get; set; }
    public decimal? BudgetAmount { get; set; }
    public string BudgetCurrency { get; set; } = "UZS";
    public string? LegalBasis { get; set; }

    public DateTime? RfqPreparedAt { get; set; }
    public string? RfqDocumentStorageKey { get; set; }
    public string? RfqDocumentFileName { get; set; }
    public bool RfqPublishedAtgSite { get; set; }
    public bool RfqPublishedTenderweek { get; set; }
    public bool RfqSentToVendor { get; set; }
    public bool RfqSentToDistributor { get; set; }
    public bool RfqOpenSearchDone { get; set; }

    public bool TzIssueFound { get; set; }
    public string? TzIssueDescription { get; set; }
    public DateTime? TzIssueResolvedAt { get; set; }

    public DateTime? PlanPreparedAt { get; set; }
    public DateTime? PlanSentToManagementAt { get; set; }
    public DateTime? PlanSubmittedToPortalAt { get; set; }
    public DateTime? PlanApprovedAt { get; set; }
    public DateTime? PlanRegisteredAt { get; set; }

    public DateTime? PortalApprovalStartedAt { get; set; }
    public PortalApprovalType? PortalApprovalType { get; set; }
    public string? PortalBudgetNumber { get; set; }

    public MarketingRecordStatus Status { get; set; } = MarketingRecordStatus.WaitingExecutor;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ProcurementRequestDetail Request { get; set; } = null!;
    public User? MarketingExecutor { get; set; }
    public User? AssignedByManager { get; set; }
    public ICollection<MarketingOffer> Offers { get; set; } = [];
    public ICollection<RfqDispatch> RfqDispatches { get; set; } = [];
    public ICollection<MarketingRfqChannelRequest> RfqChannelRequests { get; set; } = [];
    public ICollection<MarketingProcurementPlan> Plans { get; set; } = [];
    public ICollection<MarketingPortalApproval> PortalApprovals { get; set; } = [];
}
