namespace ATG.Platform.Domain.Entities;

public class MemoCoordinator
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid UserId { get; set; }
    public DateTime? CoordinatedAt { get; set; }

    public MemoDetail Memo { get; set; } = null!;
    public User User { get; set; } = null!;
}
