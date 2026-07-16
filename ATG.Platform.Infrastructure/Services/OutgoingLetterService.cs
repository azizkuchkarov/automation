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

public class OutgoingLetterService(AppDbContext db, IAuditService audit) : IOutgoingLetterService
{
    private const string TranslationDept = "HO-DCPR-TRNS";

    public async Task<Result<OutgoingLetterPermissionsDto>> GetPermissionsAsync(
        Guid actorId, Guid? documentId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<OutgoingLetterPermissionsDto>.Fail("User not found");

        OutgoingLetterDetail? detail = null;
        if (documentId.HasValue)
            detail = await LoadDetailAsync(documentId.Value, ct);

        var phase = detail?.Phase ?? OutgoingLetterPhase.Draft;
        var isInitiator = detail?.Document.AuthorId == actorId;
        var isRegistrar = IsRegistrar(actor);
        var isDeptHead = detail is not null && detail.DeptHeadId == actorId;
        var isTranslationDept = IsTranslationDept(actor);
        var isSupervisingDeputy = detail?.SupervisingDeputyId == actorId;
        var isFirstDeputy = detail?.FirstDeputyId == actorId;
        var isGeneralDirector = detail?.GeneralDirectorId == actorId;

        return Result<OutgoingLetterPermissionsDto>.Ok(new OutgoingLetterPermissionsDto(
            isInitiator,
            isRegistrar,
            isDeptHead,
            isTranslationDept,
            isSupervisingDeputy,
            isFirstDeputy,
            isGeneralDirector,
            CanCreate(actor),
            isInitiator && (phase is OutgoingLetterPhase.Draft or OutgoingLetterPhase.NeedsRevision),
            isInitiator && phase == OutgoingLetterPhase.Draft && detail?.RequiresTranslation == true,
            isInitiator && phase == OutgoingLetterPhase.ReadyForEds,
            isDeptHead && phase == OutgoingLetterPhase.AwaitingDeptHeadApproval,
            isDeptHead && phase == OutgoingLetterPhase.AwaitingDeptHeadApproval,
            isInitiator && phase == OutgoingLetterPhase.SpecialistCoordination,
            isInitiator && phase == OutgoingLetterPhase.DepartmentCoordination,
            isInitiator && phase is OutgoingLetterPhase.SpecialistCoordination or OutgoingLetterPhase.DepartmentCoordination,
            isSupervisingDeputy && phase == OutgoingLetterPhase.AwaitingSupervisingDeputyApproval,
            isFirstDeputy && phase == OutgoingLetterPhase.AwaitingFirstDeputyApproval,
            isGeneralDirector && phase == OutgoingLetterPhase.AwaitingGeneralDirectorApproval,
            isInitiator && phase == OutgoingLetterPhase.EdsFinalized,
            isInitiator && phase == OutgoingLetterPhase.AwaitingRegistration,
            isRegistrar && phase == OutgoingLetterPhase.AwaitingRegistration,
            isRegistrar && phase == OutgoingLetterPhase.AwaitingPaperSignature,
            isRegistrar && phase == OutgoingLetterPhase.AwaitingDispatch,
            isRegistrar && phase == OutgoingLetterPhase.AwaitingArchive,
            CanView(actor, detail)));
    }

    public async Task<Result<IReadOnlyList<OutgoingLetterUserDto>>> GetDeptHeadsAsync(
        Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<IReadOnlyList<OutgoingLetterUserDto>>.Fail("User not found");

        var users = await db.Users.AsNoTracking()
            .Include(u => u.Department)
            .Where(u => u.IsActive && u.Role == UserRole.HONachalnik
                && u.Organization.Code == HoMasterData.OrganizationCode)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);

        return Result<IReadOnlyList<OutgoingLetterUserDto>>.Ok(users.Select(MapUser).ToList());
    }

    public async Task<Result<IReadOnlyList<OutgoingLetterUserDto>>> GetTopManagersAsync(
        Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<IReadOnlyList<OutgoingLetterUserDto>>.Fail("User not found");

        var users = await db.Users.AsNoTracking()
            .Include(u => u.Department)
            .Where(u => u.IsActive && u.Role == UserRole.HOTopManager
                && u.Organization.Code == HoMasterData.OrganizationCode)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);

        return Result<IReadOnlyList<OutgoingLetterUserDto>>.Ok(users.Select(MapUser).ToList());
    }

    public async Task<Result<IReadOnlyList<OutgoingLetterUserDto>>> GetCoordinatorsAsync(
        Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<IReadOnlyList<OutgoingLetterUserDto>>.Fail("User not found");

        var users = await db.Users.AsNoTracking()
            .Include(u => u.Department)
            .Where(u => u.IsActive && u.Organization.Code == HoMasterData.OrganizationCode
                && (u.Role == UserRole.HOEngineer || u.Role == UserRole.HONachalnik))
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);

        return Result<IReadOnlyList<OutgoingLetterUserDto>>.Ok(users.Select(MapUser).ToList());
    }

    public async Task<Result<OutgoingLetterDto>> GetByIdAsync(Guid id, Guid actorId, CancellationToken ct = default)
    {
        var detail = await LoadDetailAsync(id, ct);
        if (detail is null) return Result<OutgoingLetterDto>.Fail("Letter not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanView(actor, detail))
            return Result<OutgoingLetterDto>.Fail("Access denied");

        return Result<OutgoingLetterDto>.Ok(await MapDetailAsync(detail, ct));
    }

    public async Task<Result<OutgoingLetterDto>> CreateAsync(
        CreateOutgoingLetterRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<OutgoingLetterDto>.Fail("User not found");
        if (!CanCreate(actor))
            return Result<OutgoingLetterDto>.Fail("Access denied");

        if (string.IsNullOrWhiteSpace(request.Title))
            return Result<OutgoingLetterDto>.Fail("Subject is required");
        if (actor.DepartmentId is null)
            return Result<OutgoingLetterDto>.Fail("Department is required");

        var number = await GeneratePendingNumberAsync(ct);
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            Number = number,
            Title = request.Title.Trim(),
            TitleRu = request.TitleRu?.Trim(),
            Type = DocumentType.Outgoing,
            Status = DocumentStatus.Draft,
            AuthorId = actorId,
            OrganizationId = actor.OrganizationId,
            DepartmentId = actor.DepartmentId.Value,
            ReceiverName = request.AddresseeName?.Trim(),
            AttachmentFileName = request.AttachmentFileName?.Trim(),
            ExternalReference = request.AttachmentStorageKey?.Trim(),
        };

        var letter = new OutgoingLetterDetail
        {
            DocumentId = doc.Id,
            Document = doc,
            Phase = request.RequiresTranslation ? OutgoingLetterPhase.Draft : OutgoingLetterPhase.ReadyForEds,
            RequiresTranslation = request.RequiresTranslation,
        };

        db.Documents.Add(doc);
        db.OutgoingLetterDetails.Add(letter);
        await AddActivityAsync(doc, actorId, "outgoing_draft_created", null, DocumentStatus.Draft, null, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "OutgoingLetterCreated", "Document", doc.Id, number, ip, ct);

        return await GetByIdAsync(doc.Id, actorId, ct);
    }

    public async Task<Result<OutgoingLetterDto>> UpdateDraftAsync(
        Guid id, CreateOutgoingLetterRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<OutgoingLetterDto>.Fail("Letter not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || detail.Document.AuthorId != actorId)
            return Result<OutgoingLetterDto>.Fail("Access denied");
        if (detail.Phase is not (OutgoingLetterPhase.Draft or OutgoingLetterPhase.NeedsRevision))
            return Result<OutgoingLetterDto>.Fail("Letter is not editable");

        detail.Document.Title = request.Title.Trim();
        detail.Document.TitleRu = request.TitleRu?.Trim();
        detail.Document.ReceiverName = request.AddresseeName?.Trim();
        detail.Document.AttachmentFileName = request.AttachmentFileName?.Trim();
        detail.Document.ExternalReference = request.AttachmentStorageKey?.Trim();
        detail.RequiresTranslation = request.RequiresTranslation;
        detail.RevisionNotes = null;
        detail.Phase = request.RequiresTranslation ? OutgoingLetterPhase.Draft : OutgoingLetterPhase.ReadyForEds;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "OutgoingLetterDraftUpdated", "Document", id, detail.Document.Number, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<OutgoingLetterDto>> SendToTranslationAsync(
        Guid id, SendOutgoingToTranslationRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<OutgoingLetterDto>.Fail("Letter not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || detail.Document.AuthorId != actorId)
            return Result<OutgoingLetterDto>.Fail("Access denied");
        if (detail.Phase != OutgoingLetterPhase.Draft || !detail.RequiresTranslation)
            return Result<OutgoingLetterDto>.Fail("Letter is not awaiting translation");

        var languageError = TranslationLanguages.Validate(request.TranslatingLanguages, request.SourceLanguage);
        if (languageError is not null)
            return Result<OutgoingLetterDto>.Fail(languageError);

        var sourceLanguage = request.SourceLanguage.Trim().ToLowerInvariant();
        var targetLanguages = TranslationLanguages.Join(request.TranslatingLanguages);

        var deptCode = HelpDeskRouting.ResolveDepartmentCode(TicketCategory.Translator, actor.Organization.Code);
        if (deptCode is null)
            return Result<OutgoingLetterDto>.Fail("Translation HelpDesk routing not found");

        var dept = await db.Departments.FirstOrDefaultAsync(d =>
            d.OrganizationId == actor.OrganizationId && d.Code == deptCode && d.IsActive, ct)
            ?? await db.Departments.FirstOrDefaultAsync(d => d.Code == deptCode && d.IsActive, ct);
        if (dept is null)
            return Result<OutgoingLetterDto>.Fail($"Department {deptCode} not found");

        var ticketNumber = await GenerateHelpDeskNumberAsync(ct);
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Number = ticketNumber,
            Title = $"Outgoing letter translation — {detail.Document.Number}",
            Description = BuildTranslationDescription(detail, sourceLanguage, TranslationLanguages.Parse(targetLanguages)),
            Category = TicketCategory.Translator,
            Priority = TicketPriority.Medium,
            Status = TicketStatus.Open,
            RequesterId = actorId,
            OrganizationId = actor.OrganizationId,
            TargetDepartmentId = dept.Id,
            SourceLanguage = sourceLanguage,
            TranslatingLanguage = targetLanguages,
            LinkedDocumentId = detail.DocumentId,
        };
        db.Tickets.Add(ticket);

        detail.HelpDeskTicketId = ticket.Id;
        detail.SourceLanguage = sourceLanguage;
        detail.TranslatingLanguage = targetLanguages;
        detail.Phase = OutgoingLetterPhase.TranslationPending;
        detail.SentToTranslationAt = DateTime.UtcNow;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "sent_to_translation", null, detail.Document.Status, ticket.Number, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "OutgoingLetterSentToTranslation", "Document", id, ticket.Number, ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task NotifyHelpDeskTranslationCompletedAsync(Guid ticketId, CancellationToken ct = default)
    {
        var detail = await db.OutgoingLetterDetails
            .Include(d => d.Document)
            .FirstOrDefaultAsync(d =>
                d.HelpDeskTicketId == ticketId && d.Phase == OutgoingLetterPhase.TranslationPending, ct);
        if (detail is null) return;

        detail.Phase = OutgoingLetterPhase.ReadyForEds;
        detail.TranslationReturnedAt = DateTime.UtcNow;
        detail.Document.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<Result<OutgoingLetterDto>> SubmitToEdsAsync(
        Guid id, SubmitOutgoingToEdsRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<OutgoingLetterDto>.Fail("Letter not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || detail.Document.AuthorId != actorId)
            return Result<OutgoingLetterDto>.Fail("Access denied");
        if (detail.Phase != OutgoingLetterPhase.ReadyForEds)
            return Result<OutgoingLetterDto>.Fail("Letter is not ready for EDS submission");

        var deptHead = await db.Users.FirstOrDefaultAsync(u =>
            u.Id == request.DeptHeadId && u.IsActive && u.Role == UserRole.HONachalnik, ct);
        if (deptHead is null) return Result<OutgoingLetterDto>.Fail("Invalid department head");

        if (request.SupervisingDeputyId.HasValue)
        {
            var sd = await ValidateTopManagerAsync(request.SupervisingDeputyId.Value, ct);
            if (sd is null) return Result<OutgoingLetterDto>.Fail("Invalid supervising deputy");
            detail.SupervisingDeputyId = sd.Id;
        }
        if (request.FirstDeputyId.HasValue)
        {
            var fd = await ValidateTopManagerAsync(request.FirstDeputyId.Value, ct);
            if (fd is null) return Result<OutgoingLetterDto>.Fail("Invalid first deputy");
            detail.FirstDeputyId = fd.Id;
        }
        if (request.GeneralDirectorId.HasValue)
        {
            var gd = await ValidateTopManagerAsync(request.GeneralDirectorId.Value, ct);
            if (gd is null) return Result<OutgoingLetterDto>.Fail("Invalid general director");
            detail.GeneralDirectorId = gd.Id;
        }

        detail.DeptHeadId = deptHead.Id;
        detail.Phase = OutgoingLetterPhase.AwaitingDeptHeadApproval;
        detail.SubmittedToEdsAt = DateTime.UtcNow;
        detail.Document.Status = DocumentStatus.InReview;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await CreateLinkedTaskAsync(
            deptHead.Id, actorId, deptHead.DepartmentId ?? detail.Document.DepartmentId, deptHead.OrganizationId,
            $"Outgoing letter {detail.Document.Number} — dept head approval",
            detail.Document.Title, detail.DocumentId, ct);

        await AddActivityAsync(detail.Document, actorId, "submitted_to_eds", DocumentStatus.Draft,
            DocumentStatus.InReview, deptHead.FullName, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "OutgoingLetterSubmittedToEds", "Document", id, deptHead.FullName, ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public Task<Result<OutgoingLetterDto>> ApproveDeptHeadAsync(
        Guid id, OutgoingApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default)
        => ApproveAsync(id, actorId, ip, OutgoingLetterPhase.AwaitingDeptHeadApproval,
            OutgoingLetterPhase.SpecialistCoordination, d => d.DeptHeadApprovedAt = DateTime.UtcNow,
            "dept_head_approved", ct);

    public Task<Result<OutgoingLetterDto>> RejectDeptHeadAsync(
        Guid id, OutgoingRevisionRequest request, Guid actorId, string? ip, CancellationToken ct = default)
        => RejectAsync(id, request, actorId, ip, d => d.DeptHeadId == actorId, ct);

    public async Task<Result<OutgoingLetterDto>> AddCoordinatorsAsync(
        Guid id, OutgoingCoordinatorRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<OutgoingLetterDto>.Fail("Letter not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || detail.Document.AuthorId != actorId)
            return Result<OutgoingLetterDto>.Fail("Access denied");

        var expectedPhase = request.ForDepartment
            ? OutgoingLetterPhase.DepartmentCoordination
            : OutgoingLetterPhase.SpecialistCoordination;
        if (detail.Phase != expectedPhase)
            return Result<OutgoingLetterDto>.Fail("Invalid coordination phase");

        foreach (var userId in request.UserIds.Distinct())
        {
            if (detail.Coordinators.Any(c => c.UserId == userId)) continue;
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, ct);
            if (user is null) return Result<OutgoingLetterDto>.Fail("Invalid coordinator selected");

            detail.Coordinators.Add(new OutgoingLetterCoordinator
            {
                Id = Guid.NewGuid(),
                DocumentId = detail.DocumentId,
                UserId = userId,
                ForDepartment = request.ForDepartment,
            });
        }

        detail.Document.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<OutgoingLetterDto>> CompleteSpecialistCoordinationAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<OutgoingLetterDto>.Fail("Letter not found");
        if (detail.Document.AuthorId != actorId) return Result<OutgoingLetterDto>.Fail("Access denied");
        if (detail.Phase != OutgoingLetterPhase.SpecialistCoordination)
            return Result<OutgoingLetterDto>.Fail("Invalid phase");

        detail.Phase = OutgoingLetterPhase.DepartmentCoordination;
        detail.Document.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<OutgoingLetterDto>> CompleteDepartmentCoordinationAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<OutgoingLetterDto>.Fail("Letter not found");
        if (detail.Document.AuthorId != actorId) return Result<OutgoingLetterDto>.Fail("Access denied");
        if (detail.Phase != OutgoingLetterPhase.DepartmentCoordination)
            return Result<OutgoingLetterDto>.Fail("Invalid phase");
        if (!detail.SupervisingDeputyId.HasValue)
            return Result<OutgoingLetterDto>.Fail("Supervising deputy must be selected before approval chain");

        detail.Phase = OutgoingLetterPhase.AwaitingSupervisingDeputyApproval;
        detail.CoordinationCompletedAt = DateTime.UtcNow;
        detail.Document.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public Task<Result<OutgoingLetterDto>> ApproveSupervisingDeputyAsync(
        Guid id, OutgoingApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default)
        => ApproveAsync(id, actorId, ip, OutgoingLetterPhase.AwaitingSupervisingDeputyApproval,
            OutgoingLetterPhase.AwaitingFirstDeputyApproval, d => d.SupervisingDeputyApprovedAt = DateTime.UtcNow,
            "supervising_deputy_approved", ct);

    public Task<Result<OutgoingLetterDto>> ApproveFirstDeputyAsync(
        Guid id, OutgoingApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default)
        => ApproveAsync(id, actorId, ip, OutgoingLetterPhase.AwaitingFirstDeputyApproval,
            OutgoingLetterPhase.AwaitingGeneralDirectorApproval, d => d.FirstDeputyApprovedAt = DateTime.UtcNow,
            "first_deputy_approved", ct);

    public Task<Result<OutgoingLetterDto>> ApproveGeneralDirectorAsync(
        Guid id, OutgoingApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default)
        => ApproveAsync(id, actorId, ip, OutgoingLetterPhase.AwaitingGeneralDirectorApproval,
            OutgoingLetterPhase.EdsFinalized, d => d.GeneralDirectorApprovedAt = DateTime.UtcNow,
            "general_director_approved", ct);

    public async Task<Result<OutgoingLetterDto>> RejectApprovalAsync(
        Guid id, OutgoingRevisionRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<OutgoingLetterDto>.Fail("Letter not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<OutgoingLetterDto>.Fail("Access denied");

        var canReject = detail.Phase switch
        {
            OutgoingLetterPhase.AwaitingSupervisingDeputyApproval => detail.SupervisingDeputyId == actorId,
            OutgoingLetterPhase.AwaitingFirstDeputyApproval => detail.FirstDeputyId == actorId,
            OutgoingLetterPhase.AwaitingGeneralDirectorApproval => detail.GeneralDirectorId == actorId,
            _ => false
        };
        if (!canReject) return Result<OutgoingLetterDto>.Fail("Access denied");

        return await RejectAsync(id, request, actorId, ip, _ => true, ct);
    }

    public async Task<Result<OutgoingLetterDto>> FinalizeEdsAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<OutgoingLetterDto>.Fail("Letter not found");
        if (detail.Document.AuthorId != actorId) return Result<OutgoingLetterDto>.Fail("Access denied");
        if (detail.Phase != OutgoingLetterPhase.EdsFinalized)
            return Result<OutgoingLetterDto>.Fail("EDS approval is not finalized yet");

        detail.Phase = OutgoingLetterPhase.AwaitingRegistration;
        detail.EdsFinalizedAt = DateTime.UtcNow;
        detail.Document.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<OutgoingLetterDto>> SendToRegistrarAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<OutgoingLetterDto>.Fail("Letter not found");
        if (detail.Document.AuthorId != actorId) return Result<OutgoingLetterDto>.Fail("Access denied");
        if (detail.Phase != OutgoingLetterPhase.AwaitingRegistration)
            return Result<OutgoingLetterDto>.Fail("Letter is not ready for registrar");

        detail.SentToRegistrarAt = DateTime.UtcNow;
        detail.Document.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<OutgoingLetterDto>> RegisterAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<OutgoingLetterDto>.Fail("Letter not found");
        if (!IsRegistrar(await GetActorAsync(actorId, ct) ?? throw new InvalidOperationException()))
            return Result<OutgoingLetterDto>.Fail("Access denied");
        if (detail.Phase != OutgoingLetterPhase.AwaitingRegistration)
            return Result<OutgoingLetterDto>.Fail("Letter is not awaiting registration");

        detail.Document.Number = await GenerateFormalNumberAsync(ct);
        detail.Document.Status = DocumentStatus.Registered;
        detail.Document.RegisteredAt = DateTime.UtcNow;
        detail.RegisteredAt = DateTime.UtcNow;
        detail.Phase = OutgoingLetterPhase.AwaitingPaperSignature;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "registered_in_eds", DocumentStatus.InReview,
            DocumentStatus.Registered, detail.Document.Number, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "OutgoingLetterRegistered", "Document", id, detail.Document.Number, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public Task<Result<OutgoingLetterDto>> ConfirmPaperSignatureAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
        => RegistrarStepAsync(id, actorId, ip, OutgoingLetterPhase.AwaitingPaperSignature,
            OutgoingLetterPhase.AwaitingDispatch, d => d.PaperSignedAt = DateTime.UtcNow, "paper_signed", ct);

    public Task<Result<OutgoingLetterDto>> ConfirmDispatchAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
        => RegistrarStepAsync(id, actorId, ip, OutgoingLetterPhase.AwaitingDispatch,
            OutgoingLetterPhase.AwaitingArchive, d => d.DispatchedAt = DateTime.UtcNow, "dispatched", ct);

    public Task<Result<OutgoingLetterDto>> ArchiveAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
        => RegistrarStepAsync(id, actorId, ip, OutgoingLetterPhase.AwaitingArchive,
            OutgoingLetterPhase.Completed, d =>
            {
                d.ArchivedAt = DateTime.UtcNow;
                d.CompletedAt = DateTime.UtcNow;
            }, "archived", ct);

    private async Task<Result<OutgoingLetterDto>> RegistrarStepAsync(
        Guid id, Guid actorId, string? ip,
        OutgoingLetterPhase from, OutgoingLetterPhase to,
        Action<OutgoingLetterDetail> apply, string action, CancellationToken ct)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<OutgoingLetterDto>.Fail("Letter not found");
        if (!IsRegistrar(await GetActorAsync(actorId, ct) ?? throw new InvalidOperationException()))
            return Result<OutgoingLetterDto>.Fail("Access denied");
        if (detail.Phase != from) return Result<OutgoingLetterDto>.Fail("Invalid phase");

        apply(detail);
        detail.Phase = to;
        if (to == OutgoingLetterPhase.Completed)
            detail.Document.Status = DocumentStatus.Archived;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, action, null, detail.Document.Status, null, ct);
        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    private async Task<Result<OutgoingLetterDto>> ApproveAsync(
        Guid id, Guid actorId, string? ip,
        OutgoingLetterPhase from, OutgoingLetterPhase to,
        Action<OutgoingLetterDetail> apply, string action, CancellationToken ct)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<OutgoingLetterDto>.Fail("Letter not found");
        if (detail.Phase != from) return Result<OutgoingLetterDto>.Fail("Invalid phase");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<OutgoingLetterDto>.Fail("Access denied");

        var allowed = from switch
        {
            OutgoingLetterPhase.AwaitingDeptHeadApproval => detail.DeptHeadId == actorId,
            OutgoingLetterPhase.AwaitingSupervisingDeputyApproval => detail.SupervisingDeputyId == actorId,
            OutgoingLetterPhase.AwaitingFirstDeputyApproval => detail.FirstDeputyId == actorId,
            OutgoingLetterPhase.AwaitingGeneralDirectorApproval => detail.GeneralDirectorId == actorId,
            _ => false
        };
        if (!allowed) return Result<OutgoingLetterDto>.Fail("Access denied");

        apply(detail);
        detail.Phase = to;
        detail.Document.UpdatedAt = DateTime.UtcNow;
        await AddActivityAsync(detail.Document, actorId, action, null, detail.Document.Status, null, ct);
        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    private async Task<Result<OutgoingLetterDto>> RejectAsync(
        Guid id, OutgoingRevisionRequest request, Guid actorId, string? ip,
        Func<OutgoingLetterDetail, bool> canReject, CancellationToken ct)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<OutgoingLetterDto>.Fail("Letter not found");
        if (string.IsNullOrWhiteSpace(request.Comment))
            return Result<OutgoingLetterDto>.Fail("Revision comment is required");
        if (!canReject(detail)) return Result<OutgoingLetterDto>.Fail("Access denied");

        detail.RevisionNotes = request.Comment.Trim();
        detail.Phase = OutgoingLetterPhase.NeedsRevision;
        detail.Document.Status = DocumentStatus.Draft;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "revision_requested", null, DocumentStatus.Draft,
            request.Comment.Trim(), ct);
        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    private async Task<User?> ValidateTopManagerAsync(Guid id, CancellationToken ct) =>
        await db.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsActive && u.Role == UserRole.HOTopManager, ct);

    private async Task<OutgoingLetterDetail?> LoadDetailAsync(Guid id, CancellationToken ct) =>
        await db.OutgoingLetterDetails.AsNoTracking()
            .Include(d => d.Document).ThenInclude(doc => doc.Author)
            .Include(d => d.Document).ThenInclude(doc => doc.Department)
            .Include(d => d.DeptHead).Include(d => d.SupervisingDeputy)
            .Include(d => d.FirstDeputy).Include(d => d.GeneralDirector)
            .Include(d => d.Coordinators).ThenInclude(c => c.User)
            .Include(d => d.Comments).ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(d => d.DocumentId == id, ct);

    private async Task<OutgoingLetterDetail?> LoadDetailTrackedAsync(Guid id, CancellationToken ct) =>
        await db.OutgoingLetterDetails
            .Include(d => d.Document).ThenInclude(doc => doc.Author)
            .Include(d => d.Document).ThenInclude(doc => doc.Department)
            .Include(d => d.Coordinators)
            .FirstOrDefaultAsync(d => d.DocumentId == id, ct);

    private async Task<OutgoingLetterDto> MapDetailAsync(OutgoingLetterDetail d, CancellationToken ct)
    {
        string? ticketNumber = null;
        if (d.HelpDeskTicketId.HasValue)
        {
            ticketNumber = await db.Tickets.AsNoTracking()
                .Where(t => t.Id == d.HelpDeskTicketId.Value)
                .Select(t => t.Number)
                .FirstOrDefaultAsync(ct);
        }

        return new OutgoingLetterDto(
            d.DocumentId, d.Document.Number, d.Document.Title, d.Document.TitleRu,
            d.Document.Status.ToString(), d.Phase, d.Document.AuthorId, d.Document.Author.FullName,
            d.Document.ReceiverName, d.Document.AttachmentFileName, d.Document.ExternalReference,
            d.TranslatedAttachmentFileName, d.TranslatedAttachmentStorageKey,
            d.Document.OrganizationId, d.Document.DepartmentId,
            d.Document.Department.Name, d.Document.Department.NameEn,
            d.RequiresTranslation, d.SourceLanguage, TranslationLanguages.Parse(d.TranslatingLanguage),
            d.HelpDeskTicketId, ticketNumber,
            d.DeptHeadId, d.DeptHead?.FullName,
            d.SupervisingDeputyId, d.SupervisingDeputy?.FullName,
            d.FirstDeputyId, d.FirstDeputy?.FullName,
            d.GeneralDirectorId, d.GeneralDirector?.FullName,
            d.RevisionNotes,
            d.SentToTranslationAt, d.TranslationReturnedAt, d.SubmittedToEdsAt, d.DeptHeadApprovedAt,
            d.RegisteredAt, d.DispatchedAt, d.CompletedAt,
            d.Coordinators.OrderBy(c => c.CoordinatedAt).Select(c => new OutgoingLetterCoordinatorDto(
                c.Id, c.UserId, c.User.FullName, c.ForDepartment, c.CoordinatedAt)).ToList(),
            d.Comments.OrderBy(c => c.CreatedAt).Select(c => new OutgoingLetterCommentDto(
                c.Id, c.AuthorId, c.Author.FullName, c.Body, c.CreatedAt)).ToList(),
            d.Document.CreatedAt, d.Document.UpdatedAt);
    }

    private static string BuildTranslationDescription(
        OutgoingLetterDetail detail, string sourceLanguage, IReadOnlyList<string> targetLanguages) =>
        string.Join(Environment.NewLine, new[]
        {
            "Outgoing letter translation request (DCS)",
            $"Document: {detail.Document.Number}",
            $"Subject: {detail.Document.Title}",
            $"Source language: {sourceLanguage}",
            $"Target language(s): {string.Join(", ", targetLanguages)}",
            detail.Document.ReceiverName is not null ? $"Addressee: {detail.Document.ReceiverName}" : null,
            detail.Document.AttachmentFileName is not null ? $"Attachment: {detail.Document.AttachmentFileName}" : null,
        }.Where(l => l is not null));

    private async Task<string> GenerateHelpDeskNumberAsync(CancellationToken ct)
    {
        var prefix = $"HD-{DateTime.UtcNow.Year}-";
        var last = await db.Tickets.Where(t => t.Number.StartsWith(prefix))
            .OrderByDescending(t => t.Number).Select(t => t.Number).FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var n)) seq = n + 1;
        return $"{prefix}{seq:D5}";
    }

    private async Task<string> GeneratePendingNumberAsync(CancellationToken ct)
    {
        var prefix = $"OUT-PEND-{DateTime.UtcNow.Year}-";
        var last = await db.Documents.Where(d => d.Number.StartsWith(prefix))
            .OrderByDescending(d => d.Number).Select(d => d.Number).FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var n)) seq = n + 1;
        return $"{prefix}{seq:D4}";
    }

    private async Task<string> GenerateFormalNumberAsync(CancellationToken ct)
    {
        var prefix = $"{DcsRouting.NumberPrefix(DocumentType.Outgoing)}-{DateTime.UtcNow.Year}-";
        var last = await db.Documents.Where(d => d.Number.StartsWith(prefix) && !d.Number.Contains("PEND"))
            .OrderByDescending(d => d.Number).Select(d => d.Number).FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var n)) seq = n + 1;
        return $"{prefix}{seq:D4}";
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
            Source = TaskSource.DCS,
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
        var prefix = $"TSK-{DateTime.UtcNow.Year}-";
        var last = await db.WorkTasks.Where(t => t.Number.StartsWith(prefix))
            .OrderByDescending(t => t.Number).Select(t => t.Number).FirstOrDefaultAsync(ct);
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
        await db.Users.Include(u => u.Organization).Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    private static OutgoingLetterUserDto MapUser(User u) => new(
        u.Id, u.FullName, u.Email, u.EmployeeId,
        u.Department?.Name ?? "", u.Department?.NameEn ?? "");

    private static bool IsRegistrar(User u) =>
        string.Equals(u.Email, DcsRouting.IncomingRegistrarEmail, StringComparison.OrdinalIgnoreCase)
        || u.Role == UserRole.SuperAdmin;

    private static bool IsTranslationDept(User u) =>
        u.Department?.Code == TranslationDept || u.Role == UserRole.SuperAdmin;

    private static bool CanCreate(User u) =>
        u.Organization.Code == HoMasterData.OrganizationCode || u.Role == UserRole.SuperAdmin;

    private static bool CanView(User actor, OutgoingLetterDetail? detail)
    {
        if (detail is null) return CanCreate(actor);
        if (detail.Document.AuthorId == actor.Id) return true;
        if (IsRegistrar(actor) || actor.Role == UserRole.SuperAdmin) return true;
        if (IsTranslationDept(actor)) return true;
        if (detail.DeptHeadId == actor.Id) return true;
        if (detail.SupervisingDeputyId == actor.Id) return true;
        if (detail.FirstDeputyId == actor.Id) return true;
        if (detail.GeneralDirectorId == actor.Id) return true;
        if (detail.Coordinators.Any(c => c.UserId == actor.Id)) return true;
        if (actor.Role == UserRole.HOTopManager) return true;
        return false;
    }
}
