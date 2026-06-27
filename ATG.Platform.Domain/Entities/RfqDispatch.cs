using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

public class RfqDispatch
{
    public Guid Id { get; set; }
    public Guid MarketingRecordId { get; set; }
    public RfqDispatchType DispatchType { get; set; }
    public string? RecipientName { get; set; }
    public string? RecipientEmail { get; set; }
    public string? RecipientPhone { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResponseReceivedAt { get; set; }
    public DateTime? FollowupSentAt { get; set; }
    public bool FollowupPhoneCalled { get; set; }
    public string? Notes { get; set; }

    public MarketingRecord Record { get; set; } = null!;
}
