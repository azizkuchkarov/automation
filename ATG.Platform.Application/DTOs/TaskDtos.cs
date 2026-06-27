using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Application.DTOs;

public record TaskListItemDto(
    Guid Id,
    string Number,
    string Title,
    WorkTaskStatus Status,
    TaskPriority Priority,
    TaskSource Source,
    bool IsEditable,
    Guid? ExternalId,
    string AssigneeName,
    string AssigneeId,
    string DepartmentName,
    string DepartmentNameEn,
    string CreatedByName,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateTaskRequest(
    string Title,
    string Description,
    Guid AssigneeId,
    TaskPriority Priority = TaskPriority.Medium,
    DateTime? DueDate = null);

public record UpdateTaskStatusRequest(WorkTaskStatus Status);

public record TaskStatusSliceDto(WorkTaskStatus Status, int Count, double Percent);

public record TaskSourceSliceDto(TaskSource Source, int Count, double Percent);

public record TaskTrendPointDto(string Label, int New, int InProgress, int Done);

public record EmployeeTaskSummaryDto(
    Guid UserId,
    string FullName,
    string? EmployeeId,
    int NewCount,
    int InProgressCount,
    int DoneCount,
    int Total);

public record TaskAnalyticsDto(
    string Scope,
    string ScopeLabel,
    Guid? OrganizationId,
    Guid? DepartmentId,
    int TotalNew,
    int TotalInProgress,
    int TotalDone,
    int TotalCancelled,
    int TotalActive,
    double CompletionRate,
    IReadOnlyList<TaskStatusSliceDto> StatusDistribution,
    IReadOnlyList<TaskSourceSliceDto> BySource,
    IReadOnlyList<TaskTrendPointDto> WeeklyTrend,
    IReadOnlyList<TaskListItemDto> RecentTasks,
    IReadOnlyList<EmployeeTaskSummaryDto>? ByEmployee);

public record TaskNavigationUnitDto(
    Guid Id,
    string Name,
    string NameEn,
    string Code,
    string UnitType,
    Guid OrganizationId,
    int TaskCount,
    IReadOnlyList<TaskNavigationUnitDto> Children);

public record TaskNavigationOrgDto(
    Guid Id,
    string Name,
    string Code,
    string OrgType,
    int TaskCount,
    IReadOnlyList<TaskNavigationUnitDto> Units);

public record TaskNavigationDto(IReadOnlyList<TaskNavigationOrgDto> Organizations);
