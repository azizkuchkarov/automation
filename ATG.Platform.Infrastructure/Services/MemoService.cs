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

public class MemoService(AppDbContext db, IAuditService audit) : IMemoService
{
    private const string TranslationDept = "HO-DCPR-TRNS";

    public async Task<Result<MemoPermissionsDto>> GetPermissionsAsync(
        Guid actorId, Guid? documentId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<MemoPermissionsDto>.Fail("User not found");

        MemoDetail? detail = null;
        if (documentId.HasValue)
            detail = await LoadDetailAsync(documentId.Value, ct);

        var phase = detail?.Phase ?? MemoPhase.Draft;
        var isInitiator = detail?.Document.AuthorId == actorId;
        var isDeptHead = detail?.DeptHeadId == actorId;
        var isResolutionManager = detail?.ResolutionManagerId == actorId;
        var isRecipient = detail?.Recipients.Any(r =>
            r.UserId == actorId || (r.DepartmentId.HasValue && r.DepartmentId == actor.DepartmentId)) == true;
        var isAssignee = detail?.Document.AssigneeId == actorId;
        var isRoutedDeptManager = detail is not null && IsDeptManager(actor) &&
            actor.DepartmentId == detail.Document.DepartmentId;

        return Result<MemoPermissionsDto>.Ok(new MemoPermissionsDto(
            isInitiator,
            isDeptHead,
            isResolutionManager,
            isRecipient,
            isAssignee,
            isRoutedDeptManager,
            CanCreate(actor),
            isInitiator && phase is MemoPhase.Draft or MemoPhase.NeedsRevision,
            isInitiator && phase == MemoPhase.Draft && detail?.RequiresTranslation == true,
            isInitiator && phase == MemoPhase.ReadyForSubmit,
            isInitiator && phase == MemoPhase.SpecialistCoordination,
            isDeptHead && phase == MemoPhase.AwaitingDeptHeadApproval,
            isDeptHead && phase == MemoPhase.AwaitingDeptHeadApproval,
            isInitiator && phase == MemoPhase.Registered,
            isResolutionManager && phase is MemoPhase.AwaitingTopManagement or MemoPhase.RoutedToDepartment,
            isResolutionManager && phase is MemoPhase.AwaitingTopManagement or MemoPhase.RoutedToDepartment,
            isResolutionManager && phase == MemoPhase.AwaitingTopManagement,
            isRoutedDeptManager && phase == MemoPhase.RoutedToDepartment,
            isAssignee && phase == MemoPhase.AwaitingAcceptance,
            isAssignee && phase is MemoPhase.InExecution or MemoPhase.ExecutionNeedsRevision,
            isRoutedDeptManager && phase == MemoPhase.AwaitingReview,
            isRoutedDeptManager && phase == MemoPhase.AwaitingReview,
            isInitiator && phase == MemoPhase.AwaitingArchive,
            CanView(actor, detail)));
    }

    public async Task<Result<IReadOnlyList<MemoUserDto>>> GetTopManagersAsync(
        Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<IReadOnlyList<MemoUserDto>>.Fail("User not found");

        var users = await db.Users.AsNoTracking()
            .Include(u => u.Department)
            .Where(u => u.IsActive && u.Role == UserRole.HOTopManager &&
                        u.Organization.Code == HoMasterData.OrganizationCode)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);

        return Result<IReadOnlyList<MemoUserDto>>.Ok(users.Select(MapUser).ToList());
    }

    public async Task<Result<IReadOnlyList<MemoUserDto>>> GetDeptHeadsAsync(
        Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<IReadOnlyList<MemoUserDto>>.Fail("User not found");

        var users = await db.Users.AsNoTracking()
            .Include(u => u.Department)
            .Where(u => u.IsActive && u.Role == UserRole.HONachalnik &&
                        u.Organization.Code == HoMasterData.OrganizationCode)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);

        return Result<IReadOnlyList<MemoUserDto>>.Ok(users.Select(MapUser).ToList());
    }

    public async Task<Result<IReadOnlyList<MemoUserDto>>> GetCoordinatorsAsync(
        Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<IReadOnlyList<MemoUserDto>>.Fail("User not found");

        var users = await db.Users.AsNoTracking()
            .Include(u => u.Department)
            .Where(u => u.IsActive && u.Organization.Code == HoMasterData.OrganizationCode &&
                        (u.Role == UserRole.HOEngineer || u.Role == UserRole.HONachalnik))
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);

        return Result<IReadOnlyList<MemoUserDto>>.Ok(users.Select(MapUser).ToList());
    }

    public async Task<Result<IReadOnlyList<MemoDepartmentDto>>> GetDepartmentsAsync(
        Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<IReadOnlyList<MemoDepartmentDto>>.Fail("User not found");

        var departments = await db.Departments.AsNoTracking()
            .Include(d => d.Organization)
            .Where(d => d.IsActive && d.Organization.Code == HoMasterData.OrganizationCode)
            .OrderBy(d => d.Code)
            .ToListAsync(ct);

        return Result<IReadOnlyList<MemoDepartmentDto>>.Ok(
            departments.Select(d => new MemoDepartmentDto(d.Id, d.Code, d.Name, d.NameEn)).ToList());
    }

    public async Task<Result<IReadOnlyList<MemoUserDto>>> GetDepartmentWorkersAsync(
        Guid documentId, Guid actorId, CancellationToken ct = default)
    {
        var detail = await LoadDetailAsync(documentId, ct);
        if (detail is null) return Result<IReadOnlyList<MemoUserDto>>.Fail("Memo not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsDeptManager(actor) || actor.DepartmentId != detail.Document.DepartmentId)
            return Result<IReadOnlyList<MemoUserDto>>.Fail("Access denied");

        var users = await db.Users.AsNoTracking()
            .Include(u => u.Department)
            .Where(u => u.IsActive && u.DepartmentId == detail.Document.DepartmentId)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);

        return Result<IReadOnlyList<MemoUserDto>>.Ok(users.Select(MapUser).ToList());
    }

    public async Task<Result<MemoDto>> GetByIdAsync(Guid id, Guid actorId, CancellationToken ct = default)
    {
        var detail = await LoadDetailAsync(id, ct);
        if (detail is null) return Result<MemoDto>.Fail("Memo not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanView(actor, detail))
            return Result<MemoDto>.Fail("Access denied");

        return Result<MemoDto>.Ok(await MapDetailAsync(detail, ct));
    }

    public async Task<Result<MemoDto>> CreateAsync(
        CreateMemoRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<MemoDto>.Fail("User not found");
        if (!CanCreate(actor))
            return Result<MemoDto>.Fail("Access denied");
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result<MemoDto>.Fail("Subject is required");
        if (actor.DepartmentId is null)
            return Result<MemoDto>.Fail("Department is required");

        var number = await GeneratePendingNumberAsync(ct);
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            Number = number,
            Title = request.Title.Trim(),
            TitleRu = request.TitleRu?.Trim(),
            Type = DocumentType.Memo,
            Status = DocumentStatus.Draft,
            AuthorId = actorId,
            OrganizationId = actor.OrganizationId,
            DepartmentId = actor.DepartmentId.Value,
            AttachmentFileName = request.AttachmentFileName?.Trim(),
            ExternalReference = request.AttachmentStorageKey?.Trim(),
        };

        var detail = new MemoDetail
        {
            DocumentId = doc.Id,
            Document = doc,
            Phase = request.RequiresTranslation ? MemoPhase.Draft : MemoPhase.ReadyForSubmit,
            RequiresTranslation = request.RequiresTranslation,
        };

        foreach (var recipient in request.Recipients ?? [])
        {
            if (!recipient.UserId.HasValue && !recipient.DepartmentId.HasValue) continue;
            if (recipient.UserId.HasValue && recipient.DepartmentId.HasValue) continue;
            if (detail.Recipients.Any(r => r.UserId == recipient.UserId && r.DepartmentId == recipient.DepartmentId))
                continue;

            detail.Recipients.Add(new MemoRecipient
            {
                Id = Guid.NewGuid(),
                DocumentId = doc.Id,
                UserId = recipient.UserId,
                DepartmentId = recipient.DepartmentId,
                ForInformation = recipient.ForInformation,
            });
        }

        db.Documents.Add(doc);
        db.Set<MemoDetail>().Add(detail);
        await AddActivityAsync(doc, actorId, "memo_draft_created", null, DocumentStatus.Draft, null, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "MemoCreated", "Document", doc.Id, number, ip, ct);

        return await GetByIdAsync(doc.Id, actorId, ct);
    }

    public async Task<Result<MemoDto>> UpdateDraftAsync(
        Guid id, CreateMemoRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<MemoDto>.Fail("Memo not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || detail.Document.AuthorId != actorId)
            return Result<MemoDto>.Fail("Access denied");
        if (detail.Phase is not (MemoPhase.Draft or MemoPhase.NeedsRevision))
            return Result<MemoDto>.Fail("Memo is not editable");
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result<MemoDto>.Fail("Subject is required");

        detail.Document.Title = request.Title.Trim();
        detail.Document.TitleRu = request.TitleRu?.Trim();
        detail.Document.AttachmentFileName = request.AttachmentFileName?.Trim();
        detail.Document.ExternalReference = request.AttachmentStorageKey?.Trim();
        detail.RequiresTranslation = request.RequiresTranslation;
        detail.RevisionNotes = null;
        detail.Phase = request.RequiresTranslation ? MemoPhase.Draft : MemoPhase.ReadyForSubmit;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        detail.Recipients.Clear();
        foreach (var recipient in request.Recipients ?? [])
        {
            if (!recipient.UserId.HasValue && !recipient.DepartmentId.HasValue) continue;
            if (recipient.UserId.HasValue && recipient.DepartmentId.HasValue) continue;
            if (detail.Recipients.Any(r => r.UserId == recipient.UserId && r.DepartmentId == recipient.DepartmentId))
                continue;

            detail.Recipients.Add(new MemoRecipient
            {
                Id = Guid.NewGuid(),
                DocumentId = detail.DocumentId,
                UserId = recipient.UserId,
                DepartmentId = recipient.DepartmentId,
                ForInformation = recipient.ForInformation,
            });
        }

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "MemoDraftUpdated", "Document", id, detail.Document.Number, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<MemoDto>> SendToTranslationAsync(
        Guid id, SendMemoToTranslationRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<MemoDto>.Fail("Memo not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || detail.Document.AuthorId != actorId)
            return Result<MemoDto>.Fail("Access denied");
        if (detail.Phase != MemoPhase.Draft || !detail.RequiresTranslation)
            return Result<MemoDto>.Fail("Memo is not awaiting translation");

        var languageError = TranslationLanguages.Validate(request.TranslatingLanguages, request.SourceLanguage);
        if (languageError is not null)
            return Result<MemoDto>.Fail(languageError);

        var sourceLanguage = request.SourceLanguage.Trim().ToLowerInvariant();
        var targetLanguages = request.TranslatingLanguages
            .Select(c => c.Trim().ToLowerInvariant())
            .Where(c => c.Length > 0)
            .Distinct()
            .ToList();
        var targetLanguagesJoined = TranslationLanguages.Join(targetLanguages);

        var deptCode = HelpDeskRouting.ResolveDepartmentCode(TicketCategory.Translator, actor.Organization.Code)
            ?? TranslationDept;
        var dept = await db.Departments.FirstOrDefaultAsync(d =>
            d.OrganizationId == actor.OrganizationId && d.Code == deptCode && d.IsActive, ct)
            ?? await db.Departments.FirstOrDefaultAsync(d => d.Code == deptCode && d.IsActive, ct);
        if (dept is null)
            return Result<MemoDto>.Fail($"Department {deptCode} not found");

        var ticketNumber = await GenerateHelpDeskNumberAsync(ct);
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Number = ticketNumber,
            Title = $"Memo translation - {detail.Document.Number}",
            Description = BuildTranslationDescription(detail, sourceLanguage, targetLanguages),
            Category = TicketCategory.Translator,
            Priority = TicketPriority.Medium,
            Status = TicketStatus.Open,
            RequesterId = actorId,
            OrganizationId = actor.OrganizationId,
            TargetDepartmentId = dept.Id,
            SourceLanguage = sourceLanguage,
            TranslatingLanguage = targetLanguagesJoined,
            LinkedDocumentId = detail.DocumentId,
        };
        db.Tickets.Add(ticket);

        detail.HelpDeskTicketId = ticket.Id;
        detail.SourceLanguage = sourceLanguage;
        detail.TranslatingLanguage = targetLanguagesJoined;
        detail.Phase = MemoPhase.TranslationPending;
        detail.SentToTranslationAt = DateTime.UtcNow;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "sent_to_translation", null, detail.Document.Status, ticket.Number, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "MemoSentToTranslation", "Document", id, ticket.Number, ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task NotifyHelpDeskTranslationCompletedAsync(Guid ticketId, CancellationToken ct = default)
    {
        var detail = await db.Set<MemoDetail>()
            .Include(d => d.Document)
            .FirstOrDefaultAsync(d =>
                d.HelpDeskTicketId == ticketId && d.Phase == MemoPhase.TranslationPending, ct);
        if (detail is null) return;

        detail.Phase = MemoPhase.ReadyForSubmit;
        detail.TranslationReturnedAt = DateTime.UtcNow;
        detail.Document.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<Result<MemoDto>> SubmitForApprovalAsync(
        Guid id, SubmitMemoForApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<MemoDto>.Fail("Memo not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || detail.Document.AuthorId != actorId)
            return Result<MemoDto>.Fail("Access denied");
        if (detail.Phase != MemoPhase.ReadyForSubmit)
            return Result<MemoDto>.Fail("Memo is not ready for submit");

        var deptHead = await db.Users.FirstOrDefaultAsync(u =>
            u.Id == request.DeptHeadId && u.IsActive && u.Role == UserRole.HONachalnik, ct);
        if (deptHead is null) return Result<MemoDto>.Fail("Invalid department head");

        detail.DeptHeadId = deptHead.Id;
        detail.SubmittedAt = DateTime.UtcNow;
        detail.Document.Status = DocumentStatus.InReview;
        detail.Phase = request.RequiresSpecialistCoordination
            ? MemoPhase.SpecialistCoordination
            : MemoPhase.AwaitingDeptHeadApproval;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        if (!request.RequiresSpecialistCoordination)
        {
            await CreateLinkedTaskAsync(
                deptHead.Id, actorId, deptHead.DepartmentId ?? detail.Document.DepartmentId, deptHead.OrganizationId,
                $"Memo {detail.Document.Number} - dept head approval",
                detail.Document.Title,
                detail.DocumentId, ct);
        }

        await AddActivityAsync(detail.Document, actorId, "submitted_for_approval", DocumentStatus.Draft,
            DocumentStatus.InReview, deptHead.FullName, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "MemoSubmittedForApproval", "Document", id, deptHead.FullName, ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<MemoDto>> AddCoordinatorsAsync(
        Guid id, MemoCoordinatorRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<MemoDto>.Fail("Memo not found");
        if (detail.Document.AuthorId != actorId) return Result<MemoDto>.Fail("Access denied");
        if (detail.Phase != MemoPhase.SpecialistCoordination)
            return Result<MemoDto>.Fail("Memo is not in specialist coordination");

        foreach (var userId in request.UserIds.Distinct())
        {
            if (detail.Coordinators.Any(c => c.UserId == userId)) continue;

            var user = await db.Users.FirstOrDefaultAsync(u =>
                u.Id == userId && u.IsActive &&
                (u.Role == UserRole.HOEngineer || u.Role == UserRole.HONachalnik), ct);
            if (user is null) return Result<MemoDto>.Fail("Invalid coordinator selected");

            detail.Coordinators.Add(new MemoCoordinator
            {
                Id = Guid.NewGuid(),
                DocumentId = detail.DocumentId,
                UserId = userId,
            });

            await CreateLinkedTaskAsync(
                user.Id, actorId, user.DepartmentId ?? detail.Document.DepartmentId, user.OrganizationId,
                $"Memo {detail.Document.Number} - specialist coordination",
                detail.Document.Title,
                detail.DocumentId, ct);
        }

        detail.Document.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "MemoCoordinatorsAdded", "Document", id, null, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<MemoDto>> CompleteSpecialistCoordinationAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<MemoDto>.Fail("Memo not found");
        if (detail.Document.AuthorId != actorId) return Result<MemoDto>.Fail("Access denied");
        if (detail.Phase != MemoPhase.SpecialistCoordination)
            return Result<MemoDto>.Fail("Memo is not in specialist coordination");
        if (!detail.DeptHeadId.HasValue)
            return Result<MemoDto>.Fail("Department head is not selected");

        var deptHead = await db.Users.FirstOrDefaultAsync(u => u.Id == detail.DeptHeadId.Value && u.IsActive, ct);
        if (deptHead is null) return Result<MemoDto>.Fail("Department head not found");

        detail.CoordinationCompletedAt = DateTime.UtcNow;
        detail.Phase = MemoPhase.AwaitingDeptHeadApproval;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await CreateLinkedTaskAsync(
            deptHead.Id, actorId, deptHead.DepartmentId ?? detail.Document.DepartmentId, deptHead.OrganizationId,
            $"Memo {detail.Document.Number} - dept head approval",
            detail.Document.Title,
            detail.DocumentId, ct);

        await AddActivityAsync(detail.Document, actorId, "specialist_coordination_completed", null, detail.Document.Status, null, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "MemoSpecialistCoordinationCompleted", "Document", id, null, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public Task<Result<MemoDto>> ApproveDeptHeadAsync(
        Guid id, MemoApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default)
        => ApproveDeptHeadCoreAsync(id, request, actorId, ip, ct);

    private async Task<Result<MemoDto>> ApproveDeptHeadCoreAsync(
        Guid id, MemoApprovalRequest request, Guid actorId, string? ip, CancellationToken ct)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<MemoDto>.Fail("Memo not found");
        if (detail.DeptHeadId != actorId) return Result<MemoDto>.Fail("Access denied");
        if (detail.Phase != MemoPhase.AwaitingDeptHeadApproval)
            return Result<MemoDto>.Fail("Memo is not awaiting department head approval");

        if (!string.IsNullOrWhiteSpace(request.Comment))
        {
            detail.Comments.Add(new MemoComment
            {
                Id = Guid.NewGuid(),
                DocumentId = detail.DocumentId,
                AuthorId = actorId,
                Body = request.Comment.Trim(),
            });
        }

        detail.DeptHeadApprovedAt = DateTime.UtcNow;
        detail.Phase = MemoPhase.Registered;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "dept_head_approved", null, detail.Document.Status, null, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "MemoDeptHeadApproved", "Document", id, null, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public Task<Result<MemoDto>> RejectDeptHeadAsync(
        Guid id, MemoRevisionRequest request, Guid actorId, string? ip, CancellationToken ct = default)
        => RejectDeptHeadCoreAsync(id, request, actorId, ip, ct);

    private async Task<Result<MemoDto>> RejectDeptHeadCoreAsync(
        Guid id, MemoRevisionRequest request, Guid actorId, string? ip, CancellationToken ct)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<MemoDto>.Fail("Memo not found");
        if (detail.DeptHeadId != actorId) return Result<MemoDto>.Fail("Access denied");
        if (detail.Phase != MemoPhase.AwaitingDeptHeadApproval)
            return Result<MemoDto>.Fail("Memo is not awaiting department head approval");
        if (string.IsNullOrWhiteSpace(request.Comment))
            return Result<MemoDto>.Fail("Revision comment is required");

        detail.RevisionNotes = request.Comment.Trim();
        detail.Phase = MemoPhase.NeedsRevision;
        detail.Document.Status = DocumentStatus.Draft;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        detail.Comments.Add(new MemoComment
        {
            Id = Guid.NewGuid(),
            DocumentId = detail.DocumentId,
            AuthorId = actorId,
            Body = request.Comment.Trim(),
        });

        await AddActivityAsync(detail.Document, actorId, "dept_head_rejected", null, DocumentStatus.Draft, request.Comment.Trim(), ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "MemoDeptHeadRejected", "Document", id, null, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<MemoDto>> RegisterAndDistributeAsync(
        Guid id, RegisterMemoRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<MemoDto>.Fail("Memo not found");
        if (detail.Document.AuthorId != actorId) return Result<MemoDto>.Fail("Access denied");
        if (detail.Phase != MemoPhase.Registered)
            return Result<MemoDto>.Fail("Memo is not ready for registration");

        User? resolutionManager = null;
        if (request.RequiresTopManagementResolution)
        {
            if (!request.ResolutionManagerId.HasValue)
                return Result<MemoDto>.Fail("Resolution manager is required");

            resolutionManager = await db.Users.FirstOrDefaultAsync(u =>
                u.Id == request.ResolutionManagerId.Value &&
                u.IsActive &&
                u.Role == UserRole.HOTopManager, ct);
            if (resolutionManager is null)
                return Result<MemoDto>.Fail("Invalid resolution manager");
        }

        detail.Document.Number = await GenerateNumberAsync(ct);
        detail.Document.Status = DocumentStatus.Registered;
        detail.Document.RegisteredAt = DateTime.UtcNow;
        detail.RegisteredAt = DateTime.UtcNow;
        detail.RequiresTopManagementResolution = request.RequiresTopManagementResolution;
        detail.ResolutionManagerId = request.RequiresTopManagementResolution ? request.ResolutionManagerId : null;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        foreach (var recipient in detail.Recipients)
        {
            if (recipient.NotifiedAt.HasValue) continue;

            if (recipient.UserId.HasValue)
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == recipient.UserId.Value && u.IsActive, ct);
                if (user is null) continue;

                var task = await CreateLinkedTaskAsync(
                    user.Id, actorId, user.DepartmentId ?? detail.Document.DepartmentId, user.OrganizationId,
                    $"Memo {detail.Document.Number} - recipient notification",
                    detail.Document.Title,
                    detail.DocumentId, ct);

                recipient.TaskId = task.Id;
                recipient.NotifiedAt = DateTime.UtcNow;
                continue;
            }

            if (recipient.DepartmentId.HasValue)
            {
                var deptUser = await db.Users
                    .Where(u => u.IsActive && u.DepartmentId == recipient.DepartmentId.Value && u.Role == UserRole.HONachalnik)
                    .OrderBy(u => u.LastName)
                    .FirstOrDefaultAsync(ct)
                    ?? await db.Users
                        .Where(u => u.IsActive && u.DepartmentId == recipient.DepartmentId.Value)
                        .OrderBy(u => u.LastName)
                        .FirstOrDefaultAsync(ct);

                if (deptUser is null) continue;

                var task = await CreateLinkedTaskAsync(
                    deptUser.Id, actorId, deptUser.DepartmentId ?? detail.Document.DepartmentId, deptUser.OrganizationId,
                    $"Memo {detail.Document.Number} - department notification",
                    detail.Document.Title,
                    detail.DocumentId, ct);

                recipient.UserId = deptUser.Id;
                recipient.TaskId = task.Id;
                recipient.NotifiedAt = DateTime.UtcNow;
            }
        }

        if (resolutionManager is not null)
        {
            await CreateLinkedTaskAsync(
                resolutionManager.Id, actorId,
                resolutionManager.DepartmentId ?? detail.Document.DepartmentId, resolutionManager.OrganizationId,
                $"Memo {detail.Document.Number} - top management resolution",
                detail.Document.Title,
                detail.DocumentId, ct);

            detail.Phase = MemoPhase.AwaitingTopManagement;
            detail.Document.Status = DocumentStatus.InReview;
        }
        else
        {
            detail.Phase = MemoPhase.AwaitingArchive;
            detail.Document.Status = DocumentStatus.Approved;
        }

        await AddActivityAsync(detail.Document, actorId, "registered_and_distributed", DocumentStatus.InReview,
            detail.Document.Status, detail.Document.Number, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "MemoRegisteredAndDistributed", "Document", id, detail.Document.Number, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<MemoDto>> InformRecipientsAsync(
        Guid id, InformMemoRecipientsRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<MemoDto>.Fail("Memo not found");
        if (detail.ResolutionManagerId != actorId) return Result<MemoDto>.Fail("Access denied");
        if (detail.Phase is not (MemoPhase.AwaitingTopManagement or MemoPhase.RoutedToDepartment))
            return Result<MemoDto>.Fail("Memo is not available for recipient informing");

        foreach (var userId in request.UserIds.Distinct())
        {
            if (detail.Recipients.Any(r => r.UserId == userId)) continue;

            var user = await db.Users.FirstOrDefaultAsync(u =>
                u.Id == userId && u.IsActive && u.Role == UserRole.HOTopManager, ct);
            if (user is null) return Result<MemoDto>.Fail("Invalid top manager selected");

            var task = await CreateLinkedTaskAsync(
                user.Id, actorId, user.DepartmentId ?? detail.Document.DepartmentId, user.OrganizationId,
                $"Memo {detail.Document.Number} - for information",
                detail.Document.Title,
                detail.DocumentId, ct);

            detail.Recipients.Add(new MemoRecipient
            {
                Id = Guid.NewGuid(),
                DocumentId = detail.DocumentId,
                UserId = user.Id,
                ForInformation = true,
                NotifiedAt = DateTime.UtcNow,
                TaskId = task.Id,
            });
        }

        detail.Document.UpdatedAt = DateTime.UtcNow;
        await AddActivityAsync(detail.Document, actorId, "informed_recipients", null, detail.Document.Status,
            $"{request.UserIds.Count} user(s)", ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "MemoRecipientsInformed", "Document", id, null, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<MemoDto>> RouteToDepartmentAsync(
        Guid id, RouteMemoRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<MemoDto>.Fail("Memo not found");
        if (detail.ResolutionManagerId != actorId) return Result<MemoDto>.Fail("Access denied");
        if (detail.Phase != MemoPhase.AwaitingTopManagement)
            return Result<MemoDto>.Fail("Memo is not awaiting top management routing");

        var dept = await db.Departments.Include(d => d.Organization)
            .FirstOrDefaultAsync(d => d.Id == request.DepartmentId && d.IsActive, ct);
        if (dept is null) return Result<MemoDto>.Fail("Department not found");

        var deptHead = await db.Users
            .Where(u => u.IsActive && u.DepartmentId == dept.Id && u.Role == UserRole.HONachalnik)
            .OrderBy(u => u.LastName)
            .FirstOrDefaultAsync(ct)
            ?? await db.Users
                .Where(u => u.IsActive && u.DepartmentId == dept.Id)
                .OrderBy(u => u.LastName)
                .FirstOrDefaultAsync(ct);
        if (deptHead is null) return Result<MemoDto>.Fail("No staff in target department");

        await CreateLinkedTaskAsync(
            deptHead.Id, actorId, dept.Id, dept.OrganizationId,
            $"Memo {detail.Document.Number} - routed to {dept.Code}",
            detail.Document.Title,
            detail.DocumentId, ct);

        if (!string.IsNullOrWhiteSpace(request.Comment))
        {
            detail.Comments.Add(new MemoComment
            {
                Id = Guid.NewGuid(),
                DocumentId = detail.DocumentId,
                AuthorId = actorId,
                Body = request.Comment.Trim(),
            });
        }

        if (!detail.Recipients.Any(r => r.DepartmentId == dept.Id))
        {
            detail.Recipients.Add(new MemoRecipient
            {
                Id = Guid.NewGuid(),
                DocumentId = detail.DocumentId,
                DepartmentId = dept.Id,
                UserId = deptHead.Id,
                ForInformation = false,
                NotifiedAt = DateTime.UtcNow,
            });
        }

        detail.RoutedById = actorId;
        detail.RoutedToDepartmentId = dept.Id;
        detail.RoutedAt = DateTime.UtcNow;
        detail.Phase = MemoPhase.RoutedToDepartment;
        detail.Document.DepartmentId = dept.Id;
        detail.Document.Status = DocumentStatus.InReview;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "routed_to_department", null, detail.Document.Status, dept.Code, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "MemoRoutedToDepartment", "Document", id, dept.Code, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<MemoDto>> AssignWorkerAsync(
        Guid id, AssignMemoRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<MemoDto>.Fail("Memo not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsDeptManager(actor) || actor.DepartmentId != detail.Document.DepartmentId)
            return Result<MemoDto>.Fail("Access denied");
        if (detail.Phase != MemoPhase.RoutedToDepartment)
            return Result<MemoDto>.Fail("Memo is not ready for assignment");
        if (string.IsNullOrWhiteSpace(request.AssignmentTask))
            return Result<MemoDto>.Fail("Assignment task is required");

        var worker = await db.Users.FirstOrDefaultAsync(u =>
            u.Id == request.AssigneeId && u.IsActive && u.DepartmentId == detail.Document.DepartmentId, ct);
        if (worker is null) return Result<MemoDto>.Fail("Assignee must be in your department");

        var dueDate = request.DueDate?.ToUniversalTime();
        await CreateLinkedTaskAsync(
            worker.Id, actorId, detail.Document.DepartmentId, detail.Document.OrganizationId,
            $"Execute memo {detail.Document.Number}",
            request.AssignmentTask.Trim(),
            detail.DocumentId, ct, dueDate);

        detail.Document.AssigneeId = worker.Id;
        detail.AssignmentTask = request.AssignmentTask.Trim();
        detail.DueDate = dueDate;
        detail.RequiresResponse = request.RequiresResponse;
        detail.Phase = MemoPhase.AwaitingAcceptance;
        detail.Document.Status = DocumentStatus.InReview;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "assigned_worker", null, detail.Document.Status, worker.FullName, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "MemoWorkerAssigned", "Document", id, worker.FullName, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<MemoDto>> AcceptExecutionAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<MemoDto>.Fail("Memo not found");
        if (detail.Document.AssigneeId != actorId) return Result<MemoDto>.Fail("Access denied");
        if (detail.Phase != MemoPhase.AwaitingAcceptance)
            return Result<MemoDto>.Fail("Memo is not awaiting acceptance");

        detail.ExecutorAcceptedAt = DateTime.UtcNow;
        detail.Phase = MemoPhase.InExecution;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "execution_accepted", null, detail.Document.Status, null, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "MemoExecutionAccepted", "Document", id, null, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<MemoDto>> ReportCompletionAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<MemoDto>.Fail("Memo not found");
        if (detail.Document.AssigneeId != actorId) return Result<MemoDto>.Fail("Access denied");
        if (detail.Phase is not (MemoPhase.InExecution or MemoPhase.ExecutionNeedsRevision))
            return Result<MemoDto>.Fail("Memo is not in execution");

        detail.ReportedAt = DateTime.UtcNow;
        detail.Phase = MemoPhase.AwaitingReview;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "reported_completion", null, detail.Document.Status, null, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "MemoCompletionReported", "Document", id, null, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<MemoDto>> RequestExecutionRevisionAsync(
        Guid id, MemoCommentRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<MemoDto>.Fail("Memo not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsDeptManager(actor) || actor.DepartmentId != detail.Document.DepartmentId)
            return Result<MemoDto>.Fail("Access denied");
        if (detail.Phase != MemoPhase.AwaitingReview)
            return Result<MemoDto>.Fail("Memo is not awaiting review");
        if (string.IsNullOrWhiteSpace(request.Body))
            return Result<MemoDto>.Fail("Revision comment is required");

        detail.Comments.Add(new MemoComment
        {
            Id = Guid.NewGuid(),
            DocumentId = detail.DocumentId,
            AuthorId = actorId,
            Body = request.Body.Trim(),
        });

        detail.ReviewedAt = DateTime.UtcNow;
        detail.Phase = MemoPhase.ExecutionNeedsRevision;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "execution_revision_requested", null, detail.Document.Status, null, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "MemoExecutionRevisionRequested", "Document", id, null, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<MemoDto>> AcceptCompletionAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<MemoDto>.Fail("Memo not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsDeptManager(actor) || actor.DepartmentId != detail.Document.DepartmentId)
            return Result<MemoDto>.Fail("Access denied");
        if (detail.Phase != MemoPhase.AwaitingReview)
            return Result<MemoDto>.Fail("Memo is not awaiting review");

        detail.ReviewedAt = DateTime.UtcNow;
        detail.Phase = MemoPhase.AwaitingArchive;
        detail.Document.Status = DocumentStatus.Approved;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "completion_accepted", DocumentStatus.InReview, DocumentStatus.Approved, null, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "MemoCompletionAccepted", "Document", id, null, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<MemoDto>> ArchiveAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<MemoDto>.Fail("Memo not found");
        if (detail.Document.AuthorId != actorId) return Result<MemoDto>.Fail("Access denied");
        if (detail.Phase != MemoPhase.AwaitingArchive)
            return Result<MemoDto>.Fail("Memo is not ready for archive");

        detail.ArchivedAt = DateTime.UtcNow;
        detail.CompletedAt = DateTime.UtcNow;
        detail.Phase = MemoPhase.Completed;
        detail.Document.Status = DocumentStatus.Archived;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "archived", DocumentStatus.Approved, DocumentStatus.Archived, null, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "MemoArchived", "Document", id, null, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<MemoCommentDto>> AddCommentAsync(
        Guid id, MemoCommentRequest request, Guid actorId, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<MemoCommentDto>.Fail("Memo not found");
        if (string.IsNullOrWhiteSpace(request.Body))
            return Result<MemoCommentDto>.Fail("Comment is required");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanView(actor, detail))
            return Result<MemoCommentDto>.Fail("Access denied");

        var comment = new MemoComment
        {
            Id = Guid.NewGuid(),
            DocumentId = detail.DocumentId,
            AuthorId = actorId,
            Body = request.Body.Trim(),
        };
        detail.Comments.Add(comment);
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "MemoCommentAdded", "Document", id, null, null, ct);

        return Result<MemoCommentDto>.Ok(new MemoCommentDto(
            comment.Id,
            comment.AuthorId,
            actor.FullName,
            comment.Body,
            comment.CreatedAt));
    }

    private async Task<MemoDetail?> LoadDetailAsync(Guid id, CancellationToken ct) =>
        await db.Set<MemoDetail>().AsNoTracking()
            .Include(d => d.Document).ThenInclude(doc => doc.Author)
            .Include(d => d.Document).ThenInclude(doc => doc.Assignee)
            .Include(d => d.Document).ThenInclude(doc => doc.Department)
            .Include(d => d.DeptHead)
            .Include(d => d.ResolutionManager)
            .Include(d => d.RoutedBy)
            .Include(d => d.RoutedToDepartment)
            .Include(d => d.Recipients).ThenInclude(r => r.User)
            .Include(d => d.Recipients).ThenInclude(r => r.Department)
            .Include(d => d.Coordinators).ThenInclude(c => c.User)
            .Include(d => d.Comments).ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(d => d.DocumentId == id, ct);

    private async Task<MemoDetail?> LoadDetailTrackedAsync(Guid id, CancellationToken ct) =>
        await db.Set<MemoDetail>()
            .Include(d => d.Document)
            .Include(d => d.Recipients)
            .Include(d => d.Coordinators)
            .Include(d => d.Comments)
            .FirstOrDefaultAsync(d => d.DocumentId == id, ct);

    private static MemoDto MapDetail(MemoDetail d, string? helpDeskTicketNumber = null) => new(
        d.DocumentId,
        d.Document.Number,
        d.Document.Title,
        d.Document.TitleRu,
        d.Document.Status,
        d.Phase,
        d.Document.AuthorId,
        d.Document.Author.FullName,
        d.Document.AttachmentFileName,
        d.Document.ExternalReference,
        d.TranslatedAttachmentFileName,
        d.TranslatedAttachmentStorageKey,
        d.Document.OrganizationId,
        d.Document.DepartmentId,
        d.Document.Department.Name,
        d.Document.Department.NameEn,
        d.Document.AssigneeId,
        d.Document.Assignee?.FullName,
        d.RequiresTranslation,
        d.SourceLanguage,
        TranslationLanguages.Parse(d.TranslatingLanguage),
        d.HelpDeskTicketId,
        helpDeskTicketNumber,
        d.DeptHeadId,
        d.DeptHead?.FullName,
        d.RequiresTopManagementResolution,
        d.ResolutionManagerId,
        d.ResolutionManager?.FullName,
        d.RoutedToDepartmentId,
        d.RoutedToDepartment?.NameEn ?? d.RoutedToDepartment?.Name,
        d.AssignmentTask,
        d.DueDate,
        d.RequiresResponse,
        d.RevisionNotes,
        d.Recipients.OrderBy(r => r.NotifiedAt).Select(r => new MemoRecipientDto(
            r.Id,
            r.UserId,
            r.User?.FullName,
            r.DepartmentId,
            r.Department?.Name,
            r.Department?.NameEn,
            r.ForInformation,
            r.NotifiedAt)).ToList(),
        d.Coordinators.OrderBy(c => c.CoordinatedAt).Select(c => new MemoCoordinatorDto(
            c.Id,
            c.UserId,
            c.User.FullName,
            c.CoordinatedAt)).ToList(),
        d.Comments.OrderBy(c => c.CreatedAt).Select(c => new MemoCommentDto(
            c.Id,
            c.AuthorId,
            c.Author.FullName,
            c.Body,
            c.CreatedAt)).ToList(),
        d.Document.CreatedAt,
        d.Document.UpdatedAt);

    private async Task<MemoDto> MapDetailAsync(MemoDetail d, CancellationToken ct)
    {
        string? ticketNumber = null;
        if (d.HelpDeskTicketId.HasValue)
        {
            ticketNumber = await db.Tickets.AsNoTracking()
                .Where(t => t.Id == d.HelpDeskTicketId.Value)
                .Select(t => t.Number)
                .FirstOrDefaultAsync(ct);
        }

        return MapDetail(d, ticketNumber);
    }

    private static string BuildTranslationDescription(
        MemoDetail detail, string sourceLanguage, IReadOnlyList<string> targetLanguages)
    {
        var lines = new List<string>
        {
            "Memo translation request (DCS)",
            $"Document: {detail.Document.Number}",
            $"Subject: {detail.Document.Title}",
            $"Source language: {sourceLanguage}",
            $"Target language(s): {string.Join(", ", targetLanguages)}",
        };
        if (!string.IsNullOrWhiteSpace(detail.Document.AttachmentFileName))
            lines.Add($"Attachment: {detail.Document.AttachmentFileName}");
        return string.Join(Environment.NewLine, lines);
    }

    private static MemoUserDto MapUser(User u) => new(
        u.Id,
        u.FullName,
        u.Email,
        u.EmployeeId,
        u.Department?.Name ?? "",
        u.Department?.NameEn ?? "");

    private async Task<WorkTask> CreateLinkedTaskAsync(
        Guid assigneeId, Guid createdById, Guid deptId, Guid orgId,
        string title, string description, Guid documentId, CancellationToken ct,
        DateTime? dueDate = null)
    {
        var task = new WorkTask
        {
            Id = Guid.NewGuid(),
            Number = await GenerateTaskNumberAsync(ct),
            Title = title,
            Description = description,
            Status = WorkTaskStatus.New,
            Priority = TaskPriority.Medium,
            Source = TaskSource.DCS,
            ExternalId = documentId,
            AssigneeId = assigneeId,
            CreatedById = createdById,
            OrganizationId = orgId,
            DepartmentId = deptId,
            DueDate = dueDate,
        };
        db.WorkTasks.Add(task);
        return task;
    }

    private async Task<string> GeneratePendingNumberAsync(CancellationToken ct)
    {
        var prefix = $"MEMO-PEND-{DateTime.UtcNow.Year}-";
        var last = await db.Documents.Where(d => d.Number.StartsWith(prefix))
            .OrderByDescending(d => d.Number)
            .Select(d => d.Number)
            .FirstOrDefaultAsync(ct);

        var seq = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var n)) seq = n + 1;
        return $"{prefix}{seq:D4}";
    }

    private async Task<string> GenerateNumberAsync(CancellationToken ct)
    {
        var prefix = $"MEMO-{DateTime.UtcNow.Year}-";
        var last = await db.Documents.Where(d => d.Number.StartsWith(prefix) && !d.Number.Contains("PEND"))
            .OrderByDescending(d => d.Number)
            .Select(d => d.Number)
            .FirstOrDefaultAsync(ct);

        var seq = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var n)) seq = n + 1;
        return $"{prefix}{seq:D4}";
    }

    private async Task<string> GenerateHelpDeskNumberAsync(CancellationToken ct)
    {
        var prefix = $"HD-{DateTime.UtcNow.Year}-";
        var last = await db.Tickets.Where(t => t.Number.StartsWith(prefix))
            .OrderByDescending(t => t.Number)
            .Select(t => t.Number)
            .FirstOrDefaultAsync(ct);

        var seq = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var n)) seq = n + 1;
        return $"{prefix}{seq:D5}";
    }

    private async Task<string> GenerateTaskNumberAsync(CancellationToken ct)
    {
        var prefix = $"TSK-{DateTime.UtcNow.Year}-";
        var last = await db.WorkTasks.Where(t => t.Number.StartsWith(prefix))
            .OrderByDescending(t => t.Number)
            .Select(t => t.Number)
            .FirstOrDefaultAsync(ct);

        var seq = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var n)) seq = n + 1;
        return $"{prefix}{seq:D4}";
    }

    private async Task AddActivityAsync(
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
        });

        await Task.CompletedTask;
    }

    private async Task<User?> GetActorAsync(Guid id, CancellationToken ct) =>
        await db.Users
            .Include(u => u.Organization)
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive, ct);

    private static bool CanCreate(User u) =>
        u.Organization.Code == HoMasterData.OrganizationCode || u.Role == UserRole.SuperAdmin;

    private static bool IsDeptManager(User u) =>
        u.Role == UserRole.HONachalnik || u.Role == UserRole.SuperAdmin;

    private static bool IsTranslationDept(User u) =>
        u.Department?.Code == TranslationDept || u.Role == UserRole.SuperAdmin;

    private static bool CanView(User actor, MemoDetail? detail)
    {
        if (detail is null) return CanCreate(actor);
        if (CanCreate(actor) && detail.Document.AuthorId == actor.Id) return true;
        if (actor.Role == UserRole.SuperAdmin) return true;
        if (IsTranslationDept(actor)) return true;
        if (detail.Document.AuthorId == actor.Id) return true;
        if (detail.DeptHeadId == actor.Id) return true;
        if (detail.ResolutionManagerId == actor.Id) return true;
        if (detail.Document.AssigneeId == actor.Id) return true;
        if (detail.Recipients.Any(r => r.UserId == actor.Id)) return true;
        if (detail.Recipients.Any(r => r.DepartmentId.HasValue && r.DepartmentId == actor.DepartmentId)) return true;
        if (detail.Coordinators.Any(c => c.UserId == actor.Id)) return true;
        if (IsDeptManager(actor) && actor.DepartmentId == detail.Document.DepartmentId) return true;
        if (actor.Role == UserRole.HOTopManager) return true;
        return false;
    }
}
