using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Application.DTOs;

public record IncomingLetterUserDto(
    Guid Id,
    string FullName,
    string Email,
    string? EmployeeId,
    string DepartmentName,
    string DepartmentNameEn);

public record IncomingLetterDepartmentDto(
    Guid Id,
    string Code,
    string Name,
    string NameEn);

public record IncomingLetterRecipientDto(
    Guid Id,
    Guid UserId,
    string UserName,
    bool Informed,
    DateTime? InformedAt,
    Guid? TaskId);

public record IncomingLetterCommentDto(
    Guid Id,
    Guid AuthorId,
    string AuthorName,
    string Body,
    DateTime CreatedAt);

public record IncomingLetterDto(
    Guid Id,
    string Number,
    string Title,
    string? TitleRu,
    DocumentStatus Status,
    IncomingLetterPhase Phase,
    Guid AuthorId,
    string AuthorName,
    string? IncomingNumber,
    DateTime? IncomingDate,
    string? RecordBook,
    string? SenderName,
    string? ReceiverName,
    string? AttachmentFileName,
    int TranslationRequestCount,
    Guid OrganizationId,
    Guid DepartmentId,
    string DepartmentName,
    string DepartmentNameEn,
    Guid? AssigneeId,
    string? AssigneeName,
    Guid? RoutedToDepartmentId,
    string? RoutedToDepartmentName,
    string? RoutedToDepartmentNameEn,
    string? RoutedByName,
    DateTime? RegisteredAt,
    DateTime? InformedAt,
    DateTime? RoutedAt,
    DateTime? CompletedAt,
    IReadOnlyList<IncomingLetterRecipientDto> Recipients,
    IReadOnlyList<IncomingLetterCommentDto> Comments,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateIncomingLetterRequest(
    string Title,
    string? TitleRu,
    string? IncomingNumber,
    DateTime? IncomingDate,
    string? RecordBook,
    string? SenderName,
    string? ReceiverName,
    string? AttachmentFileName,
    int TranslationRequestCount);

public record InformTopManagersRequest(IReadOnlyList<Guid> TopManagerIds);

public record RouteIncomingLetterRequest(
    Guid TargetDepartmentId,
    string? Comment);

public record AssignIncomingLetterRequest(
    Guid AssigneeId,
    string? Comment);

public record IncomingLetterCommentRequest(string Body);

public record IncomingLetterPermissionsDto(
    bool IsRegistrar,
    bool CanInform,
    bool CanRoute,
    bool CanAssign,
    bool CanComplete,
    bool CanComment);
