namespace ATG.Platform.Domain.Entities;

public class OrderCoordinator
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid UserId { get; set; }
    public bool ForDepartment { get; set; }
    public DateTime CoordinatedAt { get; set; } = DateTime.UtcNow;

    public OrderDetail Order { get; set; } = null!;
    public User User { get; set; } = null!;
}
