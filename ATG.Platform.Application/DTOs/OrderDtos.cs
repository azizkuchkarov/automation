using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Application.DTOs;

public record OrderUserDto(
    Guid Id,
    string FullName,
    string Email,
    string? EmployeeId,
    string DepartmentName,
    string DepartmentNameEn);

public record OrderCoordinatorDto(
    Guid Id,
    Guid UserId,
    string UserName,
    bool ForDepartment,
    DateTime CoordinatedAt);

public record OrderRecipientDto(
    Guid Id,
    Guid UserId,
    string UserName,
    DateTime? NotifiedAt);

public record OrderCommentDto(
    Guid Id,
    Guid AuthorId,
    string AuthorName,
    string Body,
    DateTime CreatedAt);

public record OrderDto(
    Guid Id,
    string Number,
    string Title,
    string? TitleRu,
    DocumentStatus Status,
    OrderPhase Phase,
    Guid AuthorId,
    string AuthorName,
    string? AttachmentFileName,
    string? AttachmentStorageKey,
    string? ScanAttachmentFileName,
    string? ScanAttachmentStorageKey,
    Guid OrganizationId,
    Guid DepartmentId,
    string DepartmentName,
    string DepartmentNameEn,
    Guid? DeptHeadId,
    string? DeptHeadName,
    Guid? LegalHeadId,
    string? LegalHeadName,
    Guid? SupervisingDeputyId,
    string? SupervisingDeputyName,
    Guid? FirstDeputyId,
    string? FirstDeputyName,
    Guid? GeneralDirectorId,
    string? GeneralDirectorName,
    string? RevisionNotes,
    IReadOnlyList<OrderCoordinatorDto> Coordinators,
    IReadOnlyList<OrderRecipientDto> Recipients,
    IReadOnlyList<OrderCommentDto> Comments,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateOrderRequest(
    string Title,
    string? TitleRu,
    string? AttachmentFileName,
    string? AttachmentStorageKey = null);

public record SubmitOrderRequest(
    Guid DeptHeadId,
    Guid SupervisingDeputyId,
    Guid FirstDeputyId,
    Guid GeneralDirectorId,
    bool RequiresSpecialistCoordination);

public record OrderCoordinatorRequest(IReadOnlyList<Guid> UserIds, bool ForDepartment);

public record OrderDistributionRequest(IReadOnlyList<Guid> UserIds);

public record OrderScanUploadRequest(string FileName, string StorageKey);

public record OrderApprovalRequest(string? Comment);

public record OrderRevisionRequest(string Comment);

public record OrderCommentRequest(string Body);

public record OrderPermissionsDto(
    bool IsInitiator,
    bool IsRegistrar,
    bool IsDeptHead,
    bool IsLegalHead,
    bool IsSupervisingDeputy,
    bool IsFirstDeputy,
    bool IsGeneralDirector,
    bool CanCreate,
    bool CanEditDraft,
    bool CanSubmitForApproval,
    bool CanApproveDeptHead,
    bool CanRejectDeptHead,
    bool CanManageSpecialistCoordination,
    bool CanManageDepartmentCoordination,
    bool CanApproveLegal,
    bool CanRejectLegal,
    bool CanApproveSupervisingDeputy,
    bool CanApproveFirstDeputy,
    bool CanApproveGeneralDirector,
    bool CanRejectApproval,
    bool CanFinalizeEds,
    bool CanSendToRegistrar,
    bool CanRegister,
    bool CanConfirmPaperSignature,
    bool CanUploadScan,
    bool CanDistribute,
    bool CanArchive,
    bool CanView);
