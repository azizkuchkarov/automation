using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Application.DTOs;

public record OutgoingLetterUserDto(
    Guid Id,
    string FullName,
    string Email,
    string? EmployeeId,
    string DepartmentName,
    string DepartmentNameEn);

public record OutgoingLetterCoordinatorDto(
    Guid Id,
    Guid UserId,
    string UserName,
    bool ForDepartment,
    DateTime CoordinatedAt);

public record OutgoingLetterCommentDto(
    Guid Id,
    Guid AuthorId,
    string AuthorName,
    string Body,
    DateTime CreatedAt);

public record OutgoingLetterDto(
    Guid Id,
    string Number,
    string Title,
    string? TitleRu,
    string Status,
    OutgoingLetterPhase Phase,
    Guid AuthorId,
    string AuthorName,
    string? AddresseeName,
    string? AttachmentFileName,
    string? AttachmentStorageKey,
    string? TranslatedAttachmentFileName,
    string? TranslatedAttachmentStorageKey,
    Guid OrganizationId,
    Guid DepartmentId,
    string DepartmentName,
    string DepartmentNameEn,
    bool RequiresTranslation,
    string? SourceLanguage,
    IReadOnlyList<string> TranslatingLanguages,
    Guid? HelpDeskTicketId,
    string? HelpDeskTicketNumber,
    Guid? DeptHeadId,
    string? DeptHeadName,
    Guid? SupervisingDeputyId,
    string? SupervisingDeputyName,
    Guid? FirstDeputyId,
    string? FirstDeputyName,
    Guid? GeneralDirectorId,
    string? GeneralDirectorName,
    string? RevisionNotes,
    DateTime? SentToTranslationAt,
    DateTime? TranslationReturnedAt,
    DateTime? SubmittedToEdsAt,
    DateTime? DeptHeadApprovedAt,
    DateTime? RegisteredAt,
    DateTime? DispatchedAt,
    DateTime? CompletedAt,
    IReadOnlyList<OutgoingLetterCoordinatorDto> Coordinators,
    IReadOnlyList<OutgoingLetterCommentDto> Comments,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateOutgoingLetterRequest(
    string Title,
    string? TitleRu,
    string? AddresseeName,
    string? AttachmentFileName,
    bool RequiresTranslation,
    string? AttachmentStorageKey = null);

public record SendOutgoingToTranslationRequest(string SourceLanguage, IReadOnlyList<string> TranslatingLanguages);

public record SubmitOutgoingToEdsRequest(
    Guid DeptHeadId,
    Guid? SupervisingDeputyId,
    Guid? FirstDeputyId,
    Guid? GeneralDirectorId);

public record OutgoingCoordinatorRequest(IReadOnlyList<Guid> UserIds, bool ForDepartment);

public record OutgoingApprovalRequest(string? Comment);

public record OutgoingRevisionRequest(string Comment);

public record OutgoingLetterCommentRequest(string Body);

public record OutgoingLetterPermissionsDto(
    bool IsInitiator,
    bool IsRegistrar,
    bool IsDeptHead,
    bool IsTranslationDept,
    bool IsSupervisingDeputy,
    bool IsFirstDeputy,
    bool IsGeneralDirector,
    bool CanCreate,
    bool CanEditDraft,
    bool CanSendToTranslation,
    bool CanSubmitToEds,
    bool CanApproveDeptHead,
    bool CanRejectDeptHead,
    bool CanManageSpecialistCoordination,
    bool CanManageDepartmentCoordination,
    bool CanSkipCoordination,
    bool CanApproveSupervisingDeputy,
    bool CanApproveFirstDeputy,
    bool CanApproveGeneralDirector,
    bool CanFinalizeEds,
    bool CanSendToRegistrar,
    bool CanRegister,
    bool CanConfirmPaperSignature,
    bool CanConfirmDispatch,
    bool CanArchive,
    bool CanView);
