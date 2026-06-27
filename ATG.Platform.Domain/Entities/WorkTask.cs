using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

public class WorkTask
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WorkTaskStatus Status { get; set; } = WorkTaskStatus.New;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public TaskSource Source { get; set; } = TaskSource.Manual;
    public Guid? ExternalId { get; set; }

    public Guid AssigneeId { get; set; }
    public Guid CreatedById { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid DepartmentId { get; set; }

    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public User Assignee { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
    public Department Department { get; set; } = null!;
}
