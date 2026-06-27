using ATG.Platform.Application.Common;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Data;
using ATG.Platform.Infrastructure.HelpDesk;
using ATG.Platform.Infrastructure.Seeds;
using Microsoft.EntityFrameworkCore;

namespace ATG.Platform.Infrastructure.Services;

public class HelpDeskService(AppDbContext db, IAuditService audit) : IHelpDeskService
{
    public IReadOnlyList<HelpDeskCategoryDto> GetCategories() =>
        HelpDeskRouting.Categories.Select(c => new HelpDeskCategoryDto(
            c.Category, c.NameEn, c.NameRu, c.Icon, c.Color)).ToList();

    public async Task<Result<PagedResult<TicketListItemDto>>> GetTicketsAsync(
        Guid actorId,
        string view,
        int page,
        int pageSize,
        TicketCategory? category,
        TicketStatus? status,
        CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<PagedResult<TicketListItemDto>>.Fail("User not found");

        var query = TicketQuery();

        query = view.ToLowerInvariant() switch
        {
            "mine" => query.Where(t => t.RequesterId == actorId),
            "assigned" => query.Where(t => t.AssigneeId == actorId),
            "queue" => query.Where(t =>
                t.TargetDepartmentId == actor.DepartmentId &&
                t.Status != TicketStatus.Closed &&
                t.Status != TicketStatus.Cancelled),
            "all" when IsPlatformAdmin(actor) => query,
            "all" => query.Where(t =>
                t.RequesterId == actorId ||
                t.AssigneeId == actorId ||
                t.TargetDepartmentId == actor.DepartmentId),
            _ => query.Where(t => t.RequesterId == actorId || t.AssigneeId == actorId)
        };

        if (category.HasValue) query = query.Where(t => t.Category == category);
        if (status.HasValue) query = query.Where(t => t.Status == status);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Result<PagedResult<TicketListItemDto>>.Ok(new PagedResult<TicketListItemDto>(
            items.Select(MapListItem).ToList(), total, page, pageSize));
    }

    public async Task<Result<TicketBoardDto>> GetBoardAsync(Guid actorId, TicketCategory? category, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<TicketBoardDto>.Fail("User not found");

        var query = TicketQuery();
        if (!IsPlatformAdmin(actor))
            query = query.Where(t =>
                t.RequesterId == actorId ||
                t.AssigneeId == actorId ||
                t.TargetDepartmentId == actor.DepartmentId);

        if (category.HasValue) query = query.Where(t => t.Category == category);

        var tickets = await query.OrderByDescending(t => t.UpdatedAt).ToListAsync(ct);
        var items = tickets.Select(MapListItem).ToList();

        return Result<TicketBoardDto>.Ok(new TicketBoardDto(
            items.Where(t => t.Status == TicketStatus.Open).ToList(),
            items.Where(t => t.Status == TicketStatus.Assigned).ToList(),
            items.Where(t => t.Status == TicketStatus.Accepted).ToList(),
            items.Where(t => t.Status == TicketStatus.InProgress).ToList(),
            items.Where(t => t.Status == TicketStatus.Done).ToList(),
            items.Where(t => t.Status == TicketStatus.Closed).ToList()));
    }

    public async Task<Result<TicketDto>> GetByIdAsync(Guid id, Guid actorId, CancellationToken ct = default)
    {
        var ticket = await FullTicketQuery().FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null) return Result<TicketDto>.Fail("Ticket not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanView(actor, ticket))
            return Result<TicketDto>.Fail("Access denied");

        return Result<TicketDto>.Ok(MapTicket(ticket, actor));
    }

    public async Task<Result<TicketDto>> CreateAsync(CreateTicketRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<TicketDto>.Fail("User not found");

        if (string.IsNullOrWhiteSpace(request.Title))
            return Result<TicketDto>.Fail("Title is required");

        var deptCode = HelpDeskRouting.ResolveDepartmentCode(request.Category, actor.Organization.Code);
        if (deptCode is null)
            return Result<TicketDto>.Fail("No routing for this category");

        var dept = await db.Departments.FirstOrDefaultAsync(d =>
            d.OrganizationId == actor.OrganizationId && d.Code == deptCode && d.IsActive, ct);

        if (dept is null)
        {
            dept = await db.Departments.FirstOrDefaultAsync(d => d.Code == deptCode && d.IsActive, ct);
            if (dept is null)
                return Result<TicketDto>.Fail($"Department {deptCode} not found");
        }

        var number = await GenerateNumberAsync(ct);
        var ticket = new Ticket
        {
            Number = number,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? "",
            Category = request.Category,
            Priority = request.Priority,
            Status = TicketStatus.Open,
            RequesterId = actorId,
            OrganizationId = actor.OrganizationId,
            TargetDepartmentId = dept.Id
        };

        db.Tickets.Add(ticket);
        await AddActivityAsync(ticket, actorId, "created", null, TicketStatus.Open, $"Routed to {dept.Code}", ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "TicketCreated", "Ticket", ticket.Id, ticket.Number, ip, ct);

        return await GetByIdAsync(ticket.Id, actorId, ct);
    }

    public async Task<Result<TicketDto>> AssignAsync(Guid id, AssignTicketRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null) return Result<TicketDto>.Fail("Ticket not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanAssign(actor, ticket))
            return Result<TicketDto>.Fail("Only department manager or platform admin can assign tickets");

        if (ticket.Status is not TicketStatus.Open and not TicketStatus.Assigned)
            return Result<TicketDto>.Fail("Ticket cannot be assigned in current status");

        var assignee = await db.Users.Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == request.AssigneeId && u.IsActive, ct);
        if (assignee is null) return Result<TicketDto>.Fail("Assignee not found");
        if (assignee.DepartmentId != ticket.TargetDepartmentId)
            return Result<TicketDto>.Fail("Assignee must belong to target department");

        var from = ticket.Status;
        ticket.AssigneeId = assignee.Id;
        ticket.AssignedById = actorId;
        ticket.Status = TicketStatus.Assigned;
        ticket.AssignedAt = DateTime.UtcNow;
        ticket.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(ticket, actorId, "assigned", from, TicketStatus.Assigned,
            $"Assigned to {assignee.FullName}", ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "TicketAssigned", "Ticket", ticket.Id, assignee.Email, ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public Task<Result<TicketDto>> AcceptAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default)
        => TransitionAsync(id, actorId, ip, TicketStatus.Assigned, TicketStatus.Accepted, "accepted",
            t => t.AcceptedAt = DateTime.UtcNow, ct);

    public Task<Result<TicketDto>> StartAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default)
        => TransitionAsync(id, actorId, ip, [TicketStatus.Accepted, TicketStatus.Assigned], TicketStatus.InProgress, "started",
            t => t.StartedAt = DateTime.UtcNow, ct);

    public Task<Result<TicketDto>> CompleteAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default)
        => TransitionAsync(id, actorId, ip, TicketStatus.InProgress, TicketStatus.Done, "completed",
            t => t.CompletedAt = DateTime.UtcNow, ct);

    public async Task<Result<TicketDto>> CloseAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null) return Result<TicketDto>.Fail("Ticket not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<TicketDto>.Fail("User not found");

        if (ticket.Status != TicketStatus.Done)
            return Result<TicketDto>.Fail("Ticket must be Done before closing");

        if (ticket.RequesterId != actorId && !IsPlatformAdmin(actor))
            return Result<TicketDto>.Fail("Only requester can close the ticket");

        var from = ticket.Status;
        ticket.Status = TicketStatus.Closed;
        ticket.ClosedAt = DateTime.UtcNow;
        ticket.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(ticket, actorId, "closed", from, TicketStatus.Closed, null, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "TicketClosed", "Ticket", ticket.Id, ticket.Number, ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<TicketCommentDto>> AddCommentAsync(
        Guid id, AddTicketCommentRequest request, Guid actorId, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null) return Result<TicketCommentDto>.Fail("Ticket not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanView(actor, ticket))
            return Result<TicketCommentDto>.Fail("Access denied");

        if (request.IsInternal && ticket.RequesterId == actorId && !IsDeptStaff(actor, ticket))
            return Result<TicketCommentDto>.Fail("Requester cannot add internal notes");

        var comment = new TicketComment
        {
            TicketId = id,
            AuthorId = actorId,
            Body = request.Body.Trim(),
            IsInternal = request.IsInternal
        };
        db.TicketComments.Add(comment);
        ticket.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Result<TicketCommentDto>.Ok(new TicketCommentDto(
            comment.Id, actorId, actor.FullName, comment.Body, comment.IsInternal, comment.CreatedAt));
    }

    public async Task<Result<IReadOnlyList<HelpDeskAssigneeDto>>> GetAssigneesAsync(Guid ticketId, Guid actorId, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct);
        if (ticket is null) return Result<IReadOnlyList<HelpDeskAssigneeDto>>.Fail("Ticket not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanAssign(actor, ticket))
            return Result<IReadOnlyList<HelpDeskAssigneeDto>>.Fail("Access denied");

        var users = await db.Users
            .Where(u => u.IsActive && u.DepartmentId == ticket.TargetDepartmentId)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);

        var dtos = users.Select(u => new HelpDeskAssigneeDto(u.Id, u.FullName, u.Email, u.Role.ToString())).ToList();
        return Result<IReadOnlyList<HelpDeskAssigneeDto>>.Ok(dtos);
    }

    public async Task<Result<HelpDeskDashboardDto>> GetAdminDashboardAsync(Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsPlatformAdmin(actor))
            return Result<HelpDeskDashboardDto>.Fail("Access denied");

        var open = await db.Tickets.CountAsync(t => t.Status == TicketStatus.Open, ct);
        var inProgress = await db.Tickets.CountAsync(t =>
            t.Status == TicketStatus.InProgress || t.Status == TicketStatus.Accepted || t.Status == TicketStatus.Assigned, ct);
        var done = await db.Tickets.CountAsync(t => t.Status == TicketStatus.Done, ct);
        var closed = await db.Tickets.CountAsync(t => t.Status == TicketStatus.Closed, ct);

        var recent = await TicketQuery()
            .OrderByDescending(t => t.UpdatedAt)
            .Take(20)
            .ToListAsync(ct);

        return Result<HelpDeskDashboardDto>.Ok(new HelpDeskDashboardDto(
            open, inProgress, done, closed, recent.Select(MapListItem).ToList()));
    }

    public async Task<Result<HelpDeskAdminControlDto>> GetAdminControlAsync(Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsPlatformAdmin(actor))
            return Result<HelpDeskAdminControlDto>.Fail("Access denied");

        var dashboardResult = await GetAdminDashboardAsync(actorId, ct);
        if (!dashboardResult.IsSuccess)
            return Result<HelpDeskAdminControlDto>.Fail(dashboardResult.Error!);

        var orgs = await db.Organizations.AsNoTracking()
            .Where(o => o.Code == HoMasterData.OrganizationCode || o.Code == BmgmcMasterData.OrganizationCode)
            .ToDictionaryAsync(o => o.Code, ct);

        var deptCodes = HelpDeskRouting.Categories
            .SelectMany(c => new[]
            {
                HelpDeskRouting.ResolveDepartmentCode(c.Category, HoMasterData.OrganizationCode),
                HelpDeskRouting.ResolveDepartmentCode(c.Category, BmgmcMasterData.OrganizationCode)
            })
            .Where(c => c is not null)
            .Distinct()
            .ToList();

        var departments = await db.Departments.AsNoTracking()
            .Include(d => d.Organization)
            .Where(d => deptCodes.Contains(d.Code))
            .ToListAsync(ct);

        var deptIds = departments.Select(d => d.Id).ToList();
        var staff = await db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.DepartmentId != null && deptIds.Contains(u.DepartmentId.Value))
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);

        var ticketStats = await db.Tickets.AsNoTracking()
            .Where(t => t.Status != TicketStatus.Closed && t.Status != TicketStatus.Cancelled)
            .GroupBy(t => new { t.Category, t.TargetDepartmentId })
            .Select(g => new { g.Key.Category, g.Key.TargetDepartmentId, Count = g.Count() })
            .ToListAsync(ct);

        var openByDept = await db.Tickets.AsNoTracking()
            .Where(t => t.Status == TicketStatus.Open)
            .GroupBy(t => new { t.Category, t.TargetDepartmentId })
            .Select(g => new { g.Key.Category, g.Key.TargetDepartmentId, Count = g.Count() })
            .ToListAsync(ct);

        var categories = HelpDeskRouting.Categories.Select(cat =>
        {
            var routes = new List<HelpDeskOrgRoutingDto>();
            foreach (var orgCode in new[] { HoMasterData.OrganizationCode, BmgmcMasterData.OrganizationCode })
            {
                var resolvedCode = HelpDeskRouting.ResolveDepartmentCode(cat.Category, orgCode);
                if (resolvedCode is null || !orgs.TryGetValue(orgCode, out var org)) continue;

                var dept = departments.FirstOrDefault(d =>
                    d.Code == resolvedCode && d.OrganizationId == org.Id);
                if (dept is null) continue;

                var deptStaff = staff.Where(u => u.DepartmentId == dept.Id).ToList();
                var assigners = deptStaff
                    .Where(u => IsDeptManager(u) || u.Role == UserRole.HOTopManager)
                    .Select(MapStaff)
                    .ToList();
                var engineers = deptStaff
                    .Where(u => !IsDeptManager(u) && u.Role != UserRole.HOTopManager && u.Role != UserRole.SuperAdmin)
                    .Select(MapStaff)
                    .ToList();

                var active = ticketStats
                    .Where(t => t.Category == cat.Category && t.TargetDepartmentId == dept.Id)
                    .Sum(t => t.Count);
                var open = openByDept
                    .Where(t => t.Category == cat.Category && t.TargetDepartmentId == dept.Id)
                    .Sum(t => t.Count);

                routes.Add(new HelpDeskOrgRoutingDto(
                    org.Code, org.Name, dept.Id, dept.Code,
                    dept.Name, dept.NameEn, open, active, assigners, engineers));
            }

            return new HelpDeskCategoryRoutingDto(
                cat.Category, cat.NameEn, cat.NameRu, cat.Icon, cat.Color, routes);
        }).ToList();

        return Result<HelpDeskAdminControlDto>.Ok(
            new HelpDeskAdminControlDto(dashboardResult.Data!, categories));
    }

    private static HelpDeskStaffDto MapStaff(User u) => new(
        u.Id, u.EmployeeId, u.FullName, u.Email, u.Role.ToString(), u.JobTitleEn, u.JobTitleRu);

    private Task<Result<TicketDto>> TransitionAsync(
        Guid id,
        Guid actorId,
        string? ip,
        TicketStatus fromStatus,
        TicketStatus toStatus,
        string action,
        Action<Ticket> apply,
        CancellationToken ct)
        => TransitionAsync(id, actorId, ip, [fromStatus], toStatus, action, apply, ct);

    private async Task<Result<TicketDto>> TransitionAsync(
        Guid id,
        Guid actorId,
        string? ip,
        TicketStatus[] fromStatuses,
        TicketStatus toStatus,
        string action,
        Action<Ticket> apply,
        CancellationToken ct)
    {
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null) return Result<TicketDto>.Fail("Ticket not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<TicketDto>.Fail("User not found");

        if (!fromStatuses.Contains(ticket.Status))
            return Result<TicketDto>.Fail($"Invalid status transition from {ticket.Status}");

        if (toStatus is TicketStatus.Accepted or TicketStatus.InProgress or TicketStatus.Done)
        {
            if (ticket.AssigneeId != actorId && !IsPlatformAdmin(actor))
                return Result<TicketDto>.Fail("Only assignee can perform this action");
        }

        var from = ticket.Status;
        ticket.Status = toStatus;
        ticket.UpdatedAt = DateTime.UtcNow;
        apply(ticket);

        await AddActivityAsync(ticket, actorId, action, from, toStatus, null, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, $"Ticket{char.ToUpper(action[0])}{action[1..]}", "Ticket", ticket.Id, ticket.Number, ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    private async Task AddActivityAsync(
        Ticket ticket, Guid actorId, string action,
        TicketStatus? from, TicketStatus? to, string? details, CancellationToken ct)
    {
        db.TicketActivities.Add(new TicketActivity
        {
            TicketId = ticket.Id,
            ActorId = actorId,
            Action = action,
            FromStatus = from,
            ToStatus = to,
            Details = details
        });
        await Task.CompletedTask;
    }

    private async Task<string> GenerateNumberAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"HD-{year}-";
        var last = await db.Tickets
            .Where(t => t.Number.StartsWith(prefix))
            .OrderByDescending(t => t.Number)
            .Select(t => t.Number)
            .FirstOrDefaultAsync(ct);

        var seq = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var n))
            seq = n + 1;

        return $"{prefix}{seq:D5}";
    }

    private IQueryable<Ticket> TicketQuery() =>
        db.Tickets
            .Include(t => t.Requester)
            .Include(t => t.Assignee)
            .Include(t => t.TargetDepartment);

    private IQueryable<Ticket> FullTicketQuery() =>
        TicketQuery()
            .Include(t => t.Organization)
            .Include(t => t.AssignedBy)
            .Include(t => t.Comments).ThenInclude(c => c.Author)
            .Include(t => t.Activities).ThenInclude(a => a.Actor);

    private async Task<User?> GetActorAsync(Guid id, CancellationToken ct) =>
        await db.Users.Include(u => u.Organization).Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    private static bool IsPlatformAdmin(User u) =>
        u.Role is UserRole.SuperAdmin or UserRole.HOTopManager;

    private static bool IsDeptManager(User u) =>
        u.Role is UserRole.HONachalnik or UserRole.BMGMCNachalnikiOtdeli or UserRole.BMGMCManager;

    private static bool CanAssign(User actor, Ticket ticket) =>
        IsPlatformAdmin(actor) ||
        (IsDeptManager(actor) && actor.DepartmentId == ticket.TargetDepartmentId);

    private static bool IsDeptStaff(User actor, Ticket ticket) =>
        actor.DepartmentId == ticket.TargetDepartmentId;

    private static bool CanView(User actor, Ticket ticket) =>
        IsPlatformAdmin(actor) ||
        ticket.RequesterId == actor.Id ||
        ticket.AssigneeId == actor.Id ||
        actor.DepartmentId == ticket.TargetDepartmentId;

    private static TicketListItemDto MapListItem(Ticket t) => new(
        t.Id, t.Number, t.Title, t.Category, t.Status, t.Priority,
        t.Requester.FullName, t.Assignee?.FullName,
        t.TargetDepartment.Name, t.TargetDepartment.NameEn,
        t.CreatedAt, t.UpdatedAt);

    private static TicketDto MapTicket(Ticket t, User viewer)
    {
        var isStaff = IsDeptStaff(viewer, t) || IsPlatformAdmin(viewer);
        var comments = t.Comments
            .Where(c => !c.IsInternal || isStaff)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new TicketCommentDto(
                c.Id, c.AuthorId, c.Author.FullName, c.Body, c.IsInternal, c.CreatedAt))
            .ToList();

        var activities = t.Activities
            .OrderBy(a => a.CreatedAt)
            .Select(a => new TicketActivityDto(
                a.Id, a.ActorId, a.Actor.FullName, a.Action,
                a.FromStatus, a.ToStatus, a.Details, a.CreatedAt))
            .ToList();

        return new TicketDto(
            t.Id, t.Number, t.Title, t.Description, t.Category, t.Status, t.Priority,
            t.RequesterId, t.Requester.FullName, t.Requester.Email,
            t.OrganizationId, t.Organization.Name,
            t.TargetDepartmentId, t.TargetDepartment.Name, t.TargetDepartment.NameEn,
            t.AssigneeId, t.Assignee?.FullName,
            t.AssignedById, t.AssignedBy?.FullName,
            t.CreatedAt, t.UpdatedAt,
            t.AssignedAt, t.AcceptedAt, t.StartedAt, t.CompletedAt, t.ClosedAt,
            comments, activities);
    }
}
