using ATG.Platform.Application.Common;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Data;
using ATG.Platform.Infrastructure.Hr;
using ATG.Platform.Infrastructure.Seeds;
using Microsoft.EntityFrameworkCore;

namespace ATG.Platform.Infrastructure.Services;

public class HrLeaveRequestService(
    AppDbContext db,
    IAuditService audit,
    INotificationService notifications) : IHrLeaveRequestService
{
    private const int HrParallelGroup = 0;
    private const int SequentialGroup = 1;

    private static readonly HrLeaveApprovalRole[] SequentialRoleOrder =
    [
        HrLeaveApprovalRole.DeputyDepartmentHead,
        HrLeaveApprovalRole.DepartmentHead,
        HrLeaveApprovalRole.SupervisingDeputyGd,
        HrLeaveApprovalRole.GeneralDirector,
    ];

    private static readonly string[] HoHrReviewerEmails =
    [
        "kh.karimberdieva@atg.uz",
        "g.rakhmatullaeva@atg.uz",
    ];

    public async Task<Result<IReadOnlyList<HrLeaveListItemDto>>> GetMyRequestsAsync(Guid actorId, CancellationToken ct = default)
    {
        var items = await db.HrLeaveRequestDetails.AsNoTracking()
            .Include(d => d.Document).ThenInclude(doc => doc.Author)
            .Include(d => d.Document).ThenInclude(doc => doc.Department)
            .Include(d => d.Items)
            .Where(d => d.Document.AuthorId == actorId)
            .OrderByDescending(d => d.Document.CreatedAt)
            .Select(d => MapListItem(d))
            .ToListAsync(ct);
        return Result<IReadOnlyList<HrLeaveListItemDto>>.Ok(items);
    }

    public async Task<Result<IReadOnlyList<HrLeaveListItemDto>>> GetHrQueueAsync(Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<IReadOnlyList<HrLeaveListItemDto>>.Fail("User not found");
        if (!IsHrStaff(actor)) return Result<IReadOnlyList<HrLeaveListItemDto>>.Fail("Access denied");

        var hrDeptIds = await GetAccessibleHrDepartmentIdsAsync(actor, ct);
        var items = await db.HrLeaveRequestDetails.AsNoTracking()
            .Include(d => d.Document).ThenInclude(doc => doc.Author)
            .Include(d => d.Document).ThenInclude(doc => doc.Department)
            .Include(d => d.Items)
            .Where(d => hrDeptIds.Contains(d.HrDepartmentId)
                && (d.Phase == HrLeaveRequestPhase.HrReview || d.Phase == HrLeaveRequestPhase.AwaitingApproval))
            .OrderByDescending(d => d.Document.UpdatedAt)
            .Select(d => MapListItem(d))
            .ToListAsync(ct);
        return Result<IReadOnlyList<HrLeaveListItemDto>>.Ok(items);
    }

    public async Task<Result<HrLeaveRequestDto>> GetByIdAsync(Guid id, Guid actorId, CancellationToken ct = default)
    {
        var detail = await LoadDetailAsync(id, ct);
        if (detail is null) return Result<HrLeaveRequestDto>.Fail("Request not found");
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanView(actor, detail)) return Result<HrLeaveRequestDto>.Fail("Access denied");
        return Result<HrLeaveRequestDto>.Ok(await MapDetailAsync(detail, actor, ct));
    }

    public async Task<Result<HrLeaveRequestDto>> CreateAsync(
        CreateHrLeaveRequestRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<HrLeaveRequestDto>.Fail("User not found");
        if (actor.DepartmentId is null) return Result<HrLeaveRequestDto>.Fail("Your profile has no department");

        var validation = ValidateItems(request.Items);
        if (validation is not null) return Result<HrLeaveRequestDto>.Fail(validation);

        var hrDept = await ResolveHrDepartmentAsync(actor, ct);
        if (hrDept is null) return Result<HrLeaveRequestDto>.Fail("HR department not found");

        var number = await GenerateNumberAsync(ct);
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            Number = number,
            Title = HrLeaveTextBuilder.BuildDocumentTitle(MapItemsForTitle(request.Items), request.PeriodLabel.Trim()),
            Type = DocumentType.HrLeaveRequest,
            Status = DocumentStatus.Draft,
            AuthorId = actorId,
            OrganizationId = actor.OrganizationId,
            DepartmentId = actor.DepartmentId.Value,
            AssigneeId = actorId,
        };

        var detail = new HrLeaveRequestDetail
        {
            DocumentId = doc.Id,
            Document = doc,
            Phase = HrLeaveRequestPhase.Draft,
            Track = HrLeaveRouting.ResolveTrack(actor),
            HrDepartmentId = hrDept.Id,
            PeriodLabel = request.PeriodLabel.Trim(),
            RequestDate = DateTimeNormalization.ToUtc(request.RequestDate),
        };

        AddItems(detail, request.Items);
        db.Documents.Add(doc);
        db.HrLeaveRequestDetails.Add(detail);
        await AddActivityAsync(doc, actorId, "created", null, DocumentStatus.Draft, null, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "HrLeaveCreated", "Document", doc.Id, number, ip, ct);

        var loaded = await LoadDetailAsync(doc.Id, ct);
        return Result<HrLeaveRequestDto>.Ok(await MapDetailAsync(loaded!, actor, ct));
    }

    public async Task<Result<HrLeaveRequestDto>> UpdateAsync(
        Guid id, UpdateHrLeaveRequestRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<HrLeaveRequestDto>.Fail("Request not found");
        if (detail.Phase != HrLeaveRequestPhase.Draft)
            return Result<HrLeaveRequestDto>.Fail("Only draft requests can be edited");
        if (detail.Document.AuthorId != actorId)
            return Result<HrLeaveRequestDto>.Fail("Access denied");

        var validation = ValidateItems(request.Items);
        if (validation is not null) return Result<HrLeaveRequestDto>.Fail(validation);

        detail.PeriodLabel = request.PeriodLabel.Trim();
        detail.RequestDate = DateTimeNormalization.ToUtc(request.RequestDate);
        detail.Document.Title = HrLeaveTextBuilder.BuildDocumentTitle(MapItemsForTitle(request.Items), detail.PeriodLabel);
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await db.HrLeaveRequestItems.Where(i => i.DocumentId == id).ExecuteDeleteAsync(ct);
        detail.Items.Clear();
        AddItems(detail, request.Items);

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "HrLeaveUpdated", "Document", id, null, ip, ct);

        var actor = await GetActorAsync(actorId, ct);
        return Result<HrLeaveRequestDto>.Ok(await MapDetailAsync(detail, actor!, ct));
    }

    public async Task<Result<HrLeaveRequestDto>> SubmitAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<HrLeaveRequestDto>.Fail("Request not found");
        if (detail.Phase != HrLeaveRequestPhase.Draft)
            return Result<HrLeaveRequestDto>.Fail("Request is already submitted");
        if (detail.Document.AuthorId != actorId)
            return Result<HrLeaveRequestDto>.Fail("Access denied");
        if (detail.Items.Count == 0)
            return Result<HrLeaveRequestDto>.Fail("Add at least one leave item");

        var hrHead = await GetDepartmentHeadAsync(detail.HrDepartmentId, ct);
        detail.Phase = HrLeaveRequestPhase.HrReview;
        detail.Document.Status = DocumentStatus.InReview;
        detail.Document.AssigneeId = hrHead?.Id ?? actorId;
        detail.Document.DepartmentId = detail.HrDepartmentId;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddHrReviewersAsync(detail, ct);
        var task = await CreateLinkedTaskAsync(
            hrHead?.Id ?? actorId,
            actorId,
            detail.HrDepartmentId,
            detail.Document.OrganizationId,
            $"HR leave review — {detail.Document.Number}",
            detail.Document.Title,
            detail.Document.Id,
            ct);
        detail.HrTaskId = task.Id;

        await AddActivityAsync(detail.Document, actorId, "submitted", DocumentStatus.Draft, DocumentStatus.InReview, null, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "HrLeaveSubmitted", "Document", id, null, ip, ct);
        await NotifyHrReviewersAsync(detail, ct);

        var actor = await GetActorAsync(actorId, ct);
        return Result<HrLeaveRequestDto>.Ok(await MapDetailAsync(detail, actor!, ct));
    }

    public async Task<Result<HrLeaveRequestDto>> HrReviewAsync(
        Guid id, HrLeaveApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<HrLeaveRequestDto>.Fail("Request not found");
        if (detail.Phase != HrLeaveRequestPhase.HrReview)
            return Result<HrLeaveRequestDto>.Fail("Request is not in HR review");

        var approver = GetPendingHrReviewer(detail, actorId);
        if (approver is null) return Result<HrLeaveRequestDto>.Fail("You are not a pending HR reviewer for this request");

        approver.Status = HrLeaveApproverStatus.Approved;
        approver.DecidedAt = DateTime.UtcNow;
        approver.Comment = request.Comment?.Trim();
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "hr_reviewed", null, detail.Document.Status,
            approver.User?.FullName, ct);

        if (AllHrReviewersApproved(detail))
        {
            detail.HrReviewCompletedAt = DateTime.UtcNow;
            await BuildSequentialApproversAsync(detail, ct);
            detail.Phase = HrLeaveRequestPhase.AwaitingApproval;
            await NotifyNextApproverAsync(detail, ct);
        }

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "HrLeaveHrReviewed", "Document", id, null, ip, ct);

        var actor = await GetActorAsync(actorId, ct);
        return Result<HrLeaveRequestDto>.Ok(await MapDetailAsync(detail, actor!, ct));
    }

    public async Task<Result<HrLeaveRequestDto>> ApproveAsync(
        Guid id, HrLeaveApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<HrLeaveRequestDto>.Fail("Request not found");
        if (detail.Phase != HrLeaveRequestPhase.AwaitingApproval)
            return Result<HrLeaveRequestDto>.Fail("Request is not awaiting approval");

        var approver = GetNextPendingSequentialApprover(detail);
        if (approver is null) return Result<HrLeaveRequestDto>.Fail("No pending approvers");
        if (approver.UserId != actorId)
            return Result<HrLeaveRequestDto>.Fail("Wait for the previous approver in the chain");

        approver.Status = HrLeaveApproverStatus.Approved;
        approver.DecidedAt = DateTime.UtcNow;
        approver.Comment = request.Comment?.Trim();
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "approved", null, detail.Document.Status,
            approver.Role.ToString(), ct);

        if (detail.Approvers.Where(a => a.ApprovalGroup == SequentialGroup)
            .All(a => a.Status == HrLeaveApproverStatus.Approved))
        {
            detail.Phase = HrLeaveRequestPhase.Approved;
            detail.ApprovedAt = DateTime.UtcNow;
            detail.Document.Status = DocumentStatus.Approved;
            await AddActivityAsync(detail.Document, actorId, "fully_approved", null,
                DocumentStatus.Approved, null, ct);
        }
        else
        {
            await NotifyNextApproverAsync(detail, ct);
        }

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "HrLeaveApproved", "Document", id, approver.Role.ToString(), ip, ct);

        var actor = await GetActorAsync(actorId, ct);
        return Result<HrLeaveRequestDto>.Ok(await MapDetailAsync(detail, actor!, ct));
    }

    public async Task<Result<HrLeaveRequestDto>> RejectAsync(
        Guid id, HrLeaveApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<HrLeaveRequestDto>.Fail("Request not found");
        if (detail.Phase is not (HrLeaveRequestPhase.HrReview or HrLeaveRequestPhase.AwaitingApproval))
            return Result<HrLeaveRequestDto>.Fail("Request cannot be rejected in this phase");

        if (string.IsNullOrWhiteSpace(request.Comment))
            return Result<HrLeaveRequestDto>.Fail("Rejection reason is required");

        HrLeaveApprover? approver = detail.Phase == HrLeaveRequestPhase.HrReview
            ? GetPendingHrReviewer(detail, actorId)
            : GetNextPendingSequentialApprover(detail);

        if (approver is null || approver.UserId != actorId)
            return Result<HrLeaveRequestDto>.Fail("You cannot reject this request now");

        approver.Status = HrLeaveApproverStatus.Rejected;
        approver.DecidedAt = DateTime.UtcNow;
        approver.Comment = request.Comment.Trim();
        detail.Phase = HrLeaveRequestPhase.Rejected;
        detail.Document.Status = DocumentStatus.Rejected;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "rejected", null, DocumentStatus.Rejected,
            request.Comment.Trim(), ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "HrLeaveRejected", "Document", id, null, ip, ct);
        await notifications.NotifyDcsApprovalRejectedAsync(
            detail.Document.AuthorId, detail.Document.Number, detail.DocumentId, ct);

        var actor = await GetActorAsync(actorId, ct);
        return Result<HrLeaveRequestDto>.Ok(await MapDetailAsync(detail, actor!, ct));
    }

    private async Task AddHrReviewersAsync(HrLeaveRequestDetail detail, CancellationToken ct)
    {
        var isHo = detail.HrDepartment.Code == HrLeaveRouting.HoHrDepartmentCode;
        var query = db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.DepartmentId == detail.HrDepartmentId);

        List<User> reviewers;
        if (isHo)
        {
            var preferred = await query.Where(u => HoHrReviewerEmails.Contains(u.Email)).ToListAsync(ct);
            reviewers = preferred.Count > 0
                ? preferred
                : await query.Where(u => u.Role == UserRole.HOEngineer).Take(2).ToListAsync(ct);
        }
        else
        {
            reviewers = await query.Where(u => u.Role == UserRole.BMGMCEngineer).Take(2).ToListAsync(ct);
        }

        var order = 0;
        foreach (var user in reviewers)
        {
            detail.Approvers.Add(new HrLeaveApprover
            {
                Id = Guid.NewGuid(),
                DocumentId = detail.DocumentId,
                UserId = user.Id,
                Role = HrLeaveApprovalRole.HrSpecialist,
                Status = HrLeaveApproverStatus.Pending,
                SortOrder = order++,
                ApprovalGroup = HrParallelGroup,
            });
        }
    }

    private async Task BuildSequentialApproversAsync(HrLeaveRequestDetail detail, CancellationToken ct)
    {
        var author = await db.Users.AsNoTracking()
            .Include(u => u.Department)
            .FirstAsync(u => u.Id == detail.Document.AuthorId, ct);

        var chain = new List<(HrLeaveApprovalRole Role, User? User)>();
        var deptHead = author.DepartmentId is Guid deptId
            ? await GetDepartmentHeadAsync(deptId, ct)
            : null;
        var deputy = author.DepartmentId is Guid dId
            ? await GetDepartmentDeputyAsync(dId, ct)
            : null;

        if (detail.Track == HrLeaveTrack.Specialist)
        {
            if (deputy is not null && deputy.Id != author.Id)
                chain.Add((HrLeaveApprovalRole.DeputyDepartmentHead, deputy));
            if (deptHead is not null && deptHead.Id != author.Id)
                chain.Add((HrLeaveApprovalRole.DepartmentHead, deptHead));
        }
        else
        {
            if (detail.Track == HrLeaveTrack.DepartmentHead && deptHead?.Id == author.Id)
            {
                // skip deputy/head for department head initiator
            }
            else if (deptHead is not null && deptHead.Id != author.Id)
            {
                chain.Add((HrLeaveApprovalRole.DepartmentHead, deptHead));
            }

            var supervising = await GetSupervisingDeputyAsync(author.OrganizationId, ct);
            if (supervising is not null)
                chain.Add((HrLeaveApprovalRole.SupervisingDeputyGd, supervising));
        }

        var gd = await GetGeneralDirectorAsync(author.Organization, ct);
        if (gd is not null)
            chain.Add((HrLeaveApprovalRole.GeneralDirector, gd));

        var order = 0;
        foreach (var (role, user) in chain.Where(c => c.User is not null))
        {
            detail.Approvers.Add(new HrLeaveApprover
            {
                Id = Guid.NewGuid(),
                DocumentId = detail.DocumentId,
                UserId = user!.Id,
                Role = role,
                Status = HrLeaveApproverStatus.Pending,
                SortOrder = order++,
                ApprovalGroup = SequentialGroup,
            });
        }
    }

    private static HrLeaveApprover? GetPendingHrReviewer(HrLeaveRequestDetail detail, Guid actorId) =>
        detail.Approvers.FirstOrDefault(a =>
            a.ApprovalGroup == HrParallelGroup
            && a.UserId == actorId
            && a.Status == HrLeaveApproverStatus.Pending);

    private static bool AllHrReviewersApproved(HrLeaveRequestDetail detail) =>
        detail.Approvers.Where(a => a.ApprovalGroup == HrParallelGroup).All(a => a.Status == HrLeaveApproverStatus.Approved);

    private static HrLeaveApprover? GetNextPendingSequentialApprover(HrLeaveRequestDetail detail) =>
        detail.Approvers
            .Where(a => a.ApprovalGroup == SequentialGroup && a.Status == HrLeaveApproverStatus.Pending)
            .OrderBy(a => a.SortOrder)
            .FirstOrDefault();

    private async Task NotifyHrReviewersAsync(HrLeaveRequestDetail detail, CancellationToken ct)
    {
        foreach (var a in detail.Approvers.Where(x => x.ApprovalGroup == HrParallelGroup))
        {
            await notifications.NotifyDcsApprovalRequiredAsync(
                a.UserId, detail.Document.Number, detail.Document.Title, detail.DocumentId, ct);
        }
    }

    private async Task NotifyNextApproverAsync(HrLeaveRequestDetail detail, CancellationToken ct)
    {
        var next = detail.Phase == HrLeaveRequestPhase.HrReview
            ? detail.Approvers.FirstOrDefault(a => a.ApprovalGroup == HrParallelGroup && a.Status == HrLeaveApproverStatus.Pending)
            : GetNextPendingSequentialApprover(detail);
        if (next is null) return;
        await notifications.NotifyDcsApprovalRequiredAsync(
            next.UserId, detail.Document.Number, detail.Document.Title, detail.DocumentId, ct);
    }

    private async Task<User?> GetDepartmentHeadAsync(Guid departmentId, CancellationToken ct) =>
        await db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.DepartmentId == departmentId
                && (u.Role == UserRole.HONachalnik || u.Role == UserRole.BMGMCNachalnikiOtdeli))
            .OrderBy(u => u.LastName)
            .FirstOrDefaultAsync(ct);

    private async Task<User?> GetDepartmentDeputyAsync(Guid departmentId, CancellationToken ct)
    {
        var users = await db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.DepartmentId == departmentId
                && (u.Role == UserRole.HONachalnik || u.Role == UserRole.BMGMCNachalnikiOtdeli))
            .OrderBy(u => u.LastName)
            .ToListAsync(ct);
        return users.Count > 1 ? users[1] : null;
    }

    private async Task<User?> GetSupervisingDeputyAsync(Guid organizationId, CancellationToken ct) =>
        await db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.OrganizationId == organizationId && u.Role == UserRole.HOTopManager)
            .OrderBy(u => u.LastName)
            .Skip(1)
            .FirstOrDefaultAsync(ct)
            ?? await db.Users.AsNoTracking()
                .Where(u => u.IsActive && u.OrganizationId == organizationId && u.Role == UserRole.BMGMCManager)
                .OrderBy(u => u.LastName)
                .FirstOrDefaultAsync(ct);

    private async Task<User?> GetGeneralDirectorAsync(Organization org, CancellationToken ct)
    {
        if (org.OrgType == OrgType.HeadOffice || org.Code == "HO")
        {
            return await db.Users.AsNoTracking()
                .Include(u => u.Organization)
                .Where(u => u.IsActive && u.Organization.Code == HoMasterData.OrganizationCode
                    && u.Role == UserRole.HOTopManager)
                .OrderBy(u => u.LastName)
                .FirstOrDefaultAsync(ct);
        }

        return await db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.Role == UserRole.BMGMCManager)
            .OrderBy(u => u.LastName)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<Department?> ResolveHrDepartmentAsync(User actor, CancellationToken ct)
    {
        var code = HrLeaveRouting.ResolveHrDepartmentCode(actor.Organization);
        var orgCode = code == HrLeaveRouting.HoHrDepartmentCode
            ? HoMasterData.OrganizationCode
            : BmgmcMasterData.OrganizationCode;
        return await db.Departments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Code == code && d.Organization.Code == orgCode, ct);
    }

    private async Task<List<Guid>> GetAccessibleHrDepartmentIdsAsync(User actor, CancellationToken ct)
    {
        var hoHr = await db.Departments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Code == HrLeaveRouting.HoHrDepartmentCode
                && d.Organization.Code == HoMasterData.OrganizationCode, ct);
        var bmgmcHr = await db.Departments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Code == HrLeaveRouting.BmgmcHrDepartmentCode
                && d.Organization.Code == BmgmcMasterData.OrganizationCode, ct);

        if (actor.Organization.Code == HoMasterData.OrganizationCode && hoHr is not null)
            return [hoHr.Id];
        if (bmgmcHr is not null && (actor.Organization.Code == BmgmcMasterData.OrganizationCode
            || actor.Organization.OrgType == OrgType.Station
            || actor.Organization.Parent?.Code == BmgmcMasterData.OrganizationCode))
            return [bmgmcHr.Id];

        var ids = new List<Guid>();
        if (hoHr is not null) ids.Add(hoHr.Id);
        if (bmgmcHr is not null) ids.Add(bmgmcHr.Id);
        return ids;
    }

    private static bool IsHrStaff(User actor) =>
        actor.Department?.Code is HrLeaveRouting.HoHrDepartmentCode or HrLeaveRouting.BmgmcHrDepartmentCode
        || actor.Role is UserRole.SuperAdmin;

    private static bool CanView(User actor, HrLeaveRequestDetail detail)
    {
        if (actor.Role == UserRole.SuperAdmin) return true;
        if (detail.Document.AuthorId == actor.Id) return true;
        if (IsHrStaff(actor) && actor.DepartmentId == detail.HrDepartmentId) return true;
        if (detail.Approvers.Any(a => a.UserId == actor.Id)) return true;
        return false;
    }

    private async Task<HrLeaveRequestDto> MapDetailAsync(HrLeaveRequestDetail detail, User actor, CancellationToken ct)
    {
        string? taskNumber = null;
        if (detail.HrTaskId is Guid taskId)
        {
            taskNumber = await db.WorkTasks.AsNoTracking()
                .Where(t => t.Id == taskId)
                .Select(t => t.Number)
                .FirstOrDefaultAsync(ct);
        }

        var activities = await db.DocumentActivities.AsNoTracking()
            .Include(a => a.Actor)
            .Where(a => a.DocumentId == detail.DocumentId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(ct);

        var items = detail.Items.OrderBy(i => i.SortOrder).Select(i =>
        {
            var (ru, en) = HrLeaveTextBuilder.BuildItemText(i, detail.PeriodLabel);
            return new HrLeaveItemDto(i.Id, i.Type, i.DateFrom, i.DateTo, i.DaysCount, i.NoteRu, i.NoteEn, i.SortOrder, ru, en);
        }).ToList();

        var approvers = detail.Approvers
            .OrderBy(a => a.ApprovalGroup).ThenBy(a => a.SortOrder)
            .Select(a => new HrLeaveApproverDto(
                a.Id, a.UserId, a.User?.FullName ?? "", a.Role, a.Status, a.SortOrder, a.ApprovalGroup,
                a.DecidedAt, a.Comment, a.User?.Department?.Name, a.User?.Department?.NameEn))
            .ToList();

        var permissions = BuildPermissions(actor, detail);
        return new HrLeaveRequestDto(
            detail.DocumentId,
            detail.Document.Number,
            detail.Document.Status,
            detail.Phase,
            detail.Track,
            detail.PeriodLabel,
            detail.RequestDate,
            detail.Document.Author.FullName,
            detail.Document.Department.Name,
            detail.Document.Department.NameEn,
            detail.Document.Organization.Name,
            detail.HrDepartment.Name,
            detail.HrDepartment.NameEn,
            taskNumber,
            detail.Document.CreatedAt,
            detail.Document.UpdatedAt,
            items,
            approvers,
            activities.Select(a => new HrLeaveTimelineEventDto(
                a.Id, a.Action, a.Actor?.FullName ?? "", a.Details, a.CreatedAt)).ToList(),
            permissions);
    }

    private static HrLeavePermissionsDto BuildPermissions(User actor, HrLeaveRequestDetail detail)
    {
        var isAuthor = detail.Document.AuthorId == actor.Id;
        var pendingHr = GetPendingHrReviewer(detail, actor.Id);
        var pendingSeq = GetNextPendingSequentialApprover(detail);

        return new HrLeavePermissionsDto(
            CanCreate: true,
            CanEdit: isAuthor && detail.Phase == HrLeaveRequestPhase.Draft,
            CanSubmit: isAuthor && detail.Phase == HrLeaveRequestPhase.Draft && detail.Items.Count > 0,
            CanHrReview: detail.Phase == HrLeaveRequestPhase.HrReview && pendingHr is not null,
            CanApprove: detail.Phase == HrLeaveRequestPhase.AwaitingApproval
                && pendingSeq?.UserId == actor.Id,
            CanReject: (detail.Phase == HrLeaveRequestPhase.HrReview && pendingHr is not null)
                || (detail.Phase == HrLeaveRequestPhase.AwaitingApproval && pendingSeq?.UserId == actor.Id));
    }

    private static HrLeaveListItemDto MapListItem(HrLeaveRequestDetail d) => new(
        d.DocumentId,
        d.Document.Number,
        d.Document.Status,
        d.Phase,
        d.Document.Author.FullName,
        d.Document.Department.Name,
        d.Document.Department.NameEn,
        d.RequestDate,
        d.Document.CreatedAt,
        d.Items.Count);

    private static void AddItems(HrLeaveRequestDetail detail, IReadOnlyList<CreateHrLeaveItemRequest> items)
    {
        var order = 0;
        foreach (var item in items)
        {
            detail.Items.Add(new HrLeaveRequestItem
            {
                Id = Guid.NewGuid(),
                DocumentId = detail.DocumentId,
                Type = item.Type,
                DateFrom = DateTimeNormalization.ToUtc(item.DateFrom),
                DateTo = DateTimeNormalization.ToUtc(item.DateTo),
                DaysCount = item.DaysCount,
                NoteRu = item.NoteRu?.Trim(),
                NoteEn = item.NoteEn?.Trim(),
                SortOrder = order++,
            });
        }
    }

    private static List<HrLeaveRequestItem> MapItemsForTitle(IReadOnlyList<CreateHrLeaveItemRequest> items) =>
        items.Select((item, i) => new HrLeaveRequestItem
        {
            Type = item.Type,
            SortOrder = i,
            DateFrom = item.DateFrom,
            DateTo = item.DateTo,
            DaysCount = item.DaysCount,
        }).ToList();

    private static string? ValidateItems(IReadOnlyList<CreateHrLeaveItemRequest> items)
    {
        if (items.Count == 0) return "Add at least one leave item";
        foreach (var item in items)
        {
            if (item.Type == HrLeaveItemType.CompensationDays && (item.DaysCount is null or < 1))
                return "Compensation days count is required";
            if (item.Type is HrLeaveItemType.RegularLeave or HrLeaveItemType.UnpaidLeave or HrLeaveItemType.PartialPayLeave)
            {
                if (item.DateFrom is null || item.DateTo is null)
                    return "Date range is required for this leave type";
                if (item.DateTo < item.DateFrom)
                    return "End date must be on or after start date";
            }
        }
        return null;
    }

    private async Task<HrLeaveRequestDetail?> LoadDetailAsync(Guid id, CancellationToken ct) =>
        await DetailQuery().FirstOrDefaultAsync(d => d.DocumentId == id, ct);

    private async Task<HrLeaveRequestDetail?> LoadDetailTrackedAsync(Guid id, CancellationToken ct) =>
        await DetailQuery(tracked: true).FirstOrDefaultAsync(d => d.DocumentId == id, ct);

    private IQueryable<HrLeaveRequestDetail> DetailQuery(bool tracked = false)
    {
        var q = tracked ? db.HrLeaveRequestDetails.AsQueryable() : db.HrLeaveRequestDetails.AsNoTracking();
        return q
            .Include(d => d.Document).ThenInclude(doc => doc.Author)
            .Include(d => d.Document).ThenInclude(doc => doc.Department)
            .Include(d => d.Document).ThenInclude(doc => doc.Organization)
            .Include(d => d.HrDepartment)
            .Include(d => d.Items)
            .Include(d => d.Approvers).ThenInclude(a => a.User).ThenInclude(u => u.Department);
    }

    private async Task<User?> GetActorAsync(Guid actorId, CancellationToken ct) =>
        await db.Users
            .Include(u => u.Organization).ThenInclude(o => o.Parent)
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == actorId && u.IsActive, ct);

    private async Task<string> GenerateNumberAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"HLV-{year}-";
        var last = await db.Documents.AsNoTracking()
            .Where(d => d.Number.StartsWith(prefix))
            .OrderByDescending(d => d.Number)
            .Select(d => d.Number)
            .FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var n))
            seq = n + 1;
        return $"{prefix}{seq:D3}";
    }

    private async Task<WorkTask> CreateLinkedTaskAsync(
        Guid assigneeId, Guid createdById, Guid deptId, Guid orgId,
        string title, string description, Guid documentId, CancellationToken ct)
    {
        var task = new WorkTask
        {
            Id = Guid.NewGuid(),
            Number = await GenerateTaskNumberAsync(ct),
            Title = title,
            Description = description,
            Status = WorkTaskStatus.New,
            Priority = TaskPriority.Medium,
            Source = TaskSource.HR,
            ExternalId = documentId,
            AssigneeId = assigneeId,
            CreatedById = createdById,
            OrganizationId = orgId,
            DepartmentId = deptId,
        };
        db.WorkTasks.Add(task);
        return task;
    }

    private async Task<string> GenerateTaskNumberAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"TSK-{year}-";
        var last = await db.WorkTasks.AsNoTracking()
            .Where(t => t.Number.StartsWith(prefix))
            .OrderByDescending(t => t.Number)
            .Select(t => t.Number)
            .FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var n))
            seq = n + 1;
        return $"{prefix}{seq:D3}";
    }

    private Task AddActivityAsync(
        Document doc, Guid actorId, string action, DocumentStatus? from, DocumentStatus? to, string? details, CancellationToken ct)
    {
        db.DocumentActivities.Add(new DocumentActivity
        {
            Id = Guid.NewGuid(),
            DocumentId = doc.Id,
            ActorId = actorId,
            Action = action,
            FromStatus = from,
            ToStatus = to,
            Details = details,
            CreatedAt = DateTime.UtcNow,
        });
        doc.UpdatedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }
}
