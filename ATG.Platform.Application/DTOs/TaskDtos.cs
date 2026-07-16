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

public record TaskPrioritySliceDto(TaskPriority Priority, int Count, double Percent);

public record TaskAgingBucketDto(string Key, int Count, double Percent, int MinDays, int? MaxDays);

public record TaskVelocityPointDto(string Label, int Completed, double MovingAverage);

public record TaskInsightDto(string Code, string Severity, double? Value, string? Context);

public record TaskHealthScoreDto(
    double Score,
    string Grade,
    double CompletionComponent,
    double SlaComponent,
    double VelocityComponent,
    double BalanceComponent,
    double RiskPenalty);

public record TaskSlaMetricsDto(
    double CompliancePercent,
    int WithDueDate,
    int OnTime,
    int Late,
    int AtRisk);

public record TaskCycleTimeDto(double P50Days, double P75Days, double P90Days, double MeanDays);

public record TaskHeatmapCellDto(int DayOfWeek, string Label, int Created, int Completed, int Intensity);

public record TaskForecastPointDto(string Label, int Actual, int? Forecast, bool IsProjected);

public record TaskBurndownPointDto(string Label, int Remaining, int Ideal, int Completed);

public record TaskRiskItemDto(
    Guid Id,
    string Number,
    string Title,
    string AssigneeName,
    TaskPriority Priority,
    double RiskScore,
    string RiskLevel,
    int AgeDays,
    bool IsOverdue);

public record TaskWorkloadBalanceDto(
    double BalanceScore,
    double GiniCoefficient,
    int AssigneeCount,
    double AvgLoad,
    int MaxLoad);

public record TaskPriorityStatusCellDto(TaskPriority Priority, WorkTaskStatus Status, int Count);

public record EmployeeTaskSummaryDto(
    Guid UserId,
    string FullName,
    string? EmployeeId,
    int NewCount,
    int InProgressCount,
    int DoneCount,
    int Total,
    double CompletionRate);

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
    IReadOnlyList<TaskPrioritySliceDto> ByPriority,
    IReadOnlyList<TaskAgingBucketDto> AgingBuckets,
    IReadOnlyList<TaskTrendPointDto> WeeklyTrend,
    IReadOnlyList<TaskVelocityPointDto> VelocityTrend,
    int OverdueCount,
    double AvgResolutionDays,
    double ThroughputChangePercent,
    IReadOnlyList<TaskInsightDto> Insights,
    TaskHealthScoreDto HealthScore,
    TaskSlaMetricsDto SlaMetrics,
    TaskCycleTimeDto CycleTime,
    IReadOnlyList<TaskHeatmapCellDto> ActivityHeatmap,
    IReadOnlyList<TaskForecastPointDto> CompletionForecast,
    IReadOnlyList<TaskBurndownPointDto> Burndown,
    IReadOnlyList<TaskRiskItemDto> RiskQueue,
    TaskWorkloadBalanceDto WorkloadBalance,
    IReadOnlyList<TaskPriorityStatusCellDto> PriorityMatrix,
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
