namespace ATG.Platform.Domain.Entities;

public class OrderComment
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid AuthorId { get; set; }
    public string Body { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public OrderDetail Order { get; set; } = null!;
    public User Author { get; set; } = null!;
}
