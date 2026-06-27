using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

public class TicketActivity
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Guid ActorId { get; set; }
    public string Action { get; set; } = string.Empty;
    public TicketStatus? FromStatus { get; set; }
    public TicketStatus? ToStatus { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Ticket Ticket { get; set; } = null!;
    public User Actor { get; set; } = null!;
}
