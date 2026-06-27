using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

public class DocumentActivity
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid ActorId { get; set; }
    public string Action { get; set; } = string.Empty;
    public DocumentStatus? FromStatus { get; set; }
    public DocumentStatus? ToStatus { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Document Document { get; set; } = null!;
    public User Actor { get; set; } = null!;
}
