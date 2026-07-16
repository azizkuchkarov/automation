using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Application.DTOs;

public record MemoUserDto(
    Guid Id,
    string FullName,
    string Email,
    string? EmployeeId,
    string DepartmentName,
    string DepartmentNameEn);

public record MemoDepartmentDto(
    Guid Id,
    string Code,
    string Name,
    string NameEn);

public record MemoRecipientDto(
    Guid Id,
    Guid? UserId,
    string? UserName,
    Guid? DepartmentId,
    string? DepartmentName,
    string? DepartmentNameEn,
    bool ForInformation,
    DateTime? NotifiedAt);

public record MemoCoordinatorDto(
    Guid Id,
    Guid UserId,
    string UserName,
    DateTime? CoordinatedAt);

public record MemoCommentDto(
    Guid Id,
    Guid AuthorId,
    string AuthorName,
    string Body,
    DateTime CreatedAt);

public record MemoDto(
    Guid Id,
    string Number,
    string Title,
    string? TitleRu,
    DocumentStatus Status,
    MemoPhase Phase,
    Guid AuthorId,
    string AuthorName,
    string? AttachmentFileName,
    string? AttachmentStorageKey,
    string? TranslatedAttachmentFileName,
    string? TranslatedAttachmentStorageKey,
    Guid OrganizationId,
    Guid DepartmentId,
    string DepartmentName,
    string DepartmentNameEn,
    Guid? AssigneeId,
    string? AssigneeName,
    bool RequiresTranslation,
    string? SourceLanguage,
    IReadOnlyList<string> TranslatingLanguages,
    Guid? HelpDeskTicketId,
    string? HelpDeskTicketNumber,
    Guid? DeptHeadId,
    string? DeptHeadName,
    bool RequiresTopManagementResolution,
    Guid? ResolutionManagerId,
    string? ResolutionManagerName,
    Guid? RoutedToDepartmentId,
    string? RoutedDepartmentName,
    string? AssignmentTask,
    DateTime? DueDate,
    bool RequiresResponse,
    string? RevisionNotes,
    IReadOnlyList<MemoRecipientDto> Recipients,
    IReadOnlyList<MemoCoordinatorDto> Coordinators,
    IReadOnlyList<MemoCommentDto> Comments,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateMemoRequest(
    string Title,
    string? TitleRu,
    string? AttachmentFileName,
    bool RequiresTranslation,
    string? AttachmentStorageKey = null,
    IReadOnlyList<MemoRecipientInput>? Recipients = null);

public record MemoRecipientInput(
    Guid? UserId,
    Guid? DepartmentId,
    bool ForInformation = true);

public record SendMemoToTranslationRequest(string SourceLanguage, IReadOnlyList<string> TranslatingLanguages);

public record SubmitMemoForApprovalRequest(
    Guid DeptHeadId,
    bool RequiresSpecialistCoordination);

public record MemoCoordinatorRequest(IReadOnlyList<Guid> UserIds);

public record RegisterMemoRequest(
    bool RequiresTopManagementResolution,
    Guid? ResolutionManagerId);

public record InformMemoRecipientsRequest(IReadOnlyList<Guid> UserIds);

public record RouteMemoRequest(Guid DepartmentId, string? Comment);

public record AssignMemoRequest(
    Guid AssigneeId,
    string AssignmentTask,
    DateTime? DueDate,
    bool RequiresResponse);

public record MemoApprovalRequest(string? Comment);

public record MemoRevisionRequest(string Comment);

public record MemoCommentRequest(string Body);

public record MemoPermissionsDto(
    bool IsInitiator,
    bool IsDeptHead,
    bool IsResolutionManager,
    bool IsRecipient,
    bool IsAssignee,
    bool IsRoutedDeptManager,
    bool CanCreate,
    bool CanEditDraft,
    bool CanSendToTranslation,
    bool CanSubmitForApproval,
    bool CanManageSpecialistCoordination,
    bool CanApproveDeptHead,
    bool CanRejectDeptHead,
    bool CanRegisterAndDistribute,
    bool CanActAsTopManagement,
    bool CanInformRecipients,
    bool CanRouteToDepartment,
    bool CanAssignWorker,
    bool CanAcceptExecution,
    bool CanReportCompletion,
    bool CanRequestRevision,
    bool CanAcceptCompletion,
    bool CanArchive,
    bool CanView);
