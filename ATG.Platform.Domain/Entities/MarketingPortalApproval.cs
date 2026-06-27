using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

public class MarketingPortalApproval
{
    public Guid Id { get; set; }
    public Guid MarketingRecordId { get; set; }
    public Guid? ProcurementPlanId { get; set; }
    public PortalApprovalType ApprovalType { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? BudgetNumber { get; set; }
    public DateTime? ReminderSentAt { get; set; }
    public string? Notes { get; set; }

    public MarketingRecord Record { get; set; } = null!;
    public MarketingProcurementPlan? ProcurementPlan { get; set; }
}
