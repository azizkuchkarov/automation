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

public class ProcurementRequestService(AppDbContext db, IAuditService audit, IMarketingService marketing) : IProcurementRequestService
{
    private const string BmgmcTech = "BMGMC-TECH";
    private const string BmgmcAdm = "BMGMC-ADM";
    private const string BmgmcTrans = "BMGMC-TRANS";
    private const string HoMkt = "HO-MKT";
    private const string HoMktMkt = "HO-MKT-MKT";
    private const string HoMktTnd = "HO-MKT-TND";
    private const string HoCproc = "HO-CPROC";

    public IReadOnlyList<ProcurementStepDto> GetSteps() =>
        ProcurementRequestSteps.Definitions
            .Select(s => new ProcurementStepDto(s.Number, s.TitleRu, s.TitleEn))
            .ToList();

    public IReadOnlyList<ProcurementMarketingStepDto> GetMarketingSteps() =>
        MarketingRequestSteps.Definitions
            .Select(s => new ProcurementMarketingStepDto(
                s.Number, s.TitleRu, s.TitleEn, s.HintRu, s.HintEn,
                s.HasBranch, s.BranchHintRu, s.BranchHintEn))
            .ToList();

    public async Task<Result<ProcurementCreateOptionsDto>> GetCreateOptionsAsync(Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<ProcurementCreateOptionsDto>.Fail("User not found");

        var canTas = IsTasStaff(actor);
        var canExpress = CanCreateExpress(actor);
        ProcurementRequestFlow? defaultFlow = canTas ? ProcurementRequestFlow.TechnicalAffairs
            : canExpress ? ProcurementRequestFlow.Express : null;

        return Result<ProcurementCreateOptionsDto>.Ok(new ProcurementCreateOptionsDto(canTas, canExpress, defaultFlow));
    }

    public async Task<Result<IReadOnlyList<ProcurementInitiatorDepartmentDto>>> GetInitiatorDepartmentsAsync(
        Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsTasStaff(actor))
            return Result<IReadOnlyList<ProcurementInitiatorDepartmentDto>>.Fail("Access denied");

        var (orgIds, bmgmcId) = await GetBmgmcAndStationOrgIdsAsync(ct);
        var departments = await db.Departments.AsNoTracking()
            .Include(d => d.Organization)
            .Where(d => d.IsActive && orgIds.Contains(d.OrganizationId) && d.Code != BmgmcTech)
            .OrderBy(d => d.OrganizationId == bmgmcId ? 0 : 1)
            .ThenBy(d => d.Organization.Name)
            .ThenBy(d => d.Name)
            .Select(d => new ProcurementInitiatorDepartmentDto(
                d.Id,
                d.Name,
                d.NameEn,
                d.Organization.Name,
                d.Organization.Code,
                d.OrganizationId != bmgmcId))
            .ToListAsync(ct);

        return Result<IReadOnlyList<ProcurementInitiatorDepartmentDto>>.Ok(departments);
    }

    public async Task<Result<IReadOnlyList<ProcurementRequestUserDto>>> GetInitiatorsAsync(
        Guid actorId, Guid departmentId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsTasStaff(actor))
            return Result<IReadOnlyList<ProcurementRequestUserDto>>.Fail("Access denied");

        if (departmentId == Guid.Empty)
            return Result<IReadOnlyList<ProcurementRequestUserDto>>.Fail("Department is required");

        if (!await IsEligibleInitiatorDepartmentAsync(departmentId, ct))
            return Result<IReadOnlyList<ProcurementRequestUserDto>>.Fail("Department not found");

        var users = await db.Users.AsNoTracking()
            .Include(u => u.Department)
            .Include(u => u.Organization)
            .Where(u => u.IsActive && u.DepartmentId == departmentId)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);

        return Result<IReadOnlyList<ProcurementRequestUserDto>>.Ok(users.Select(MapUser).ToList());
    }

    public async Task<Result<IReadOnlyList<ProcurementRequestUserDto>>> GetResponsibleUsersAsync(Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsTasStaff(actor))
            return Result<IReadOnlyList<ProcurementRequestUserDto>>.Fail("Access denied");

        var users = await db.Users.AsNoTracking()
            .Include(u => u.Department)
            .Include(u => u.Organization)
            .Where(u => u.IsActive && u.Department != null && u.Department.Code == BmgmcTech)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);

        return Result<IReadOnlyList<ProcurementRequestUserDto>>.Ok(users.Select(MapUser).ToList());
    }

    public async Task<Result<ProcurementRequestDto>> GetByIdAsync(Guid id, Guid actorId, CancellationToken ct = default)
    {
        var detail = await LoadDetailAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanView(actor, detail))
            return Result<ProcurementRequestDto>.Fail("Access denied");

        var dto = MapDetail(detail);
        string? marketingTaskNumber = null;
        string? contractsTaskNumber = null;
        if (detail.MarketingTaskId is not null)
            marketingTaskNumber = await db.WorkTasks.AsNoTracking()
                .Where(t => t.Id == detail.MarketingTaskId)
                .Select(t => t.Number)
                .FirstOrDefaultAsync(ct);
        if (detail.ContractsTaskId is not null)
            contractsTaskNumber = await db.WorkTasks.AsNoTracking()
                .Where(t => t.Id == detail.ContractsTaskId)
                .Select(t => t.Number)
                .FirstOrDefaultAsync(ct);

        return Result<ProcurementRequestDto>.Ok(dto with
        {
            MarketingTaskNumber = marketingTaskNumber,
            ContractsTaskNumber = contractsTaskNumber,
            MarketingPermissions = BuildMarketingPermissions(actor, detail)
        });
    }

    public async Task<Result<ProcurementRequestDto>> CreateTasAsync(
        CreateTasProcurementRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<ProcurementRequestDto>.Fail("User not found");
        if (!IsTasStaff(actor))
            return Result<ProcurementRequestDto>.Fail("Only BMGMC Technical Affairs Section can open this request type");

        if (string.IsNullOrWhiteSpace(request.EamNumber))
            return Result<ProcurementRequestDto>.Fail("EAM request number is required");
        if (string.IsNullOrWhiteSpace(request.ProcurementName))
            return Result<ProcurementRequestDto>.Fail("Procurement name is required");
        if (request.Deadline == default)
            return Result<ProcurementRequestDto>.Fail("Deadline is required");

        var initiator = await db.Users.Include(u => u.Department).Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == request.InitiatorId && u.IsActive, ct);
        if (initiator is null) return Result<ProcurementRequestDto>.Fail("Initiator not found");
        if (initiator.DepartmentId is null || !await IsEligibleInitiatorDepartmentAsync(initiator.DepartmentId.Value, ct))
            return Result<ProcurementRequestDto>.Fail("Initiator must belong to a BMGMC department or station");

        var responsible = await db.Users.Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == request.ResponsibleId && u.IsActive, ct);
        if (responsible is null || responsible.Department?.Code != BmgmcTech)
            return Result<ProcurementRequestDto>.Fail("Responsible must be a BMGMC Technical Affairs user");

        var dept = await GetDepartmentAsync(BmgmcTech, BmgmcMasterData.OrganizationCode, ct);
        if (dept is null) return Result<ProcurementRequestDto>.Fail("Technical Affairs department not found");

        var bmgmcOrg = await db.Organizations.FirstAsync(o => o.Code == BmgmcMasterData.OrganizationCode, ct);
        var number = await GeneratePendingNumberAsync(ct);

        var doc = new Document
        {
            Id = Guid.NewGuid(),
            Number = number,
            Title = request.ProcurementName.Trim(),
            Type = DocumentType.ProcurementRequest,
            Status = DocumentStatus.InReview,
            AuthorId = actorId,
            OrganizationId = bmgmcOrg.Id,
            DepartmentId = dept.Id,
            AssigneeId = responsible.Id,
            ExternalReference = request.EamNumber.Trim(),
            DueDate = DateTimeNormalization.ToUtc(request.Deadline),
        };

        var detail = new ProcurementRequestDetail
        {
            DocumentId = doc.Id,
            Document = doc,
            Flow = ProcurementRequestFlow.TechnicalAffairs,
            Phase = ProcurementRequestPhase.InProgress,
            CurrentStep = 1,
            InitiatorId = initiator.Id,
            InitiatorDepartmentId = initiator.DepartmentId,
            EamNumber = request.EamNumber.Trim(),
            EamFormationDate = DateTimeNormalization.ToUtc(request.EamFormationDate),
        };

        db.Documents.Add(doc);
        db.ProcurementRequestDetails.Add(detail);
        await AddDocumentActivityAsync(doc, actorId, "created", null, DocumentStatus.InReview,
            $"TAS request for {initiator.FullName}", ct);

        var task = await CreateLinkedTaskAsync(
            responsible.Id, actorId, dept.Id, bmgmcOrg.Id,
            $"Procurement request {number}",
            request.ProcurementName.Trim(),
            doc.Id, ct);
        task.DueDate = doc.DueDate;
        detail.ResponsibleTaskId = task.Id;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementRequestCreated", "Document", doc.Id, number, ip, ct);

        return await GetByIdAsync(doc.Id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> CreateExpressAsync(
        CreateExpressProcurementRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<ProcurementRequestDto>.Fail("User not found");
        if (!CanCreateExpress(actor))
            return Result<ProcurementRequestDto>.Fail("You cannot open express procurement requests");

        var subjectEn = request.SubjectEn?.Trim();
        var subjectRu = request.SubjectRu?.Trim();
        var legacySubject = request.Subject?.Trim();
        if (string.IsNullOrWhiteSpace(subjectEn))
            subjectEn = legacySubject;
        if (string.IsNullOrWhiteSpace(subjectRu))
            subjectRu = legacySubject;

        if (string.IsNullOrWhiteSpace(subjectEn))
            return Result<ProcurementRequestDto>.Fail("Subject (English) is required");
        if (string.IsNullOrWhiteSpace(subjectRu))
            return Result<ProcurementRequestDto>.Fail("Subject (Russian) is required");
        if (request.Approvers.Count == 0)
            return Result<ProcurementRequestDto>.Fail("At least one approver is required");

        var deptId = actor.DepartmentId;
        if (deptId is null)
            return Result<ProcurementRequestDto>.Fail("Your profile has no department");

        var number = await GeneratePendingNumberAsync(ct);
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            Number = number,
            Title = subjectEn,
            TitleRu = subjectRu,
            Type = DocumentType.ProcurementRequest,
            Status = DocumentStatus.InReview,
            AuthorId = actorId,
            OrganizationId = actor.OrganizationId,
            DepartmentId = deptId.Value,
            AssigneeId = actorId,
        };

        var detail = new ProcurementRequestDetail
        {
            DocumentId = doc.Id,
            Document = doc,
            Flow = ProcurementRequestFlow.Express,
            Phase = ProcurementRequestPhase.AwaitingApproval,
            CurrentStep = 0,
            InitiatorId = actorId,
            InitiatorDepartmentId = deptId,
        };

        db.Documents.Add(doc);
        db.ProcurementRequestDetails.Add(detail);
        await AddApproversAsync(detail, request.Approvers, ct);
        await AddAttachmentsAsync(detail, request.Attachments, actorId, ct);
        await AddDocumentActivityAsync(doc, actorId, "created", null, DocumentStatus.InReview,
            "Express procurement request", ct);

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementRequestCreated", "Document", doc.Id, number, ip, ct);

        return await GetByIdAsync(doc.Id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> CompleteStepAsync(
        Guid id, int step, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanWorkAsResponsible(actor, detail))
            return Result<ProcurementRequestDto>.Fail("Access denied");

        if (detail.Flow != ProcurementRequestFlow.TechnicalAffairs)
            return Result<ProcurementRequestDto>.Fail("Steps apply only to Technical Affairs requests");
        if (detail.Phase != ProcurementRequestPhase.InProgress)
            return Result<ProcurementRequestDto>.Fail("Request is not in progress");
        if (step < 1 || step > 8)
            return Result<ProcurementRequestDto>.Fail("Use submit for step 9");
        if (detail.CurrentStep != step)
            return Result<ProcurementRequestDto>.Fail($"Current step is {detail.CurrentStep}");

        detail.CurrentStep = step + 1;
        detail.Document.UpdatedAt = DateTime.UtcNow;
        await AddDocumentActivityAsync(detail.Document, actorId, "step_completed", null, detail.Document.Status,
            $"Step {step} completed", ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementStepCompleted", "Document", id, step.ToString(), ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> SubmitStep9Async(
        Guid id, SubmitStep9Request request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanWorkAsResponsible(actor, detail))
            return Result<ProcurementRequestDto>.Fail("Access denied");

        if (detail.Flow != ProcurementRequestFlow.TechnicalAffairs || detail.CurrentStep != 9)
            return Result<ProcurementRequestDto>.Fail("Step 9 is not active");
        if (request.Approvers.Count == 0)
            return Result<ProcurementRequestDto>.Fail("Approvers are required");
        if (request.Attachments.Count == 0)
            return Result<ProcurementRequestDto>.Fail("TA / MR / SR attachments are required");

        await db.ProcurementRequestApprovers.Where(a => a.DocumentId == id).ExecuteDeleteAsync(ct);
        await db.ProcurementRequestAttachments.Where(a => a.DocumentId == id).ExecuteDeleteAsync(ct);
        foreach (var a in detail.Approvers.ToList())
            db.Entry(a).State = EntityState.Detached;
        foreach (var a in detail.Attachments.ToList())
            db.Entry(a).State = EntityState.Detached;
        detail.Approvers.Clear();
        detail.Attachments.Clear();
        await AddApproversAsync(detail, request.Approvers, ct);
        await AddAttachmentsAsync(detail, request.Attachments, actorId, ct);

        detail.Phase = ProcurementRequestPhase.AwaitingApproval;
        detail.Document.Status = DocumentStatus.InReview;
        detail.Document.UpdatedAt = DateTime.UtcNow;
        await AddDocumentActivityAsync(detail.Document, actorId, "submitted_for_approval", null,
            DocumentStatus.InReview, "Step 9 — approval initiated", ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementSubmittedForApproval", "Document", id, "step9", ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> ApproveAsync(
        Guid id, ProcurementApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.AwaitingApproval)
            return Result<ProcurementRequestDto>.Fail("Request is not awaiting approval");

        var approver = detail.Approvers.FirstOrDefault(a => a.UserId == actorId && a.Status == ProcurementApproverStatus.Pending);
        if (approver is null) return Result<ProcurementRequestDto>.Fail("You are not a pending approver");

        approver.Status = ProcurementApproverStatus.Approved;
        approver.DecidedAt = DateTime.UtcNow;
        approver.Comment = request.Comment?.Trim();
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddDocumentActivityAsync(detail.Document, actorId, "approved", null, detail.Document.Status,
            approver.Role.ToString(), ct);

        if (detail.Approvers.All(a => a.Status == ProcurementApproverStatus.Approved))
        {
            await RegisterRequestAsync(detail, actorId, ct);
            await HandoffToMarketingAsync(detail, actorId, ct);
        }

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementApproved", "Document", id, approver.Role.ToString(), ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> RejectAsync(
        Guid id, ProcurementApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.AwaitingApproval)
            return Result<ProcurementRequestDto>.Fail("Request is not awaiting approval");

        var approver = detail.Approvers.FirstOrDefault(a => a.UserId == actorId && a.Status == ProcurementApproverStatus.Pending);
        if (approver is null) return Result<ProcurementRequestDto>.Fail("You are not a pending approver");

        approver.Status = ProcurementApproverStatus.Rejected;
        approver.DecidedAt = DateTime.UtcNow;
        approver.Comment = request.Comment?.Trim();
        detail.Document.Status = DocumentStatus.Rejected;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddDocumentActivityAsync(detail.Document, actorId, "rejected", null, DocumentStatus.Rejected,
            approver.Role.ToString(), ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementRejected", "Document", id, approver.Role.ToString(), ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> ForwardToContractsAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Marketing)
            return Result<ProcurementRequestDto>.Fail("Request is not at Marketing");
        if (detail.MarketingSubPhase != ProcurementMarketingSubPhase.Completed)
            return Result<ProcurementRequestDto>.Fail("Marketing workflow must be completed before forwarding to Contracts");
        if (detail.MarketingCurrentStep < MarketingRequestSteps.TotalSteps)
            return Result<ProcurementRequestDto>.Fail("All marketing steps must be completed before forwarding to Contracts");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanForwardToContracts(actor, detail))
            return Result<ProcurementRequestDto>.Fail("Access denied");

        var cprocDept = await GetDepartmentAsync(HoCproc, HoMasterData.OrganizationCode, ct);
        if (cprocDept is null) return Result<ProcurementRequestDto>.Fail("Contracts department not found");

        var assignee = await db.Users
            .Where(u => u.IsActive && u.DepartmentId == cprocDept.Id && u.Role == UserRole.HONachalnik)
            .OrderBy(u => u.LastName)
            .FirstOrDefaultAsync(ct)
            ?? await db.Users
                .Where(u => u.IsActive && u.DepartmentId == cprocDept.Id)
                .OrderBy(u => u.LastName)
                .FirstAsync(ct);

        var hoOrg = await db.Organizations.FirstAsync(o => o.Code == HoMasterData.OrganizationCode, ct);
        var task = await CreateLinkedTaskAsync(
            assignee.Id, actorId, cprocDept.Id, hoOrg.Id,
            $"Contracts review — {detail.Document.Number}",
            detail.Document.Title,
            detail.Document.Id, ct);

        detail.Phase = ProcurementRequestPhase.Contracts;
        detail.ContractsTaskId = task.Id;
        detail.Document.DepartmentId = cprocDept.Id;
        detail.Document.OrganizationId = hoOrg.Id;
        detail.Document.AssigneeId = assignee.Id;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddDocumentActivityAsync(detail.Document, actorId, "handoff_contracts", null,
            detail.Document.Status, $"Task {task.Number} → {assignee.FullName}", ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementForwardedToContracts", "Document", id, task.Number, ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<IReadOnlyList<ProcurementMarketingQueueItemDto>>> GetMarketingQueueAsync(
        Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsMarketingStaff(actor))
            return Result<IReadOnlyList<ProcurementMarketingQueueItemDto>>.Fail("Access denied");

        var items = await db.ProcurementRequestDetails.AsNoTracking()
            .Include(d => d.Document).ThenInclude(doc => doc.Assignee)
            .Include(d => d.MarketingSpecialist)
            .Where(d => d.Phase == ProcurementRequestPhase.Marketing)
            .OrderByDescending(d => d.Document.UpdatedAt)
            .ToListAsync(ct);

        var result = items.Select(d =>
        {
            var stepDef = MarketingRequestSteps.Definitions
                .First(s => s.Number == Math.Min(d.MarketingCurrentStep, MarketingRequestSteps.TotalSteps));
            return new ProcurementMarketingQueueItemDto(
                d.DocumentId,
                d.Document.Number,
                d.Document.Title,
                d.Document.TitleRu,
                d.MarketingSubPhase,
                d.MarketingCurrentStep,
                stepDef.TitleRu,
                stepDef.TitleEn,
                d.Document.Assignee?.FullName,
                d.MarketingSpecialist?.FullName,
                d.Document.RegisteredAt ?? d.Document.CreatedAt,
                d.Document.UpdatedAt);
        }).ToList();

        return Result<IReadOnlyList<ProcurementMarketingQueueItemDto>>.Ok(result);
    }

    public async Task<Result<IReadOnlyList<ProcurementRequestUserDto>>> GetMarketingWorkersAsync(
        Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsMarketingStaff(actor))
            return Result<IReadOnlyList<ProcurementRequestUserDto>>.Fail("Access denied");

        var deptIds = await GetMarketingDepartmentIdsAsync(ct);
        var users = await db.Users.AsNoTracking()
            .Include(u => u.Department)
            .Include(u => u.Organization)
            .Where(u => u.IsActive && u.DepartmentId != null && deptIds.Contains(u.DepartmentId.Value))
            .OrderBy(u => u.Department!.Code).ThenBy(u => u.LastName)
            .ToListAsync(ct);

        return Result<IReadOnlyList<ProcurementRequestUserDto>>.Ok(users.Select(MapUser).ToList());
    }

    public Task<Result<ProcurementRequestDto>> AcceptMarketingAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default) =>
        Task.FromResult(Result<ProcurementRequestDto>.Fail("Use marketing step 1: accept and assign a Marketing Engineer"));

    public async Task<Result<ProcurementRequestDto>> AssignMarketingSpecialistAsync(
        Guid id, AssignMarketingSpecialistRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Marketing || detail.MarketingCurrentStep != 1)
            return Result<ProcurementRequestDto>.Fail("Specialist can only be assigned during marketing step 1");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanManageMarketing(actor, detail))
            return Result<ProcurementRequestDto>.Fail("Access denied");

        await AssignMarketingSpecialistInternalAsync(detail, request.SpecialistId, actorId, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementMarketingAssigned", "Document", id, request.SpecialistId.ToString(), ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> CompleteMarketingAsync(
        Guid id, MarketingActionRequest request, Guid actorId, string? ip, CancellationToken ct = default) =>
        await CompleteMarketingStepAsync(id, MarketingRequestSteps.TotalSteps,
            new CompleteMarketingStepRequest(null, request.Comment), actorId, ip, ct);

    public async Task<Result<ProcurementRequestDto>> CompleteMarketingStepAsync(
        Guid id, int step, CompleteMarketingStepRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Marketing)
            return Result<ProcurementRequestDto>.Fail("Request is not at Marketing");
        if (detail.MarketingCurrentStep != step)
            return Result<ProcurementRequestDto>.Fail($"Expected marketing step {detail.MarketingCurrentStep}, not {step}");
        if (detail.MarketingActiveBranch is not null)
            return Result<ProcurementRequestDto>.Fail("Resolve the active branch before completing this step");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanCompleteMarketingStep(actor, detail, step))
            return Result<ProcurementRequestDto>.Fail("Access denied");

        if (step == 1)
        {
            if (request.SpecialistId is null && detail.MarketingSpecialistId is null)
                return Result<ProcurementRequestDto>.Fail("Marketing Engineer must be assigned at step 1");
            if (request.SpecialistId is Guid specialistId)
                await AssignMarketingSpecialistInternalAsync(detail, specialistId, actorId, ct);
            detail.MarketingAcceptedAt ??= DateTime.UtcNow;
        }

        var action = $"marketing_step_{step}_completed";
        var actionDetails = string.IsNullOrWhiteSpace(request.Comment)
            ? MarketingRequestSteps.Definitions.First(s => s.Number == step).TitleEn
            : request.Comment.Trim();

        if (step >= MarketingRequestSteps.TotalSteps)
        {
            detail.MarketingSubPhase = ProcurementMarketingSubPhase.Completed;
            detail.MarketingCompletedAt = DateTime.UtcNow;
            action = "marketing_completed";
            actionDetails = string.IsNullOrWhiteSpace(request.Comment)
                ? "Marketing process completed"
                : request.Comment.Trim();
        }
        else
        {
            detail.MarketingCurrentStep = step + 1;
            SyncMarketingSubPhase(detail);
        }

        detail.Document.UpdatedAt = DateTime.UtcNow;
        await AddDocumentActivityAsync(detail.Document, actorId, action, null, detail.Document.Status, actionDetails, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementMarketingStepCompleted", "Document", id, $"step={step}", ip, ct);
        await marketing.SyncStatusFromWorkflowAsync(id, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> RecordMarketingBranchAsync(
        Guid id, MarketingBranchRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Marketing)
            return Result<ProcurementRequestDto>.Fail("Request is not at Marketing");

        var expected = BranchForStep(detail.MarketingCurrentStep);
        if (expected is null || expected != request.Branch)
            return Result<ProcurementRequestDto>.Fail("This branch is not valid for the current marketing step");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<ProcurementRequestDto>.Fail("Access denied");

        string action;
        string details;
        if (request.Resolve)
        {
            if (detail.MarketingActiveBranch != request.Branch)
                return Result<ProcurementRequestDto>.Fail("No matching active branch to resolve");
            if (!CanResolveMarketingBranch(actor, detail))
                return Result<ProcurementRequestDto>.Fail("Access denied");
            detail.MarketingActiveBranch = null;
            detail.MarketingBranchStartedAt = null;
            action = "marketing_branch_resolved";
            details = string.IsNullOrWhiteSpace(request.Comment)
                ? request.Branch.ToString()
                : request.Comment.Trim();
        }
        else
        {
            if (detail.MarketingActiveBranch is not null)
                return Result<ProcurementRequestDto>.Fail("Another branch is already active");
            if (!CanRecordMarketingBranch(actor, detail))
                return Result<ProcurementRequestDto>.Fail("Access denied");
            detail.MarketingActiveBranch = request.Branch;
            detail.MarketingBranchStartedAt = DateTime.UtcNow;
            action = "marketing_branch_recorded";
            details = string.IsNullOrWhiteSpace(request.Comment)
                ? request.Branch.ToString()
                : request.Comment.Trim();
        }

        detail.Document.UpdatedAt = DateTime.UtcNow;
        await AddDocumentActivityAsync(detail.Document, actorId, action, null, detail.Document.Status, details, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementMarketingBranch", "Document", id, $"{action}:{request.Branch}", ip, ct);
        await marketing.SyncStatusFromWorkflowAsync(id, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    private async Task AssignMarketingSpecialistInternalAsync(
        ProcurementRequestDetail detail, Guid specialistId, Guid actorId, CancellationToken ct)
    {
        var deptIds = await GetMarketingDepartmentIdsAsync(ct);
        var specialist = await db.Users.FirstOrDefaultAsync(
            u => u.Id == specialistId && u.IsActive && u.DepartmentId != null && deptIds.Contains(u.DepartmentId.Value), ct)
            ?? throw new InvalidOperationException("Specialist must be a Marketing department employee");

        detail.MarketingSpecialistId = specialist.Id;
        detail.Document.AssigneeId = specialist.Id;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        if (detail.MarketingTaskId is Guid taskId)
        {
            var task = await db.WorkTasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);
            if (task is not null)
            {
                task.AssigneeId = specialist.Id;
                task.UpdatedAt = DateTime.UtcNow;
            }
        }

        await AddDocumentActivityAsync(detail.Document, actorId, "marketing_assigned", null,
            detail.Document.Status, specialist.FullName, ct);
    }

    private async Task RegisterRequestAsync(ProcurementRequestDetail detail, Guid actorId, CancellationToken ct)
    {
        if (detail.Document.RegisteredAt is not null) return;

        var registeredNumber = await GenerateRegisteredNumberAsync(ct);
        detail.Document.Number = registeredNumber;
        detail.Document.RegisteredAt = DateTime.UtcNow;
        detail.Document.Status = DocumentStatus.Registered;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddDocumentActivityAsync(detail.Document, actorId, "registered", DocumentStatus.InReview,
            DocumentStatus.Registered, registeredNumber, ct);
    }

    private async Task HandoffToMarketingAsync(ProcurementRequestDetail detail, Guid actorId, CancellationToken ct)
    {
        var mktDept = await GetDepartmentAsync(HoMkt, HoMasterData.OrganizationCode, ct);
        if (mktDept is null) throw new InvalidOperationException("HO Marketing department not found");

        var assignee = await db.Users
            .Where(u => u.IsActive && u.DepartmentId == mktDept.Id &&
                        u.Role == UserRole.HONachalnik)
            .OrderBy(u => u.LastName)
            .FirstOrDefaultAsync(ct)
            ?? await db.Users
                .Where(u => u.IsActive && u.DepartmentId == mktDept.Id)
                .OrderBy(u => u.LastName)
                .FirstAsync(ct);

        var hoOrg = await db.Organizations.FirstAsync(o => o.Code == HoMasterData.OrganizationCode, ct);
        var task = await CreateLinkedTaskAsync(
            assignee.Id, actorId, mktDept.Id, hoOrg.Id,
            $"Marketing review — {detail.Document.Number}",
            detail.Document.Title,
            detail.Document.Id, ct);

        if (detail.Flow == ProcurementRequestFlow.TechnicalAffairs)
            detail.CurrentStep = ProcurementRequestSteps.TotalSteps;

        detail.Phase = ProcurementRequestPhase.Marketing;
        detail.MarketingSubPhase = ProcurementMarketingSubPhase.Pending;
        detail.MarketingCurrentStep = 1;
        detail.MarketingActiveBranch = null;
        detail.MarketingBranchStartedAt = null;
        detail.MarketingTaskId = task.Id;
        detail.Document.Status = DocumentStatus.InReview;
        detail.Document.DepartmentId = mktDept.Id;
        detail.Document.OrganizationId = hoOrg.Id;
        detail.Document.AssigneeId = assignee.Id;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddDocumentActivityAsync(detail.Document, actorId, "handoff_marketing", DocumentStatus.Registered,
            DocumentStatus.InReview, $"Task {task.Number} → {assignee.FullName}", ct);

        await marketing.CreateFromProcurementAsync(detail.DocumentId, ct);
    }

    private async Task<ProcurementRequestDetail?> LoadDetailAsync(Guid id, CancellationToken ct) =>
        await db.ProcurementRequestDetails.AsNoTracking()
            .Include(d => d.Document).ThenInclude(doc => doc.Author)
            .Include(d => d.Document).ThenInclude(doc => doc.Assignee)
            .Include(d => d.Document).ThenInclude(doc => doc.Organization)
            .Include(d => d.Document).ThenInclude(doc => doc.Department)
            .Include(d => d.Document).ThenInclude(doc => doc.Activities).ThenInclude(a => a.Actor)
            .Include(d => d.Initiator)
            .Include(d => d.InitiatorDepartment)
            .Include(d => d.MarketingSpecialist)
            .Include(d => d.Approvers).ThenInclude(a => a.User)
            .Include(d => d.Attachments).ThenInclude(a => a.UploadedBy)
            .FirstOrDefaultAsync(d => d.DocumentId == id, ct);

    private async Task<ProcurementRequestDetail?> LoadDetailTrackedAsync(Guid id, CancellationToken ct) =>
        await db.ProcurementRequestDetails
            .Include(d => d.Document)
            .Include(d => d.Approvers)
            .Include(d => d.Attachments)
            .FirstOrDefaultAsync(d => d.DocumentId == id, ct);

    private static ProcurementRequestDto MapDetail(ProcurementRequestDetail d) => new(
        d.DocumentId,
        d.Document.Number,
        d.Document.Title,
        d.Document.TitleRu,
        d.Document.Status,
        d.Document.RegisteredAt is not null,
        d.Flow,
        d.Phase,
        d.CurrentStep,
        d.Document.AuthorId,
        d.Document.Author.FullName,
        d.Document.AssigneeId,
        d.Document.Assignee?.FullName,
        d.InitiatorId,
        d.Initiator?.FullName,
        d.InitiatorDepartmentId,
        d.InitiatorDepartment?.Name,
        d.InitiatorDepartment?.NameEn,
        d.EamNumber,
        d.EamFormationDate,
        d.Document.DueDate,
        d.Document.OrganizationId,
        d.Document.Organization.Name,
        d.Document.DepartmentId,
        d.Document.Department.Name,
        d.Document.Department.NameEn,
        d.ResponsibleTaskId,
        d.MarketingTaskId,
        null,
        d.ContractsTaskId,
        null,
        d.MarketingSubPhase,
        d.MarketingSpecialistId,
        d.MarketingSpecialist?.FullName,
        d.MarketingAcceptedAt,
        d.MarketingCompletedAt,
        null,
        d.MarketingCurrentStep,
        d.MarketingActiveBranch,
        MarketingRequestSteps.Definitions
            .Select(s => new ProcurementMarketingStepDto(
                s.Number, s.TitleRu, s.TitleEn, s.HintRu, s.HintEn,
                s.HasBranch, s.BranchHintRu, s.BranchHintEn))
            .ToList(),
        ProcurementRequestSteps.Definitions
            .Select(s => new ProcurementStepDto(s.Number, s.TitleRu, s.TitleEn)).ToList(),
        d.Approvers.OrderBy(a => a.SortOrder).Select(a => new ProcurementApproverDto(
            a.Id, a.UserId, a.User.FullName, a.Role, a.Status, a.SortOrder, a.DecidedAt, a.Comment)).ToList(),
        d.Attachments.OrderByDescending(a => a.UploadedAt).Select(a => new ProcurementAttachmentDto(
            a.Id, a.Kind, a.FileName, a.StorageKey, a.UploadedBy.FullName, a.UploadedAt)).ToList(),
        d.Document.RegisteredAt,
        d.Document.Activities.OrderBy(a => a.CreatedAt).Select(a => new ProcurementTimelineEventDto(
            a.Id, a.Action, a.Actor.FullName, a.Details, a.CreatedAt)).ToList(),
        ProcurementTopologyBuilder.Build(d),
        d.Document.CreatedAt,
        d.Document.UpdatedAt);

    private async Task<(HashSet<Guid> OrgIds, Guid BmgmcId)> GetBmgmcAndStationOrgIdsAsync(CancellationToken ct)
    {
        var bmgmcId = await db.Organizations
            .Where(o => o.Code == BmgmcMasterData.OrganizationCode)
            .Select(o => o.Id)
            .FirstAsync(ct);

        var orgIds = await db.Organizations.AsNoTracking()
            .Where(o => o.IsActive && (o.Id == bmgmcId || o.ParentId == bmgmcId))
            .Select(o => o.Id)
            .ToListAsync(ct);

        return (orgIds.ToHashSet(), bmgmcId);
    }

    private async Task<bool> IsEligibleInitiatorDepartmentAsync(Guid departmentId, CancellationToken ct)
    {
        var (orgIds, _) = await GetBmgmcAndStationOrgIdsAsync(ct);
        return await db.Departments.AsNoTracking()
            .AnyAsync(d => d.Id == departmentId && d.IsActive
                && orgIds.Contains(d.OrganizationId) && d.Code != BmgmcTech, ct);
    }

    private static ProcurementRequestUserDto MapUser(User u) => new(
        u.Id, u.FullName, u.Email, u.EmployeeId,
        u.Department?.Name ?? "", u.Department?.NameEn ?? "", u.Organization.Name);

    private async Task AddApproversAsync(
        ProcurementRequestDetail detail,
        IReadOnlyList<ExpressApproverInput> approvers,
        CancellationToken ct)
    {
        var order = 0;
        foreach (var input in approvers)
        {
            var exists = await db.Users.AsNoTracking().AnyAsync(u => u.Id == input.UserId && u.IsActive, ct);
            if (!exists) throw new InvalidOperationException($"Approver {input.UserId} not found");

            var entity = new ProcurementRequestApprover
            {
                Id = Guid.NewGuid(),
                DocumentId = detail.DocumentId,
                UserId = input.UserId,
                Role = input.Role,
                SortOrder = order++,
            };
            db.ProcurementRequestApprovers.Add(entity);
            detail.Approvers.Add(entity);
        }
    }

    private Task AddAttachmentsAsync(
        ProcurementRequestDetail detail,
        IReadOnlyList<ExpressAttachmentInput> attachments,
        Guid actorId,
        CancellationToken ct)
    {
        foreach (var input in attachments)
        {
            if (string.IsNullOrWhiteSpace(input.FileName)) continue;
            var entity = new ProcurementRequestAttachment
            {
                Id = Guid.NewGuid(),
                DocumentId = detail.DocumentId,
                Kind = input.Kind,
                FileName = input.FileName.Trim(),
                StorageKey = input.StorageKey?.Trim(),
                UploadedById = actorId,
                UploadedAt = DateTime.UtcNow,
            };
            db.ProcurementRequestAttachments.Add(entity);
            detail.Attachments.Add(entity);
        }
        return Task.CompletedTask;
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

    private async Task<string> GeneratePendingNumberAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var pattern = $"PREQ-{year}-";
        var last = await db.Documents
            .Where(d => d.Number.StartsWith(pattern))
            .OrderByDescending(d => d.Number)
            .Select(d => d.Number)
            .FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && int.TryParse(last[(pattern.Length)..], out var n))
            seq = n + 1;
        return $"{pattern}{seq:D3}";
    }

    private async Task<string> GenerateRegisteredNumberAsync(CancellationToken ct)
    {
        var prefix = DcsRouting.NumberPrefix(DocumentType.ProcurementRequest);
        var year = DateTime.UtcNow.Year;
        var pattern = $"{prefix}-{year}-";
        var last = await db.Documents
            .Where(d => d.Number.StartsWith(pattern))
            .OrderByDescending(d => d.Number)
            .Select(d => d.Number)
            .FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && int.TryParse(last[(pattern.Length)..], out var n))
            seq = n + 1;
        return $"{pattern}{seq:D4}";
    }

    private async Task<string> GenerateTaskNumberAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var pattern = $"TSK-{year}-";
        var last = await db.WorkTasks
            .Where(t => t.Number.StartsWith(pattern))
            .OrderByDescending(t => t.Number)
            .Select(t => t.Number)
            .FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && int.TryParse(last[(pattern.Length)..], out var n))
            seq = n + 1;
        return $"{pattern}{seq:D4}";
    }

    private async Task<Department?> GetDepartmentAsync(string code, string orgCode, CancellationToken ct)
    {
        var dept = await db.Departments.Include(d => d.Organization)
            .FirstOrDefaultAsync(d => d.Code == code && d.Organization.Code == orgCode && d.IsActive, ct);
        return dept ?? await db.Departments.Include(d => d.Organization)
            .FirstOrDefaultAsync(d => d.Code == code && d.IsActive, ct);
    }

    private async Task AddDocumentActivityAsync(
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

    private async Task<User?> GetActorAsync(Guid actorId, CancellationToken ct) =>
        await db.Users
            .Include(u => u.Organization).ThenInclude(o => o.Parent)
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == actorId && u.IsActive, ct);

    private static bool IsPlatformAdmin(User u) =>
        u.Role is UserRole.SuperAdmin or UserRole.HOTopManager;

    private static bool IsTasStaff(User u) => u.Department?.Code == BmgmcTech;

    private static bool CanCreateExpress(User u)
    {
        if (u.Organization.Code == HoMasterData.OrganizationCode) return true;
        var code = u.Department?.Code;
        return code is BmgmcAdm or BmgmcTrans;
    }

    private async Task<List<Guid>> GetMarketingDepartmentIdsAsync(CancellationToken ct)
    {
        var mktDept = await db.Departments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Code == HoMkt, ct);
        if (mktDept is null) return [];

        return await db.Departments.AsNoTracking()
            .Where(d => d.Id == mktDept.Id || d.ParentId == mktDept.Id)
            .Select(d => d.Id)
            .ToListAsync(ct);
    }

    private static ProcurementMarketingPermissionsDto BuildMarketingPermissions(User actor, ProcurementRequestDetail detail)
    {
        if (detail.Phase != ProcurementRequestPhase.Marketing)
            return new ProcurementMarketingPermissionsDto(false, false, false, false, false, false, false, 1);

        var step = detail.MarketingCurrentStep;
        var stepDef = MarketingRequestSteps.Definitions.FirstOrDefault(s => s.Number == step);
        var hasBranch = stepDef?.HasBranch == true;
        var activeBranch = detail.MarketingActiveBranch;

        return new ProcurementMarketingPermissionsDto(
            step == 1 && detail.MarketingSubPhase == ProcurementMarketingSubPhase.Pending && CanManageMarketing(actor, detail),
            step == 1 && detail.MarketingSubPhase != ProcurementMarketingSubPhase.Completed && CanManageMarketing(actor, detail),
            step == MarketingRequestSteps.TotalSteps && detail.MarketingSubPhase == ProcurementMarketingSubPhase.InProgress && CanCompleteMarketing(actor, detail),
            detail.MarketingSubPhase == ProcurementMarketingSubPhase.Completed && CanForwardToContracts(actor, detail),
            CanCompleteMarketingStep(actor, detail, step),
            hasBranch && activeBranch is null && CanRecordMarketingBranch(actor, detail),
            activeBranch is not null && CanResolveMarketingBranch(actor, detail),
            step);
    }

    private static void SyncMarketingSubPhase(ProcurementRequestDetail detail)
    {
        if (detail.MarketingSubPhase == ProcurementMarketingSubPhase.Completed) return;
        detail.MarketingSubPhase = detail.MarketingCurrentStep <= 1
            ? ProcurementMarketingSubPhase.Pending
            : ProcurementMarketingSubPhase.InProgress;
    }

    private static MarketingBranchType? BranchForStep(int step) => step switch
    {
        2 => MarketingBranchType.TzEscalation,
        5 => MarketingBranchType.ResponseFollowUp,
        6 => MarketingBranchType.KpNegotiation,
        7 => MarketingBranchType.ManagementRevision,
        9 => MarketingBranchType.PortalExpedite,
        _ => null,
    };

    private static bool CanCompleteMarketingStep(User actor, ProcurementRequestDetail detail, int step)
    {
        if (detail.MarketingActiveBranch is not null) return false;
        if (detail.MarketingCurrentStep != step) return false;
        if (detail.MarketingSubPhase == ProcurementMarketingSubPhase.Completed) return false;
        if (IsPlatformAdmin(actor)) return true;
        return step switch
        {
            1 => CanManageMarketing(actor, detail),
            11 => CanCompleteMarketing(actor, detail),
            7 or 8 or 9 => CanCompleteMarketing(actor, detail),
            _ => IsMarketingEngineer(actor, detail),
        };
    }

    private static bool CanRecordMarketingBranch(User actor, ProcurementRequestDetail detail) =>
        IsPlatformAdmin(actor) || IsMarketingEngineer(actor, detail) || CanManageMarketing(actor, detail);

    private static bool CanResolveMarketingBranch(User actor, ProcurementRequestDetail detail) =>
        CanRecordMarketingBranch(actor, detail);

    private static bool IsMarketingEngineer(User actor, ProcurementRequestDetail detail)
    {
        if (IsPlatformAdmin(actor)) return true;
        if (detail.MarketingSpecialistId == actor.Id) return true;
        return detail.MarketingSpecialistId is null
            && detail.Document.AssigneeId == actor.Id
            && IsMarketingDeptCode(actor.Department?.Code);
    }

    private static bool IsMarketingDeptCode(string? code) =>
        code is HoMkt or HoMktMkt or HoMktTnd;

    private static bool IsMarketingStaff(User u) =>
        IsPlatformAdmin(u) || IsMarketingDeptCode(u.Department?.Code);

    private static bool CanManageMarketing(User actor, ProcurementRequestDetail detail)
    {
        if (IsPlatformAdmin(actor)) return true;
        if (detail.Document.AssigneeId == actor.Id && IsMarketingStaff(actor)) return true;
        return IsMarketingDeptCode(actor.Department?.Code) && IsDeptManager(actor);
    }

    private static bool CanCompleteMarketing(User actor, ProcurementRequestDetail detail)
    {
        if (IsPlatformAdmin(actor)) return true;
        if (detail.MarketingSpecialistId == actor.Id) return true;
        if (detail.Document.AssigneeId == actor.Id && IsMarketingStaff(actor)) return true;
        return IsMarketingDeptCode(actor.Department?.Code) && IsDeptManager(actor);
    }

    private static bool CanView(User actor, ProcurementRequestDetail detail)
    {
        if (IsPlatformAdmin(actor)) return true;
        if (detail.Document.AuthorId == actor.Id) return true;
        if (detail.Document.AssigneeId == actor.Id) return true;
        if (detail.MarketingSpecialistId == actor.Id) return true;
        if (detail.InitiatorId == actor.Id) return true;
        if (detail.Approvers.Any(a => a.UserId == actor.Id)) return true;
        if (detail.Phase == ProcurementRequestPhase.Marketing && IsMarketingStaff(actor)) return true;
        if (actor.DepartmentId == detail.Document.DepartmentId && IsDeptManager(actor)) return true;
        return false;
    }

    private static bool CanWorkAsResponsible(User actor, ProcurementRequestDetail detail) =>
        detail.Document.AssigneeId == actor.Id || IsPlatformAdmin(actor);

    private static bool CanForwardToContracts(User actor, ProcurementRequestDetail detail)
    {
        if (IsPlatformAdmin(actor)) return true;
        if (detail.Document.AssigneeId == actor.Id) return true;
        return actor.Department?.Code == HoMkt && IsDeptManager(actor);
    }

    private static bool IsDeptManager(User u) =>
        u.Role is UserRole.HONachalnik or UserRole.BMGMCNachalnikiOtdeli or UserRole.BMGMCManager;
}
