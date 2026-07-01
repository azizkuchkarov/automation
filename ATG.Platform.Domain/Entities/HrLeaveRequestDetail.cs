using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

public class HrLeaveRequestDetail
{
    public Guid DocumentId { get; set; }
    public HrLeaveRequestPhase Phase { get; set; } = HrLeaveRequestPhase.Draft;
    public HrLeaveTrack Track { get; set; } = HrLeaveTrack.Specialist;
    public Guid HrDepartmentId { get; set; }
    public string PeriodLabel { get; set; } = "";
    public DateTime RequestDate { get; set; }
    public Guid? HrTaskId { get; set; }
    public DateTime? HrReviewCompletedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public Document Document { get; set; } = null!;
    public Department HrDepartment { get; set; } = null!;
    public ICollection<HrLeaveRequestItem> Items { get; set; } = [];
    public ICollection<HrLeaveApprover> Approvers { get; set; } = [];
}
