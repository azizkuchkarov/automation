namespace ATG.Platform.Domain.Entities;

public class IncomingLetterRecipient
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid UserId { get; set; }
    public bool Informed { get; set; }
    public bool ForInformation { get; set; } = true;
    public DateTime? InformedAt { get; set; }
    public Guid? TaskId { get; set; }

    public IncomingLetterDetail Letter { get; set; } = null!;
    public User User { get; set; } = null!;
}
