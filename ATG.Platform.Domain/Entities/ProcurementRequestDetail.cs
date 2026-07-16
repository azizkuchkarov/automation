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

    public ProcurementRegion Region { get; set; }

    public string? RegionLabelRu { get; set; }

    public string? RegionLabelEn { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public string? EamNumber { get; set; }

    public DateTime? EamFormationDate { get; set; }

    public TasRequisitionType? TasRequisitionType { get; set; }

    public Guid? ResponsibleTaskId { get; set; }

    public Guid? TasResponsibleId { get; set; }

    public Guid? MarketingTaskId { get; set; }

    public ProcurementMarketingSubPhase MarketingSubPhase { get; set; } = ProcurementMarketingSubPhase.Pending;

    public int MarketingCurrentStep { get; set; } = 1;

    public Guid? MarketingSpecialistId { get; set; }

    public DateTime? MarketingAssignedAt { get; set; }

    public DateTime? MarketingAcceptedAt { get; set; }

    public DateTime? MarketingCompletedAt { get; set; }

    public MarketingBranchType? MarketingActiveBranch { get; set; }

    public DateTime? MarketingBranchStartedAt { get; set; }

    public Guid? ContractsTaskId { get; set; }

    public ProcurementContractsSubPhase ContractsSubPhase { get; set; } = ProcurementContractsSubPhase.Pending;

    public ContractsProcurementSectionType? ContractsProcurementSection { get; set; }

    public DateTime? ContractsSectionRoutedAt { get; set; }

    public Guid? ContractsSpecialistId { get; set; }

    public DateTime? ContractsAssignedAt { get; set; }

    public DateTime? ContractsAcceptedAt { get; set; }

    public ContractsIntProcurementVariant? ContractsIntVariant { get; set; }

    public int ContractsIntCurrentStep { get; set; }

    public DateTime? ContractsIntVariantSelectedAt { get; set; }

    public DateTime? ContractsIntCompletedAt { get; set; }

    public string? ContractsIntContractRegistrationNumber { get; set; }

    public DateTime? ContractsIntContractRegisteredAt { get; set; }

    public bool ContractsIntSecretariatPending { get; set; }

    public Guid? ContractsIntSecretariatUserId { get; set; }

    public ContractsDomProcurementVariant? ContractsDomVariant { get; set; }

    public int ContractsDomCurrentStep { get; set; }

    public DateTime? ContractsDomVariantSelectedAt { get; set; }

    public DateTime? ContractsDomCompletedAt { get; set; }

    public string? ContractsDomContractRegistrationNumber { get; set; }

    public DateTime? ContractsDomContractRegisteredAt { get; set; }

    public bool ContractsDomContractsAdminPending { get; set; }

    public Guid? ContractsDomContractsAdminUserId { get; set; }

    public DateTime? ContractsDomPriceRequestDate { get; set; }

    public DateTime? ContractsDomPriceResponseDueDate { get; set; }

    public DateTime? ContractsDomDeliveryDueDate { get; set; }

    public DateTime? ContractsDomActualDeliveryDate { get; set; }

    public DateTime? ContractsDomLastTerminationAt { get; set; }

    public Guid? PaymentTaskId { get; set; }

    public ProcurementPaymentSubPhase PaymentSubPhase { get; set; } = ProcurementPaymentSubPhase.Pending;

    public Guid? PaymentSpecialistId { get; set; }

    public DateTime? PaymentAssignedAt { get; set; }

    public DateTime? PaymentAcceptedAt { get; set; }

    public DateTime? MarketingPlanApprovalSubmittedAt { get; set; }

    public string? MarketingPlanRegistrationNumber { get; set; }

    public DateTime? MarketingPlanRegisteredAt { get; set; }

    public Guid? MarketingPlanRegisteredById { get; set; }



    public Document Document { get; set; } = null!;

    public User? Initiator { get; set; }

    public User? TasResponsible { get; set; }

    public Department? InitiatorDepartment { get; set; }

    public User? MarketingSpecialist { get; set; }

    public User? ContractsSpecialist { get; set; }

    public User? ContractsIntSecretariatUser { get; set; }

    public User? ContractsDomContractsAdminUser { get; set; }

    public User? PaymentSpecialist { get; set; }

    public MarketingRecord? MarketingRecord { get; set; }

    public ICollection<ProcurementRequestApprover> Approvers { get; set; } = [];

    public ICollection<ProcurementMarketingPlanApprover> MarketingPlanApprovers { get; set; } = [];

    public ICollection<ProcurementRequestAttachment> Attachments { get; set; } = [];

    public ICollection<ProcurementContractsIntStepFile> ContractsIntStepFiles { get; set; } = [];

    public ICollection<ProcurementContractsIntStepApprover> ContractsIntStepApprovers { get; set; } = [];

    public ICollection<ProcurementContractsDomStepFile> ContractsDomStepFiles { get; set; } = [];

    public ICollection<ProcurementContractsDomStepApprover> ContractsDomStepApprovers { get; set; } = [];

    public ICollection<ProcurementStepComment> StepComments { get; set; } = [];

}


