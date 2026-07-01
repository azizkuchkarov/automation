using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Application.DTOs;

public record HrLeaveItemDto(
    Guid Id,
    HrLeaveItemType Type,
    DateTime? DateFrom,
    DateTime? DateTo,
    int? DaysCount,
    string? NoteRu,
    string? NoteEn,
    int SortOrder,
    string TextRu,
    string TextEn);

public record HrLeaveApproverDto(
    Guid Id,
    Guid UserId,
    string UserName,
    HrLeaveApprovalRole Role,
    HrLeaveApproverStatus Status,
    int SortOrder,
    int ApprovalGroup,
    DateTime? DecidedAt,
    string? Comment,
    string? DepartmentName,
    string? DepartmentNameEn);

public record HrLeaveTimelineEventDto(
    Guid Id,
    string Action,
    string ActorName,
    string? Details,
    DateTime CreatedAt);

public record HrLeavePermissionsDto(
    bool CanCreate,
    bool CanEdit,
    bool CanSubmit,
    bool CanHrReview,
    bool CanApprove,
    bool CanReject);

public record HrLeaveRequestDto(
    Guid Id,
    string Number,
    DocumentStatus Status,
    HrLeaveRequestPhase Phase,
    HrLeaveTrack Track,
    string PeriodLabel,
    DateTime RequestDate,
    string AuthorName,
    string DepartmentName,
    string DepartmentNameEn,
    string OrganizationName,
    string HrDepartmentName,
    string HrDepartmentNameEn,
    string? HrTaskNumber,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<HrLeaveItemDto> Items,
    IReadOnlyList<HrLeaveApproverDto> Approvers,
    IReadOnlyList<HrLeaveTimelineEventDto> Timeline,
    HrLeavePermissionsDto Permissions);

public record HrLeaveListItemDto(
    Guid Id,
    string Number,
    DocumentStatus Status,
    HrLeaveRequestPhase Phase,
    string AuthorName,
    string DepartmentName,
    string DepartmentNameEn,
    DateTime RequestDate,
    DateTime CreatedAt,
    int ItemCount);

public record CreateHrLeaveItemRequest(
    HrLeaveItemType Type,
    DateTime? DateFrom,
    DateTime? DateTo,
    int? DaysCount,
    string? NoteRu,
    string? NoteEn);

public record CreateHrLeaveRequestRequest(
    string PeriodLabel,
    DateTime RequestDate,
    IReadOnlyList<CreateHrLeaveItemRequest> Items);

public record UpdateHrLeaveRequestRequest(
    string PeriodLabel,
    DateTime RequestDate,
    IReadOnlyList<CreateHrLeaveItemRequest> Items);

public record HrLeaveApprovalRequest(string? Comment);
