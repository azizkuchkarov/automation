using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

public class MarketingRfqChannelRequest
{
    public Guid Id { get; set; }
    public Guid MarketingRecordId { get; set; }
    public Guid DocumentId { get; set; }
    public MarketingRfqChannelType Channel { get; set; }
    public MarketingRfqChannelStatus Status { get; set; } = MarketingRfqChannelStatus.Open;

    public Guid? HelpDeskTicketId { get; set; }
    public Guid? WorkTaskId { get; set; }
    public Guid? AssignedUserId { get; set; }

    public string? ExternalNumber { get; set; }
    public Guid CreatedById { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public MarketingRecord Record { get; set; } = null!;
    public User? AssignedUser { get; set; }
}
