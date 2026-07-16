using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

public class OrderDetail
{
    public Guid DocumentId { get; set; }
    public OrderPhase Phase { get; set; } = OrderPhase.Draft;
    public Guid? DeptHeadId { get; set; }
    public Guid? LegalHeadId { get; set; }
    public Guid? SupervisingDeputyId { get; set; }
    public Guid? FirstDeputyId { get; set; }
    public Guid? GeneralDirectorId { get; set; }
    public string? RevisionNotes { get; set; }
    public string? ScanAttachmentFileName { get; set; }
    public string? ScanAttachmentStorageKey { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? DeptHeadApprovedAt { get; set; }
    public DateTime? LegalApprovedAt { get; set; }
    public DateTime? SupervisingDeputyApprovedAt { get; set; }
    public DateTime? FirstDeputyApprovedAt { get; set; }
    public DateTime? GeneralDirectorApprovedAt { get; set; }
    public DateTime? EdsFinalizedAt { get; set; }
    public DateTime? SentToRegistrarAt { get; set; }
    public DateTime? RegisteredAt { get; set; }
    public DateTime? PaperSignedAt { get; set; }
    public DateTime? ScanUploadedAt { get; set; }
    public DateTime? DistributedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CoordinationCompletedAt { get; set; }

    public Document Document { get; set; } = null!;
    public User? DeptHead { get; set; }
    public User? LegalHead { get; set; }
    public User? SupervisingDeputy { get; set; }
    public User? FirstDeputy { get; set; }
    public User? GeneralDirector { get; set; }
    public ICollection<OrderCoordinator> Coordinators { get; set; } = [];
    public ICollection<OrderRecipient> Recipients { get; set; } = [];
    public ICollection<OrderComment> Comments { get; set; } = [];
}
