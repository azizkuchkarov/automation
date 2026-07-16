using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Application.DTOs;

public record HelpDeskCategoryDto(
    TicketCategory Category,
    string NameEn,
    string NameRu,
    string Icon,
    string Color);

public record TicketDto(
    Guid Id,
    string Number,
    string Title,
    string Description,
    TicketCategory Category,
    TicketStatus Status,
    TicketPriority Priority,
    Guid RequesterId,
    string RequesterName,
    string RequesterEmail,
    Guid OrganizationId,
    string OrganizationName,
    Guid TargetDepartmentId,
    string TargetDepartmentName,
    string TargetDepartmentNameEn,
    Guid? AssigneeId,
    string? AssigneeName,
    Guid? AssignedById,
    string? AssignedByName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? AssignedAt,
    DateTime? AcceptedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    DateTime? ClosedAt,
    string? SourceLanguage,
    IReadOnlyList<string> TranslatingLanguages,
    Guid? LinkedDocumentId,
    string? LinkedOriginalFileName,
    string? LinkedOriginalStorageKey,
    string? LinkedTranslatedFileName,
    string? LinkedTranslatedStorageKey,
    IReadOnlyList<TicketCommentDto> Comments,
    IReadOnlyList<TicketActivityDto> Activities);

public record TicketListItemDto(
    Guid Id,
    string Number,
    string Title,
    TicketCategory Category,
    TicketStatus Status,
    TicketPriority Priority,
    string RequesterName,
    string? AssigneeName,
    string TargetDepartmentName,
    string TargetDepartmentNameEn,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record TicketCommentDto(
    Guid Id,
    Guid AuthorId,
    string AuthorName,
    string Body,
    bool IsInternal,
    DateTime CreatedAt);

public record TicketActivityDto(
    Guid Id,
    Guid ActorId,
    string ActorName,
    string Action,
    TicketStatus? FromStatus,
    TicketStatus? ToStatus,
    string? Details,
    DateTime CreatedAt);

public record CreateTicketRequest(
    string Title,
    string Description,
    TicketCategory Category,
    TicketPriority Priority,
    string? SourceLanguage = null,
    string? TranslatingLanguage = null);

public record AssignTicketRequest(Guid AssigneeId);

public record AddTicketCommentRequest(string Body, bool IsInternal = false);

public record UploadTranslationDocumentRequest(string FileName, string StorageKey);

public record HelpDeskDashboardDto(
    int TotalOpen,
    int TotalInProgress,
    int TotalDone,
    int TotalClosed,
    IReadOnlyList<TicketListItemDto> RecentTickets);

public record TicketBoardDto(
    IReadOnlyList<TicketListItemDto> Open,
    IReadOnlyList<TicketListItemDto> Assigned,
    IReadOnlyList<TicketListItemDto> Accepted,
    IReadOnlyList<TicketListItemDto> InProgress,
    IReadOnlyList<TicketListItemDto> Done,
    IReadOnlyList<TicketListItemDto> Closed);

public record HelpDeskAssigneeDto(Guid Id, string FullName, string Email, string Role);

public record HelpDeskStaffDto(
    Guid Id,
    string? EmployeeId,
    string FullName,
    string Email,
    string Role,
    string? JobTitleEn,
    string? JobTitleRu);

public record HelpDeskOrgRoutingDto(
    string OrganizationCode,
    string OrganizationName,
    Guid DepartmentId,
    string DepartmentCode,
    string DepartmentName,
    string DepartmentNameEn,
    int OpenTickets,
    int ActiveTickets,
    IReadOnlyList<HelpDeskStaffDto> Assigners,
    IReadOnlyList<HelpDeskStaffDto> Engineers);

public record HelpDeskCategoryRoutingDto(
    TicketCategory Category,
    string NameEn,
    string NameRu,
    string Icon,
    string Color,
    IReadOnlyList<HelpDeskOrgRoutingDto> Routes);

public record HelpDeskAdminControlDto(
    HelpDeskDashboardDto Dashboard,
    IReadOnlyList<HelpDeskCategoryRoutingDto> Categories);
