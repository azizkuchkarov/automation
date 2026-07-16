using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

public class MarketingProcurementPlan
{
    public Guid Id { get; set; }
    public Guid MarketingRecordId { get; set; }
    public int Version { get; set; } = 1;
    public ProcurementMethodType ProcurementMethod { get; set; }
    public MarketingPlanRegistrationMethod? RegistrationMethod { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? TemplateStorageKey { get; set; }
    public string? TemplateFileName { get; set; }
    public DateTime? RegisteredAt { get; set; }
    public decimal? StartPrice { get; set; }
    public string? StartPriceCurrency { get; set; }
    public bool VatConsidered { get; set; }
    public string? Incoterms { get; set; }
    public string? CompetitionCriteria { get; set; }
    public string? EvaluationGroupMembers { get; set; }
    public string? NdsNote { get; set; }
    public MarketingPlanStatus Status { get; set; } = MarketingPlanStatus.Draft;
    public string? RejectionNotes { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? AttachmentKey { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public MarketingRecord Record { get; set; } = null!;
}
