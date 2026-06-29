using ATG.Platform.Application.Common;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Application.Mappings;
using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Data;
using ATG.Platform.Infrastructure.Seeds;
using ATG.Platform.Infrastructure.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ATG.Platform.Infrastructure.Services;

public class TaskService(AppDbContext db, IAuditService audit, IMarketingRfqChannelService rfqChannels) : ITaskService
{
    public async Task<Result<TaskNavigationDto>> GetNavigationAsync(Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<TaskNavigationDto>.Fail("User not found");
        if (!CanUseOrgNavigation(actor))
            return Result<TaskNavigationDto>.Ok(new TaskNavigationDto([]));

        var orgs = await db.Organizations.Where(o => o.IsActive).ToListAsync(ct);
        var depts = await db.Departments.Where(d => d.IsActive).ToListAsync(ct);
        var allTasks = await LoadUnifiedTasksAsync(actor, null, null, ct);

        var countByDept = allTasks.GroupBy(t => t.DepartmentId).ToDictionary(g => g.Key, g => g.Count());
        var countByOrg = allTasks.GroupBy(t => t.OrganizationId).ToDictionary(g => g.Key, g => g.Count());

        int OrgCount(Guid orgId)
        {
            var org = orgs.First(o => o.Id == orgId);
            var orgDeptIds = depts.Where(d => d.OrganizationId == orgId).Select(d => d.Id).ToHashSet();
            var childOrgIds = orgs.Where(o => o.ParentId == orgId).Select(o => o.Id).ToList();
            var direct = countByOrg.GetValueOrDefault(orgId);
            var fromDepts = allTasks.Count(t => orgDeptIds.Contains(t.DepartmentId));
            var fromChildren = childOrgIds.Sum(id => OrgCount(id));
            return Math.Max(direct, fromDepts) + fromChildren;
        }

        TaskNavigationUnitDto BuildDeptNode(Department dept, List<Department> allDepts)
        {
            var children = allDepts
                .Where(d => d.ParentId == dept.Id)
                .OrderBy(d => TopologyOrder.GetDepartmentOrder(d.Code))
                .Select(d => BuildDeptNode(d, allDepts))
                .ToList();
            var count = countByDept.GetValueOrDefault(dept.Id) + children.Sum(c => c.TaskCount);
            return new TaskNavigationUnitDto(
                dept.Id, dept.Name, dept.NameEn, dept.Code, "department",
                dept.OrganizationId, count, children);
        }

        TaskNavigationUnitDto BuildStationNode(Organization station, List<Department> allDepts)
        {
            var stationDepts = allDepts
                .Where(d => d.OrganizationId == station.Id && d.ParentId == null)
                .OrderBy(d => TopologyOrder.GetDepartmentOrder(d.Code))
                .Select(d => BuildDeptNode(d, allDepts))
                .ToList();
            var count = countByOrg.GetValueOrDefault(station.Id) + stationDepts.Sum(d => d.TaskCount);
            return new TaskNavigationUnitDto(
                station.Id, station.Name, station.Name, station.Code, "station",
                station.Id, count, stationDepts);
        }

        var result = new List<TaskNavigationOrgDto>();
        foreach (var root in GetNavigationRoots(actor, orgs))
        {
            var units = new List<TaskNavigationUnitDto>();

            if (root.OrgType == OrgType.HeadOffice)
            {
                units.AddRange(depts
                    .Where(d => d.OrganizationId == root.Id && d.ParentId == null)
                    .OrderBy(d => TopologyOrder.GetDepartmentOrder(d.Code))
                    .Select(d => BuildDeptNode(d, depts)));
            }
            else if (root.OrgType == OrgType.BMGMC)
            {
                units.AddRange(depts
                    .Where(d => d.OrganizationId == root.Id && d.ParentId == null)
                    .OrderBy(d => TopologyOrder.GetDepartmentOrder(d.Code))
                    .Select(d => BuildDeptNode(d, depts)));

                units.AddRange(orgs
                    .Where(o => o.ParentId == root.Id && o.OrgType == OrgType.Station)
                    .OrderBy(o => TopologyOrder.GetOrganizationOrder(o.Code))
                    .Select(s => BuildStationNode(s, depts)));
            }

            result.Add(new TaskNavigationOrgDto(
                root.Id, root.Name, root.Code, root.OrgType.ToString(),
                OrgCount(root.Id), units));
        }

        return Result<TaskNavigationDto>.Ok(new TaskNavigationDto(result));
    }

    public async Task<Result<TaskAnalyticsDto>> GetAnalyticsAsync(
        Guid actorId,
        Guid? organizationId,
        Guid? departmentId,
        CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<TaskAnalyticsDto>.Fail("User not found");

        if (!CanAccessScope(actor, organizationId, departmentId))
            return Result<TaskAnalyticsDto>.Fail("Access denied");

        var tasks = await LoadUnifiedTasksAsync(actor, organizationId, departmentId, ct);

        var scopeLabel = await ResolveScopeLabelAsync(organizationId, departmentId, actor, ct);
        var scopeType = departmentId.HasValue ? "department" : organizationId.HasValue ? "organization" :
            CanUseOrgNavigation(actor) ? "organization" : IsDeptManager(actor) ? "department" : "personal";

        return Result<TaskAnalyticsDto>.Ok(BuildAnalytics(tasks, scopeType, scopeLabel, organizationId, departmentId));
    }

    public async Task<Result<PagedResult<TaskListItemDto>>> GetTasksAsync(
        Guid actorId,
        string view,
        int page,
        int pageSize,
        WorkTaskStatus? status,
        TaskSource? source,
        Guid? organizationId,
        Guid? departmentId,
        CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<PagedResult<TaskListItemDto>>.Fail("User not found");

        if (!CanAccessScope(actor, organizationId, departmentId))
            return Result<PagedResult<TaskListItemDto>>.Fail("Access denied");

        var tasks = await LoadUnifiedTasksAsync(actor, organizationId, departmentId, ct);

        tasks = view.ToLowerInvariant() switch
        {
            "mine" => tasks.Where(t => t.AssigneeId == actorId).ToList(),
            "department" when IsDeptManager(actor) =>
                tasks.Where(t => t.DepartmentId == actor.DepartmentId).ToList(),
            "all" when IsSuperAdmin(actor) || IsHoTopManager(actor) || actor.Role == UserRole.BMGMCManager => tasks,
            _ => tasks.Where(t => t.AssigneeId == actorId).ToList(),
        };

        if (status.HasValue) tasks = tasks.Where(t => t.Status == status).ToList();
        if (source.HasValue) tasks = tasks.Where(t => t.Source == source).ToList();

        var total = tasks.Count;
        var items = tasks
            .OrderByDescending(t => t.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => t.ToDto())
            .ToList();

        return Result<PagedResult<TaskListItemDto>>.Ok(new PagedResult<TaskListItemDto>(items, total, page, pageSize));
    }

    public async Task<Result<TaskListItemDto>> CreateAsync(
        CreateTaskRequest request,
        Guid actorId,
        string? ip,
        CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<TaskListItemDto>.Fail("User not found");

        if (!CanCreateForOthers(actor) && request.AssigneeId != actorId)
            return Result<TaskListItemDto>.Fail("You can only create tasks for yourself");

        var assignee = await db.Users.FirstOrDefaultAsync(u => u.Id == request.AssigneeId && u.IsActive, ct);
        if (assignee is null) return Result<TaskListItemDto>.Fail("Assignee not found");
        if (!CanAssignTo(actor, assignee))
            return Result<TaskListItemDto>.Fail("Cannot assign task to this user");

        var deptId = assignee.DepartmentId ?? actor.DepartmentId;
        if (deptId is null)
            return Result<TaskListItemDto>.Fail("Assignee has no department");

        var task = new WorkTask
        {
            Id = Guid.NewGuid(),
            Number = await GenerateNumberAsync(ct),
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? "",
            Status = WorkTaskStatus.New,
            Priority = request.Priority,
            Source = TaskSource.Manual,
            AssigneeId = assignee.Id,
            CreatedById = actorId,
            OrganizationId = assignee.OrganizationId,
            DepartmentId = deptId.Value,
            DueDate = request.DueDate,
        };

        db.WorkTasks.Add(task);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "TaskCreated", "WorkTask", task.Id, task.Number, ip, ct);

        var created = await TaskQuery().FirstAsync(t => t.Id == task.Id, ct);
        return Result<TaskListItemDto>.Ok(UnifiedTaskItem.FromWorkTask(created).ToDto());
    }

    public async Task<Result<TaskListItemDto>> UpdateStatusAsync(
        Guid id,
        UpdateTaskStatusRequest request,
        Guid actorId,
        string? ip,
        CancellationToken ct = default)
    {
        var task = await db.WorkTasks.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task is null) return Result<TaskListItemDto>.Fail("Task not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<TaskListItemDto>.Fail("User not found");
        if (!CanModifyTask(actor, task))
            return Result<TaskListItemDto>.Fail("Access denied");
        if (!IsValidTransition(task.Status, request.Status))
            return Result<TaskListItemDto>.Fail($"Cannot transition from {task.Status} to {request.Status}");

        task.Status = request.Status;
        task.UpdatedAt = DateTime.UtcNow;
        if (request.Status == WorkTaskStatus.InProgress && task.StartedAt is null)
            task.StartedAt = DateTime.UtcNow;
        if (request.Status == WorkTaskStatus.Done)
            task.CompletedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "TaskStatusUpdated", "WorkTask", task.Id, request.Status.ToString(), ip, ct);

        if (request.Status == WorkTaskStatus.Done)
            await rfqChannels.NotifyWorkTaskCompletedAsync(task.Id, ct);

        var updated = await TaskQuery().FirstAsync(t => t.Id == id, ct);
        return Result<TaskListItemDto>.Ok(UnifiedTaskItem.FromWorkTask(updated).ToDto());
    }

    public async Task<Result<IReadOnlyList<UserDto>>> GetAssignableUsersAsync(Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<IReadOnlyList<UserDto>>.Fail("User not found");

        IQueryable<User> query = db.Users.Where(u => u.IsActive);

        if (IsHoTopManager(actor))
            query = query.Where(u => u.Organization.Code == HoMasterData.OrganizationCode);
        else if (actor.Role == UserRole.BMGMCManager)
            query = query.Where(u => u.Organization.Code == BmgmcMasterData.OrganizationCode
                || u.Organization.Parent != null && u.Organization.Parent.Code == BmgmcMasterData.OrganizationCode);
        else if (IsDeptManager(actor) && actor.DepartmentId.HasValue)
            query = query.Where(u => u.DepartmentId == actor.DepartmentId);
        else
            query = query.Where(u => u.Id == actorId);

        var users = await query
            .Include(u => u.Organization).ThenInclude(o => o.Parent)
            .Include(u => u.Department)
            .Include(u => u.Position)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);

        return Result<IReadOnlyList<UserDto>>.Ok(users.Select(u => u.ToDto()).ToList());
    }

    private async Task<List<UnifiedTaskItem>> LoadUnifiedTasksAsync(
        User actor,
        Guid? organizationId,
        Guid? departmentId,
        CancellationToken ct)
    {
        var workTasks = await GetScopedWorkTasksQuery(actor, organizationId, departmentId).ToListAsync(ct);
        var tickets = await GetScopedTicketsQuery(actor, organizationId, departmentId).ToListAsync(ct);

        var linkedTicketIds = workTasks
            .Where(t => t.Source == TaskSource.HelpDesk && t.ExternalId.HasValue)
            .Select(t => t.ExternalId!.Value)
            .ToHashSet();

        var unified = workTasks.Select(UnifiedTaskItem.FromWorkTask).ToList();
        unified.AddRange(tickets
            .Where(t => !linkedTicketIds.Contains(t.Id))
            .Select(UnifiedTaskItem.FromTicket)
            .Where(t => t is not null)!);

        return unified;
    }

    private IQueryable<WorkTask> GetScopedWorkTasksQuery(User actor, Guid? organizationId, Guid? departmentId)
    {
        var query = TaskQuery();

        if (departmentId.HasValue)
            query = query.Where(t => t.DepartmentId == departmentId);
        else if (organizationId.HasValue)
        {
            var orgIds = GetOrgSubtreeIds(organizationId.Value);
            query = query.Where(t => orgIds.Contains(t.OrganizationId));
        }
        else
            query = ApplyActorScope(query, actor);

        return query;
    }

    private IQueryable<Ticket> GetScopedTicketsQuery(User actor, Guid? organizationId, Guid? departmentId)
    {
        var query = db.Tickets
            .Include(t => t.Requester)
            .Include(t => t.Assignee)
            .Include(t => t.TargetDepartment)
            .Include(t => t.Organization).ThenInclude(o => o.Parent)
            .Where(t => t.Status != TicketStatus.Cancelled);

        if (departmentId.HasValue)
            query = query.Where(t => t.TargetDepartmentId == departmentId);
        else if (organizationId.HasValue)
        {
            var orgIds = GetOrgSubtreeIds(organizationId.Value);
            query = query.Where(t => orgIds.Contains(t.OrganizationId));
        }
        else if (IsHoTopManager(actor))
            query = query.Where(t => t.Organization.Code == HoMasterData.OrganizationCode);
        else if (actor.Role == UserRole.BMGMCManager)
            query = query.Where(t =>
                t.Organization.Code == BmgmcMasterData.OrganizationCode ||
                (t.Organization.Parent != null && t.Organization.Parent.Code == BmgmcMasterData.OrganizationCode));
        else if (IsDeptManager(actor) && actor.DepartmentId.HasValue)
            query = query.Where(t => t.TargetDepartmentId == actor.DepartmentId);
        else
            query = query.Where(t => t.AssigneeId == actor.Id || t.RequesterId == actor.Id);

        return query;
    }

    private IQueryable<WorkTask> ApplyActorScope(IQueryable<WorkTask> query, User actor)
    {
        if (IsSuperAdmin(actor)) return query;
        if (IsHoTopManager(actor))
            return query.Where(t => t.Organization.Code == HoMasterData.OrganizationCode);
        if (actor.Role == UserRole.BMGMCManager)
            return query.Where(t =>
                t.Organization.Code == BmgmcMasterData.OrganizationCode ||
                (t.Organization.Parent != null && t.Organization.Parent.Code == BmgmcMasterData.OrganizationCode));
        if (IsDeptManager(actor) && actor.DepartmentId.HasValue)
            return query.Where(t => t.DepartmentId == actor.DepartmentId);
        return query.Where(t => t.AssigneeId == actor.Id);
    }

    private HashSet<Guid> GetOrgSubtreeIds(Guid orgId)
    {
        var orgs = db.Organizations.AsNoTracking().Where(o => o.IsActive).ToList();
        var result = new HashSet<Guid> { orgId };
        void AddChildren(Guid parentId)
        {
            foreach (var child in orgs.Where(o => o.ParentId == parentId))
            {
                result.Add(child.Id);
                AddChildren(child.Id);
            }
        }
        AddChildren(orgId);
        return result;
    }

    private static List<Organization> GetNavigationRoots(User actor, List<Organization> orgs)
    {
        if (IsSuperAdmin(actor))
        {
            return orgs
                .Where(o => o.Code is HoMasterData.OrganizationCode or BmgmcMasterData.OrganizationCode)
                .OrderBy(o => TopologyOrder.GetOrganizationOrder(o.Code))
                .ToList();
        }
        if (IsHoTopManager(actor))
            return orgs.Where(o => o.Code == HoMasterData.OrganizationCode).ToList();
        if (actor.Role == UserRole.BMGMCManager)
            return orgs.Where(o => o.Code == BmgmcMasterData.OrganizationCode).ToList();
        return [];
    }

    private static bool CanUseOrgNavigation(User actor) =>
        IsSuperAdmin(actor) || IsHoTopManager(actor) || actor.Role == UserRole.BMGMCManager;

    private static bool CanAccessScope(User actor, Guid? organizationId, Guid? departmentId)
    {
        if (!organizationId.HasValue && !departmentId.HasValue) return true;
        if (IsSuperAdmin(actor)) return true;
        if (departmentId.HasValue && IsDeptManager(actor) && actor.DepartmentId == departmentId) return true;
        if (IsHoTopManager(actor) && organizationId.HasValue) return true;
        if (actor.Role == UserRole.BMGMCManager) return true;
        return IsSuperAdmin(actor);
    }

    private async Task<string> ResolveScopeLabelAsync(
        Guid? organizationId, Guid? departmentId, User actor, CancellationToken ct)
    {
        if (departmentId.HasValue)
        {
            var d = await db.Departments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == departmentId, ct);
            return d?.Name ?? "Department";
        }
        if (organizationId.HasValue)
        {
            var o = await db.Organizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == organizationId, ct);
            return o?.Name ?? "Organization";
        }
        if (IsSuperAdmin(actor)) return "Enterprise";
        if (IsHoTopManager(actor)) return "Tashkent Head Office";
        if (actor.Role == UserRole.BMGMCManager) return "BMGMC";
        if (IsDeptManager(actor)) return actor.Department?.Name ?? "Department";
        return actor.FullName;
    }

    private static TaskAnalyticsDto BuildAnalytics(
        List<UnifiedTaskItem> tasks,
        string scope,
        string scopeLabel,
        Guid? organizationId,
        Guid? departmentId)
    {
        var total = tasks.Count;
        var newCount = tasks.Count(t => t.Status == WorkTaskStatus.New);
        var inProgress = tasks.Count(t => t.Status == WorkTaskStatus.InProgress);
        var done = tasks.Count(t => t.Status == WorkTaskStatus.Done);
        var cancelled = tasks.Count(t => t.Status == WorkTaskStatus.Cancelled);
        var active = total - cancelled;
        var completionRate = active > 0 ? Math.Round(done * 100.0 / active, 1) : 0;

        var distribution = new[] { WorkTaskStatus.New, WorkTaskStatus.InProgress, WorkTaskStatus.Done, WorkTaskStatus.Cancelled }
            .Where(s => s != WorkTaskStatus.Cancelled || cancelled > 0)
            .Select(s => new TaskStatusSliceDto(s, tasks.Count(t => t.Status == s), total > 0 ? Math.Round(tasks.Count(t => t.Status == s) * 100.0 / total, 1) : 0))
            .ToList();

        var bySource = Enum.GetValues<TaskSource>()
            .Select(src =>
            {
                var count = tasks.Count(t => t.Source == src);
                return new TaskSourceSliceDto(src, count, total > 0 ? Math.Round(count * 100.0 / total, 1) : 0);
            })
            .Where(s => s.Count > 0)
            .ToList();

        var weeklyTrend = BuildWeeklyTrend(tasks);
        var recent = tasks.OrderByDescending(t => t.UpdatedAt).Take(8).Select(t => t.ToDto()).ToList();

        IReadOnlyList<EmployeeTaskSummaryDto>? byEmployee = null;
        if (scope is "department" or "organization")
        {
            byEmployee = tasks
                .GroupBy(t => t.AssigneeId)
                .Select(g => new EmployeeTaskSummaryDto(
                    g.Key,
                    g.First().AssigneeName,
                    g.First().AssigneeEmployeeId,
                    g.Count(t => t.Status == WorkTaskStatus.New),
                    g.Count(t => t.Status == WorkTaskStatus.InProgress),
                    g.Count(t => t.Status == WorkTaskStatus.Done),
                    g.Count(t => t.Status != WorkTaskStatus.Cancelled)))
                .OrderByDescending(e => e.Total)
                .ThenBy(e => e.FullName)
                .ToList();
        }

        return new TaskAnalyticsDto(
            scope, scopeLabel, organizationId, departmentId,
            newCount, inProgress, done, cancelled, active, completionRate,
            distribution, bySource, weeklyTrend, recent, byEmployee);
    }

    private static List<TaskTrendPointDto> BuildWeeklyTrend(List<UnifiedTaskItem> tasks)
    {
        var points = new List<TaskTrendPointDto>();
        var today = DateTime.UtcNow.Date;
        for (var i = 5; i >= 0; i--)
        {
            var weekStart = today.AddDays(-7 * i);
            var weekEnd = weekStart.AddDays(7);
            points.Add(new TaskTrendPointDto(
                weekStart.ToString("dd MMM"),
                tasks.Count(t => t.CreatedAt >= weekStart && t.CreatedAt < weekEnd),
                tasks.Count(t => t.Status == WorkTaskStatus.InProgress && t.UpdatedAt >= weekStart && t.UpdatedAt < weekEnd),
                tasks.Count(t => t.CompletedAt >= weekStart && t.CompletedAt < weekEnd)));
        }
        return points;
    }

    private static bool IsValidTransition(WorkTaskStatus from, WorkTaskStatus to) => (from, to) switch
    {
        (WorkTaskStatus.New, WorkTaskStatus.InProgress) => true,
        (WorkTaskStatus.New, WorkTaskStatus.Cancelled) => true,
        (WorkTaskStatus.InProgress, WorkTaskStatus.Done) => true,
        (WorkTaskStatus.InProgress, WorkTaskStatus.Cancelled) => true,
        (WorkTaskStatus.InProgress, WorkTaskStatus.New) => true,
        _ => false
    };

    private static bool CanCreateForOthers(User actor) =>
        IsSuperAdmin(actor) || IsHoTopManager(actor) || IsDeptManager(actor) || actor.Role == UserRole.BMGMCManager;

    private static bool CanAssignTo(User actor, User assignee)
    {
        if (IsSuperAdmin(actor)) return true;
        if (IsHoTopManager(actor))
            return assignee.Organization.Code == HoMasterData.OrganizationCode;
        if (actor.Role == UserRole.BMGMCManager)
            return assignee.Organization.Code == BmgmcMasterData.OrganizationCode
                || assignee.Organization.Parent?.Code == BmgmcMasterData.OrganizationCode;
        if (IsDeptManager(actor))
            return actor.DepartmentId == assignee.DepartmentId;
        return actor.Id == assignee.Id;
    }

    private static bool CanModifyTask(User actor, WorkTask task) =>
        task.AssigneeId == actor.Id || IsSuperAdmin(actor) ||
        (IsDeptManager(actor) && actor.DepartmentId == task.DepartmentId);

    private static bool IsSuperAdmin(User u) => u.Role == UserRole.SuperAdmin;
    private static bool IsHoTopManager(User u) => u.Role == UserRole.HOTopManager;
    private static bool IsDeptManager(User u) =>
        u.Role is UserRole.HONachalnik or UserRole.BMGMCNachalnikiOtdeli or UserRole.BMGMCManager;

    private IQueryable<WorkTask> TaskQuery() =>
        db.WorkTasks
            .Include(t => t.Assignee)
            .Include(t => t.CreatedBy)
            .Include(t => t.Department)
            .Include(t => t.Organization).ThenInclude(o => o.Parent);

    private async Task<User?> GetActorAsync(Guid id, CancellationToken ct) =>
        await db.Users.Include(u => u.Organization).ThenInclude(o => o.Parent)
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    private async Task<string> GenerateNumberAsync(CancellationToken ct)
    {
        var count = await db.WorkTasks.CountAsync(ct);
        return $"TSK-{count + 1:D5}";
    }
}
