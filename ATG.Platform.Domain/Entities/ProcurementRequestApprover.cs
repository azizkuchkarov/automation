using ATG.Platform.Domain.Enums;



namespace ATG.Platform.Domain.Entities;



public class ProcurementRequestApprover

{

    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public Guid UserId { get; set; }

    public ProcurementApproverRole Role { get; set; }

    public ProcurementApproverStatus Status { get; set; } = ProcurementApproverStatus.Pending;

    public int SortOrder { get; set; }

    public DateTime? DecidedAt { get; set; }

    public string? Comment { get; set; }



    public ProcurementRequestDetail Request { get; set; } = null!;

    public User User { get; set; } = null!;

}


