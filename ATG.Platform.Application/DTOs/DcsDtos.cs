using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Application.DTOs;

public record DcsTypeDto(
    DocumentType Type,
    string NameEn,
    string NameRu,
    string Section,
    string Icon,
    string Color);

public record DcsStaffDto(
    Guid Id,
    string? EmployeeId,
    string FullName,
    string Email,
    string Role,
    string? JobTitleEn,
    string? JobTitleRu);

public record DcsOrgRoutingDto(
    string OrganizationCode,
    string OrganizationName,
    Guid DepartmentId,
    string DepartmentCode,
    string DepartmentName,
    string DepartmentNameEn,
    int DraftCount,
    int ActiveCount,
    IReadOnlyList<DcsStaffDto> Assigners,
    IReadOnlyList<DcsStaffDto> Handlers,
    DcsStaffDto? DesignatedRegistrar);

public record DcsCategoryRoutingDto(
    DocumentType Type,
    string NameEn,
    string NameRu,
    string Section,
    string Icon,
    string Color,
    IReadOnlyList<DcsOrgRoutingDto> Routes);

public record DcsDashboardDto(
    int TotalDraft,
    int TotalInReview,
    int TotalApproved,
    int TotalArchived,
    IReadOnlyList<DocumentListItemDto> RecentDocuments);

public record DcsAdminControlDto(
    DcsDashboardDto Dashboard,
    IReadOnlyList<DcsCategoryRoutingDto> Categories);

public record DocumentListItemDto(
    Guid Id,
    string Number,
    string Title,
    DocumentType Type,
    DocumentStatus Status,
    string AuthorName,
    string? AssigneeName,
    string DepartmentName,
    string DepartmentNameEn,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record DocumentActivityDto(
    Guid Id,
    Guid ActorId,
    string ActorName,
    string Action,
    DocumentStatus? FromStatus,
    DocumentStatus? ToStatus,
    string? Details,
    DateTime CreatedAt);

public record DocumentDto(
    Guid Id,
    string Number,
    string Title,
    string Description,
    DocumentType Type,
    DocumentStatus Status,
    Guid AuthorId,
    string AuthorName,
    string? AuthorNameEn,
    string AuthorEmail,
    Guid OrganizationId,
    string OrganizationName,
    Guid DepartmentId,
    string DepartmentName,
    string DepartmentNameEn,
    Guid? AssigneeId,
    string? AssigneeName,
    string? ExternalReference,
    DateTime? RegisteredAt,
    DateTime? DueDate,
    string? TitleRu,
    string? IncomingNumber,
    DateTime? IncomingDate,
    string? RecordBook,
    string? SenderName,
    string? ReceiverName,
    string? ReceiverNameEn,
    string? AttachmentFileName,
    int TranslationRequestCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<DocumentActivityDto> Activities);

public record CreateDocumentRequest(
    string Title,
    string Description,
    DocumentType Type,
    string? ExternalReference,
    DateTime? DueDate);

public record UpdateDocumentStatusRequest(DocumentStatus Status);
