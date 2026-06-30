using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

public class UserNotification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
