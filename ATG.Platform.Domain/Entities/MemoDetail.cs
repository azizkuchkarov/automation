using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

public class MemoDetail
{
    public Guid DocumentId { get; set; }
    public MemoPhase Phase { get; set; } = MemoPhase.Draft;
    public bool RequiresTranslation { get; set; }
    public Guid? HelpDeskTicketId { get; set; }
    public string? SourceLanguage { get; set; }
    public string? TranslatingLanguage { get; set; }
    public string? TranslatedAttachmentFileName { get; set; }
    public string? TranslatedAttachmentStorageKey { get; set; }
    public Guid? DeptHeadId { get; set; }
    public bool RequiresTopManagementResolution { get; set; }
    public Guid? ResolutionManagerId { get; set; }
    public Guid? RoutedById { get; set; }
    public Guid? RoutedToDepartmentId { get; set; }
    public string? AssignmentTask { get; set; }
    public DateTime? DueDate { get; set; }
    public bool RequiresResponse { get; set; }
    public string? RevisionNotes { get; set; }
    public DateTime? SentToTranslationAt { get; set; }
    public DateTime? TranslationReturnedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? DeptHeadApprovedAt { get; set; }
    public DateTime? CoordinationCompletedAt { get; set; }
    public DateTime? RegisteredAt { get; set; }
    public DateTime? RoutedAt { get; set; }
    public DateTime? ExecutorAcceptedAt { get; set; }
    public DateTime? ReportedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Document Document { get; set; } = null!;
    public User? DeptHead { get; set; }
    public User? ResolutionManager { get; set; }
    public User? RoutedBy { get; set; }
    public Department? RoutedToDepartment { get; set; }
    public ICollection<MemoRecipient> Recipients { get; set; } = [];
    public ICollection<MemoCoordinator> Coordinators { get; set; } = [];
    public ICollection<MemoComment> Comments { get; set; } = [];
}
