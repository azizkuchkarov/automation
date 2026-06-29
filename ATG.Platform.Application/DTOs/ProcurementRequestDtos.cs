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

    DateTime? DueDate,

    Guid OrganizationId,

    string OrganizationName,

    Guid DepartmentId,

    string DepartmentName,

    string DepartmentNameEn,

    Guid? ResponsibleTaskId,

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

    Guid? ContractsSpecialistId,

    string? ContractsSpecialistName,

    DateTime? ContractsAssignedAt,

    DateTime? ContractsAcceptedAt,

    ProcurementMarketingPermissionsDto? MarketingPermissions,

    ProcurementContractsPermissionsDto? ContractsPermissions,

    DateTime? MarketingPlanApprovalSubmittedAt,

    string? MarketingPlanRegistrationNumber,

    DateTime? MarketingPlanRegisteredAt,

    ProcurementMarketingPlanPermissionsDto? MarketingPlanPermissions,

    IReadOnlyList<ProcurementMarketingPlanApproverDto> MarketingPlanApprovers,

    int MarketingCurrentStep,

    MarketingBranchType? MarketingActiveBranch,

    IReadOnlyList<ProcurementMarketingStepDto> MarketingSteps,

    IReadOnlyList<ProcurementStepDto> Steps,

    IReadOnlyList<ProcurementApproverDto> Approvers,

    IReadOnlyList<ProcurementAttachmentDto> Attachments,

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

    string ProcurementName,

    Guid ResponsibleId,

    DateTime EamFormationDate,

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

    bool CanComplete,

    bool CanForwardToContracts,

    bool CanCompleteCurrentStep,

    bool CanRecordBranch,

    bool CanResolveBranch,

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



public record AcceptMarketingRequest(string? Comment);



public record ProcurementContractsPermissionsDto(

    bool CanAccept,

    bool CanAssign);



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


