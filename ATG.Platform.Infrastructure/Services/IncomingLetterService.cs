using ATG.Platform.Application.Common;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Data;
using ATG.Platform.Infrastructure.Dcs;
using ATG.Platform.Infrastructure.HelpDesk;
using ATG.Platform.Infrastructure.Seeds;
using Microsoft.EntityFrameworkCore;

namespace ATG.Platform.Infrastructure.Services;

public class IncomingLetterService(AppDbContext db, IAuditService audit) : IIncomingLetterService
{
    private const string ClericalDept = "HO-DCPR-CLER";
    private const string TranslationDept = "HO-DCPR-TRNS";

    public async Task<Result<IncomingLetterPermissionsDto>> GetPermissionsAsync(
        Guid actorId, Guid? documentId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<IncomingLetterPermissionsDto>.Fail("User not found");

        IncomingLetterDetail? detail = null;
        if (documentId.HasValue)
            detail = await LoadDetailAsync(documentId.Value, ct);

        var isRegistrar = IsRegistrar(actor);
        var isTranslationDept = IsTranslationDept(actor);
        var phase = detail?.Phase ?? IncomingLetterPhase.Received;
        var isResolutionManager = detail?.ResolutionManagerId == actorId;
        var isRecipient = detail?.Recipients.Any(r => r.UserId == actorId) == true;
        var isAssignee = detail?.Document.AssigneeId == actorId;
        var isRoutedDeptManager = detail is not null && IsDeptManager(actor) &&
            actor.DepartmentId == detail.Document.DepartmentId;

        return Result<IncomingLetterPermissionsDto>.Ok(new IncomingLetterPermissionsDto(
            isRegistrar,
            isTranslationDept,
            isResolutionManager,
            isRegistrar && phase == IncomingLetterPhase.Received && detail?.RequiresTranslation == true,
            isTranslationDept && phase == IncomingLetterPhase.TranslationPending,
            isRegistrar && phase == IncomingLetterPhase.ReadyForRegistration,
            isRegistrar && phase == IncomingLetterPhase.Registered,
            isResolutionManager && (phase == IncomingLetterPhase.AwaitingResolution || phase == IncomingLetterPhase.RoutedToDepartment),
            isResolutionManager && phase == IncomingLetterPhase.AwaitingResolution,
            isRoutedDeptManager && phase == IncomingLetterPhase.RoutedToDepartment,
            isAssignee && phase == IncomingLetterPhase.AwaitingAcceptance,
            isAssignee && (phase == IncomingLetterPhase.InExecution || phase == IncomingLetterPhase.NeedsRevision),
            isRoutedDeptManager && phase == IncomingLetterPhase.AwaitingReview,
            isRoutedDeptManager && phase == IncomingLetterPhase.AwaitingReview,
            isRegistrar && phase == IncomingLetterPhase.AwaitingArchive,
            CanView(actor, detail)));
    }

    public async Task<Result<IReadOnlyList<IncomingLetterUserDto>>> GetTopManagersAsync(
        Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || (!IsRegistrar(actor) && actor.Role != UserRole.HOTopManager))
            return Result<IReadOnlyList<IncomingLetterUserDto>>.Fail("Access denied");

        var users = await db.Users.AsNoTracking()
            .Include(u => u.Department)
            .Where(u => u.IsActive && u.Role == UserRole.HOTopManager &&
                        u.Organization.Code == HoMasterData.OrganizationCode)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);

        return Result<IReadOnlyList<IncomingLetterUserDto>>.Ok(users.Select(MapUser).ToList());
    }

    public async Task<Result<IReadOnlyList<IncomingLetterDepartmentDto>>> GetDepartmentsAsync(
        Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || actor.Role != UserRole.HOTopManager)
            return Result<IReadOnlyList<IncomingLetterDepartmentDto>>.Fail("Access denied");

        var depts = await db.Departments.AsNoTracking()
            .Include(d => d.Organization)
            .Where(d => d.IsActive && d.Organization.Code == HoMasterData.OrganizationCode)
            .OrderBy(d => d.Code)
            .ToListAsync(ct);

        return Result<IReadOnlyList<IncomingLetterDepartmentDto>>.Ok(
            depts.Select(d => new IncomingLetterDepartmentDto(d.Id, d.Code, d.Name, d.NameEn)).ToList());
    }

    public async Task<Result<IReadOnlyList<IncomingLetterUserDto>>> GetDepartmentWorkersAsync(
        Guid documentId, Guid actorId, CancellationToken ct = default)
    {
        var detail = await LoadDetailAsync(documentId, ct);
        if (detail is null) return Result<IReadOnlyList<IncomingLetterUserDto>>.Fail("Letter not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsDeptManager(actor) || actor.DepartmentId != detail.Document.DepartmentId)
            return Result<IReadOnlyList<IncomingLetterUserDto>>.Fail("Access denied");

        var users = await db.Users.AsNoTracking()
            .Include(u => u.Department)
            .Where(u => u.IsActive && u.DepartmentId == detail.Document.DepartmentId)
            .OrderBy(u => u.LastName)
            .ToListAsync(ct);

        return Result<IReadOnlyList<IncomingLetterUserDto>>.Ok(users.Select(MapUser).ToList());
    }

    public async Task<Result<IncomingLetterDto>> GetByIdAsync(Guid id, Guid actorId, CancellationToken ct = default)
    {
        var detail = await LoadDetailAsync(id, ct);
        if (detail is null) return Result<IncomingLetterDto>.Fail("Letter not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanView(actor, detail))
            return Result<IncomingLetterDto>.Fail("Access denied");

        return Result<IncomingLetterDto>.Ok(await MapDetailAsync(detail, ct));
    }

    public async Task<Result<IncomingLetterDto>> CreateAsync(
        CreateIncomingLetterRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<IncomingLetterDto>.Fail("User not found");
        if (!IsRegistrar(actor))
            return Result<IncomingLetterDto>.Fail("Only the designated registrar can register incoming letters");

        if (string.IsNullOrWhiteSpace(request.Title))
            return Result<IncomingLetterDto>.Fail("Subject is required");

        var dept = await db.Departments.FirstOrDefaultAsync(d =>
            d.Code == ClericalDept && d.Organization.Code == HoMasterData.OrganizationCode, ct);
        if (dept is null) return Result<IncomingLetterDto>.Fail("Clerical department not found");

        var number = await GeneratePendingNumberAsync(ct);
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            Number = number,
            Title = request.Title.Trim(),
            TitleRu = request.TitleRu?.Trim(),
            Type = DocumentType.Incoming,
            Status = DocumentStatus.Draft,
            AuthorId = actorId,
            OrganizationId = actor.OrganizationId,
            DepartmentId = dept.Id,
            IncomingNumber = request.IncomingNumber?.Trim(),
            IncomingDate = request.IncomingDate,
            RecordBook = request.RecordBook?.Trim(),
            SenderName = request.SenderName?.Trim(),
            ReceiverName = request.ReceiverName?.Trim(),
            AttachmentFileName = request.AttachmentFileName?.Trim(),
            TranslationRequestCount = request.TranslationRequestCount,
            ExternalReference = request.AttachmentStorageKey?.Trim(),
        };

        var letter = new IncomingLetterDetail
        {
            DocumentId = doc.Id,
            Document = doc,
            Phase = request.RequiresTranslation
                ? IncomingLetterPhase.Received
                : IncomingLetterPhase.ReadyForRegistration,
            RequiresTranslation = request.RequiresTranslation,
        };

        db.Documents.Add(doc);
        db.IncomingLetterDetails.Add(letter);
        await AddActivityAsync(doc, actorId, "letter_received", null, DocumentStatus.Draft, null, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "IncomingLetterReceived", "Document", doc.Id, number, ip, ct);

        return await GetByIdAsync(doc.Id, actorId, ct);
    }

    public async Task<Result<IncomingLetterDto>> SendToTranslationAsync(
        Guid id, SendToTranslationRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<IncomingLetterDto>.Fail("Letter not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsRegistrar(actor))
            return Result<IncomingLetterDto>.Fail("Access denied");
        if (detail.Phase != IncomingLetterPhase.Received || !detail.RequiresTranslation)
            return Result<IncomingLetterDto>.Fail("Letter is not awaiting translation");

        var languageError = TranslationLanguages.Validate(request.TranslatingLanguages, request.SourceLanguage);
        if (languageError is not null)
            return Result<IncomingLetterDto>.Fail(languageError);

        var sourceLanguage = request.SourceLanguage.Trim().ToLowerInvariant();
        var targetLanguages = request.TranslatingLanguages
            .Select(c => c.Trim().ToLowerInvariant())
            .Where(c => c.Length > 0)
            .Distinct()
            .ToList();
        var targetLanguagesJoined = TranslationLanguages.Join(targetLanguages);

        var deptCode = HelpDeskRouting.ResolveDepartmentCode(TicketCategory.Translator, actor.Organization.Code);
        if (deptCode is null)
            return Result<IncomingLetterDto>.Fail("Translation HelpDesk routing not found");

        var dept = await db.Departments.FirstOrDefaultAsync(d =>
            d.OrganizationId == actor.OrganizationId && d.Code == deptCode && d.IsActive, ct)
            ?? await db.Departments.FirstOrDefaultAsync(d => d.Code == deptCode && d.IsActive, ct);
        if (dept is null)
            return Result<IncomingLetterDto>.Fail($"Department {deptCode} not found");

        var ticketNumber = await GenerateHelpDeskNumberAsync(ct);
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Number = ticketNumber,
            Title = $"Incoming letter translation — {detail.Document.Number}",
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
        db.TicketActivities.Add(new TicketActivity
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            ActorId = actorId,
            Action = "created",
            ToStatus = TicketStatus.Open,
            Details = $"Routed to {dept.Code} from incoming letter",
        });

        detail.HelpDeskTicketId = ticket.Id;
        detail.SourceLanguage = sourceLanguage;
        detail.TranslatingLanguage = targetLanguagesJoined;
        detail.Phase = IncomingLetterPhase.TranslationPending;
        detail.SentToTranslationAt = DateTime.UtcNow;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "sent_to_translation", null,
            detail.Document.Status, ticket.Number, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "IncomingLetterSentToTranslation", "Document", id, ticket.Number, ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task NotifyHelpDeskTranslationCompletedAsync(Guid ticketId, CancellationToken ct = default)
    {
        var detail = await db.IncomingLetterDetails
            .Include(d => d.Document)
            .FirstOrDefaultAsync(d =>
                d.HelpDeskTicketId == ticketId && d.Phase == IncomingLetterPhase.TranslationPending, ct);
        if (detail is null) return;

        detail.Phase = IncomingLetterPhase.ReadyForRegistration;
        detail.TranslationReturnedAt = DateTime.UtcNow;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        db.DocumentActivities.Add(new DocumentActivity
        {
            Id = Guid.NewGuid(),
            DocumentId = detail.DocumentId,
            ActorId = detail.Document.AuthorId,
            Action = "translation_completed",
            Details = "HelpDesk translation completed",
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<Result<IncomingLetterDto>> CompleteTranslationAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<IncomingLetterDto>.Fail("Letter not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsTranslationDept(actor))
            return Result<IncomingLetterDto>.Fail("Access denied");
        if (detail.Phase != IncomingLetterPhase.TranslationPending)
            return Result<IncomingLetterDto>.Fail("Letter is not in translation");

        detail.Phase = IncomingLetterPhase.ReadyForRegistration;
        detail.TranslationReturnedAt = DateTime.UtcNow;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "translation_completed", null,
            detail.Document.Status, null, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "IncomingLetterTranslationCompleted", "Document", id, null, ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<IncomingLetterDto>> RegisterInEdsAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<IncomingLetterDto>.Fail("Letter not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsRegistrar(actor))
            return Result<IncomingLetterDto>.Fail("Access denied");

        var canRegister = detail.Phase == IncomingLetterPhase.ReadyForRegistration ||
            (detail.Phase == IncomingLetterPhase.Received && !detail.RequiresTranslation);
        if (!canRegister)
            return Result<IncomingLetterDto>.Fail("Letter is not ready for EDS registration");

        detail.Document.Number = await GenerateNumberAsync(ct);
        detail.Document.Status = DocumentStatus.Registered;
        detail.Document.RegisteredAt = DateTime.UtcNow;
        detail.Document.UpdatedAt = DateTime.UtcNow;
        detail.Phase = IncomingLetterPhase.Registered;

        await AddActivityAsync(detail.Document, actorId, "registered_in_eds", DocumentStatus.Draft,
            DocumentStatus.Registered, detail.Document.Number, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "IncomingLetterRegisteredInEds", "Document", id, detail.Document.Number, ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<IncomingLetterDto>> SendForResolutionAsync(
        Guid id, SendForResolutionRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<IncomingLetterDto>.Fail("Letter not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsRegistrar(actor))
            return Result<IncomingLetterDto>.Fail("Access denied");
        if (detail.Phase != IncomingLetterPhase.Registered)
            return Result<IncomingLetterDto>.Fail("Letter must be registered before resolution");
        if (request.ResolutionManagerIds.Count == 0)
            return Result<IncomingLetterDto>.Fail("Select at least one top manager");

        var managerIds = request.ResolutionManagerIds.Distinct().ToList();
        User? primaryManager = null;
        var managerNames = new List<string>();
        var existingRecipientUserIds = await LoadRecipientUserIdsAsync(id, ct);

        foreach (var (managerId, index) in managerIds.Select((id, i) => (id, i)))
        {
            var manager = await db.Users.FirstOrDefaultAsync(u =>
                u.Id == managerId && u.IsActive && u.Role == UserRole.HOTopManager, ct);
            if (manager is null) return Result<IncomingLetterDto>.Fail("Invalid top manager selected");

            var task = await CreateLinkedTaskAsync(
                manager.Id, actorId, manager.DepartmentId ?? detail.Document.DepartmentId, manager.OrganizationId,
                $"Incoming letter {detail.Document.Number} — resolution",
                detail.Document.Title,
                detail.DocumentId, ct);

            managerNames.Add(manager.FullName);

            if (index == 0)
            {
                primaryManager = manager;
                detail.ResolutionManagerId = manager.Id;
                continue;
            }

            if (!existingRecipientUserIds.Add(manager.Id)) continue;

            db.IncomingLetterRecipients.Add(new IncomingLetterRecipient
            {
                Id = Guid.NewGuid(),
                DocumentId = detail.DocumentId,
                UserId = manager.Id,
                Informed = true,
                ForInformation = false,
                InformedAt = DateTime.UtcNow,
                TaskId = task.Id,
            });
        }

        if (primaryManager is null)
            return Result<IncomingLetterDto>.Fail("Select at least one top manager");

        detail.SentForResolutionAt = DateTime.UtcNow;
        detail.InformedAt = DateTime.UtcNow;
        detail.Phase = IncomingLetterPhase.AwaitingResolution;
        detail.Document.Status = DocumentStatus.InReview;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "sent_for_resolution", DocumentStatus.Registered,
            DocumentStatus.InReview, string.Join(", ", managerNames), ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "IncomingLetterSentForResolution", "Document", id,
            string.Join(", ", managerNames), ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<IncomingLetterDto>> InformAdditionalManagersAsync(
        Guid id, InformTopManagersRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<IncomingLetterDto>.Fail("Letter not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || detail.ResolutionManagerId != actorId)
            return Result<IncomingLetterDto>.Fail("Access denied");
        if (detail.Phase is not (IncomingLetterPhase.AwaitingResolution or IncomingLetterPhase.RoutedToDepartment))
            return Result<IncomingLetterDto>.Fail("Letter is not available for additional notification");
        if (request.TopManagerIds.Count == 0)
            return Result<IncomingLetterDto>.Fail("Select at least one top manager");

        var existingRecipientUserIds = await LoadRecipientUserIdsAsync(id, ct);

        foreach (var userId in request.TopManagerIds.Distinct())
        {
            if (userId == detail.ResolutionManagerId) continue;
            if (!existingRecipientUserIds.Add(userId)) continue;

            var tm = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && u.Role == UserRole.HOTopManager, ct);
            if (tm is null) return Result<IncomingLetterDto>.Fail("Invalid top manager selected");

            var task = await CreateLinkedTaskAsync(
                tm.Id, actorId, tm.DepartmentId ?? detail.Document.DepartmentId, tm.OrganizationId,
                $"Incoming letter {detail.Document.Number} — for information",
                detail.Document.Title,
                detail.DocumentId, ct);

            db.IncomingLetterRecipients.Add(new IncomingLetterRecipient
            {
                Id = Guid.NewGuid(),
                DocumentId = detail.DocumentId,
                UserId = tm.Id,
                Informed = true,
                ForInformation = true,
                InformedAt = DateTime.UtcNow,
                TaskId = task.Id,
            });
        }

        detail.Document.UpdatedAt = DateTime.UtcNow;
        await AddActivityAsync(detail.Document, actorId, "informed_additional_managers", null,
            detail.Document.Status, $"{request.TopManagerIds.Count} manager(s)", ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "IncomingLetterInformedAdditional", "Document", id, null, ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<IncomingLetterDto>> RouteToDepartmentAsync(
        Guid id, RouteIncomingLetterRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<IncomingLetterDto>.Fail("Letter not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || detail.ResolutionManagerId != actorId)
            return Result<IncomingLetterDto>.Fail("Access denied");
        if (detail.Phase != IncomingLetterPhase.AwaitingResolution)
            return Result<IncomingLetterDto>.Fail("Letter is not awaiting routing");

        var dept = await db.Departments.Include(d => d.Organization)
            .FirstOrDefaultAsync(d => d.Id == request.TargetDepartmentId && d.IsActive, ct);
        if (dept is null) return Result<IncomingLetterDto>.Fail("Department not found");

        var deptHead = await db.Users
            .Where(u => u.IsActive && u.DepartmentId == dept.Id && u.Role == UserRole.HONachalnik)
            .OrderBy(u => u.LastName)
            .FirstOrDefaultAsync(ct)
            ?? await db.Users
                .Where(u => u.IsActive && u.DepartmentId == dept.Id)
                .OrderBy(u => u.LastName)
                .FirstOrDefaultAsync(ct);

        if (deptHead is null) return Result<IncomingLetterDto>.Fail("No staff in target department");

        await CreateLinkedTaskAsync(
            deptHead.Id, actorId, dept.Id, dept.OrganizationId,
            $"Incoming letter — {dept.NameEn ?? dept.Name}",
            detail.Document.Title,
            detail.DocumentId, ct);

        if (!string.IsNullOrWhiteSpace(request.Comment))
            AddComment(detail.DocumentId, actorId, request.Comment);

        detail.Phase = IncomingLetterPhase.RoutedToDepartment;
        detail.RoutedById = actorId;
        detail.RoutedToDepartmentId = dept.Id;
        detail.RoutedAt = DateTime.UtcNow;
        detail.Document.DepartmentId = dept.Id;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "routed_to_department", null,
            detail.Document.Status, dept.Code, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "IncomingLetterRouted", "Document", id, dept.Code, ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<IncomingLetterDto>> AssignWorkerAsync(
        Guid id, AssignIncomingLetterRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<IncomingLetterDto>.Fail("Letter not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsDeptManager(actor) || actor.DepartmentId != detail.Document.DepartmentId)
            return Result<IncomingLetterDto>.Fail("Access denied");
        if (detail.Phase != IncomingLetterPhase.RoutedToDepartment)
            return Result<IncomingLetterDto>.Fail("Letter is not ready for assignment");

        var worker = await db.Users.FirstOrDefaultAsync(u =>
            u.Id == request.AssigneeId && u.IsActive && u.DepartmentId == detail.Document.DepartmentId, ct);
        if (worker is null) return Result<IncomingLetterDto>.Fail("Assignee must be in your department");

        var dueDate = request.DueDate?.ToUniversalTime();
        await CreateLinkedTaskAsync(
            worker.Id, actorId, detail.Document.DepartmentId, detail.Document.OrganizationId,
            $"Execute incoming letter {detail.Document.Number}",
            request.AssignmentTask?.Trim() ?? detail.Document.Title,
            detail.DocumentId, ct, dueDate);

        if (!string.IsNullOrWhiteSpace(request.Comment))
            AddComment(detail.DocumentId, actorId, request.Comment);

        detail.Document.AssigneeId = worker.Id;
        detail.AssignmentTask = request.AssignmentTask?.Trim();
        detail.DueDate = dueDate;
        detail.Phase = IncomingLetterPhase.AwaitingAcceptance;
        detail.Document.Status = DocumentStatus.InReview;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "assigned", null,
            DocumentStatus.InReview, worker.FullName, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "IncomingLetterAssigned", "Document", id, worker.FullName, ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<IncomingLetterDto>> AcceptExecutionAsync(
        Guid id, AcceptExecutionRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<IncomingLetterDto>.Fail("Letter not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || detail.Document.AssigneeId != actorId)
            return Result<IncomingLetterDto>.Fail("Access denied");
        if (detail.Phase != IncomingLetterPhase.AwaitingAcceptance)
            return Result<IncomingLetterDto>.Fail("Letter is not awaiting acceptance");

        detail.Phase = IncomingLetterPhase.InExecution;
        detail.RequiresResponse = request.RequiresResponse;
        detail.ExecutorAcceptedAt = DateTime.UtcNow;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "execution_accepted", null,
            detail.Document.Status, request.RequiresResponse ? "response_required" : "no_response", ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "IncomingLetterAccepted", "Document", id, null, ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<IncomingLetterDto>> ReportCompletionAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<IncomingLetterDto>.Fail("Letter not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || detail.Document.AssigneeId != actorId)
            return Result<IncomingLetterDto>.Fail("Access denied");
        if (detail.Phase is not (IncomingLetterPhase.InExecution or IncomingLetterPhase.NeedsRevision))
            return Result<IncomingLetterDto>.Fail("Letter is not in execution");

        detail.Phase = IncomingLetterPhase.AwaitingReview;
        detail.ReportedAt = DateTime.UtcNow;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "reported_completion", null,
            detail.Document.Status, null, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "IncomingLetterReported", "Document", id, null, ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<IncomingLetterDto>> RequestRevisionAsync(
        Guid id, IncomingLetterCommentRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<IncomingLetterDto>.Fail("Letter not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsDeptManager(actor) || actor.DepartmentId != detail.Document.DepartmentId)
            return Result<IncomingLetterDto>.Fail("Access denied");
        if (detail.Phase != IncomingLetterPhase.AwaitingReview)
            return Result<IncomingLetterDto>.Fail("Letter is not awaiting review");
        if (string.IsNullOrWhiteSpace(request.Body))
            return Result<IncomingLetterDto>.Fail("Revision comment is required");

        AddComment(detail.DocumentId, actorId, request.Body);

        detail.Phase = IncomingLetterPhase.NeedsRevision;
        detail.ReviewedAt = DateTime.UtcNow;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "revision_requested", null,
            detail.Document.Status, null, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "IncomingLetterRevisionRequested", "Document", id, null, ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<IncomingLetterDto>> AcceptCompletionAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<IncomingLetterDto>.Fail("Letter not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsDeptManager(actor) || actor.DepartmentId != detail.Document.DepartmentId)
            return Result<IncomingLetterDto>.Fail("Access denied");
        if (detail.Phase != IncomingLetterPhase.AwaitingReview)
            return Result<IncomingLetterDto>.Fail("Letter is not awaiting review");

        detail.Phase = IncomingLetterPhase.AwaitingArchive;
        detail.ReviewedAt = DateTime.UtcNow;
        detail.Document.Status = DocumentStatus.Approved;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "completion_accepted", DocumentStatus.InReview,
            DocumentStatus.Approved, null, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "IncomingLetterCompletionAccepted", "Document", id, null, ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<IncomingLetterDto>> ArchiveAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<IncomingLetterDto>.Fail("Letter not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsRegistrar(actor))
            return Result<IncomingLetterDto>.Fail("Access denied");
        if (detail.Phase != IncomingLetterPhase.AwaitingArchive)
            return Result<IncomingLetterDto>.Fail("Letter is not ready for archiving");

        detail.Phase = IncomingLetterPhase.Completed;
        detail.ArchivedAt = DateTime.UtcNow;
        detail.CompletedAt = DateTime.UtcNow;
        detail.Document.Status = DocumentStatus.Archived;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "archived", DocumentStatus.Approved,
            DocumentStatus.Archived, null, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "IncomingLetterArchived", "Document", id, null, ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<IncomingLetterCommentDto>> AddCommentAsync(
        Guid id, IncomingLetterCommentRequest request, Guid actorId, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<IncomingLetterCommentDto>.Fail("Letter not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanView(actor, detail))
            return Result<IncomingLetterCommentDto>.Fail("Access denied");
        if (string.IsNullOrWhiteSpace(request.Body))
            return Result<IncomingLetterCommentDto>.Fail("Comment is required");

        var comment = AddComment(detail.DocumentId, actorId, request.Body);
        detail.Document.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "IncomingLetterComment", "Document", id, null, null, ct);

        return Result<IncomingLetterCommentDto>.Ok(new IncomingLetterCommentDto(
            comment.Id, actorId, actor.FullName, comment.Body, comment.CreatedAt));
    }

    private async Task<IncomingLetterDetail?> LoadDetailAsync(Guid id, CancellationToken ct) =>
        await db.IncomingLetterDetails.AsNoTracking()
            .Include(d => d.Document).ThenInclude(doc => doc.Author)
            .Include(d => d.Document).ThenInclude(doc => doc.Assignee)
            .Include(d => d.Document).ThenInclude(doc => doc.Department)
            .Include(d => d.ResolutionManager)
            .Include(d => d.RoutedBy)
            .Include(d => d.RoutedToDepartment)
            .Include(d => d.Recipients).ThenInclude(r => r.User)
            .Include(d => d.Comments).ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(d => d.DocumentId == id, ct);

    private async Task<IncomingLetterDetail?> LoadDetailTrackedAsync(Guid id, CancellationToken ct) =>
        await db.IncomingLetterDetails
            .Include(d => d.Document)
            .FirstOrDefaultAsync(d => d.DocumentId == id, ct);

    private IncomingLetterComment AddComment(Guid documentId, Guid authorId, string body)
    {
        var comment = new IncomingLetterComment
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            AuthorId = authorId,
            Body = body.Trim(),
        };
        db.IncomingLetterComments.Add(comment);
        return comment;
    }

    private async Task<HashSet<Guid>> LoadRecipientUserIdsAsync(Guid documentId, CancellationToken ct) =>
        (await db.IncomingLetterRecipients.AsNoTracking()
            .Where(r => r.DocumentId == documentId)
            .Select(r => r.UserId)
            .ToListAsync(ct)).ToHashSet();

    private static IncomingLetterDto MapDetail(IncomingLetterDetail d, string? helpDeskTicketNumber = null) => new(
        d.DocumentId,
        d.Document.Number,
        d.Document.Title,
        d.Document.TitleRu,
        d.Document.Status,
        d.Phase,
        d.Document.AuthorId,
        d.Document.Author.FullName,
        d.Document.IncomingNumber,
        d.Document.IncomingDate,
        d.Document.RecordBook,
        d.Document.SenderName,
        d.Document.ReceiverName,
        d.Document.AttachmentFileName,
        d.Document.TranslationRequestCount,
        d.Document.ExternalReference,
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
        d.TranslatedAttachmentFileName,
        d.TranslatedAttachmentStorageKey,
        d.ResolutionManagerId,
        d.ResolutionManager?.FullName,
        d.RoutedToDepartmentId,
        d.RoutedToDepartment?.Name,
        d.RoutedToDepartment?.NameEn,
        d.RoutedBy?.FullName,
        d.AssignmentTask,
        d.DueDate,
        d.RequiresResponse,
        d.Document.RegisteredAt,
        d.SentToTranslationAt,
        d.TranslationReturnedAt,
        d.SentForResolutionAt,
        d.InformedAt,
        d.RoutedAt,
        d.ExecutorAcceptedAt,
        d.ReportedAt,
        d.ReviewedAt,
        d.ArchivedAt,
        d.CompletedAt,
        d.Recipients.OrderBy(r => r.InformedAt).Select(r => new IncomingLetterRecipientDto(
            r.Id, r.UserId, r.User.FullName, r.Informed, r.ForInformation, r.InformedAt, r.TaskId)).ToList(),
        d.Comments.OrderBy(c => c.CreatedAt).Select(c => new IncomingLetterCommentDto(
            c.Id, c.AuthorId, c.Author.FullName, c.Body, c.CreatedAt)).ToList(),
        d.Document.CreatedAt,
        d.Document.UpdatedAt);

    private async Task<IncomingLetterDto> MapDetailAsync(IncomingLetterDetail d, CancellationToken ct)
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
        IncomingLetterDetail detail, string sourceLanguage, IReadOnlyList<string> targetLanguages)
    {
        var targetLabel = string.Join(", ", targetLanguages);
        var lines = new List<string>
        {
            "Incoming letter translation request (DCS)",
            $"Document: {detail.Document.Number}",
            $"Subject: {detail.Document.Title}",
            $"Source language: {sourceLanguage}",
            $"Target language(s): {targetLabel}",
        };
        if (!string.IsNullOrWhiteSpace(detail.Document.SenderName))
            lines.Add($"Sender: {detail.Document.SenderName}");
        if (!string.IsNullOrWhiteSpace(detail.Document.AttachmentFileName))
            lines.Add($"Attachment: {detail.Document.AttachmentFileName}");
        return string.Join(Environment.NewLine, lines);
    }

    private async Task<string> GenerateHelpDeskNumberAsync(CancellationToken ct)
    {
        var prefix = $"HD-{DateTime.UtcNow.Year}-";
        var last = await db.Tickets.Where(t => t.Number.StartsWith(prefix))
            .OrderByDescending(t => t.Number).Select(t => t.Number).FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var n)) seq = n + 1;
        return $"{prefix}{seq:D5}";
    }

    private static IncomingLetterUserDto MapUser(User u) => new(
        u.Id, u.FullName, u.Email, u.EmployeeId,
        u.Department?.Name ?? "", u.Department?.NameEn ?? "");

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

    private async Task<string> GenerateNumberAsync(CancellationToken ct)
    {
        var prefix = $"{DcsRouting.NumberPrefix(DocumentType.Incoming)}-{DateTime.UtcNow.Year}-";
        var last = await db.Documents.Where(d => d.Number.StartsWith(prefix) && !d.Number.Contains("PEND"))
            .OrderByDescending(d => d.Number).Select(d => d.Number).FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && int.TryParse(last[(prefix.Length)..], out var n)) seq = n + 1;
        return $"{prefix}{seq:D4}";
    }

    private async Task<string> GeneratePendingNumberAsync(CancellationToken ct)
    {
        var prefix = $"IN-PEND-{DateTime.UtcNow.Year}-";
        var last = await db.Documents.Where(d => d.Number.StartsWith(prefix))
            .OrderByDescending(d => d.Number).Select(d => d.Number).FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && int.TryParse(last[(prefix.Length)..], out var n)) seq = n + 1;
        return $"{prefix}{seq:D4}";
    }

    private async Task<string> GenerateTaskNumberAsync(CancellationToken ct)
    {
        await Task.CompletedTask;
        return $"TSK-{Guid.NewGuid():N}"[..20];
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
        await db.Users.Include(u => u.Organization).Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive, ct);

    private static bool IsRegistrar(User u) =>
        u.Email.Equals(DcsRouting.IncomingRegistrarEmail, StringComparison.OrdinalIgnoreCase) ||
        u.Role == UserRole.SuperAdmin;

    private static bool IsTranslationDept(User u) =>
        u.Department?.Code == TranslationDept || u.Role == UserRole.SuperAdmin;

    private static bool IsDeptManager(User u) =>
        u.Role is UserRole.HONachalnik or UserRole.BMGMCNachalnikiOtdeli or UserRole.BMGMCManager;

    private static bool CanView(User actor, IncomingLetterDetail? detail)
    {
        if (detail is null) return IsRegistrar(actor) || actor.Role == UserRole.SuperAdmin;
        if (IsRegistrar(actor) || actor.Role == UserRole.SuperAdmin) return true;
        if (IsTranslationDept(actor)) return true;
        if (detail.Document.AuthorId == actor.Id) return true;
        if (detail.ResolutionManagerId == actor.Id) return true;
        if (detail.Recipients.Any(r => r.UserId == actor.Id)) return true;
        if (detail.Document.AssigneeId == actor.Id) return true;
        if (IsDeptManager(actor) && actor.DepartmentId == detail.Document.DepartmentId) return true;
        if (actor.Role == UserRole.HOTopManager) return true;
        return false;
    }
}
