using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

public class OutgoingLetterDetail
{
    public Guid DocumentId { get; set; }
    public OutgoingLetterPhase Phase { get; set; } = OutgoingLetterPhase.Draft;
    public bool RequiresTranslation { get; set; }
    public Guid? HelpDeskTicketId { get; set; }
    public string? SourceLanguage { get; set; }
    public string? TranslatingLanguage { get; set; }
    public string? TranslatedAttachmentFileName { get; set; }
    public string? TranslatedAttachmentStorageKey { get; set; }
    public Guid? DeptHeadId { get; set; }
    public Guid? SupervisingDeputyId { get; set; }
    public Guid? FirstDeputyId { get; set; }
    public Guid? GeneralDirectorId { get; set; }
    public string? RevisionNotes { get; set; }
    public DateTime? SentToTranslationAt { get; set; }
    public DateTime? TranslationReturnedAt { get; set; }
    public DateTime? SubmittedToEdsAt { get; set; }
    public DateTime? DeptHeadApprovedAt { get; set; }
    public DateTime? CoordinationCompletedAt { get; set; }
    public DateTime? SupervisingDeputyApprovedAt { get; set; }
    public DateTime? FirstDeputyApprovedAt { get; set; }
    public DateTime? GeneralDirectorApprovedAt { get; set; }
    public DateTime? EdsFinalizedAt { get; set; }
    public DateTime? SentToRegistrarAt { get; set; }
    public DateTime? RegisteredAt { get; set; }
    public DateTime? PaperSignedAt { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Document Document { get; set; } = null!;
    public User? DeptHead { get; set; }
    public User? SupervisingDeputy { get; set; }
    public User? FirstDeputy { get; set; }
    public User? GeneralDirector { get; set; }
    public ICollection<OutgoingLetterCoordinator> Coordinators { get; set; } = [];
    public ICollection<OutgoingLetterComment> Comments { get; set; } = [];
}
