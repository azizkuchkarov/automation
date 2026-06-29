using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

public class ProcurementStepComment
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public ProcurementWorkflowPhase Phase { get; set; }
    public int StepNumber { get; set; }
    public Guid AuthorId { get; set; }
    public string Body { get; set; } = string.Empty;
    public ProcurementStepCommentKind Kind { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ProcurementRequestDetail Request { get; set; } = null!;
    public User Author { get; set; } = null!;
}
