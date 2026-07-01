using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

public class HrLeaveApprover
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid UserId { get; set; }
    public HrLeaveApprovalRole Role { get; set; }
    public HrLeaveApproverStatus Status { get; set; } = HrLeaveApproverStatus.Pending;
    public int SortOrder { get; set; }
    public int ApprovalGroup { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? Comment { get; set; }

    public HrLeaveRequestDetail Request { get; set; } = null!;
    public User User { get; set; } = null!;
}
