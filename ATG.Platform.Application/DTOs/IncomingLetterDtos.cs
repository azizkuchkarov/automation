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

    bool ForInformation,

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

    string? AttachmentStorageKey,

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

    string? TranslatedAttachmentFileName,

    string? TranslatedAttachmentStorageKey,

    Guid? ResolutionManagerId,

    string? ResolutionManagerName,

    Guid? RoutedToDepartmentId,

    string? RoutedToDepartmentName,

    string? RoutedToDepartmentNameEn,

    string? RoutedByName,

    string? AssignmentTask,

    DateTime? DueDate,

    bool RequiresResponse,

    DateTime? RegisteredAt,

    DateTime? SentToTranslationAt,

    DateTime? TranslationReturnedAt,

    DateTime? SentForResolutionAt,

    DateTime? InformedAt,

    DateTime? RoutedAt,

    DateTime? ExecutorAcceptedAt,

    DateTime? ReportedAt,

    DateTime? ReviewedAt,

    DateTime? ArchivedAt,

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

    int TranslationRequestCount,

    bool RequiresTranslation,

    string? AttachmentStorageKey = null);



public record SendToTranslationRequest(string SourceLanguage, IReadOnlyList<string> TranslatingLanguages);



public record SendForResolutionRequest(IReadOnlyList<Guid> ResolutionManagerIds);



public record InformTopManagersRequest(IReadOnlyList<Guid> TopManagerIds);



public record RouteIncomingLetterRequest(

    Guid TargetDepartmentId,

    string? Comment);



public record AssignIncomingLetterRequest(

    Guid AssigneeId,

    string? AssignmentTask,

    DateTime? DueDate,

    string? Comment);



public record AcceptExecutionRequest(bool RequiresResponse);



public record IncomingLetterCommentRequest(string Body);



public record IncomingLetterPermissionsDto(

    bool IsRegistrar,

    bool IsTranslationDept,

    bool IsResolutionManager,

    bool CanSendToTranslation,

    bool CanCompleteTranslation,

    bool CanRegisterInEds,

    bool CanSendForResolution,

    bool CanInformAdditional,

    bool CanRoute,

    bool CanAssign,

    bool CanAccept,

    bool CanReport,

    bool CanRequestRevision,

    bool CanAcceptCompletion,

    bool CanArchive,

    bool CanComment);



public static class TranslationLanguageOptions

{

    public static readonly IReadOnlyList<string> Codes =

        ["en", "ru", "uz", "zh", "ko", "fr", "de", "ar", "tr", "other"];

}



public static class TranslationLanguages

{

    public static string Join(IEnumerable<string> codes) =>

        string.Join(',', codes.Select(Normalize).Where(c => c.Length > 0).Distinct());



    public static IReadOnlyList<string> Parse(string? value)

    {

        if (string.IsNullOrWhiteSpace(value)) return [];

        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)

            .Select(Normalize)

            .Where(c => c.Length > 0)

            .Distinct()

            .ToList();

    }



    public static string? Validate(IReadOnlyList<string>? targets, string? sourceLanguage)

    {

        if (targets is null || targets.Count == 0)

            return "At least one target language is required";



        var source = Normalize(sourceLanguage ?? "");

        if (source.Length == 0)

            return "Source language is required";



        if (!TranslationLanguageOptions.Codes.Contains(source))

            return "Invalid source language selected";



        var normalized = targets.Select(Normalize).Where(c => c.Length > 0).Distinct().ToList();

        if (normalized.Count == 0)

            return "At least one target language is required";



        if (normalized.Any(c => !TranslationLanguageOptions.Codes.Contains(c)))

            return "Invalid target language selected";



        if (normalized.Contains(source))

            return "Target languages must differ from source language";



        return null;

    }



    private static string Normalize(string code) => code.Trim().ToLowerInvariant();

}


