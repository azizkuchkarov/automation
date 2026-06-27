using ATG.Platform.Application.DTOs;
using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Infrastructure.Tasks;

internal sealed class UnifiedTaskItem
{
    public Guid Id { get; init; }
    public string Number { get; init; } = "";
    public string Title { get; init; } = "";
    public WorkTaskStatus Status { get; init; }
    public TaskPriority Priority { get; init; }
    public TaskSource Source { get; init; }
    public Guid? ExternalId { get; init; }
    public bool IsEditable { get; init; }
    public Guid AssigneeId { get; init; }
    public string AssigneeName { get; init; } = "";
    public string? AssigneeEmployeeId { get; init; }
    public Guid DepartmentId { get; init; }
    public string DepartmentName { get; init; } = "";
    public string DepartmentNameEn { get; init; } = "";
    public Guid OrganizationId { get; init; }
    public string CreatedByName { get; init; } = "";
    public DateTime? DueDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }

    public static UnifiedTaskItem FromWorkTask(WorkTask t) => new()
    {
        Id = t.Id,
        Number = t.Number,
        Title = t.Title,
        Status = t.Status,
        Priority = t.Priority,
        Source = t.Source,
        ExternalId = t.ExternalId,
        IsEditable = true,
        AssigneeId = t.AssigneeId,
        AssigneeName = t.Assignee.FullName,
        AssigneeEmployeeId = t.Assignee.EmployeeId,
        DepartmentId = t.DepartmentId,
        DepartmentName = t.Department.Name,
        DepartmentNameEn = t.Department.NameEn,
        OrganizationId = t.OrganizationId,
        CreatedByName = t.CreatedBy.FullName,
        DueDate = t.DueDate,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt,
        CompletedAt = t.CompletedAt,
    };

    public static UnifiedTaskItem? FromTicket(Ticket ticket)
    {
        if (ticket.Status == TicketStatus.Cancelled) return null;

        var assigneeId = ticket.AssigneeId ?? ticket.RequesterId;
        var assignee = ticket.Assignee ?? ticket.Requester;

        return new UnifiedTaskItem
        {
            Id = ticket.Id,
            Number = ticket.Number,
            Title = ticket.Title,
            Status = MapTicketStatus(ticket.Status),
            Priority = MapTicketPriority(ticket.Priority),
            Source = TaskSource.HelpDesk,
            ExternalId = ticket.Id,
            IsEditable = false,
            AssigneeId = assigneeId,
            AssigneeName = assignee.FullName,
            AssigneeEmployeeId = assignee.EmployeeId,
            DepartmentId = ticket.TargetDepartmentId,
            DepartmentName = ticket.TargetDepartment.Name,
            DepartmentNameEn = ticket.TargetDepartment.NameEn,
            OrganizationId = ticket.OrganizationId,
            CreatedByName = ticket.Requester.FullName,
            DueDate = null,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            CompletedAt = ticket.CompletedAt,
        };
    }

    public TaskListItemDto ToDto() => new(
        Id, Number, Title, Status, Priority, Source, IsEditable, ExternalId,
        AssigneeName, AssigneeId.ToString(),
        DepartmentName, DepartmentNameEn,
        CreatedByName, DueDate, CreatedAt, UpdatedAt);

    private static WorkTaskStatus MapTicketStatus(TicketStatus status) => status switch
    {
        TicketStatus.Open or TicketStatus.Assigned or TicketStatus.Accepted => WorkTaskStatus.New,
        TicketStatus.InProgress => WorkTaskStatus.InProgress,
        TicketStatus.Done or TicketStatus.Closed => WorkTaskStatus.Done,
        _ => WorkTaskStatus.Cancelled,
    };

    private static TaskPriority MapTicketPriority(TicketPriority priority) => priority switch
    {
        TicketPriority.Low => TaskPriority.Low,
        TicketPriority.High => TaskPriority.High,
        TicketPriority.Critical => TaskPriority.Critical,
        _ => TaskPriority.Medium,
    };
}
