using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

public class IncomingLetterDetail
{
    public Guid DocumentId { get; set; }
    public IncomingLetterPhase Phase { get; set; } = IncomingLetterPhase.Received;
    public bool RequiresTranslation { get; set; }
    public string? SourceLanguage { get; set; }
    public string? TranslatingLanguage { get; set; }
    public Guid? HelpDeskTicketId { get; set; }
    public string? TranslatedAttachmentFileName { get; set; }
    public string? TranslatedAttachmentStorageKey { get; set; }
    public DateTime? SentToTranslationAt { get; set; }
    public DateTime? TranslationReturnedAt { get; set; }
    public Guid? ResolutionManagerId { get; set; }
    public DateTime? SentForResolutionAt { get; set; }
    public DateTime? InformedAt { get; set; }
    public Guid? RoutedById { get; set; }
    public Guid? RoutedToDepartmentId { get; set; }
    public DateTime? RoutedAt { get; set; }
    public string? AssignmentTask { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ExecutorAcceptedAt { get; set; }
    public bool RequiresResponse { get; set; }
    public DateTime? ReportedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Document Document { get; set; } = null!;
    public User? ResolutionManager { get; set; }
    public User? RoutedBy { get; set; }
    public Department? RoutedToDepartment { get; set; }
    public ICollection<IncomingLetterRecipient> Recipients { get; set; } = [];
    public ICollection<IncomingLetterComment> Comments { get; set; } = [];
}
