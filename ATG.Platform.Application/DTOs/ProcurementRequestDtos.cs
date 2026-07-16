using ATG.Platform.Domain.Enums;



namespace ATG.Platform.Application.DTOs;



public record ProcurementStepDto(int Number, string TitleRu, string TitleEn);



public record ProcurementInitiatorDepartmentDto(
    Guid Id,
    string Name,
    string NameEn,
    string OrganizationName,
    string OrganizationCode,
    bool IsStation);



public record ProcurementRequestUserDto(

    Guid Id,

    string FullName,

    string Email,

    string? EmployeeId,

    string DepartmentName,

    string DepartmentNameEn,

    string OrganizationName);



public record ProcurementApproverDto(

    Guid Id,

    Guid UserId,

    string UserName,

    ProcurementApproverRole Role,

    ProcurementApproverStatus Status,

    int SortOrder,

    DateTime? DecidedAt,

    string? Comment,

    string? DepartmentName,

    string? DepartmentNameEn,

    string? OrganizationName,

    string? OrganizationNameEn,

    string? JobTitleRu,

    string? JobTitleEn,

    string UserEmail,

    string? EmployeeId);



public record ProcurementAttachmentDto(
    Guid Id,
    ProcurementAttachmentKind Kind,
    string FileName,
    string? StorageKey,
    string UploadedByName,
    DateTime UploadedAt);

/// <summary>Unified process file (uploaded or system-generated) with ownership trail.</summary>
public record ProcurementProcessDocumentDto(
    string Id,
    string FileName,
    string? StorageKey,
    string Source,
    string Phase,
    string Category,
    string? DepartmentName,
    string? DepartmentNameEn,
    string? UserName,
    DateTime? At);



public record ProcurementTimelineEventDto(

    Guid Id,

    string Action,

    string ActorName,

    string? Details,

    DateTime CreatedAt);



public enum ProcurementTopologyNodeStatus

{

    Pending,

    Active,

    Completed,

    Skipped

}



public record ProcurementTopologyNodeDto(

    string Key,

    string LabelRu,

    string LabelEn,

    string? DepartmentCode,

    string? DepartmentNameRu,

    string? DepartmentNameEn,

    ProcurementTopologyNodeStatus Status,

    string? AssigneeName,

    DateTime? CompletedAt);



public record ProcurementRequestDto(

    Guid Id,

    string Number,

    string Title,

    string? TitleRu,

    DocumentStatus Status,

    bool IsRegistered,

    ProcurementRequestFlow Flow,

    ProcurementRequestPhase Phase,

    int CurrentStep,

    Guid AuthorId,

    string AuthorName,

    Guid? AssigneeId,

    string? AssigneeName,

    Guid? InitiatorId,

    string? InitiatorName,

    Guid? InitiatorDepartmentId,

    string? InitiatorDepartmentName,

    string? InitiatorDepartmentNameEn,

    ProcurementRegion Region,

    string? RegionLabelRu,

    string? RegionLabelEn,

    TaskPriority Priority,

    string? EamNumber,

    DateTime? EamFormationDate,

    TasRequisitionType? TasRequisitionType,

    DateTime? DueDate,

    Guid OrganizationId,

    string OrganizationName,

    Guid DepartmentId,

    string DepartmentName,

    string DepartmentNameEn,

    Guid? ResponsibleTaskId,

    Guid? TasResponsibleId,

    string? TasResponsibleName,

    Guid? MarketingTaskId,

    string? MarketingTaskNumber,

    Guid? ContractsTaskId,

    string? ContractsTaskNumber,

    ProcurementMarketingSubPhase MarketingSubPhase,

    Guid? MarketingSpecialistId,

    string? MarketingSpecialistName,

    DateTime? MarketingAcceptedAt,

    DateTime? MarketingAssignedAt,

    DateTime? MarketingCompletedAt,

    ProcurementContractsSubPhase ContractsSubPhase,

    ContractsProcurementSectionType? ContractsProcurementSection,

    DateTime? ContractsSectionRoutedAt,

    Guid? ContractsSpecialistId,

    string? ContractsSpecialistName,

    DateTime? ContractsAssignedAt,

    DateTime? ContractsAcceptedAt,

    ContractsIntProcurementVariant? ContractsIntVariant,

    int ContractsIntCurrentStep,

    DateTime? ContractsIntVariantSelectedAt,

    DateTime? ContractsIntCompletedAt,

    string? ContractsIntContractRegistrationNumber,

    DateTime? ContractsIntContractRegisteredAt,

    bool ContractsIntSecretariatPending,

    Guid? ContractsIntSecretariatUserId,

    string? ContractsIntSecretariatUserName,

    ContractsDomProcurementVariant? ContractsDomVariant,

    int ContractsDomCurrentStep,

    DateTime? ContractsDomVariantSelectedAt,

    DateTime? ContractsDomCompletedAt,

    string? ContractsDomContractRegistrationNumber,

    DateTime? ContractsDomContractRegisteredAt,

    bool ContractsDomContractsAdminPending,

    Guid? ContractsDomContractsAdminUserId,

    string? ContractsDomContractsAdminUserName,

    DateTime? ContractsDomPriceRequestDate,

    DateTime? ContractsDomPriceResponseDueDate,

    DateTime? ContractsDomDeliveryDueDate,

    DateTime? ContractsDomActualDeliveryDate,

    DateTime? ContractsDomLastTerminationAt,

    ProcurementPaymentSubPhase PaymentSubPhase,

    Guid? PaymentTaskId,

    Guid? PaymentSpecialistId,

    string? PaymentSpecialistName,

    DateTime? PaymentAssignedAt,

    DateTime? PaymentAcceptedAt,

    ProcurementMarketingPermissionsDto? MarketingPermissions,

    ProcurementContractsPermissionsDto? ContractsPermissions,

    ProcurementPaymentPermissionsDto? PaymentPermissions,

    IReadOnlyList<ProcurementContractsIntStepDto>? ContractsIntSteps,

    IReadOnlyList<ProcurementContractsDomStepDto>? ContractsDomSteps,

    DateTime? MarketingPlanApprovalSubmittedAt,

    string? MarketingPlanRegistrationNumber,

    DateTime? MarketingPlanRegisteredAt,

    string? MarketingRfqRegistrationNumber,

    DateTime? MarketingRfqRegisteredAt,

    string? MarketingProcurementPlanRegistrationNumber,

    DateTime? MarketingProcurementPlanRegisteredAt,

    string? MarketingProcurementPlanRegistrationMethod,

    ProcurementMarketingPlanPermissionsDto? MarketingPlanPermissions,

    IReadOnlyList<ProcurementMarketingPlanApproverDto> MarketingPlanApprovers,

    int MarketingCurrentStep,

    MarketingBranchType? MarketingActiveBranch,

    IReadOnlyList<ProcurementMarketingStepDto> MarketingSteps,

    IReadOnlyList<ProcurementStepDto> Steps,

    IReadOnlyList<ProcurementApproverDto> Approvers,

    IReadOnlyList<ProcurementAttachmentDto> Attachments,

    IReadOnlyList<ProcurementProcessDocumentDto> ProcessDocuments,

    DateTime? RegisteredAt,

    IReadOnlyList<ProcurementTimelineEventDto> Timeline,

    IReadOnlyList<ProcurementStepCommentDto> StepComments,

    IReadOnlyList<ProcurementTopologyNodeDto> Topology,

    DateTime CreatedAt,

    DateTime UpdatedAt);



public record CompleteProcurementStepRequest(string? Comment);



public record CreateTasProcurementRequest(

    string EamNumber,

    Guid InitiatorId,

    string? SubjectEn,

    string? SubjectRu,

    string? ProcurementName,

    Guid ResponsibleId,

    DateTime EamFormationDate,

    TasRequisitionType TasRequisitionType,

    DateTime Deadline,

    TaskPriority Priority = TaskPriority.Medium,

    IReadOnlyList<ExpressAttachmentInput>? Attachments = null);



public record CreateExpressProcurementRequest(

    string? SubjectEn,

    string? SubjectRu,

    string? Subject,

    IReadOnlyList<ExpressApproverInput> Approvers,

    IReadOnlyList<ExpressAttachmentInput> Attachments,

    TaskPriority Priority = TaskPriority.Medium);



public record ExpressApproverInput(Guid UserId, ProcurementApproverRole Role);



public record ExpressAttachmentInput(ProcurementAttachmentKind Kind, string FileName, string? StorageKey = null);



public record SubmitStep9Request(

    IReadOnlyList<ExpressApproverInput> Approvers,

    IReadOnlyList<ExpressAttachmentInput> Attachments);



public record ProcurementApprovalRequest(string? Comment);



public record ProcurementCreateOptionsDto(

    bool CanCreateTas,

    bool CanCreateExpress,

    ProcurementRequestFlow? DefaultFlow,

    ProcurementRequestFormContextDto? FormContext);



public record ProcurementRequestFormContextDto(

    ProcurementRegion Region,

    string RegionLabelRu,

    string RegionLabelEn,

    DateTime RegDate,

    Guid? InitiatingDepartmentId,

    string? InitiatingDepartmentName,

    string? InitiatingDepartmentNameEn,

    Guid InitiatingEmployeeId,

    string InitiatingEmployeeName,

    bool RequiresEamNumber,

    bool IsTasStaff);



public record ProcurementMarketingPermissionsDto(
    bool CanAccept,
    bool CanAssign,
    bool CanReturnToInitiator,
    bool CanComplete,
    bool CanForwardToContracts,
    bool CanCompleteCurrentStep,
    bool CanRecordBranch,
    bool CanResolveBranch,
    bool CanReviewProposals,
    bool CanReviewProposalsAsEngineer,
    int CurrentStep);



public record ProcurementMarketingStepDto(

    int Number,

    string TitleRu,

    string TitleEn,

    string HintRu,

    string HintEn,

    bool HasBranch,

    string? BranchHintRu,

    string? BranchHintEn);



public record ProcurementMarketingQueueSummaryDto(
    int Total,
    int Pending,
    int InProgress,
    int Completed);

public record ProcurementWorkflowRoleDto(
    string RoleKey,
    string TitleRu,
    string TitleEn,
    string DescriptionRu,
    string DescriptionEn,
    Guid? ManagerUserId,
    string? ManagerUserName,
    string? ManagerUserEmail,
    Guid? EngineerDepartmentId,
    string? EngineerDepartmentName,
    string? EngineerDepartmentNameEn,
    string? EngineerDepartmentCode,
    IReadOnlyList<ProcurementRequestUserDto> Engineers);

public record UpdateProcurementWorkflowRoleRequest(
    Guid? ManagerUserId,
    Guid? EngineerDepartmentId);

public record ProcurementWorkflowRolesAdminDto(
    IReadOnlyList<ProcurementWorkflowRoleDto> Roles,
    IReadOnlyList<ProcurementRequestUserDto> CandidateManagers,
    IReadOnlyList<ProcurementDepartmentOptionDto> Departments);

public record ProcurementDepartmentOptionDto(
    Guid Id,
    string Code,
    string Name,
    string NameEn);

public record ProcurementContractsQueueItemDto(
    Guid Id,
    string Number,
    string Title,
    string? TitleRu,
    ContractsProcurementSectionType? Section,
    ProcurementContractsSubPhase ContractsSubPhase,
    string? AssigneeName,
    string? ContractsSpecialistName,
    DateTime UpdatedAt);

public record ProcurementContractsBoardItemDto(
    Guid Id,
    string Number,
    string Title,
    string? TitleRu,
    ContractsProcurementSectionType? Section,
    ProcurementContractsSubPhase ContractsSubPhase,
    string? AssigneeName,
    string? ContractsSpecialistName,
    string? DomVariant,
    string? IntVariant,
    int DomCurrentStep,
    int IntCurrentStep,
    DateTime UpdatedAt);

public record ProcurementContractsBoardColumnDto(
    ProcurementContractsSubPhase SubPhase,
    string LabelRu,
    string LabelEn,
    IReadOnlyList<ProcurementContractsBoardItemDto> Items);

public record ProcurementMarketingQueueItemDto(

    Guid Id,

    string Number,

    string Title,

    string? TitleRu,

    ProcurementMarketingSubPhase MarketingSubPhase,

    int MarketingCurrentStep,

    string MarketingStepTitleRu,

    string MarketingStepTitleEn,

    string? AssigneeName,

    string? MarketingSpecialistName,

    DateTime RegisteredAt,

    DateTime UpdatedAt);



public record ProcurementStepCommentDto(

    Guid Id,

    ProcurementWorkflowPhase Phase,

    int StepNumber,

    Guid AuthorId,

    string AuthorName,

    string Body,

    ProcurementStepCommentKind Kind,

    DateTime CreatedAt);



public record AssignMarketingSpecialistRequest(Guid SpecialistId, string? Comment);

public record ReturnMarketingToInitiatorRequest(string Comment);



public record AcceptMarketingRequest(string? Comment);



public record ProcurementContractsPermissionsDto(
    bool CanAccept,
    bool CanAssign,
    bool CanRouteSection,
    bool CanSelectIntVariant,
    bool CanCompleteIntStep,
    bool CanUploadIntStepFile,
    bool CanSubmitIntStepApprovers,
    bool CanDecideIntStepApproval,
    bool CanSendToSecretariat,
    bool CanCompleteAsSecretariat,
    int ContractsIntCurrentStep,
    bool CanSelectDomVariant,
    bool CanCompleteDomStep,
    bool CanUploadDomStepFile,
    bool CanSubmitDomStepApprovers,
    bool CanDecideDomStepApproval,
    bool CanSendToContractsAdmin,
    bool CanCompleteAsContractsAdmin,
    bool CanScheduleDomStep,
    bool CanReturnDomStepToMarketing,
    bool CanRollbackDomStep,
    int ContractsDomCurrentStep);

public record ProcurementPaymentPermissionsDto(
    bool CanAssign,
    bool CanAccept);

public record ProcurementContractsIntStepFileDto(
    Guid Id,
    int StepNumber,
    string FileName,
    string? StorageKey,
    string UploadedByName,
    DateTime UploadedAt);

public record ProcurementContractsIntStepApproverDto(
    Guid Id,
    int StepNumber,
    Guid UserId,
    string FullName,
    string Email,
    ProcurementApproverStatus Status,
    int SortOrder,
    DateTime? DecidedAt,
    string? Comment);

public record ProcurementContractsIntStepDto(
    int Number,
    string TitleRu,
    string TitleEn,
    string HintRu,
    string HintEn,
    bool HasBranch,
    string? BranchHintRu,
    string? BranchHintEn,
    bool RequiresUpload,
    bool RequiresApprovers,
    bool RequiresSecretariat,
    bool RequiresRegistration,
    IReadOnlyList<ProcurementContractsIntStepFileDto> Files,
    IReadOnlyList<ProcurementContractsIntStepApproverDto> Approvers,
    bool ApproversSubmitted,
    bool AllApproversApproved,
    bool SecretariatPending);

public record SelectContractsIntVariantRequest(ContractsIntProcurementVariant Variant, string? Comment);

public record CompleteContractsIntStepRequest(string? Comment, string? RegistrationNumber = null);

public record ContractsIntStepFileInput(string FileName, string? StorageKey);

public record SubmitContractsIntStepApproversRequest(IReadOnlyList<Guid> UserIds);

public record DecideContractsIntStepApprovalRequest(bool Approve, string? Comment);

public record SendContractsIntToSecretariatRequest(string? Comment);

public record ProcurementContractsDomStepFileDto(
    Guid Id,
    int StepNumber,
    string FileName,
    string? StorageKey,
    string UploadedByName,
    DateTime UploadedAt);

public record ProcurementContractsDomStepApproverDto(
    Guid Id,
    int StepNumber,
    Guid UserId,
    string FullName,
    string Email,
    ProcurementApproverStatus Status,
    int SortOrder,
    DateTime? DecidedAt,
    string? Comment);

public record ProcurementContractsDomStepDto(
    int Number,
    string TitleRu,
    string TitleEn,
    string HintRu,
    string HintEn,
    bool HasBranch,
    string? BranchHintRu,
    string? BranchHintEn,
    bool RequiresUpload,
    bool RequiresApprovers,
    bool RequiresContractsAdmin,
    bool RequiresRegistration,
    bool RequiresScheduleDate,
    string? ScheduleLabelRu,
    string? ScheduleLabelEn,
    string? ScheduleHintRu,
    string? ScheduleHintEn,
    bool AllowsReturnToMarketing,
    bool AllowsTerminationRollback,
    int? RollbackStepNumber,
    IReadOnlyList<ProcurementContractsDomStepFileDto> Files,
    IReadOnlyList<ProcurementContractsDomStepApproverDto> Approvers,
    bool ApproversSubmitted,
    bool AllApproversApproved,
    bool ContractsAdminPending);

public record SelectContractsDomVariantRequest(ContractsDomProcurementVariant Variant, string? Comment);

public record CompleteContractsDomStepRequest(string? Comment, string? RegistrationNumber = null);

public record ScheduleContractsDomStepRequest(DateTime Date, string? Comment = null);

public record ContractsDomStepFileInput(string FileName, string? StorageKey);

public record SubmitContractsDomStepApproversRequest(IReadOnlyList<Guid> UserIds);

public record DecideContractsDomStepApprovalRequest(bool Approve, string? Comment);

public record SendContractsDomToContractsAdminRequest(string? Comment);

public record ReturnContractsDomToMarketingRequest(string? Comment);

public record RollbackContractsDomStepRequest(string? Comment);

public record RouteContractsSectionRequest(ContractsProcurementSectionType Section, string? Comment);



public record AssignContractsSpecialistRequest(Guid SpecialistId, string? Comment);



public record AcceptContractsRequest(string? Comment);



public record ProcurementMarketingPlanApproverDto(
    Guid Id,
    Guid UserId,
    string UserName,
    ProcurementMarketingPlanApproverRole Role,
    ProcurementApproverStatus Status,
    int SortOrder,
    DateTime? DecidedAt,
    string? Comment,
    string? DepartmentName,
    string? DepartmentNameEn,
    string UserEmail);

public record MarketingPlanApproverInput(Guid UserId, ProcurementMarketingPlanApproverRole Role);

public record SubmitMarketingPlanApprovalRequest(IReadOnlyList<MarketingPlanApproverInput> Approvers);

public record ConfirmMarketingRegistrationRequest(string? Comment);

public record ProcurementMarketingPlanPermissionsDto(
    bool CanSubmit,
    bool CanApprove,
    bool CanConfirmRegistration);

public record MarketingActionRequest(string? Comment);



public record CompleteMarketingStepRequest(Guid? SpecialistId, string? Comment);



public record MarketingBranchRequest(MarketingBranchType Branch, string? Comment, bool Resolve);



public record AddProcurementStepCommentRequest(

    ProcurementWorkflowPhase Phase,

    int StepNumber,

    string Body);


