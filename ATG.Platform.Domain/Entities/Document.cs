using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

public class Document
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DocumentType Type { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

    public Guid AuthorId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid? AssigneeId { get; set; }

    public string? ExternalReference { get; set; }
    public DateTime? RegisteredAt { get; set; }
    public DateTime? DueDate { get; set; }

    // Incoming / outgoing letter fields
    public string? TitleRu { get; set; }
    public string? IncomingNumber { get; set; }
    public DateTime? IncomingDate { get; set; }
    public string? RecordBook { get; set; }
    public string? SenderName { get; set; }
    public string? ReceiverName { get; set; }
    public string? AttachmentFileName { get; set; }
    public int TranslationRequestCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User Author { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
    public Department Department { get; set; } = null!;
    public User? Assignee { get; set; }
    public ICollection<DocumentActivity> Activities { get; set; } = [];
}
