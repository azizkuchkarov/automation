namespace ATG.Platform.Domain.Entities;

public class OrderRecipient
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid UserId { get; set; }
    public DateTime? NotifiedAt { get; set; }
    public Guid? TaskId { get; set; }

    public OrderDetail Order { get; set; } = null!;
    public User User { get; set; } = null!;
}
