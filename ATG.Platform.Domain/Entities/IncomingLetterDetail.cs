using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

public class IncomingLetterDetail
{
    public Guid DocumentId { get; set; }
    public IncomingLetterPhase Phase { get; set; } = IncomingLetterPhase.Registered;
    public DateTime? InformedAt { get; set; }
    public Guid? RoutedById { get; set; }
    public Guid? RoutedToDepartmentId { get; set; }
    public DateTime? RoutedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Document Document { get; set; } = null!;
    public User? RoutedBy { get; set; }
    public Department? RoutedToDepartment { get; set; }
    public ICollection<IncomingLetterRecipient> Recipients { get; set; } = [];
    public ICollection<IncomingLetterComment> Comments { get; set; } = [];
}
