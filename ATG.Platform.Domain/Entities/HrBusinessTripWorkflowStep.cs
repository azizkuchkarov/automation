using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

public class HrBusinessTripWorkflowStep
{
    public Guid Id { get; set; }
    public Guid TierId { get; set; }
    public int SortOrder { get; set; }
    public Guid ApproverUserId { get; set; }
    public HrBusinessTripApprovalRole Role { get; set; }
    public string? LabelRu { get; set; }
    public string? LabelEn { get; set; }

    public HrBusinessTripWorkflowTier Tier { get; set; } = null!;
    public User ApproverUser { get; set; } = null!;
}
