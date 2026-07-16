namespace ATG.Platform.Domain.Entities;

public class OutgoingLetterCoordinator
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid UserId { get; set; }
    public bool ForDepartment { get; set; }
    public DateTime CoordinatedAt { get; set; } = DateTime.UtcNow;

    public OutgoingLetterDetail Letter { get; set; } = null!;
    public User User { get; set; } = null!;
}
