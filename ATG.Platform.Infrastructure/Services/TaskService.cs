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

public class TaskService(AppDbContext db, IAuditService audit, IMarketingRfqChannelService rfqChannels, INotificationService notifications) : ITaskService
{
    public async Task<Result<TaskNavigationDto>> GetNavigationAsync(Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<TaskNavigationDto>.Fail("User not found");
        if (!CanUseOrgNavigation(actor))
            return Result<TaskNavigationDto>.Ok(new TaskNavigationDto([]));

        var orgs = await db.Organizations.AsNoTracking().Where(o => o.IsActive).ToListAsync(ct);
        var depts = await db.Departments.AsNoTracking().Where(d => d.IsActive).ToListAsync(ct);
        var countByDept = await GetDepartmentTaskCountsAsync(actor, ct);
        var countByOrg = await GetOrganizationTaskCountsAsync(actor, ct);

        int OrgCount(Guid orgId)
        {
            var orgDeptIds = depts.Where(d => d.OrganizationId == orgId).Select(d => d.Id).ToHashSet();
            var childOrgIds = orgs.Where(o => o.ParentId == orgId).Select(o => o.Id).ToList();
            var direct = countByOrg.GetValueOrDefault(orgId);
            var fromDepts = orgDeptIds.Sum(id => countByDept.GetValueOrDefault(id));
            var fromChildren = childOrgIds.Sum(OrgCount);
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

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var sortRows = await LoadTaskSortRowsAsync(actor, organizationId, departmentId, ct);
        sortRows = ApplyTaskListFilters(sortRows, actor, actorId, view, status, source);

        var total = sortRows.Count;
        var pageRows = sortRows
            .OrderByDescending(r => r.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = (await LoadUnifiedTasksPageAsync(pageRows, ct))
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
        if (assignee.Id != actorId)
            await notifications.NotifyTaskAssignedAsync(
                assignee.Id, task.Number, task.Title, task.Id, task.Source, task.ExternalId, ct);

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

    private sealed record TaskSortRow(
        Guid Id,
        bool IsWorkTask,
        DateTime UpdatedAt,
        WorkTaskStatus Status,
        TaskSource Source,
        Guid AssigneeId,
        Guid DepartmentId,
        Guid OrganizationId);

    private async Task<HashSet<Guid>> GetLinkedHelpDeskTicketIdsAsync(
        IQueryable<WorkTask> scopedWorkQuery, CancellationToken ct)
    {
        var ids = await scopedWorkQuery
            .Where(t => t.Source == TaskSource.HelpDesk && t.ExternalId != null)
            .Select(t => t.ExternalId!.Value)
            .ToListAsync(ct);
        return ids.ToHashSet();
    }

    private async Task<Dictionary<Guid, int>> GetDepartmentTaskCountsAsync(User actor, CancellationToken ct)
    {
        var workQuery = GetScopedWorkTasksQuery(actor, null, null);
        var linkedTicketIds = await GetLinkedHelpDeskTicketIdsAsync(workQuery, ct);

        var workCounts = await workQuery
            .GroupBy(t => t.DepartmentId)
            .Select(g => new { DeptId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var ticketCounts = await GetScopedTicketsQuery(actor, null, null)
            .Where(t => !linkedTicketIds.Contains(t.Id))
            .GroupBy(t => t.TargetDepartmentId)
            .Select(g => new { DeptId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var result = workCounts.ToDictionary(x => x.DeptId, x => x.Count);
        foreach (var row in ticketCounts)
            result[row.DeptId] = result.GetValueOrDefault(row.DeptId) + row.Count;
        return result;
    }

    private async Task<Dictionary<Guid, int>> GetOrganizationTaskCountsAsync(User actor, CancellationToken ct)
    {
        var workQuery = GetScopedWorkTasksQuery(actor, null, null);
        var linkedTicketIds = await GetLinkedHelpDeskTicketIdsAsync(workQuery, ct);

        var workCounts = await workQuery
            .GroupBy(t => t.OrganizationId)
            .Select(g => new { OrgId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var ticketCounts = await GetScopedTicketsQuery(actor, null, null)
            .Where(t => !linkedTicketIds.Contains(t.Id))
            .GroupBy(t => t.OrganizationId)
            .Select(g => new { OrgId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var result = workCounts.ToDictionary(x => x.OrgId, x => x.Count);
        foreach (var row in ticketCounts)
            result[row.OrgId] = result.GetValueOrDefault(row.OrgId) + row.Count;
        return result;
    }

    private async Task<List<TaskSortRow>> LoadTaskSortRowsAsync(
        User actor,
        Guid? organizationId,
        Guid? departmentId,
        CancellationToken ct)
    {
        var workQuery = GetScopedWorkTasksQuery(actor, organizationId, departmentId);
        var linkedTicketIds = await GetLinkedHelpDeskTicketIdsAsync(workQuery, ct);

        var workRows = await workQuery
            .Select(t => new TaskSortRow(
                t.Id, true, t.UpdatedAt, t.Status, t.Source,
                t.AssigneeId, t.DepartmentId, t.OrganizationId))
            .ToListAsync(ct);

        var ticketRows = await GetScopedTicketsQuery(actor, organizationId, departmentId)
            .Where(t => !linkedTicketIds.Contains(t.Id))
            .Select(t => new TaskSortRow(
                t.Id,
                false,
                t.UpdatedAt,
                t.Status == TicketStatus.Open || t.Status == TicketStatus.Assigned || t.Status == TicketStatus.Accepted
                    ? WorkTaskStatus.New
                    : t.Status == TicketStatus.InProgress
                        ? WorkTaskStatus.InProgress
                        : t.Status == TicketStatus.Done || t.Status == TicketStatus.Closed
                            ? WorkTaskStatus.Done
                            : WorkTaskStatus.Cancelled,
                TaskSource.HelpDesk,
                t.AssigneeId ?? t.RequesterId,
                t.TargetDepartmentId,
                t.OrganizationId))
            .ToListAsync(ct);

        workRows.AddRange(ticketRows);
        return workRows;
    }

    private static List<TaskSortRow> ApplyTaskListFilters(
        List<TaskSortRow> rows,
        User actor,
        Guid actorId,
        string view,
        WorkTaskStatus? status,
        TaskSource? source)
    {
        rows = view.ToLowerInvariant() switch
        {
            "mine" => rows.Where(r => r.AssigneeId == actorId).ToList(),
            "department" when IsDeptManager(actor) =>
                rows.Where(r => r.DepartmentId == actor.DepartmentId).ToList(),
            "all" when IsSuperAdmin(actor) || IsHoTopManager(actor) || actor.Role == UserRole.BMGMCManager => rows,
            _ => rows.Where(r => r.AssigneeId == actorId).ToList(),
        };

        if (status.HasValue)
            rows = rows.Where(r => r.Status == status).ToList();

        if (source.HasValue)
        {
            rows = source == TaskSource.HelpDesk
                ? rows.Where(r => !r.IsWorkTask).ToList()
                : rows.Where(r => r.IsWorkTask && r.Source == source).ToList();
        }

        return rows;
    }

    private async Task<List<UnifiedTaskItem>> LoadUnifiedTasksPageAsync(
        List<TaskSortRow> pageRows,
        CancellationToken ct)
    {
        if (pageRows.Count == 0)
            return [];

        var workIds = pageRows.Where(r => r.IsWorkTask).Select(r => r.Id).ToList();
        var ticketIds = pageRows.Where(r => !r.IsWorkTask).Select(r => r.Id).ToList();

        var workTasks = workIds.Count > 0
            ? await TaskQuery().Where(t => workIds.Contains(t.Id)).ToListAsync(ct)
            : [];
        var tickets = ticketIds.Count > 0
            ? await db.Tickets
                .Include(t => t.Requester)
                .Include(t => t.Assignee)
                .Include(t => t.TargetDepartment)
                .Include(t => t.Organization).ThenInclude(o => o.Parent)
                .Where(t => ticketIds.Contains(t.Id))
                .ToListAsync(ct)
            : [];

        var workById = workTasks.ToDictionary(t => t.Id);
        var ticketById = tickets.ToDictionary(t => t.Id);

        var unified = new List<UnifiedTaskItem>(pageRows.Count);
        foreach (var row in pageRows)
        {
            if (row.IsWorkTask)
                unified.Add(UnifiedTaskItem.FromWorkTask(workById[row.Id]));
            else if (UnifiedTaskItem.FromTicket(ticketById[row.Id]) is { } ticketItem)
                unified.Add(ticketItem);
        }

        return await EnrichProcurementDcsStatusesAsync(unified, ct);
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

        return await EnrichProcurementDcsStatusesAsync(unified, ct);
    }

    private async Task<List<UnifiedTaskItem>> EnrichProcurementDcsStatusesAsync(
        List<UnifiedTaskItem> items,
        CancellationToken ct)
    {
        var dcsTaskIds = items
            .Where(i => i.Source == TaskSource.DCS)
            .Select(i => i.Id)
            .ToHashSet();
        if (dcsTaskIds.Count == 0)
            return items;

        var details = await db.ProcurementRequestDetails
            .AsNoTracking()
            .Include(d => d.Document)
            .Where(d =>
                (d.ResponsibleTaskId != null && dcsTaskIds.Contains(d.ResponsibleTaskId.Value))
                || (d.MarketingTaskId != null && dcsTaskIds.Contains(d.MarketingTaskId.Value))
                || (d.ContractsTaskId != null && dcsTaskIds.Contains(d.ContractsTaskId.Value)))
            .ToListAsync(ct);

        if (details.Count == 0)
            return items;

        var derivedByTaskId = new Dictionary<Guid, DcsWorkTaskStatusResolver.ResolvedStatus>();
        foreach (var detail in details)
        {
            if (detail.ResponsibleTaskId is Guid responsibleId && dcsTaskIds.Contains(responsibleId))
                derivedByTaskId[responsibleId] = DcsWorkTaskStatusResolver.ResolveResponsible(detail);
            if (detail.MarketingTaskId is Guid marketingId && dcsTaskIds.Contains(marketingId))
                derivedByTaskId[marketingId] = DcsWorkTaskStatusResolver.ResolveMarketing(detail);
            if (detail.ContractsTaskId is Guid contractsId && dcsTaskIds.Contains(contractsId))
                derivedByTaskId[contractsId] = DcsWorkTaskStatusResolver.ResolveContracts(detail);
        }

        return items
            .Select(i => derivedByTaskId.TryGetValue(i.Id, out var derived)
                ? i.WithDerivedStatus(derived)
                : i)
            .ToList();
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
        var velocityTrend = BuildVelocityTrend(tasks);
        var byPriority = BuildPriorityDistribution(tasks, total);
        var agingBuckets = BuildAgingBuckets(tasks);
        var overdueCount = CountOverdue(tasks);
        var avgResolutionDays = CalcAvgResolutionDays(tasks);
        var throughputChange = CalcThroughputChange(tasks);
        var slaMetrics = BuildSlaMetrics(tasks);
        var cycleTime = BuildCycleTime(tasks);
        var activityHeatmap = BuildActivityHeatmap(tasks);
        var completionForecast = BuildCompletionForecast(tasks);
        var burndown = BuildBurndown(tasks);
        var riskQueue = BuildRiskQueue(tasks);
        var workloadBalance = BuildWorkloadBalance(tasks);
        var priorityMatrix = BuildPriorityMatrix(tasks);
        var healthScore = BuildHealthScore(
            completionRate, slaMetrics, throughputChange, workloadBalance, overdueCount, tasks);
        var insights = BuildInsights(
            tasks, completionRate, overdueCount, throughputChange, bySource,
            slaMetrics, workloadBalance, riskQueue, healthScore);
        var recent = tasks.OrderByDescending(t => t.UpdatedAt).Take(8).Select(t => t.ToDto()).ToList();

        IReadOnlyList<EmployeeTaskSummaryDto>? byEmployee = null;
        if (scope is "department" or "organization")
        {
            byEmployee = tasks
                .GroupBy(t => t.AssigneeId)
                .Select(g =>
                {
                    var assigneeTotal = g.Count(t => t.Status != WorkTaskStatus.Cancelled);
                    var assigneeDone = g.Count(t => t.Status == WorkTaskStatus.Done);
                    var rate = assigneeTotal > 0 ? Math.Round(assigneeDone * 100.0 / assigneeTotal, 1) : 0;
                    return new EmployeeTaskSummaryDto(
                        g.Key,
                        g.First().AssigneeName,
                        g.First().AssigneeEmployeeId,
                        g.Count(t => t.Status == WorkTaskStatus.New),
                        g.Count(t => t.Status == WorkTaskStatus.InProgress),
                        assigneeDone,
                        assigneeTotal,
                        rate);
                })
                .OrderByDescending(e => e.Total)
                .ThenBy(e => e.FullName)
                .ToList();
        }

        return new TaskAnalyticsDto(
            scope, scopeLabel, organizationId, departmentId,
            newCount, inProgress, done, cancelled, active, completionRate,
            distribution, bySource, byPriority, agingBuckets, weeklyTrend, velocityTrend,
            overdueCount, avgResolutionDays, throughputChange, insights,
            healthScore, slaMetrics, cycleTime, activityHeatmap, completionForecast,
            burndown, riskQueue, workloadBalance, priorityMatrix,
            recent, byEmployee);
    }

    private static List<TaskPrioritySliceDto> BuildPriorityDistribution(List<UnifiedTaskItem> tasks, int total) =>
        Enum.GetValues<TaskPriority>()
            .Select(p =>
            {
                var count = tasks.Count(t => t.Priority == p && t.Status != WorkTaskStatus.Cancelled);
                return new TaskPrioritySliceDto(p, count, total > 0 ? Math.Round(count * 100.0 / total, 1) : 0);
            })
            .Where(p => p.Count > 0)
            .OrderByDescending(p => p.Count)
            .ToList();

    private static List<TaskAgingBucketDto> BuildAgingBuckets(List<UnifiedTaskItem> tasks)
    {
        var active = tasks.Where(t => t.Status is WorkTaskStatus.New or WorkTaskStatus.InProgress).ToList();
        var total = active.Count;
        var today = DateTime.UtcNow.Date;

        int CountInRange(int minDays, int? maxDays) =>
            active.Count(t =>
            {
                var age = (today - t.CreatedAt.Date).TotalDays;
                return age >= minDays && (maxDays is null || age <= maxDays.Value);
            });

        var buckets = new[]
        {
            ("0_3", 0, (int?)3),
            ("4_7", 4, (int?)7),
            ("8_14", 8, (int?)14),
            ("15_plus", 15, (int?)null),
        };

        return buckets
            .Select(b =>
            {
                var count = CountInRange(b.Item2, b.Item3);
                return new TaskAgingBucketDto(b.Item1, count, total > 0 ? Math.Round(count * 100.0 / total, 1) : 0, b.Item2, b.Item3);
            })
            .ToList();
    }

    private static List<TaskVelocityPointDto> BuildVelocityTrend(List<UnifiedTaskItem> tasks)
    {
        var today = DateTime.UtcNow.Date;
        var completed = new List<int>();

        for (var i = 7; i >= 0; i--)
        {
            var weekStart = today.AddDays(-7 * i);
            var weekEnd = weekStart.AddDays(7);
            completed.Add(tasks.Count(t => t.CompletedAt >= weekStart && t.CompletedAt < weekEnd));
        }

        var points = new List<TaskVelocityPointDto>();
        for (var i = 0; i < completed.Count; i++)
        {
            var weekStart = today.AddDays(-7 * (7 - i));
            var window = completed.Skip(Math.Max(0, i - 2)).Take(Math.Min(3, i + 1)).ToList();
            var ma = window.Count > 0 ? Math.Round(window.Average(), 1) : 0;
            points.Add(new TaskVelocityPointDto(weekStart.ToString("dd MMM"), completed[i], ma));
        }

        return points;
    }

    private static int CountOverdue(List<UnifiedTaskItem> tasks) =>
        tasks.Count(t =>
            t.DueDate.HasValue
            && t.DueDate.Value.Date < DateTime.UtcNow.Date
            && t.Status is WorkTaskStatus.New or WorkTaskStatus.InProgress);

    private static double CalcAvgResolutionDays(List<UnifiedTaskItem> tasks)
    {
        var resolved = tasks
            .Where(t => t.CompletedAt.HasValue && t.Status == WorkTaskStatus.Done)
            .Select(t => (t.CompletedAt!.Value - t.CreatedAt).TotalDays)
            .Where(d => d >= 0)
            .ToList();

        return resolved.Count > 0 ? Math.Round(resolved.Average(), 1) : 0;
    }

    private static double CalcThroughputChange(List<UnifiedTaskItem> tasks)
    {
        var today = DateTime.UtcNow.Date;
        var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
        var lastWeekStart = thisWeekStart.AddDays(-7);
        var thisWeek = tasks.Count(t => t.CompletedAt >= thisWeekStart);
        var lastWeek = tasks.Count(t => t.CompletedAt >= lastWeekStart && t.CompletedAt < thisWeekStart);
        if (lastWeek == 0) return thisWeek > 0 ? 100 : 0;
        return Math.Round((thisWeek - lastWeek) * 100.0 / lastWeek, 1);
    }

    private static List<TaskInsightDto> BuildInsights(
        List<UnifiedTaskItem> tasks,
        double completionRate,
        int overdueCount,
        double throughputChange,
        IReadOnlyList<TaskSourceSliceDto> bySource,
        TaskSlaMetricsDto sla,
        TaskWorkloadBalanceDto workload,
        IReadOnlyList<TaskRiskItemDto> riskQueue,
        TaskHealthScoreDto health)
    {
        var insights = new List<TaskInsightDto>();

        if (overdueCount > 0)
            insights.Add(new TaskInsightDto("overdue", "warning", overdueCount, null));

        if (completionRate >= 75)
            insights.Add(new TaskInsightDto("high_completion", "good", completionRate, null));
        else if (completionRate < 45 && tasks.Count(t => t.Status != WorkTaskStatus.Cancelled) >= 5)
            insights.Add(new TaskInsightDto("low_completion", "warning", completionRate, null));

        if (throughputChange >= 15)
            insights.Add(new TaskInsightDto("throughput_up", "good", throughputChange, null));
        else if (throughputChange <= -15)
            insights.Add(new TaskInsightDto("throughput_down", "warning", throughputChange, null));

        var topSource = bySource.OrderByDescending(s => s.Count).FirstOrDefault();
        if (topSource is not null && topSource.Percent >= 50)
            insights.Add(new TaskInsightDto("dominant_source", "info", topSource.Percent, topSource.Source.ToString()));

        var stale = tasks.Count(t =>
            t.Status is WorkTaskStatus.New or WorkTaskStatus.InProgress
            && (DateTime.UtcNow.Date - t.CreatedAt.Date).TotalDays >= 15);
        if (stale > 0)
            insights.Add(new TaskInsightDto("stale_tasks", "warning", stale, null));

        if (sla.WithDueDate >= 3 && sla.CompliancePercent < 70)
            insights.Add(new TaskInsightDto("sla_breach", "warning", sla.CompliancePercent, null));
        else if (sla.WithDueDate >= 3 && sla.CompliancePercent >= 90)
            insights.Add(new TaskInsightDto("sla_excellent", "good", sla.CompliancePercent, null));

        if (workload.AssigneeCount >= 2 && workload.BalanceScore < 55)
            insights.Add(new TaskInsightDto("workload_imbalance", "warning", workload.BalanceScore, null));
        else if (workload.AssigneeCount >= 2 && workload.BalanceScore >= 85)
            insights.Add(new TaskInsightDto("workload_balanced", "good", workload.BalanceScore, null));

        var criticalRisk = riskQueue.Count(r => r.RiskLevel == "critical");
        if (criticalRisk > 0)
            insights.Add(new TaskInsightDto("critical_risk", "warning", criticalRisk, null));

        if (health.Score >= 85)
            insights.Add(new TaskInsightDto("health_excellent", "good", health.Score, health.Grade));
        else if (health.Score < 50)
            insights.Add(new TaskInsightDto("health_critical", "warning", health.Score, health.Grade));

        var projected = BuildCompletionForecast(tasks).LastOrDefault(p => p.IsProjected);
        if (projected?.Forecast is int f && f > 0)
        {
            var avgRecent = tasks.Count(t => t.CompletedAt >= DateTime.UtcNow.Date.AddDays(-14)) / 2.0;
            if (f > avgRecent * 1.25)
                insights.Add(new TaskInsightDto("forecast_surge", "info", f, null));
        }

        return insights;
    }

    private static TaskSlaMetricsDto BuildSlaMetrics(List<UnifiedTaskItem> tasks)
    {
        var withDue = tasks.Where(t => t.DueDate.HasValue && t.Status != WorkTaskStatus.Cancelled).ToList();
        var doneWithDue = withDue.Where(t => t.Status == WorkTaskStatus.Done && t.CompletedAt.HasValue).ToList();
        var onTime = doneWithDue.Count(t => t.CompletedAt!.Value.Date <= t.DueDate!.Value.Date);
        var late = doneWithDue.Count(t => t.CompletedAt!.Value.Date > t.DueDate!.Value.Date);
        var atRisk = withDue.Count(t =>
            t.Status is WorkTaskStatus.New or WorkTaskStatus.InProgress
            && t.DueDate!.Value.Date <= DateTime.UtcNow.Date.AddDays(3));

        var compliance = doneWithDue.Count > 0
            ? Math.Round(onTime * 100.0 / doneWithDue.Count, 1)
            : withDue.Count > 0 ? 100 : 0;

        return new TaskSlaMetricsDto(compliance, withDue.Count, onTime, late, atRisk);
    }

    private static TaskCycleTimeDto BuildCycleTime(List<UnifiedTaskItem> tasks)
    {
        var days = tasks
            .Where(t => t.CompletedAt.HasValue && t.Status == WorkTaskStatus.Done)
            .Select(t => (t.CompletedAt!.Value - t.CreatedAt).TotalDays)
            .Where(d => d >= 0)
            .OrderBy(d => d)
            .ToList();

        if (days.Count == 0)
            return new TaskCycleTimeDto(0, 0, 0, 0);

        return new TaskCycleTimeDto(
            Percentile(days, 50),
            Percentile(days, 75),
            Percentile(days, 90),
            Math.Round(days.Average(), 1));
    }

    private static double Percentile(List<double> sorted, int percentile)
    {
        if (sorted.Count == 0) return 0;
        var index = (percentile / 100.0) * (sorted.Count - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);
        if (lower == upper) return Math.Round(sorted[lower], 1);
        var weight = index - lower;
        return Math.Round(sorted[lower] * (1 - weight) + sorted[upper] * weight, 1);
    }

    private static List<TaskHeatmapCellDto> BuildActivityHeatmap(List<UnifiedTaskItem> tasks)
    {
        var labels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
        var days = new[]
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday,
        };

        return days.Select((day, i) =>
        {
            var created = tasks.Count(t => t.CreatedAt.DayOfWeek == day);
            var completed = tasks.Count(t => t.CompletedAt.HasValue && t.CompletedAt.Value.DayOfWeek == day);
            return new TaskHeatmapCellDto(i, labels[i], created, completed, created + completed);
        }).ToList();
    }

    private static List<TaskForecastPointDto> BuildCompletionForecast(List<UnifiedTaskItem> tasks)
    {
        var today = DateTime.UtcNow.Date;
        var historical = new List<(string Label, int Value)>();

        for (var i = 7; i >= 0; i--)
        {
            var weekStart = today.AddDays(-7 * i);
            var weekEnd = weekStart.AddDays(7);
            historical.Add((
                weekStart.ToString("dd MMM"),
                tasks.Count(t => t.CompletedAt >= weekStart && t.CompletedAt < weekEnd)));
        }

        var y = historical.Select(h => (double)h.Value).ToArray();
        var (slope, intercept) = LinearRegression(y);

        var points = historical
            .Select(h => new TaskForecastPointDto(h.Label, h.Value, h.Value, false))
            .ToList();

        for (var i = 1; i <= 2; i++)
        {
            var weekStart = today.AddDays(7 * i);
            var idx = historical.Count + i - 1;
            var forecast = Math.Max(0, (int)Math.Round(intercept + slope * idx));
            points.Add(new TaskForecastPointDto(
                weekStart.ToString("dd MMM"),
                0,
                forecast,
                true));
        }

        return points;
    }

    private static (double Slope, double Intercept) LinearRegression(double[] y)
    {
        if (y.Length == 0) return (0, 0);
        var n = y.Length;
        var sumX = 0.0;
        var sumY = 0.0;
        var sumXY = 0.0;
        var sumX2 = 0.0;
        for (var i = 0; i < n; i++)
        {
            sumX += i;
            sumY += y[i];
            sumXY += i * y[i];
            sumX2 += i * i;
        }

        var denom = n * sumX2 - sumX * sumX;
        if (Math.Abs(denom) < 0.0001) return (0, y.Average());
        var slope = (n * sumXY - sumX * sumY) / denom;
        var intercept = (sumY - slope * sumX) / n;
        return (slope, intercept);
    }

    private static List<TaskBurndownPointDto> BuildBurndown(List<UnifiedTaskItem> tasks)
    {
        var today = DateTime.UtcNow.Date;
        var points = new List<TaskBurndownPointDto>();
        int? startRemaining = null;

        for (var i = 7; i >= 0; i--)
        {
            var weekStart = today.AddDays(-7 * i);
            var weekEnd = weekStart.AddDays(7);

            var remaining = tasks.Count(t =>
                t.Status != WorkTaskStatus.Cancelled
                && t.CreatedAt < weekEnd
                && (!t.CompletedAt.HasValue || t.CompletedAt >= weekEnd));

            startRemaining ??= remaining;

            var completed = tasks.Count(t =>
                t.CompletedAt >= weekStart && t.CompletedAt < weekEnd);

            var weeksLeft = i + 1;
            var ideal = startRemaining > 0
                ? Math.Max(0, (int)Math.Round(startRemaining.Value * weeksLeft / 8.0))
                : 0;

            points.Add(new TaskBurndownPointDto(
                weekStart.ToString("dd MMM"),
                remaining,
                ideal,
                completed));
        }

        return points;
    }

    private static List<TaskRiskItemDto> BuildRiskQueue(List<UnifiedTaskItem> tasks)
    {
        var today = DateTime.UtcNow.Date;

        int PriorityWeight(TaskPriority p) => p switch
        {
            TaskPriority.Critical => 4,
            TaskPriority.High => 3,
            TaskPriority.Medium => 2,
            _ => 1,
        };

        return tasks
            .Where(t => t.Status is WorkTaskStatus.New or WorkTaskStatus.InProgress)
            .Select(t =>
            {
                var ageDays = (int)(today - t.CreatedAt.Date).TotalDays;
                var isOverdue = t.DueDate.HasValue && t.DueDate.Value.Date < today;
                var score = PriorityWeight(t.Priority) * 12.0
                            + ageDays * 1.5
                            + (isOverdue ? 25 : 0)
                            + (t.DueDate.HasValue && !isOverdue && (t.DueDate.Value.Date - today).TotalDays <= 2 ? 8 : 0);

                var level = score >= 55 ? "critical" : score >= 35 ? "high" : score >= 20 ? "medium" : "low";

                return new TaskRiskItemDto(
                    t.Id, t.Number, t.Title, t.AssigneeName,
                    t.Priority, Math.Round(score, 1), level, ageDays, isOverdue);
            })
            .OrderByDescending(r => r.RiskScore)
            .Take(8)
            .ToList();
    }

    private static TaskWorkloadBalanceDto BuildWorkloadBalance(List<UnifiedTaskItem> tasks)
    {
        var loads = tasks
            .Where(t => t.Status is WorkTaskStatus.New or WorkTaskStatus.InProgress)
            .GroupBy(t => t.AssigneeId)
            .Select(g => g.Count())
            .OrderBy(c => c)
            .ToList();

        if (loads.Count == 0)
            return new TaskWorkloadBalanceDto(100, 0, 0, 0, 0);

        var gini = CalcGini(loads);
        var balanceScore = Math.Round(Math.Clamp(100 - gini * 100, 0, 100), 1);

        return new TaskWorkloadBalanceDto(
            balanceScore,
            Math.Round(gini, 3),
            loads.Count,
            Math.Round(loads.Average(), 1),
            loads.Max());
    }

    private static double CalcGini(List<int> sortedValues)
    {
        var n = sortedValues.Count;
        if (n <= 1) return 0;
        var sum = sortedValues.Sum();
        if (sum == 0) return 0;

        var cumulative = 0.0;
        var giniSum = 0.0;
        for (var i = 0; i < n; i++)
        {
            cumulative += sortedValues[i];
            giniSum += (2 * (i + 1) - n - 1) * sortedValues[i];
        }

        return giniSum / (n * sum);
    }

    private static List<TaskPriorityStatusCellDto> BuildPriorityMatrix(List<UnifiedTaskItem> tasks)
    {
        var statuses = new[] { WorkTaskStatus.New, WorkTaskStatus.InProgress, WorkTaskStatus.Done };
        return Enum.GetValues<TaskPriority>()
            .SelectMany(p => statuses.Select(s => new TaskPriorityStatusCellDto(
                p, s, tasks.Count(t => t.Priority == p && t.Status == s))))
            .ToList();
    }

    private static TaskHealthScoreDto BuildHealthScore(
        double completionRate,
        TaskSlaMetricsDto sla,
        double throughputChange,
        TaskWorkloadBalanceDto workload,
        int overdueCount,
        List<UnifiedTaskItem> tasks)
    {
        var stale = tasks.Count(t =>
            t.Status is WorkTaskStatus.New or WorkTaskStatus.InProgress
            && (DateTime.UtcNow.Date - t.CreatedAt.Date).TotalDays >= 15);

        var velocityComponent = Math.Clamp(50 + throughputChange, 0, 100);
        var riskPenalty = Math.Min(30, overdueCount * 4.0 + stale * 2.0);

        var score = Math.Clamp(
            completionRate * 0.30
            + sla.CompliancePercent * 0.25
            + velocityComponent * 0.15
            + workload.BalanceScore * 0.15
            + Math.Min(100, tasks.Count(t => t.Status == WorkTaskStatus.Done) * 2) * 0.15
            - riskPenalty,
            0, 100);

        score = Math.Round(score, 1);
        var grade = score switch
        {
            >= 85 => "A",
            >= 70 => "B",
            >= 55 => "C",
            >= 40 => "D",
            _ => "F",
        };

        return new TaskHealthScoreDto(
            score, grade,
            Math.Round(completionRate * 0.30, 1),
            Math.Round(sla.CompliancePercent * 0.25, 1),
            Math.Round(velocityComponent * 0.15, 1),
            Math.Round(workload.BalanceScore * 0.15, 1),
            Math.Round(riskPenalty, 1));
    }

    private static List<TaskTrendPointDto> BuildWeeklyTrend(List<UnifiedTaskItem> tasks)
    {
        var points = new List<TaskTrendPointDto>();
        var today = DateTime.UtcNow.Date;
        for (var i = 7; i >= 0; i--)
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
        await db.Users.AsNoTracking().Include(u => u.Organization).ThenInclude(o => o.Parent)
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    private async Task<string> GenerateNumberAsync(CancellationToken ct)
    {
        var count = await db.WorkTasks.CountAsync(ct);
        return $"TSK-{count + 1:D5}";
    }
}
