using ATG.Platform.Domain.Enums;



namespace ATG.Platform.Domain.Entities;



public class ProcurementRequestDetail

{

    public Guid DocumentId { get; set; }

    public ProcurementRequestFlow Flow { get; set; }

    public ProcurementRequestPhase Phase { get; set; } = ProcurementRequestPhase.InProgress;

    public int CurrentStep { get; set; } = 1;



    public Guid? InitiatorId { get; set; }

    public Guid? InitiatorDepartmentId { get; set; }

    public string? EamNumber { get; set; }

    public DateTime? EamFormationDate { get; set; }

    public Guid? ResponsibleTaskId { get; set; }

    public Guid? MarketingTaskId { get; set; }

    public ProcurementMarketingSubPhase MarketingSubPhase { get; set; } = ProcurementMarketingSubPhase.Pending;

    public int MarketingCurrentStep { get; set; } = 1;

    public Guid? MarketingSpecialistId { get; set; }

    public DateTime? MarketingAcceptedAt { get; set; }

    public DateTime? MarketingCompletedAt { get; set; }

    public MarketingBranchType? MarketingActiveBranch { get; set; }

    public DateTime? MarketingBranchStartedAt { get; set; }

    public Guid? ContractsTaskId { get; set; }



    public Document Document { get; set; } = null!;

    public User? Initiator { get; set; }

    public Department? InitiatorDepartment { get; set; }

    public User? MarketingSpecialist { get; set; }

    public MarketingRecord? MarketingRecord { get; set; }

    public ICollection<ProcurementRequestApprover> Approvers { get; set; } = [];

    public ICollection<ProcurementRequestAttachment> Attachments { get; set; } = [];

}


