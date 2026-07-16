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

public class OrderService(AppDbContext db, IAuditService audit) : IOrderService
{
    private const string LegalDept = "HO-LEGAL";

    public async Task<Result<OrderPermissionsDto>> GetPermissionsAsync(
        Guid actorId, Guid? documentId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<OrderPermissionsDto>.Fail("User not found");

        OrderDetail? detail = null;
        if (documentId.HasValue)
            detail = await LoadDetailAsync(documentId.Value, ct);

        var phase = detail?.Phase ?? OrderPhase.Draft;
        var isInitiator = detail?.Document.AuthorId == actorId;
        var isRegistrar = IsRegistrar(actor);
        var isDeptHead = detail?.DeptHeadId == actorId;
        var isLegalHead = detail?.LegalHeadId == actorId;
        var isSupervisingDeputy = detail?.SupervisingDeputyId == actorId;
        var isFirstDeputy = detail?.FirstDeputyId == actorId;
        var isGeneralDirector = detail?.GeneralDirectorId == actorId;

        return Result<OrderPermissionsDto>.Ok(new OrderPermissionsDto(
            isInitiator,
            isRegistrar,
            isDeptHead,
            isLegalHead,
            isSupervisingDeputy,
            isFirstDeputy,
            isGeneralDirector,
            CanCreate(actor),
            isInitiator && phase is OrderPhase.Draft or OrderPhase.NeedsRevision,
            isInitiator && phase is OrderPhase.Draft or OrderPhase.NeedsRevision,
            isDeptHead && phase == OrderPhase.AwaitingDeptHeadApproval,
            isDeptHead && phase == OrderPhase.AwaitingDeptHeadApproval,
            isInitiator && phase == OrderPhase.SpecialistCoordination,
            isInitiator && phase == OrderPhase.DepartmentCoordination,
            isLegalHead && phase == OrderPhase.AwaitingLegalApproval,
            isLegalHead && phase == OrderPhase.AwaitingLegalApproval,
            isSupervisingDeputy && phase == OrderPhase.AwaitingSupervisingDeputyApproval,
            isFirstDeputy && phase == OrderPhase.AwaitingFirstDeputyApproval,
            isGeneralDirector && phase == OrderPhase.AwaitingGeneralDirectorApproval,
            (isSupervisingDeputy && phase == OrderPhase.AwaitingSupervisingDeputyApproval)
            || (isFirstDeputy && phase == OrderPhase.AwaitingFirstDeputyApproval)
            || (isGeneralDirector && phase == OrderPhase.AwaitingGeneralDirectorApproval),
            isInitiator && phase == OrderPhase.EdsFinalized,
            isInitiator && phase == OrderPhase.AwaitingRegistration,
            isRegistrar && phase == OrderPhase.AwaitingRegistration,
            isRegistrar && phase == OrderPhase.AwaitingPaperSignature,
            isRegistrar && phase == OrderPhase.AwaitingScanUpload,
            isInitiator && phase == OrderPhase.AwaitingDistribution,
            isRegistrar && phase == OrderPhase.AwaitingArchive,
            CanView(actor, detail)));
    }

    public async Task<Result<IReadOnlyList<OrderUserDto>>> GetDeptHeadsAsync(
        Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<IReadOnlyList<OrderUserDto>>.Fail("User not found");

        var users = await db.Users.AsNoTracking()
            .Include(u => u.Department)
            .Where(u => u.IsActive && u.Role == UserRole.HONachalnik
                && u.Organization.Code == HoMasterData.OrganizationCode)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);

        return Result<IReadOnlyList<OrderUserDto>>.Ok(users.Select(MapUser).ToList());
    }

    public async Task<Result<IReadOnlyList<OrderUserDto>>> GetTopManagersAsync(
        Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<IReadOnlyList<OrderUserDto>>.Fail("User not found");

        var users = await db.Users.AsNoTracking()
            .Include(u => u.Department)
            .Where(u => u.IsActive && u.Role == UserRole.HOTopManager
                && u.Organization.Code == HoMasterData.OrganizationCode)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);

        return Result<IReadOnlyList<OrderUserDto>>.Ok(users.Select(MapUser).ToList());
    }

    public async Task<Result<IReadOnlyList<OrderUserDto>>> GetCoordinatorsAsync(
        Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<IReadOnlyList<OrderUserDto>>.Fail("User not found");

        var users = await db.Users.AsNoTracking()
            .Include(u => u.Department)
            .Where(u => u.IsActive && u.Organization.Code == HoMasterData.OrganizationCode
                && (u.Role == UserRole.HOEngineer || u.Role == UserRole.HONachalnik))
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);

        return Result<IReadOnlyList<OrderUserDto>>.Ok(users.Select(MapUser).ToList());
    }

    public async Task<Result<IReadOnlyList<OrderUserDto>>> GetDistributionTargetsAsync(
        Guid documentId, Guid actorId, CancellationToken ct = default)
    {
        var detail = await LoadDetailAsync(documentId, ct);
        if (detail is null) return Result<IReadOnlyList<OrderUserDto>>.Fail("Order not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanView(actor, detail))
            return Result<IReadOnlyList<OrderUserDto>>.Fail("Access denied");

        var coordinatorIds = detail.Coordinators
            .Select(c => c.UserId)
            .Distinct()
            .ToList();

        var managerIds = await db.Users.AsNoTracking()
            .Where(u => u.IsActive
                && u.Role == UserRole.HOTopManager
                && u.Organization.Code == HoMasterData.OrganizationCode)
            .Select(u => u.Id)
            .ToListAsync(ct);

        var userIds = coordinatorIds
            .Concat(managerIds)
            .Distinct()
            .ToList();

        if (userIds.Count == 0) return Result<IReadOnlyList<OrderUserDto>>.Ok([]);

        var users = await db.Users.AsNoTracking()
            .Include(u => u.Department)
            .Where(u => userIds.Contains(u.Id) && u.IsActive)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);

        return Result<IReadOnlyList<OrderUserDto>>.Ok(users.Select(MapUser).ToList());
    }

    public async Task<Result<OrderDto>> GetByIdAsync(Guid id, Guid actorId, CancellationToken ct = default)
    {
        var detail = await LoadDetailAsync(id, ct);
        if (detail is null) return Result<OrderDto>.Fail("Order not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanView(actor, detail))
            return Result<OrderDto>.Fail("Access denied");

        return Result<OrderDto>.Ok(await MapDetailAsync(detail, ct));
    }

    public async Task<Result<OrderDto>> CreateAsync(
        CreateOrderRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<OrderDto>.Fail("User not found");
        if (!CanCreate(actor))
            return Result<OrderDto>.Fail("Access denied");
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result<OrderDto>.Fail("Title is required");
        if (actor.DepartmentId is null)
            return Result<OrderDto>.Fail("Department is required");

        var number = await GeneratePendingNumberAsync(ct);
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            Number = number,
            Title = request.Title.Trim(),
            TitleRu = request.TitleRu?.Trim(),
            Type = DocumentType.Order,
            Status = DocumentStatus.Draft,
            AuthorId = actorId,
            OrganizationId = actor.OrganizationId,
            DepartmentId = actor.DepartmentId.Value,
            AttachmentFileName = request.AttachmentFileName?.Trim(),
            ExternalReference = request.AttachmentStorageKey?.Trim(),
        };

        var detail = new OrderDetail
        {
            DocumentId = doc.Id,
            Document = doc,
            Phase = OrderPhase.Draft,
        };

        db.Documents.Add(doc);
        db.OrderDetails.Add(detail);
        await AddActivityAsync(doc, actorId, "order_draft_created", null, DocumentStatus.Draft, null, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "OrderCreated", "Document", doc.Id, number, ip, ct);

        return await GetByIdAsync(doc.Id, actorId, ct);
    }

    public async Task<Result<OrderDto>> UpdateDraftAsync(
        Guid id, CreateOrderRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<OrderDto>.Fail("Order not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || detail.Document.AuthorId != actorId)
            return Result<OrderDto>.Fail("Access denied");
        if (detail.Phase is not (OrderPhase.Draft or OrderPhase.NeedsRevision))
            return Result<OrderDto>.Fail("Order is not editable");
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result<OrderDto>.Fail("Title is required");

        detail.Document.Title = request.Title.Trim();
        detail.Document.TitleRu = request.TitleRu?.Trim();
        detail.Document.AttachmentFileName = request.AttachmentFileName?.Trim();
        detail.Document.ExternalReference = request.AttachmentStorageKey?.Trim();
        detail.RevisionNotes = null;
        detail.Phase = OrderPhase.Draft;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "OrderDraftUpdated", "Document", id, detail.Document.Number, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<OrderDto>> SubmitForApprovalAsync(
        Guid id, SubmitOrderRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<OrderDto>.Fail("Order not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || detail.Document.AuthorId != actorId)
            return Result<OrderDto>.Fail("Access denied");
        if (detail.Phase is not (OrderPhase.Draft or OrderPhase.NeedsRevision))
            return Result<OrderDto>.Fail("Order is not ready for approval");

        var deptHead = await db.Users.FirstOrDefaultAsync(u =>
            u.Id == request.DeptHeadId && u.IsActive && u.Role == UserRole.HONachalnik, ct);
        if (deptHead is null) return Result<OrderDto>.Fail("Invalid department head");

        var supervisingDeputy = await ValidateTopManagerAsync(request.SupervisingDeputyId, ct);
        if (supervisingDeputy is null) return Result<OrderDto>.Fail("Invalid supervising deputy");
        var firstDeputy = await ValidateTopManagerAsync(request.FirstDeputyId, ct);
        if (firstDeputy is null) return Result<OrderDto>.Fail("Invalid first deputy");
        var generalDirector = await ValidateTopManagerAsync(request.GeneralDirectorId, ct);
        if (generalDirector is null) return Result<OrderDto>.Fail("Invalid general director");

        detail.DeptHeadId = deptHead.Id;
        detail.SupervisingDeputyId = supervisingDeputy.Id;
        detail.FirstDeputyId = firstDeputy.Id;
        detail.GeneralDirectorId = generalDirector.Id;
        detail.SubmittedAt = DateTime.UtcNow;
        detail.Phase = OrderPhase.AwaitingDeptHeadApproval;
        detail.Document.Status = DocumentStatus.InReview;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await CreateLinkedTaskAsync(
            deptHead.Id, actorId, deptHead.DepartmentId ?? detail.Document.DepartmentId, deptHead.OrganizationId,
            $"Order {detail.Document.Number} - dept head approval",
            detail.Document.Title, detail.DocumentId, ct);

        var detailsText = request.RequiresSpecialistCoordination
            ? $"dept head: {deptHead.FullName}; specialist coordination: yes"
            : $"dept head: {deptHead.FullName}; specialist coordination: no";
        await AddActivityAsync(detail.Document, actorId, "submitted_for_approval", DocumentStatus.Draft,
            DocumentStatus.InReview, detailsText, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "OrderSubmittedForApproval", "Document", id, deptHead.FullName, ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<OrderDto>> AddCoordinatorsAsync(
        Guid id, OrderCoordinatorRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<OrderDto>.Fail("Order not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || detail.Document.AuthorId != actorId)
            return Result<OrderDto>.Fail("Access denied");

        var expectedPhase = request.ForDepartment
            ? OrderPhase.DepartmentCoordination
            : OrderPhase.SpecialistCoordination;
        if (detail.Phase != expectedPhase)
            return Result<OrderDto>.Fail("Invalid coordination phase");

        foreach (var userId in request.UserIds.Distinct())
        {
            if (detail.Coordinators.Any(c => c.UserId == userId && c.ForDepartment == request.ForDepartment)) continue;

            var user = await db.Users.FirstOrDefaultAsync(u =>
                u.Id == userId && u.IsActive &&
                (u.Role == UserRole.HOEngineer || u.Role == UserRole.HONachalnik || u.Role == UserRole.HOTopManager), ct);
            if (user is null) return Result<OrderDto>.Fail("Invalid coordinator selected");

            detail.Coordinators.Add(new OrderCoordinator
            {
                Id = Guid.NewGuid(),
                DocumentId = detail.DocumentId,
                UserId = userId,
                ForDepartment = request.ForDepartment,
            });

            await CreateLinkedTaskAsync(
                user.Id, actorId, user.DepartmentId ?? detail.Document.DepartmentId, user.OrganizationId,
                request.ForDepartment
                    ? $"Order {detail.Document.Number} - department coordination"
                    : $"Order {detail.Document.Number} - specialist coordination",
                detail.Document.Title,
                detail.DocumentId, ct);
        }

        detail.Document.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "OrderCoordinatorsAdded", "Document", id, null, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<OrderDto>> CompleteSpecialistCoordinationAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<OrderDto>.Fail("Order not found");
        if (detail.Document.AuthorId != actorId) return Result<OrderDto>.Fail("Access denied");
        if (detail.Phase != OrderPhase.SpecialistCoordination)
            return Result<OrderDto>.Fail("Invalid phase");

        detail.Phase = OrderPhase.DepartmentCoordination;
        detail.Document.UpdatedAt = DateTime.UtcNow;
        await AddActivityAsync(detail.Document, actorId, "specialist_coordination_completed", null, detail.Document.Status, null, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "OrderSpecialistCoordinationCompleted", "Document", id, null, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<OrderDto>> CompleteDepartmentCoordinationAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<OrderDto>.Fail("Order not found");
        if (detail.Document.AuthorId != actorId) return Result<OrderDto>.Fail("Access denied");
        if (detail.Phase != OrderPhase.DepartmentCoordination)
            return Result<OrderDto>.Fail("Invalid phase");

        var legalHead = await ResolveLegalHeadAsync(ct);
        if (legalHead is null)
            return Result<OrderDto>.Fail($"Legal department head ({LegalDept}) not found");

        detail.CoordinationCompletedAt = DateTime.UtcNow;
        detail.LegalHeadId = legalHead.Id;
        detail.Phase = OrderPhase.AwaitingLegalApproval;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await CreateLinkedTaskAsync(
            legalHead.Id, actorId, legalHead.DepartmentId ?? detail.Document.DepartmentId, legalHead.OrganizationId,
            $"Order {detail.Document.Number} - legal approval",
            detail.Document.Title,
            detail.DocumentId, ct);

        await AddActivityAsync(detail.Document, actorId, "department_coordination_completed", null, detail.Document.Status, legalHead.FullName, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "OrderDepartmentCoordinationCompleted", "Document", id, legalHead.FullName, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public Task<Result<OrderDto>> ApproveDeptHeadAsync(
        Guid id, OrderApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default)
        => ApproveAndRouteAsync(
            id, request, actorId, ip,
            OrderPhase.AwaitingDeptHeadApproval,
            OrderPhase.SpecialistCoordination,
            d => d.DeptHeadApprovedAt = DateTime.UtcNow,
            "dept_head_approved",
            _ => null,
            ct);

    public Task<Result<OrderDto>> RejectDeptHeadAsync(
        Guid id, OrderRevisionRequest request, Guid actorId, string? ip, CancellationToken ct = default)
        => RejectAsync(id, request, actorId, ip,
            d => d.Phase == OrderPhase.AwaitingDeptHeadApproval && d.DeptHeadId == actorId,
            "dept_head_rejected", ct);

    public Task<Result<OrderDto>> ApproveLegalAsync(
        Guid id, OrderApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default)
        => ApproveAndRouteAsync(
            id, request, actorId, ip,
            OrderPhase.AwaitingLegalApproval,
            OrderPhase.AwaitingSupervisingDeputyApproval,
            d => d.LegalApprovedAt = DateTime.UtcNow,
            "legal_approved",
            d => d.SupervisingDeputyId,
            ct);

    public Task<Result<OrderDto>> RejectLegalAsync(
        Guid id, OrderRevisionRequest request, Guid actorId, string? ip, CancellationToken ct = default)
        => RejectAsync(id, request, actorId, ip,
            d => d.Phase == OrderPhase.AwaitingLegalApproval && d.LegalHeadId == actorId,
            "legal_rejected", ct);

    public Task<Result<OrderDto>> ApproveSupervisingDeputyAsync(
        Guid id, OrderApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default)
        => ApproveAndRouteAsync(
            id, request, actorId, ip,
            OrderPhase.AwaitingSupervisingDeputyApproval,
            OrderPhase.AwaitingFirstDeputyApproval,
            d => d.SupervisingDeputyApprovedAt = DateTime.UtcNow,
            "supervising_deputy_approved",
            d => d.FirstDeputyId,
            ct);

    public Task<Result<OrderDto>> ApproveFirstDeputyAsync(
        Guid id, OrderApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default)
        => ApproveAndRouteAsync(
            id, request, actorId, ip,
            OrderPhase.AwaitingFirstDeputyApproval,
            OrderPhase.AwaitingGeneralDirectorApproval,
            d => d.FirstDeputyApprovedAt = DateTime.UtcNow,
            "first_deputy_approved",
            d => d.GeneralDirectorId,
            ct);

    public Task<Result<OrderDto>> ApproveGeneralDirectorAsync(
        Guid id, OrderApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default)
        => ApproveAndRouteAsync(
            id, request, actorId, ip,
            OrderPhase.AwaitingGeneralDirectorApproval,
            OrderPhase.EdsFinalized,
            d => d.GeneralDirectorApprovedAt = DateTime.UtcNow,
            "general_director_approved",
            _ => null,
            ct);

    public async Task<Result<OrderDto>> RejectApprovalAsync(
        Guid id, OrderRevisionRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        return await RejectAsync(id, request, actorId, ip, d =>
            d.Phase switch
            {
                OrderPhase.AwaitingSupervisingDeputyApproval => d.SupervisingDeputyId == actorId,
                OrderPhase.AwaitingFirstDeputyApproval => d.FirstDeputyId == actorId,
                OrderPhase.AwaitingGeneralDirectorApproval => d.GeneralDirectorId == actorId,
                _ => false
            }, "approval_rejected", ct);
    }

    public async Task<Result<OrderDto>> FinalizeEdsAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<OrderDto>.Fail("Order not found");
        if (detail.Document.AuthorId != actorId) return Result<OrderDto>.Fail("Access denied");
        if (detail.Phase != OrderPhase.EdsFinalized)
            return Result<OrderDto>.Fail("EDS approval is not finalized yet");

        detail.EdsFinalizedAt = DateTime.UtcNow;
        detail.Phase = OrderPhase.AwaitingRegistration;
        detail.Document.UpdatedAt = DateTime.UtcNow;
        await AddActivityAsync(detail.Document, actorId, "eds_finalized", null, detail.Document.Status, null, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "OrderEdsFinalized", "Document", id, null, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<OrderDto>> SendToRegistrarAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<OrderDto>.Fail("Order not found");
        if (detail.Document.AuthorId != actorId) return Result<OrderDto>.Fail("Access denied");
        if (detail.Phase != OrderPhase.AwaitingRegistration)
            return Result<OrderDto>.Fail("Order is not ready for registrar");

        detail.SentToRegistrarAt = DateTime.UtcNow;
        detail.Document.UpdatedAt = DateTime.UtcNow;
        await AddActivityAsync(detail.Document, actorId, "sent_to_registrar", null, detail.Document.Status, null, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "OrderSentToRegistrar", "Document", id, null, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<OrderDto>> RegisterAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<OrderDto>.Fail("Order not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsRegistrar(actor))
            return Result<OrderDto>.Fail("Access denied");
        if (detail.Phase != OrderPhase.AwaitingRegistration)
            return Result<OrderDto>.Fail("Order is not awaiting registration");

        detail.Document.Number = await GenerateFormalNumberAsync(ct);
        detail.Document.Status = DocumentStatus.Registered;
        detail.Document.RegisteredAt = DateTime.UtcNow;
        detail.RegisteredAt = DateTime.UtcNow;
        detail.Phase = OrderPhase.AwaitingPaperSignature;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "registered", DocumentStatus.InReview,
            DocumentStatus.Registered, detail.Document.Number, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "OrderRegistered", "Document", id, detail.Document.Number, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public Task<Result<OrderDto>> ConfirmPaperSignatureAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
        => RegistrarStepAsync(id, actorId, ip, OrderPhase.AwaitingPaperSignature, OrderPhase.AwaitingScanUpload,
            d => d.PaperSignedAt = DateTime.UtcNow, "paper_signature_confirmed", ct);

    public async Task<Result<OrderDto>> UploadScanAsync(
        Guid id, OrderScanUploadRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<OrderDto>.Fail("Order not found");
        if (string.IsNullOrWhiteSpace(request.FileName) || string.IsNullOrWhiteSpace(request.StorageKey))
            return Result<OrderDto>.Fail("Scan file and storage key are required");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsRegistrar(actor))
            return Result<OrderDto>.Fail("Access denied");
        if (detail.Phase != OrderPhase.AwaitingScanUpload)
            return Result<OrderDto>.Fail("Order is not awaiting scan upload");

        detail.ScanAttachmentFileName = request.FileName.Trim();
        detail.ScanAttachmentStorageKey = request.StorageKey.Trim();
        detail.ScanUploadedAt = DateTime.UtcNow;
        detail.Phase = OrderPhase.AwaitingDistribution;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "scan_uploaded", null, detail.Document.Status, request.FileName.Trim(), ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "OrderScanUploaded", "Document", id, request.FileName.Trim(), ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<OrderDto>> DistributeAsync(
        Guid id, OrderDistributionRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<OrderDto>.Fail("Order not found");
        if (detail.Document.AuthorId != actorId) return Result<OrderDto>.Fail("Access denied");
        if (detail.Phase != OrderPhase.AwaitingDistribution)
            return Result<OrderDto>.Fail("Order is not awaiting distribution");
        if (request.UserIds is null || request.UserIds.Count == 0)
            return Result<OrderDto>.Fail("At least one recipient is required");

        var allowedTargets = await GetDistributionTargetIdsAsync(detail, ct);
        foreach (var userId in request.UserIds.Distinct())
        {
            if (!allowedTargets.Contains(userId))
                return Result<OrderDto>.Fail("Invalid distribution target selected");

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, ct);
            if (user is null) return Result<OrderDto>.Fail("Recipient not found");

            var existing = detail.Recipients.FirstOrDefault(r => r.UserId == userId);
            if (existing is null)
            {
                var task = await CreateLinkedTaskAsync(
                    user.Id, actorId, user.DepartmentId ?? detail.Document.DepartmentId, user.OrganizationId,
                    $"Order {detail.Document.Number} - distribution",
                    detail.Document.Title,
                    detail.DocumentId, ct);

                detail.Recipients.Add(new OrderRecipient
                {
                    Id = Guid.NewGuid(),
                    DocumentId = detail.DocumentId,
                    UserId = userId,
                    NotifiedAt = DateTime.UtcNow,
                    TaskId = task.Id,
                });
            }
            else
            {
                existing.NotifiedAt = existing.NotifiedAt ?? DateTime.UtcNow;
            }
        }

        detail.DistributedAt = DateTime.UtcNow;
        detail.Phase = OrderPhase.AwaitingArchive;
        detail.Document.Status = DocumentStatus.Approved;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "distributed", DocumentStatus.Registered,
            DocumentStatus.Approved, $"{request.UserIds.Distinct().Count()} recipient(s)", ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "OrderDistributed", "Document", id, null, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public Task<Result<OrderDto>> ArchiveAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
        => RegistrarStepAsync(id, actorId, ip, OrderPhase.AwaitingArchive, OrderPhase.Completed, d =>
        {
            d.ArchivedAt = DateTime.UtcNow;
            d.CompletedAt = DateTime.UtcNow;
        }, "archived", ct);

    public async Task<Result<OrderCommentDto>> AddCommentAsync(
        Guid id, OrderCommentRequest request, Guid actorId, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<OrderCommentDto>.Fail("Order not found");
        if (string.IsNullOrWhiteSpace(request.Body))
            return Result<OrderCommentDto>.Fail("Comment is required");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanView(actor, detail))
            return Result<OrderCommentDto>.Fail("Access denied");

        var comment = new OrderComment
        {
            Id = Guid.NewGuid(),
            DocumentId = detail.DocumentId,
            AuthorId = actorId,
            Body = request.Body.Trim(),
        };
        detail.Comments.Add(comment);
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "OrderCommentAdded", "Document", id, null, null, ct);

        return Result<OrderCommentDto>.Ok(new OrderCommentDto(
            comment.Id,
            comment.AuthorId,
            actor.FullName,
            comment.Body,
            comment.CreatedAt));
    }

    private async Task<Result<OrderDto>> RegistrarStepAsync(
        Guid id, Guid actorId, string? ip,
        OrderPhase from, OrderPhase to,
        Action<OrderDetail> apply, string action, CancellationToken ct)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<OrderDto>.Fail("Order not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsRegistrar(actor))
            return Result<OrderDto>.Fail("Access denied");
        if (detail.Phase != from) return Result<OrderDto>.Fail("Invalid phase");

        apply(detail);
        detail.Phase = to;
        if (to == OrderPhase.Completed)
            detail.Document.Status = DocumentStatus.Archived;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, action, null, detail.Document.Status, null, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, $"Order{action}", "Document", id, null, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    private async Task<Result<OrderDto>> ApproveAndRouteAsync(
        Guid id, OrderApprovalRequest request, Guid actorId, string? ip,
        OrderPhase from, OrderPhase to,
        Action<OrderDetail> apply, string action,
        Func<OrderDetail, Guid?> nextApproverSelector,
        CancellationToken ct)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<OrderDto>.Fail("Order not found");
        if (detail.Phase != from) return Result<OrderDto>.Fail("Invalid phase");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<OrderDto>.Fail("Access denied");

        var canApprove = from switch
        {
            OrderPhase.AwaitingDeptHeadApproval => detail.DeptHeadId == actorId,
            OrderPhase.AwaitingLegalApproval => detail.LegalHeadId == actorId,
            OrderPhase.AwaitingSupervisingDeputyApproval => detail.SupervisingDeputyId == actorId,
            OrderPhase.AwaitingFirstDeputyApproval => detail.FirstDeputyId == actorId,
            OrderPhase.AwaitingGeneralDirectorApproval => detail.GeneralDirectorId == actorId,
            _ => false
        };
        if (!canApprove) return Result<OrderDto>.Fail("Access denied");

        if (!string.IsNullOrWhiteSpace(request.Comment))
        {
            detail.Comments.Add(new OrderComment
            {
                Id = Guid.NewGuid(),
                DocumentId = detail.DocumentId,
                AuthorId = actorId,
                Body = request.Comment.Trim(),
            });
        }

        apply(detail);
        detail.Phase = to;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        var nextApproverId = nextApproverSelector(detail);
        if (nextApproverId.HasValue)
        {
            var nextApprover = await db.Users.FirstOrDefaultAsync(u => u.Id == nextApproverId.Value && u.IsActive, ct);
            if (nextApprover is null) return Result<OrderDto>.Fail("Next approver not found");

            await CreateLinkedTaskAsync(
                nextApprover.Id, actorId, nextApprover.DepartmentId ?? detail.Document.DepartmentId, nextApprover.OrganizationId,
                BuildApprovalTaskTitle(detail.Document.Number, to),
                detail.Document.Title,
                detail.DocumentId, ct);
        }

        await AddActivityAsync(detail.Document, actorId, action, null, detail.Document.Status, request.Comment?.Trim(), ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, $"Order{action}", "Document", id, null, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    private async Task<Result<OrderDto>> RejectAsync(
        Guid id, OrderRevisionRequest request, Guid actorId, string? ip,
        Func<OrderDetail, bool> canReject, string action, CancellationToken ct)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<OrderDto>.Fail("Order not found");
        if (string.IsNullOrWhiteSpace(request.Comment))
            return Result<OrderDto>.Fail("Revision comment is required");
        if (!canReject(detail)) return Result<OrderDto>.Fail("Access denied");

        var text = request.Comment.Trim();
        detail.RevisionNotes = text;
        detail.Phase = OrderPhase.NeedsRevision;
        detail.Document.Status = DocumentStatus.Draft;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        detail.Comments.Add(new OrderComment
        {
            Id = Guid.NewGuid(),
            DocumentId = detail.DocumentId,
            AuthorId = actorId,
            Body = text,
        });

        await AddActivityAsync(detail.Document, actorId, action, null, DocumentStatus.Draft, text, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, $"Order{action}", "Document", id, text, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    private static string BuildApprovalTaskTitle(string number, OrderPhase phase) =>
        phase switch
        {
            OrderPhase.AwaitingSupervisingDeputyApproval => $"Order {number} - supervising deputy approval",
            OrderPhase.AwaitingFirstDeputyApproval => $"Order {number} - first deputy approval",
            OrderPhase.AwaitingGeneralDirectorApproval => $"Order {number} - general director approval",
            _ => $"Order {number} - approval",
        };

    private async Task<HashSet<Guid>> GetDistributionTargetIdsAsync(OrderDetail detail, CancellationToken ct)
    {
        var coordinatorIds = detail.Coordinators
            .Select(c => c.UserId)
            .Distinct()
            .ToList();

        var managerIds = await db.Users.AsNoTracking()
            .Where(u => u.IsActive
                && u.Role == UserRole.HOTopManager
                && u.Organization.Code == HoMasterData.OrganizationCode)
            .Select(u => u.Id)
            .ToListAsync(ct);

        return coordinatorIds.Concat(managerIds).ToHashSet();
    }

    private async Task<User?> ResolveLegalHeadAsync(CancellationToken ct) =>
        await db.Users
            .Include(u => u.Organization)
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u =>
                u.IsActive
                && u.Role == UserRole.HONachalnik
                && u.Organization.Code == HoMasterData.OrganizationCode
                && u.Department != null
                && u.Department.Code == LegalDept, ct);

    private async Task<User?> ValidateTopManagerAsync(Guid id, CancellationToken ct) =>
        await db.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsActive && u.Role == UserRole.HOTopManager, ct);

    private async Task<OrderDetail?> LoadDetailAsync(Guid id, CancellationToken ct) =>
        await db.OrderDetails.AsNoTracking()
            .Include(d => d.Document).ThenInclude(doc => doc.Author)
            .Include(d => d.Document).ThenInclude(doc => doc.Department)
            .Include(d => d.DeptHead).Include(d => d.LegalHead)
            .Include(d => d.SupervisingDeputy).Include(d => d.FirstDeputy).Include(d => d.GeneralDirector)
            .Include(d => d.Coordinators).ThenInclude(c => c.User)
            .Include(d => d.Recipients).ThenInclude(r => r.User)
            .Include(d => d.Comments).ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(d => d.DocumentId == id, ct);

    private async Task<OrderDetail?> LoadDetailTrackedAsync(Guid id, CancellationToken ct) =>
        await db.OrderDetails
            .Include(d => d.Document).ThenInclude(doc => doc.Author)
            .Include(d => d.Document).ThenInclude(doc => doc.Department)
            .Include(d => d.Coordinators)
            .Include(d => d.Recipients)
            .Include(d => d.Comments)
            .FirstOrDefaultAsync(d => d.DocumentId == id, ct);

    private Task<OrderDto> MapDetailAsync(OrderDetail d, CancellationToken ct) =>
        Task.FromResult(new OrderDto(
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
            d.ScanAttachmentFileName,
            d.ScanAttachmentStorageKey,
            d.Document.OrganizationId,
            d.Document.DepartmentId,
            d.Document.Department.Name,
            d.Document.Department.NameEn,
            d.DeptHeadId,
            d.DeptHead?.FullName,
            d.LegalHeadId,
            d.LegalHead?.FullName,
            d.SupervisingDeputyId,
            d.SupervisingDeputy?.FullName,
            d.FirstDeputyId,
            d.FirstDeputy?.FullName,
            d.GeneralDirectorId,
            d.GeneralDirector?.FullName,
            d.RevisionNotes,
            d.Coordinators.OrderBy(c => c.CoordinatedAt).Select(c => new OrderCoordinatorDto(
                c.Id, c.UserId, c.User.FullName, c.ForDepartment, c.CoordinatedAt)).ToList(),
            d.Recipients.OrderBy(r => r.NotifiedAt).Select(r => new OrderRecipientDto(
                r.Id, r.UserId, r.User.FullName, r.NotifiedAt)).ToList(),
            d.Comments.OrderBy(c => c.CreatedAt).Select(c => new OrderCommentDto(
                c.Id, c.AuthorId, c.Author.FullName, c.Body, c.CreatedAt)).ToList(),
            d.Document.CreatedAt,
            d.Document.UpdatedAt));

    private async Task<string> GeneratePendingNumberAsync(CancellationToken ct)
    {
        var prefix = $"ORD-PEND-{DateTime.UtcNow.Year}-";
        var last = await db.Documents.Where(d => d.Number.StartsWith(prefix))
            .OrderByDescending(d => d.Number).Select(d => d.Number).FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var n)) seq = n + 1;
        return $"{prefix}{seq:D4}";
    }

    private async Task<string> GenerateFormalNumberAsync(CancellationToken ct)
    {
        var prefix = $"ORD-{DateTime.UtcNow.Year}-";
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

    private static OrderUserDto MapUser(User u) => new(
        u.Id, u.FullName, u.Email, u.EmployeeId,
        u.Department?.Name ?? "", u.Department?.NameEn ?? "");

    private static bool IsRegistrar(User u) =>
        string.Equals(u.Email, DcsRouting.IncomingRegistrarEmail, StringComparison.OrdinalIgnoreCase)
        || u.Role == UserRole.SuperAdmin;

    private static bool CanCreate(User u) =>
        u.Organization.Code == HoMasterData.OrganizationCode || u.Role == UserRole.SuperAdmin;

    private static bool CanView(User actor, OrderDetail? detail)
    {
        if (detail is null) return CanCreate(actor);
        if (detail.Document.AuthorId == actor.Id) return true;
        if (IsRegistrar(actor) || actor.Role == UserRole.SuperAdmin) return true;
        if (detail.DeptHeadId == actor.Id) return true;
        if (detail.LegalHeadId == actor.Id) return true;
        if (detail.SupervisingDeputyId == actor.Id) return true;
        if (detail.FirstDeputyId == actor.Id) return true;
        if (detail.GeneralDirectorId == actor.Id) return true;
        if (detail.Coordinators.Any(c => c.UserId == actor.Id)) return true;
        if (detail.Recipients.Any(r => r.UserId == actor.Id)) return true;
        if (actor.Role == UserRole.HOTopManager) return true;
        return false;
    }
}
