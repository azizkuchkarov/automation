namespace ATG.Platform.Domain.Entities;

public class OutgoingLetterComment
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid AuthorId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public OutgoingLetterDetail Letter { get; set; } = null!;
    public User Author { get; set; } = null!;
}
