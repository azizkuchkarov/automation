using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

public class HrBusinessTripApprover
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid UserId { get; set; }
    public HrBusinessTripApprovalRole Role { get; set; }
    public HrLeaveApproverStatus Status { get; set; } = HrLeaveApproverStatus.Pending;
    public int SortOrder { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? Comment { get; set; }

    public HrBusinessTripRequestDetail Request { get; set; } = null!;
    public User User { get; set; } = null!;
}
