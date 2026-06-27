using ATG.Platform.Application.Common;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Data;
using ATG.Platform.Infrastructure.Dcs;
using ATG.Platform.Infrastructure.Seeds;
using Microsoft.EntityFrameworkCore;

namespace ATG.Platform.Infrastructure.Services;

public class DcsService(AppDbContext db, IAuditService audit) : IDcsService
{
    public IReadOnlyList<DcsTypeDto> GetTypes() =>
        DcsRouting.Types.Select(t => new DcsTypeDto(t.Type, t.NameEn, t.NameRu, t.Section, t.Icon, t.Color)).ToList();

    public async Task<Result<PagedResult<DocumentListItemDto>>> GetDocumentsAsync(
        Guid actorId,
        DocumentType type,
        string view,
        int page,
        int pageSize,
        DocumentStatus? status,
        CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<PagedResult<DocumentListItemDto>>.Fail("User not found");

        var query = DocumentQuery().Where(d => d.Type == type);

        query = view.ToLowerInvariant() switch
        {
            "mine" => query.Where(d => d.AuthorId == actorId),
            "department" => query.Where(d => d.DepartmentId == actor.DepartmentId),
            "registry" => query.Where(d => d.OrganizationId == actor.OrganizationId),
            "all" when IsPlatformAdmin(actor) => query,
            _ => query.Where(d => d.OrganizationId == actor.OrganizationId)
        };

        if (status.HasValue) query = query.Where(d => d.Status == status);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(d => d.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Result<PagedResult<DocumentListItemDto>>.Ok(new PagedResult<DocumentListItemDto>(
            items.Select(MapListItem).ToList(), total, page, pageSize));
    }

    public async Task<Result<DocumentDto>> GetByIdAsync(Guid id, Guid actorId, CancellationToken ct = default)
    {
        var doc = await FullDocumentQuery().FirstOrDefaultAsync(d => d.Id == id, ct);
        if (doc is null) return Result<DocumentDto>.Fail("Document not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanView(actor, doc))
            return Result<DocumentDto>.Fail("Access denied");

        return Result<DocumentDto>.Ok(MapDocument(doc));
    }

    public async Task<Result<DocumentDto>> CreateAsync(
        CreateDocumentRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<DocumentDto>.Fail("User not found");

        if (string.IsNullOrWhiteSpace(request.Title))
            return Result<DocumentDto>.Fail("Title is required");

        if (request.Type == DocumentType.Incoming)
            return Result<DocumentDto>.Fail("Incoming letters must be registered via the incoming letter form");

        var deptCode = DcsRouting.ResolveDepartmentCode(request.Type, actor.Organization.Code);
        if (deptCode is null)
            return Result<DocumentDto>.Fail("No routing for this document type");

        var dept = await db.Departments.FirstOrDefaultAsync(d =>
            d.OrganizationId == actor.OrganizationId && d.Code == deptCode && d.IsActive, ct);

        if (dept is null)
        {
            dept = await db.Departments.FirstOrDefaultAsync(d => d.Code == deptCode && d.IsActive, ct);
            if (dept is null)
                return Result<DocumentDto>.Fail($"Department {deptCode} not found");
        }

        var number = await GenerateNumberAsync(request.Type, ct);
        var doc = new Document
        {
            Number = number,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? "",
            Type = request.Type,
            Status = DocumentStatus.Draft,
            AuthorId = actorId,
            OrganizationId = actor.OrganizationId,
            DepartmentId = dept.Id,
            ExternalReference = request.ExternalReference?.Trim(),
            DueDate = request.DueDate
        };

        db.Documents.Add(doc);
        await AddActivityAsync(doc, actorId, "created", null, DocumentStatus.Draft,
            $"Routed to {dept.Code}", ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "DocumentCreated", "Document", doc.Id, doc.Number, ip, ct);

        return await GetByIdAsync(doc.Id, actorId, ct);
    }

    public async Task<Result<DocumentDto>> UpdateStatusAsync(
        Guid id, UpdateDocumentStatusRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var doc = await db.Documents.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (doc is null) return Result<DocumentDto>.Fail("Document not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanManage(actor, doc))
            return Result<DocumentDto>.Fail("Access denied");

        var from = doc.Status;
        doc.Status = request.Status;
        doc.UpdatedAt = DateTime.UtcNow;

        if (request.Status == DocumentStatus.Registered && doc.RegisteredAt is null)
            doc.RegisteredAt = DateTime.UtcNow;

        await AddActivityAsync(doc, actorId, "status_changed", from, request.Status, null, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "DocumentStatusChanged", "Document", doc.Id, request.Status.ToString(), ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<DcsDashboardDto>> GetAdminDashboardAsync(Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsPlatformAdmin(actor))
            return Result<DcsDashboardDto>.Fail("Access denied");

        var draft = await db.Documents.CountAsync(d => d.Status == DocumentStatus.Draft, ct);
        var inReview = await db.Documents.CountAsync(d =>
            d.Status == DocumentStatus.InReview || d.Status == DocumentStatus.Registered, ct);
        var approved = await db.Documents.CountAsync(d => d.Status == DocumentStatus.Approved, ct);
        var archived = await db.Documents.CountAsync(d =>
            d.Status == DocumentStatus.Archived || d.Status == DocumentStatus.Rejected, ct);

        var recent = await DocumentQuery()
            .OrderByDescending(d => d.UpdatedAt)
            .Take(20)
            .ToListAsync(ct);

        return Result<DcsDashboardDto>.Ok(new DcsDashboardDto(
            draft, inReview, approved, archived, recent.Select(MapListItem).ToList()));
    }

    public async Task<Result<DcsAdminControlDto>> GetAdminControlAsync(Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsPlatformAdmin(actor))
            return Result<DcsAdminControlDto>.Fail("Access denied");

        var dashboardResult = await GetAdminDashboardAsync(actorId, ct);
        if (!dashboardResult.IsSuccess)
            return Result<DcsAdminControlDto>.Fail(dashboardResult.Error!);

        var orgs = await db.Organizations.AsNoTracking()
            .Where(o => o.Code == HoMasterData.OrganizationCode || o.Code == BmgmcMasterData.OrganizationCode)
            .ToDictionaryAsync(o => o.Code, ct);

        var deptCodes = DcsRouting.Types
            .SelectMany(t => new[]
            {
                DcsRouting.ResolveDepartmentCode(t.Type, HoMasterData.OrganizationCode),
                DcsRouting.ResolveDepartmentCode(t.Type, BmgmcMasterData.OrganizationCode)
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

        var registrarEmails = DcsRouting.Types
            .Select(t => DcsRouting.GetRegistrarEmail(t.Type))
            .Where(e => e is not null)
            .Distinct()
            .ToList();

        var registrarUsers = await db.Users.AsNoTracking()
            .Where(u => u.IsActive && registrarEmails.Contains(u.Email))
            .ToListAsync(ct);

        var docStats = await db.Documents.AsNoTracking()
            .Where(d => d.Status != DocumentStatus.Archived && d.Status != DocumentStatus.Rejected)
            .GroupBy(d => new { d.Type, d.DepartmentId, d.Status })
            .Select(g => new { g.Key.Type, g.Key.DepartmentId, g.Key.Status, Count = g.Count() })
            .ToListAsync(ct);

        var categories = DcsRouting.Types.Select(cat =>
        {
            var routes = new List<DcsOrgRoutingDto>();
            foreach (var orgCode in new[] { HoMasterData.OrganizationCode, BmgmcMasterData.OrganizationCode })
            {
                var resolvedCode = DcsRouting.ResolveDepartmentCode(cat.Type, orgCode);
                if (resolvedCode is null || !orgs.TryGetValue(orgCode, out var org)) continue;

                var dept = departments.FirstOrDefault(d =>
                    d.Code == resolvedCode && d.OrganizationId == org.Id);
                if (dept is null) continue;

                var deptStaff = staff.Where(u => u.DepartmentId == dept.Id).ToList();
                var assigners = deptStaff
                    .Where(u => IsDeptManager(u) || u.Role == UserRole.HOTopManager)
                    .Select(MapStaff)
                    .ToList();
                var handlers = deptStaff
                    .Where(u => !IsDeptManager(u) && u.Role is not UserRole.HOTopManager and not UserRole.SuperAdmin)
                    .Select(MapStaff)
                    .ToList();

                DcsStaffDto? designated = null;
                var email = DcsRouting.GetRegistrarEmail(cat.Type);
                if (email is not null)
                {
                    var user = registrarUsers.FirstOrDefault(u => u.Email == email);
                    if (user is not null)
                        designated = MapStaff(user);
                }

                var draft = docStats
                    .Where(d => d.Type == cat.Type && d.DepartmentId == dept.Id && d.Status == DocumentStatus.Draft)
                    .Sum(d => d.Count);
                var active = docStats
                    .Where(d => d.Type == cat.Type && d.DepartmentId == dept.Id)
                    .Sum(d => d.Count);

                routes.Add(new DcsOrgRoutingDto(
                    org.Code, org.Name, dept.Id, dept.Code,
                    dept.Name, dept.NameEn, draft, active, assigners, handlers, designated));
            }

            return new DcsCategoryRoutingDto(
                cat.Type, cat.NameEn, cat.NameRu, cat.Section, cat.Icon, cat.Color, routes);
        }).ToList();

        return Result<DcsAdminControlDto>.Ok(
            new DcsAdminControlDto(dashboardResult.Data!, categories));
    }

    private static DcsStaffDto MapStaff(User u) => new(
        u.Id, u.EmployeeId, u.FullName, u.Email, u.Role.ToString(), u.JobTitleEn, u.JobTitleRu);

    private async Task<string> GenerateNumberAsync(DocumentType type, CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"{DcsRouting.NumberPrefix(type)}-{year}-";
        var last = await db.Documents
            .Where(d => d.Number.StartsWith(prefix))
            .OrderByDescending(d => d.Number)
            .Select(d => d.Number)
            .FirstOrDefaultAsync(ct);

        var seq = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var n))
            seq = n + 1;

        return $"{prefix}{seq:D3}";
    }

    private async Task AddActivityAsync(
        Document doc, Guid actorId, string action,
        DocumentStatus? from, DocumentStatus? to, string? details, CancellationToken ct)
    {
        db.DocumentActivities.Add(new DocumentActivity
        {
            DocumentId = doc.Id,
            ActorId = actorId,
            Action = action,
            FromStatus = from,
            ToStatus = to,
            Details = details
        });
        await Task.CompletedTask;
    }

    private IQueryable<Document> DocumentQuery() =>
        db.Documents
            .Include(d => d.Author)
            .Include(d => d.Assignee)
            .Include(d => d.Department);

    private IQueryable<Document> FullDocumentQuery() =>
        DocumentQuery()
            .Include(d => d.Organization)
            .Include(d => d.Assignee)
            .Include(d => d.Activities).ThenInclude(a => a.Actor);

    private async Task<User?> GetActorAsync(Guid id, CancellationToken ct) =>
        await db.Users.Include(u => u.Organization).Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    private static bool IsPlatformAdmin(User u) =>
        u.Role is UserRole.SuperAdmin or UserRole.HOTopManager;

    private static bool IsDeptManager(User u) =>
        u.Role is UserRole.HONachalnik or UserRole.BMGMCNachalnikiOtdeli or UserRole.BMGMCManager;

    private static bool CanView(User actor, Document doc) =>
        IsPlatformAdmin(actor) ||
        doc.OrganizationId == actor.OrganizationId;

    private static bool CanManage(User actor, Document doc) =>
        IsPlatformAdmin(actor) ||
        doc.AuthorId == actor.Id ||
        (IsDeptManager(actor) && actor.DepartmentId == doc.DepartmentId);

    private static DocumentListItemDto MapListItem(Document d) => new(
        d.Id, d.Number, d.Title, d.Type, d.Status,
        d.Author.FullName, d.Assignee?.FullName,
        d.Department.Name, d.Department.NameEn,
        d.CreatedAt, d.UpdatedAt);

    private static DocumentDto MapDocument(Document d) => new(
        d.Id, d.Number, d.Title, d.Description, d.Type, d.Status,
        d.AuthorId, d.Author.FullName, d.Author.FullNameEn, d.Author.Email,
        d.OrganizationId, d.Organization.Name,
        d.DepartmentId, d.Department.Name, d.Department.NameEn,
        d.AssigneeId, d.Assignee?.FullName,
        d.ExternalReference, d.RegisteredAt, d.DueDate,
        d.TitleRu, d.IncomingNumber, d.IncomingDate, d.RecordBook,
        d.SenderName, d.ReceiverName, d.Assignee?.FullNameEn,
        d.AttachmentFileName, d.TranslationRequestCount,
        d.CreatedAt, d.UpdatedAt,
        d.Activities.OrderBy(a => a.CreatedAt).Select(a => new DocumentActivityDto(
            a.Id, a.ActorId, a.Actor.FullName, a.Action,
            a.FromStatus, a.ToStatus, a.Details, a.CreatedAt)).ToList());
}
