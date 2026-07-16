namespace ATG.Platform.Domain.Entities;

public class MemoRecipient
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? DepartmentId { get; set; }
    public bool ForInformation { get; set; } = true;
    public DateTime? NotifiedAt { get; set; }
    public Guid? TaskId { get; set; }

    public MemoDetail Memo { get; set; } = null!;
    public User? User { get; set; }
    public Department? Department { get; set; }
}
