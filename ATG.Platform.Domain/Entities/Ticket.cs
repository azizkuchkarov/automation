using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

public class Ticket
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketCategory Category { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    public Guid RequesterId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid TargetDepartmentId { get; set; }
    public Guid? AssigneeId { get; set; }
    public Guid? AssignedById { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AssignedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public User Requester { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
    public Department TargetDepartment { get; set; } = null!;
    public User? Assignee { get; set; }
    public User? AssignedBy { get; set; }
    public ICollection<TicketComment> Comments { get; set; } = [];
    public ICollection<TicketActivity> Activities { get; set; } = [];
}
