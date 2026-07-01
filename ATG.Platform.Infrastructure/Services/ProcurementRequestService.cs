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

public class ProcurementRequestService(AppDbContext db, IAuditService audit, IMarketingService marketing, IMarketingRfqChannelService rfqChannels, INotificationService notifications) : IProcurementRequestService
{
    private const string BmgmcTech = "BMGMC-TECH";
    private const string BmgmcAdm = "BMGMC-ADM";
    private const string BmgmcTrans = "BMGMC-TRANS";
    private const string HoMkt = "HO-MKT";
    private const string HoMktMkt = "HO-MKT-MKT";
    private const string HoMktTnd = "HO-MKT-TND";
    private const string HoCproc = "HO-CPROC";
    private const string HoCprocCadm = "HO-CPROC-CADM";

    private static readonly ProcurementApproverRole[] ApproverRoleOrder =
    [
        ProcurementApproverRole.Initiator,
        ProcurementApproverRole.TasManager,
        ProcurementApproverRole.BmgmcTopManager,
        ProcurementApproverRole.SectionHead,
        ProcurementApproverRole.TopManager,
    ];

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

        var (region, regionRu, regionEn) = await ResolveRegionForUserAsync(actor, ct);
        var formContext = new ProcurementRequestFormContextDto(
            region,
            regionRu,
            regionEn,
            DateTime.UtcNow.Date,
            canTas ? null : actor.DepartmentId,
            canTas ? null : actor.Department?.Name,
            canTas ? null : actor.Department?.NameEn,
            actor.Id,
            actor.FullName,
            canTas,
            canTas);

        return Result<ProcurementCreateOptionsDto>.Ok(new ProcurementCreateOptionsDto(canTas, canExpress, defaultFlow, formContext));
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
            MarketingPermissions = BuildMarketingPermissions(actor, detail),
            ContractsPermissions = BuildContractsPermissions(actor, detail),
            MarketingPlanPermissions = BuildMarketingPlanPermissions(actor, detail),
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
        var procurementName = request.ProcurementName.Trim();
        if (procurementName.Length > 500)
            return Result<ProcurementRequestDto>.Fail("Procurement name must be 500 characters or less");
        if (request.Deadline == default)
            return Result<ProcurementRequestDto>.Fail("Deadline is required");

        var initiator = await db.Users.Include(u => u.Department).ThenInclude(dep => dep!.Organization)
            .FirstOrDefaultAsync(u => u.Id == request.InitiatorId && u.IsActive, ct);
        if (initiator is null) return Result<ProcurementRequestDto>.Fail("Initiator not found");
        if (initiator.DepartmentId is null || !await IsEligibleInitiatorDepartmentAsync(initiator.DepartmentId.Value, ct))
            return Result<ProcurementRequestDto>.Fail("Initiator must belong to a BMGMC department or station");

        var (region, regionRu, regionEn) = ResolveRegion(initiator.Department!.Organization);

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
            Title = procurementName,
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
            Region = region,
            RegionLabelRu = regionRu,
            RegionLabelEn = regionEn,
            Priority = request.Priority,
            EamNumber = request.EamNumber.Trim(),
            EamFormationDate = DateTimeNormalization.ToUtc(request.EamFormationDate),
        };

        db.Documents.Add(doc);
        db.ProcurementRequestDetails.Add(detail);
        if (request.Attachments is { Count: > 0 })
            await AddAttachmentsAsync(detail, request.Attachments, actorId, ct);
        await AddDocumentActivityAsync(doc, actorId, "created", null, DocumentStatus.InReview,
            $"TAS request for {initiator.FullName}", ct);

        var task = await CreateLinkedTaskAsync(
            responsible.Id, actorId, dept.Id, bmgmcOrg.Id,
            $"Procurement request {number}",
            procurementName,
            doc.Id, request.Priority, ct);
        task.DueDate = doc.DueDate;
        detail.ResponsibleTaskId = task.Id;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementRequestCreated", "Document", doc.Id, number, ip, ct);
        await NotifyLinkedTaskAsync(task, ct);

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

        var (region, regionRu, regionEn) = ResolveRegion(actor.Department!.Organization);
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
            Region = region,
            RegionLabelRu = regionRu,
            RegionLabelEn = regionEn,
            Priority = request.Priority,
        };

        db.Documents.Add(doc);
        db.ProcurementRequestDetails.Add(detail);
        await AddApproversAsync(detail, request.Approvers, ct);
        await AddAttachmentsAsync(detail, request.Attachments, actorId, ct);
        await AddDocumentActivityAsync(doc, actorId, "created", null, DocumentStatus.InReview,
            "Express procurement request", ct);

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementRequestCreated", "Document", doc.Id, number, ip, ct);
        await NotifyNextApproverAsync(detail, ct);

        return await GetByIdAsync(doc.Id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> CompleteStepAsync(
        Guid id, int step, CompleteProcurementStepRequest? request, Guid actorId, string? ip, CancellationToken ct = default)
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
        if (detail.Document.Status == DocumentStatus.Rejected)
            return Result<ProcurementRequestDto>.Fail("Request has been rejected");
        if (step < 1 || step > 5)
            return Result<ProcurementRequestDto>.Fail("Use submit for step 6");
        if (detail.CurrentStep != step)
            return Result<ProcurementRequestDto>.Fail($"Current step is {detail.CurrentStep}");

        var comment = request?.Comment?.Trim();
        if (string.IsNullOrWhiteSpace(comment))
            return Result<ProcurementRequestDto>.Fail("Comment is required when completing a step");

        detail.CurrentStep = step + 1;
        detail.Document.UpdatedAt = DateTime.UtcNow;
        await PersistStepCommentAsync(detail, actorId, ProcurementWorkflowPhase.TechnicalAffairs, step,
            comment, ProcurementStepCommentKind.StepCompletion, ct);
        await SetWorkTaskStatusAsync(detail.ResponsibleTaskId, WorkTaskStatus.InProgress, ct);
        await AddDocumentActivityAsync(detail.Document, actorId, "step_completed", null, detail.Document.Status,
            comment, ct);
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

        if (detail.Document.Status == DocumentStatus.Rejected)
            return Result<ProcurementRequestDto>.Fail("Request has been rejected");

        if (detail.Flow != ProcurementRequestFlow.TechnicalAffairs || detail.CurrentStep != 6)
            return Result<ProcurementRequestDto>.Fail("Step 6 is not active");
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
            DocumentStatus.InReview, "Step 6 — approval initiated", ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementSubmittedForApproval", "Document", id, "step6", ip, ct);
        await NotifyNextApproverAsync(detail, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> RejectTasAsync(
        Guid id, CompleteProcurementStepRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanWorkAsResponsible(actor, detail))
            return Result<ProcurementRequestDto>.Fail("Access denied");

        if (detail.Flow != ProcurementRequestFlow.TechnicalAffairs)
            return Result<ProcurementRequestDto>.Fail("Rejection applies only to Technical Affairs requests");
        if (detail.Phase != ProcurementRequestPhase.InProgress)
            return Result<ProcurementRequestDto>.Fail("Request is not in the TAS workflow");
        if (detail.Document.Status == DocumentStatus.Rejected)
            return Result<ProcurementRequestDto>.Fail("Request is already rejected");

        var comment = request?.Comment?.Trim();
        if (string.IsNullOrWhiteSpace(comment))
            return Result<ProcurementRequestDto>.Fail("Rejection reason is required");

        detail.Document.Status = DocumentStatus.Rejected;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await SetWorkTaskStatusAsync(detail.ResponsibleTaskId, WorkTaskStatus.Cancelled, ct);

        await PersistStepCommentAsync(detail, actorId, ProcurementWorkflowPhase.TechnicalAffairs, detail.CurrentStep,
            comment, ProcurementStepCommentKind.Note, ct);
        await AddDocumentActivityAsync(detail.Document, actorId, "tas_rejected", null, DocumentStatus.Rejected,
            comment, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementTasRejected", "Document", id, null, ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> ApproveAsync(
        Guid id, ProcurementApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.AwaitingApproval)
            return Result<ProcurementRequestDto>.Fail("Request is not awaiting approval");

        var approver = GetNextPendingApprover(detail);
        if (approver is null) return Result<ProcurementRequestDto>.Fail("No pending approvers");
        if (approver.UserId != actorId)
            return Result<ProcurementRequestDto>.Fail("Approval must follow the hierarchy — wait for the previous approver");

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

        if (detail.Phase == ProcurementRequestPhase.Marketing && detail.MarketingTaskId is Guid marketingTaskId)
            await NotifyLinkedTaskByIdAsync(marketingTaskId, ct);
        else if (detail.Approvers.Any(a => a.Status == ProcurementApproverStatus.Pending))
            await NotifyNextApproverAsync(detail, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> RejectAsync(
        Guid id, ProcurementApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.AwaitingApproval)
            return Result<ProcurementRequestDto>.Fail("Request is not awaiting approval");

        var approver = GetNextPendingApprover(detail);
        if (approver is null) return Result<ProcurementRequestDto>.Fail("No pending approvers");
        if (approver.UserId != actorId)
            return Result<ProcurementRequestDto>.Fail("Rejection must follow the hierarchy — wait for the previous approver");

        if (string.IsNullOrWhiteSpace(request.Comment))
            return Result<ProcurementRequestDto>.Fail("Rejection reason is required");

        approver.Status = ProcurementApproverStatus.Rejected;
        approver.DecidedAt = DateTime.UtcNow;
        approver.Comment = request.Comment.Trim();
        detail.Document.Status = DocumentStatus.Rejected;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddDocumentActivityAsync(detail.Document, actorId, "rejected", null, DocumentStatus.Rejected,
            approver.Role.ToString(), ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementRejected", "Document", id, approver.Role.ToString(), ip, ct);
        await notifications.NotifyDcsApprovalRejectedAsync(
            detail.Document.AuthorId, detail.Document.Number, detail.DocumentId, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> ForwardToContractsAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase == ProcurementRequestPhase.Contracts)
            return await GetByIdAsync(id, actorId, ct);
        if (detail.Phase != ProcurementRequestPhase.Marketing)
            return Result<ProcurementRequestDto>.Fail("Request is not at Marketing");
        if (detail.MarketingSubPhase != ProcurementMarketingSubPhase.Completed)
            return Result<ProcurementRequestDto>.Fail("Marketing workflow must be completed before forwarding to Contracts");
        if (detail.MarketingCurrentStep < MarketingRequestSteps.TotalSteps)
            return Result<ProcurementRequestDto>.Fail("All marketing steps must be completed before forwarding to Contracts");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanForwardToContracts(actor, detail))
            return Result<ProcurementRequestDto>.Fail("Access denied");

        try
        {
            await HandoffToContractsAsync(detail, actorId, ct);
        }
        catch (InvalidOperationException ex)
        {
            return Result<ProcurementRequestDto>.Fail(ex.Message);
        }

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementForwardedToContracts", "Document", id, null, ip, ct);
        if (detail.ContractsTaskId is Guid contractsTaskId)
            await NotifyLinkedTaskByIdAsync(contractsTaskId, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<IReadOnlyList<ProcurementRequestUserDto>>> GetContractsWorkersAsync(
        Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsContractsStaff(actor))
            return Result<IReadOnlyList<ProcurementRequestUserDto>>.Fail("Access denied");
        if (actor.DepartmentId is null)
            return Result<IReadOnlyList<ProcurementRequestUserDto>>.Fail("Department is required");

        var deptId = actor.DepartmentId.Value;
        var users = await db.Users.AsNoTracking()
            .Include(u => u.Department)
            .Include(u => u.Organization)
            .Where(u => u.IsActive && u.DepartmentId == deptId
                && (u.Role != UserRole.HONachalnik
                    || u.Email == DevTestAccounts.ContractsSectionHeadEmail))
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);

        return Result<IReadOnlyList<ProcurementRequestUserDto>>.Ok(users.Select(MapUser).ToList());
    }

    public async Task<Result<ProcurementRequestDto>> AcceptContractsAsync(
        Guid id, AcceptContractsRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Contracts)
            return Result<ProcurementRequestDto>.Fail("Request is not at Contracts");
        if (detail.ContractsSubPhase != ProcurementContractsSubPhase.WaitingAccept)
            return Result<ProcurementRequestDto>.Fail("Request is not awaiting engineer acceptance");
        if (detail.ContractsSpecialistId != actorId)
            return Result<ProcurementRequestDto>.Fail("Only the assigned engineer can accept");

        if (string.IsNullOrWhiteSpace(request.Comment))
            return Result<ProcurementRequestDto>.Fail("Comment is required when accepting");

        detail.ContractsAcceptedAt = DateTime.UtcNow;
        detail.ContractsSubPhase = ProcurementContractsSubPhase.InProgress;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await SetWorkTaskStatusAsync(detail.ContractsTaskId, WorkTaskStatus.InProgress, ct);

        await PersistStepCommentAsync(detail, actorId, ProcurementWorkflowPhase.Contracts, 1,
            request.Comment.Trim(), ProcurementStepCommentKind.Acceptance, ct);
        await AddDocumentActivityAsync(detail.Document, actorId, "contracts_accepted", null,
            detail.Document.Status, request.Comment.Trim(), ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementContractsAccepted", "Document", id, null, ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<IReadOnlyList<ProcurementRequestUserDto>>> GetMarketingPlanApproverUsersAsync(
        Guid actorId, string? search, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<IReadOnlyList<ProcurementRequestUserDto>>.Fail("User not found");
        if (!IsMarketingStaff(actor) && !IsPlatformAdmin(actor))
            return Result<IReadOnlyList<ProcurementRequestUserDto>>.Fail("Access denied");

        var query = db.Users.AsNoTracking()
            .Include(u => u.Department)
            .Include(u => u.Organization)
            .Where(u => u.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                u.FullName.Contains(term) || u.Email.Contains(term) || (u.EmployeeId != null && u.EmployeeId.Contains(term)));
        }

        var users = await query
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Take(50)
            .ToListAsync(ct);

        return Result<IReadOnlyList<ProcurementRequestUserDto>>.Ok(users.Select(MapUser).ToList());
    }

    public async Task<Result<ProcurementRequestDto>> SubmitMarketingPlanApprovalAsync(
        Guid id, SubmitMarketingPlanApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Marketing)
            return Result<ProcurementRequestDto>.Fail("Request is not at Marketing");
        if (detail.MarketingCurrentStep != 8)
            return Result<ProcurementRequestDto>.Fail("Plan approval is only available on marketing step 8");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsMarketingEngineer(actor, detail))
            return Result<ProcurementRequestDto>.Fail("Access denied");

        if (request.Approvers.Count == 0)
            return Result<ProcurementRequestDto>.Fail("At least one approver is required");
        if (!request.Approvers.Any(a => a.Role == ProcurementMarketingPlanApproverRole.PlanCeo))
            return Result<ProcurementRequestDto>.Fail("CEO approval is required");

        var last = request.Approvers[^1];
        if (last.Role != ProcurementMarketingPlanApproverRole.PlanCeo)
            return Result<ProcurementRequestDto>.Fail("CEO must be the last approver in the chain");

        await db.ProcurementMarketingPlanApprovers.Where(a => a.DocumentId == id).ExecuteDeleteAsync(ct);
        foreach (var a in detail.MarketingPlanApprovers.ToList())
            db.Entry(a).State = EntityState.Detached;
        detail.MarketingPlanApprovers.Clear();

        await AddMarketingPlanApproversAsync(detail, request.Approvers, ct);
        detail.MarketingPlanApprovalSubmittedAt = DateTime.UtcNow;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddDocumentActivityAsync(detail.Document, actorId, "marketing_plan_submitted", null,
            detail.Document.Status, $"{request.Approvers.Count} approver(s)", ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementMarketingPlanSubmitted", "Document", id, null, ip, ct);
        await marketing.SyncStatusFromWorkflowAsync(id, ct);
        await NotifyNextPlanApproverAsync(detail, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> ApproveMarketingPlanAsync(
        Guid id, ProcurementApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Marketing || detail.MarketingCurrentStep != 8)
            return Result<ProcurementRequestDto>.Fail("Plan approval is not active");

        var approver = GetNextPendingPlanApprover(detail);
        if (approver is null) return Result<ProcurementRequestDto>.Fail("No pending plan approvers");
        if (approver.UserId != actorId)
            return Result<ProcurementRequestDto>.Fail("Approval must follow the hierarchy — wait for the previous approver");

        approver.Status = ProcurementApproverStatus.Approved;
        approver.DecidedAt = DateTime.UtcNow;
        approver.Comment = request.Comment?.Trim();
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddDocumentActivityAsync(detail.Document, actorId, "marketing_plan_approved", null,
            detail.Document.Status, approver.Role.ToString(), ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementMarketingPlanApproved", "Document", id, approver.Role.ToString(), ip, ct);
        await marketing.SyncStatusFromWorkflowAsync(id, ct);
        if (detail.MarketingPlanApprovers.Any(a => a.Status == ProcurementApproverStatus.Pending))
            await NotifyNextPlanApproverAsync(detail, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> RejectMarketingPlanAsync(
        Guid id, ProcurementApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Marketing || detail.MarketingCurrentStep != 8)
            return Result<ProcurementRequestDto>.Fail("Plan approval is not active");

        var approver = GetNextPendingPlanApprover(detail);
        if (approver is null) return Result<ProcurementRequestDto>.Fail("No pending plan approvers");
        if (approver.UserId != actorId)
            return Result<ProcurementRequestDto>.Fail("Rejection must follow the hierarchy — wait for the previous approver");
        if (string.IsNullOrWhiteSpace(request.Comment))
            return Result<ProcurementRequestDto>.Fail("Rejection reason is required");

        approver.Status = ProcurementApproverStatus.Rejected;
        approver.DecidedAt = DateTime.UtcNow;
        approver.Comment = request.Comment.Trim();
        detail.MarketingPlanApprovalSubmittedAt = null;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddDocumentActivityAsync(detail.Document, actorId, "marketing_plan_rejected", null,
            detail.Document.Status, approver.Role.ToString(), ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementMarketingPlanRejected", "Document", id, approver.Role.ToString(), ip, ct);
        await marketing.SyncStatusFromWorkflowAsync(id, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> ConfirmMarketingRegistrationAsync(
        Guid id, ConfirmMarketingRegistrationRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Marketing)
            return Result<ProcurementRequestDto>.Fail("Request is not at Marketing");
        if (detail.MarketingCurrentStep != 9)
            return Result<ProcurementRequestDto>.Fail("Registration is only available on marketing step 9");
        if (detail.MarketingPlanRegisteredAt is not null)
            return Result<ProcurementRequestDto>.Fail("Marketing process is already registered");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsMarketingEngineer(actor, detail))
            return Result<ProcurementRequestDto>.Fail("Access denied");

        if (detail.MarketingPlanApprovers.Count == 0
            || !detail.MarketingPlanApprovers.All(a => a.Status == ProcurementApproverStatus.Approved))
            return Result<ProcurementRequestDto>.Fail("Procurement plan must be fully approved before registration");

        var comment = request.Comment?.Trim();
        if (string.IsNullOrWhiteSpace(comment))
            return Result<ProcurementRequestDto>.Fail("Comment is required when confirming registration");

        var regNumber = await GenerateMarketingPlanRegistrationNumberAsync(ct);
        detail.MarketingPlanRegistrationNumber = regNumber;
        detail.MarketingPlanRegisteredAt = DateTime.UtcNow;
        detail.MarketingPlanRegisteredById = actorId;
        detail.MarketingSubPhase = ProcurementMarketingSubPhase.Completed;
        detail.MarketingCompletedAt = DateTime.UtcNow;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await SetWorkTaskStatusAsync(detail.MarketingTaskId, WorkTaskStatus.Done, ct);

        var record = await db.MarketingRecords.FirstOrDefaultAsync(r => r.DocumentId == id, ct);
        if (record is not null)
        {
            record.PlanRegisteredAt = detail.MarketingPlanRegisteredAt;
            record.Status = MarketingRecordStatus.CompletedToContract;
            record.UpdatedAt = DateTime.UtcNow;
        }

        await PersistStepCommentAsync(detail, actorId, ProcurementWorkflowPhase.Marketing, 9,
            comment, ProcurementStepCommentKind.StepCompletion, ct);
        await AddDocumentActivityAsync(detail.Document, actorId, "marketing_plan_registered", null,
            detail.Document.Status, regNumber, ct);
        await AddDocumentActivityAsync(detail.Document, actorId, "marketing_completed", null,
            detail.Document.Status, comment, ct);

        try
        {
            await HandoffToContractsAsync(detail, actorId, ct);
        }
        catch (InvalidOperationException ex)
        {
            return Result<ProcurementRequestDto>.Fail(ex.Message);
        }

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementMarketingRegistered", "Document", id, regNumber, ip, ct);
        await audit.LogAsync(actorId, "ProcurementForwardedToContracts", "Document", id, "auto", ip, ct);
        await marketing.SyncStatusFromWorkflowAsync(id, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> AssignContractsSpecialistAsync(
        Guid id, AssignContractsSpecialistRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Contracts)
            return Result<ProcurementRequestDto>.Fail("Engineer can only be assigned during the Contracts phase");
        if (detail.ContractsSubPhase == ProcurementContractsSubPhase.Completed)
            return Result<ProcurementRequestDto>.Fail("Contracts phase is already completed");
        if (detail.ContractsSubPhase != ProcurementContractsSubPhase.Pending)
            return Result<ProcurementRequestDto>.Fail("Engineer is already assigned — waiting for acceptance");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanAssignContracts(actor, detail))
            return Result<ProcurementRequestDto>.Fail("Access denied");
        if (actor.DepartmentId is null)
            return Result<ProcurementRequestDto>.Fail("Department is required");

        if (string.IsNullOrWhiteSpace(request.Comment))
            return Result<ProcurementRequestDto>.Fail("Comment is required when assigning");

        if (request.SpecialistId == Guid.Empty)
            return Result<ProcurementRequestDto>.Fail("Select an engineer from your department");

        try
        {
            await AssignContractsSpecialistInternalAsync(detail, request.SpecialistId, actor.DepartmentId.Value, actorId, ct);
        }
        catch (InvalidOperationException ex)
        {
            return Result<ProcurementRequestDto>.Fail(ex.Message);
        }

        detail.ContractsAssignedAt = DateTime.UtcNow;
        detail.ContractsAcceptedAt = null;
        detail.ContractsSubPhase = ProcurementContractsSubPhase.WaitingAccept;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await PersistStepCommentAsync(detail, actorId, ProcurementWorkflowPhase.Contracts, 1,
            request.Comment.Trim(), ProcurementStepCommentKind.Assignment, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementContractsAssigned", "Document", id, request.SpecialistId.ToString(), ip, ct);

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

        var deptIds = await GetMarketingSectionDepartmentIdsAsync(ct);
        var users = await db.Users.AsNoTracking()
            .Include(u => u.Department)
            .Include(u => u.Organization)
            .Where(u => u.IsActive && u.DepartmentId != null && deptIds.Contains(u.DepartmentId.Value)
                && (u.Role != UserRole.HONachalnik
                    || u.Email == DevTestAccounts.MarketingSectionHeadEmail))
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);

        return Result<IReadOnlyList<ProcurementRequestUserDto>>.Ok(users.Select(MapUser).ToList());
    }

    public async Task<Result<ProcurementRequestDto>> AcceptMarketingAsync(
        Guid id, AcceptMarketingRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Marketing || detail.MarketingCurrentStep != 1)
            return Result<ProcurementRequestDto>.Fail("Accept is only available at marketing step 1");
        if (detail.MarketingSubPhase != ProcurementMarketingSubPhase.WaitingAccept)
            return Result<ProcurementRequestDto>.Fail("Request is not awaiting specialist acceptance");
        if (detail.MarketingSpecialistId != actorId)
            return Result<ProcurementRequestDto>.Fail("Only the assigned specialist can accept");

        if (string.IsNullOrWhiteSpace(request.Comment))
            return Result<ProcurementRequestDto>.Fail("Comment is required when accepting");

        detail.MarketingAcceptedAt = DateTime.UtcNow;
        detail.MarketingCurrentStep = 2;
        detail.MarketingSubPhase = ProcurementMarketingSubPhase.InProgress;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await SetWorkTaskStatusAsync(detail.MarketingTaskId, WorkTaskStatus.InProgress, ct);

        await PersistStepCommentAsync(detail, actorId, ProcurementWorkflowPhase.Marketing, 1,
            request.Comment.Trim(), ProcurementStepCommentKind.Acceptance, ct);
        await AddDocumentActivityAsync(detail.Document, actorId, "marketing_accepted", null,
            detail.Document.Status, request.Comment.Trim(), ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementMarketingAccepted", "Document", id, null, ip, ct);
        await marketing.AcceptAsync(id, actorId, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> AssignMarketingSpecialistAsync(
        Guid id, AssignMarketingSpecialistRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Marketing || detail.MarketingCurrentStep != 1)
            return Result<ProcurementRequestDto>.Fail("Specialist can only be assigned during marketing step 1");
        if (detail.MarketingSubPhase == ProcurementMarketingSubPhase.Completed)
            return Result<ProcurementRequestDto>.Fail("Marketing is already completed");
        if (detail.MarketingSubPhase != ProcurementMarketingSubPhase.Pending)
            return Result<ProcurementRequestDto>.Fail("Specialist is already assigned — waiting for acceptance");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanAssignMarketing(actor, detail))
            return Result<ProcurementRequestDto>.Fail("Access denied");

        if (string.IsNullOrWhiteSpace(request.Comment))
            return Result<ProcurementRequestDto>.Fail("Comment is required when assigning");

        if (request.SpecialistId == Guid.Empty)
            return Result<ProcurementRequestDto>.Fail("Select a marketing specialist");

        try
        {
            await AssignMarketingSpecialistInternalAsync(detail, request.SpecialistId, actorId, ct);
        }
        catch (InvalidOperationException ex)
        {
            return Result<ProcurementRequestDto>.Fail(ex.Message);
        }

        detail.MarketingAssignedAt = DateTime.UtcNow;
        detail.MarketingAcceptedAt = null;
        detail.MarketingSubPhase = ProcurementMarketingSubPhase.WaitingAccept;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await PersistStepCommentAsync(detail, actorId, ProcurementWorkflowPhase.Marketing, 1,
            request.Comment.Trim(), ProcurementStepCommentKind.Assignment, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementMarketingAssigned", "Document", id, request.SpecialistId.ToString(), ip, ct);
        if (detail.MarketingTaskId is Guid marketingTaskId)
            await NotifyLinkedTaskByIdAsync(marketingTaskId, ct);

        var marketingResult = await marketing.AssignExecutorAsync(id, new AssignMarketingExecutorRequest(request.SpecialistId), actorId, ct);
        if (!marketingResult.IsSuccess)
            return Result<ProcurementRequestDto>.Fail(marketingResult.Error ?? "Failed to update marketing record");

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> AddStepCommentAsync(
        Guid id, AddProcurementStepCommentRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanView(actor, detail))
            return Result<ProcurementRequestDto>.Fail("Access denied");

        if (string.IsNullOrWhiteSpace(request.Body))
            return Result<ProcurementRequestDto>.Fail("Comment cannot be empty");

        await PersistStepCommentAsync(detail, actorId, request.Phase, request.StepNumber,
            request.Body.Trim(), ProcurementStepCommentKind.Note, ct);
        detail.Document.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementStepComment", "Document", id,
            $"{request.Phase}:{request.StepNumber}", ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> CompleteMarketingAsync(
        Guid id, MarketingActionRequest request, Guid actorId, string? ip, CancellationToken ct = default) =>
        await ConfirmMarketingRegistrationAsync(id, new ConfirmMarketingRegistrationRequest(request.Comment), actorId, ip, ct);

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
            return Result<ProcurementRequestDto>.Fail("Use assign and accept actions for marketing step 1");

        if (string.IsNullOrWhiteSpace(request.Comment))
            return Result<ProcurementRequestDto>.Fail("Comment is required when completing a step");

        if (step == 4)
        {
            var step4 = await rfqChannels.ValidateStep4CompletionAsync(id, ct);
            if (!step4.Ok)
                return Result<ProcurementRequestDto>.Fail(step4.Error ?? "Step 4 requirements not met");
        }

        if (step == 9)
            return Result<ProcurementRequestDto>.Fail("Use registration confirmation for marketing step 9");

        if (step == 8)
        {
            if (detail.MarketingPlanApprovalSubmittedAt is null || detail.MarketingPlanApprovers.Count == 0)
                return Result<ProcurementRequestDto>.Fail("Submit procurement plan for approval first");
            if (!detail.MarketingPlanApprovers.All(a => a.Status == ProcurementApproverStatus.Approved))
                return Result<ProcurementRequestDto>.Fail("All plan approvers must approve before completing step 8");
        }

        var action = $"marketing_step_{step}_completed";
        var actionDetails = request.Comment.Trim();

        await PersistStepCommentAsync(detail, actorId, ProcurementWorkflowPhase.Marketing, step,
            actionDetails, ProcurementStepCommentKind.StepCompletion, ct);

        detail.MarketingCurrentStep = step + 1;
        SyncMarketingSubPhase(detail);

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
        if (!string.IsNullOrWhiteSpace(request.Comment))
        {
            await PersistStepCommentAsync(detail, actorId, ProcurementWorkflowPhase.Marketing, detail.MarketingCurrentStep,
                request.Comment.Trim(), ProcurementStepCommentKind.Branch, ct);
        }
        await AddDocumentActivityAsync(detail.Document, actorId, action, null, detail.Document.Status, details, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementMarketingBranch", "Document", id, $"{action}:{request.Branch}", ip, ct);
        await marketing.SyncStatusFromWorkflowAsync(id, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    private async Task AssignMarketingSpecialistInternalAsync(
        ProcurementRequestDetail detail, Guid specialistId, Guid actorId, CancellationToken ct)
    {
        var specialist = await db.Users.Include(u => u.Department).FirstOrDefaultAsync(
            u => u.Id == specialistId && u.IsActive && u.Department != null && u.Department.Code == HoMktMkt, ct)
            ?? throw new InvalidOperationException("Specialist must be a Marketing Section employee (HO-MKT-MKT)");

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

    private async Task AssignContractsSpecialistInternalAsync(
        ProcurementRequestDetail detail, Guid specialistId, Guid actorDeptId, Guid actorId, CancellationToken ct)
    {
        var specialist = await db.Users.Include(u => u.Department).FirstOrDefaultAsync(
            u => u.Id == specialistId && u.IsActive && u.DepartmentId == actorDeptId, ct)
            ?? throw new InvalidOperationException("Engineer must be from your department");

        if (specialist.Role == UserRole.HONachalnik
            && !string.Equals(specialist.Email, DevTestAccounts.ContractsSectionHeadEmail, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Select an engineer from your department");

        detail.ContractsSpecialistId = specialist.Id;
        detail.Document.AssigneeId = specialist.Id;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        if (detail.ContractsTaskId is Guid taskId)
        {
            var task = await db.WorkTasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);
            if (task is not null)
            {
                task.AssigneeId = specialist.Id;
                task.UpdatedAt = DateTime.UtcNow;
            }
        }

        await AddDocumentActivityAsync(detail.Document, actorId, "contracts_assigned", null,
            detail.Document.Status, specialist.FullName, ct);
    }

    private async Task HandoffToContractsAsync(ProcurementRequestDetail detail, Guid actorId, CancellationToken ct)
    {
        if (detail.Phase == ProcurementRequestPhase.Contracts) return;
        if (detail.MarketingSubPhase != ProcurementMarketingSubPhase.Completed)
            throw new InvalidOperationException("Marketing workflow must be completed before forwarding to Contracts");

        var cprocSection = await GetDepartmentAsync(HoCprocCadm, HoMasterData.OrganizationCode, ct)
            ?? await GetDepartmentAsync(HoCproc, HoMasterData.OrganizationCode, ct)
            ?? throw new InvalidOperationException("Contracts department not found");

        var assignee = await ResolveContractsSectionHeadAsync(ct);
        var hoOrg = await db.Organizations.FirstAsync(o => o.Code == HoMasterData.OrganizationCode, ct);
        var task = await CreateLinkedTaskAsync(
            assignee.Id, actorId, cprocSection.Id, hoOrg.Id,
            $"Contracts review — {detail.Document.Number}",
            detail.Document.Title,
            detail.Document.Id, detail.Priority, ct);

        detail.Phase = ProcurementRequestPhase.Contracts;
        detail.ContractsTaskId = task.Id;
        detail.ContractsSubPhase = ProcurementContractsSubPhase.Pending;
        detail.ContractsSpecialistId = null;
        detail.ContractsAssignedAt = null;
        detail.ContractsAcceptedAt = null;
        detail.Document.DepartmentId = cprocSection.Id;
        detail.Document.OrganizationId = hoOrg.Id;
        detail.Document.AssigneeId = assignee.Id;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddDocumentActivityAsync(detail.Document, actorId, "handoff_contracts", null,
            detail.Document.Status, $"Task {task.Number} → {assignee.FullName}", ct);
    }

    private async Task<User> ResolveContractsSectionHeadAsync(CancellationToken ct)
    {
        var devHead = await db.Users
            .FirstOrDefaultAsync(u => u.IsActive && u.Email == DevTestAccounts.ContractsSectionHeadEmail, ct);
        if (devHead is not null)
            return devHead;

        var section = await GetDepartmentAsync(HoCprocCadm, HoMasterData.OrganizationCode, ct)
            ?? throw new InvalidOperationException("Contracts section not found");

        return await db.Users
            .Where(u => u.IsActive && u.DepartmentId == section.Id && u.Role == UserRole.HONachalnik)
            .OrderBy(u => u.LastName)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Contracts section head not found");
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

        var assignee = await ResolveMarketingSectionHeadAsync(ct);

        var hoOrg = await db.Organizations.FirstAsync(o => o.Code == HoMasterData.OrganizationCode, ct);
        var task = await CreateLinkedTaskAsync(
            assignee.Id, actorId, mktDept.Id, hoOrg.Id,
            $"Marketing review — {detail.Document.Number}",
            detail.Document.Title,
            detail.Document.Id, detail.Priority, ct);

        if (detail.Flow == ProcurementRequestFlow.TechnicalAffairs)
            detail.CurrentStep = ProcurementRequestSteps.TotalSteps;

        await SetWorkTaskStatusAsync(detail.ResponsibleTaskId, WorkTaskStatus.Done, ct);

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

        var marketingResult = await marketing.CreateFromProcurementAsync(detail.DocumentId, ct);
        if (!marketingResult.IsSuccess)
            throw new InvalidOperationException(marketingResult.Error ?? "Failed to create marketing record");
    }

    private async Task<User> ResolveMarketingSectionHeadAsync(CancellationToken ct)
    {
        var devHead = await db.Users
            .FirstOrDefaultAsync(u => u.IsActive && u.Email == DevTestAccounts.MarketingSectionHeadEmail, ct);
        if (devHead is not null)
            return devHead;

        var mktDept = await GetDepartmentAsync(HoMkt, HoMasterData.OrganizationCode, ct)
            ?? throw new InvalidOperationException("HO Marketing department not found");
        var mktSection = await GetDepartmentAsync(HoMktMkt, HoMasterData.OrganizationCode, ct);

        var assignee = mktSection is not null
            ? await db.Users
                .Where(u => u.IsActive && u.DepartmentId == mktSection.Id && u.Role == UserRole.HONachalnik)
                .OrderBy(u => u.LastName)
                .FirstOrDefaultAsync(ct)
            : null;
        assignee ??= await db.Users
            .Where(u => u.IsActive && u.DepartmentId == mktDept.Id && u.Role == UserRole.HONachalnik)
            .OrderBy(u => u.LastName)
            .FirstOrDefaultAsync(ct)
            ?? await db.Users
                .Where(u => u.IsActive && u.DepartmentId == mktDept.Id)
                .OrderBy(u => u.LastName)
                .FirstAsync(ct);

        return assignee;
    }

    private static ProcurementRequestApprover? GetNextPendingApprover(ProcurementRequestDetail detail) =>
        detail.Approvers
            .Where(a => a.Status == ProcurementApproverStatus.Pending)
            .OrderBy(a => Array.IndexOf(ApproverRoleOrder, a.Role))
            .FirstOrDefault();

    private static ProcurementMarketingPlanApprover? GetNextPendingPlanApprover(ProcurementRequestDetail detail) =>
        detail.MarketingPlanApprovers
            .Where(a => a.Status == ProcurementApproverStatus.Pending)
            .OrderBy(a => a.SortOrder)
            .FirstOrDefault();

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
            .Include(d => d.ContractsSpecialist)
            .Include(d => d.Approvers).ThenInclude(a => a.User).ThenInclude(u => u.Department)
            .Include(d => d.Approvers).ThenInclude(a => a.User).ThenInclude(u => u.Organization)
            .Include(d => d.MarketingPlanApprovers).ThenInclude(a => a.User).ThenInclude(u => u.Department)
            .Include(d => d.Attachments).ThenInclude(a => a.UploadedBy)
            .Include(d => d.StepComments).ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(d => d.DocumentId == id, ct);

    private async Task<ProcurementRequestDetail?> LoadDetailTrackedAsync(Guid id, CancellationToken ct) =>
        await db.ProcurementRequestDetails
            .Include(d => d.Document)
            .Include(d => d.Approvers)
            .Include(d => d.MarketingPlanApprovers)
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
        d.Region,
        d.RegionLabelRu,
        d.RegionLabelEn,
        d.Priority,
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
        d.MarketingAssignedAt,
        d.MarketingCompletedAt,
        d.ContractsSubPhase,
        d.ContractsSpecialistId,
        d.ContractsSpecialist?.FullName,
        d.ContractsAssignedAt,
        d.ContractsAcceptedAt,
        null,
        null,
        d.MarketingPlanApprovalSubmittedAt,
        d.MarketingPlanRegistrationNumber,
        d.MarketingPlanRegisteredAt,
        null,
        d.MarketingPlanApprovers.OrderBy(a => a.SortOrder).Select(a => new ProcurementMarketingPlanApproverDto(
            a.Id, a.UserId, a.User.FullName, a.Role, a.Status, a.SortOrder, a.DecidedAt, a.Comment,
            a.User.Department?.Name, a.User.Department?.NameEn, a.User.Email)).ToList(),
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
            a.Id, a.UserId, a.User.FullName, a.Role, a.Status, a.SortOrder, a.DecidedAt, a.Comment,
            a.User.Department?.Name, a.User.Department?.NameEn,
            a.User.Organization?.Name, null,
            a.User.JobTitleRu, a.User.JobTitleEn,
            a.User.Email, a.User.EmployeeId)).ToList(),
        d.Attachments.OrderByDescending(a => a.UploadedAt).Select(a => new ProcurementAttachmentDto(
            a.Id, a.Kind, a.FileName, a.StorageKey, a.UploadedBy.FullName, a.UploadedAt)).ToList(),
        d.Document.RegisteredAt,
        d.Document.Activities.OrderBy(a => a.CreatedAt).Select(a => new ProcurementTimelineEventDto(
            a.Id, a.Action, a.Actor.FullName, a.Details, a.CreatedAt)).ToList(),
        d.StepComments.OrderBy(c => c.CreatedAt).Select(c => new ProcurementStepCommentDto(
            c.Id, c.Phase, c.StepNumber, c.AuthorId, c.Author.FullName, c.Body, c.Kind, c.CreatedAt)).ToList(),
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

    private async Task AddMarketingPlanApproversAsync(
        ProcurementRequestDetail detail,
        IReadOnlyList<MarketingPlanApproverInput> approvers,
        CancellationToken ct)
    {
        var order = 0;
        foreach (var input in approvers)
        {
            var exists = await db.Users.AsNoTracking().AnyAsync(u => u.Id == input.UserId && u.IsActive, ct);
            if (!exists) throw new InvalidOperationException($"Approver {input.UserId} not found");

            var entity = new ProcurementMarketingPlanApprover
            {
                Id = Guid.NewGuid(),
                DocumentId = detail.DocumentId,
                UserId = input.UserId,
                Role = input.Role,
                SortOrder = order++,
            };
            db.ProcurementMarketingPlanApprovers.Add(entity);
            detail.MarketingPlanApprovers.Add(entity);
        }
    }

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
        string title, string description, Guid documentId, TaskPriority priority, CancellationToken ct)
    {
        var task = new WorkTask
        {
            Id = Guid.NewGuid(),
            Number = await GenerateTaskNumberAsync(ct),
            Title = title,
            Description = description,
            Status = WorkTaskStatus.New,
            Priority = priority,
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

    private async Task<string> GenerateMarketingPlanRegistrationNumberAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var pattern = $"MKT-PLAN-{year}-";
        var last = await db.ProcurementRequestDetails
            .Where(d => d.MarketingPlanRegistrationNumber != null
                && d.MarketingPlanRegistrationNumber.StartsWith(pattern))
            .OrderByDescending(d => d.MarketingPlanRegistrationNumber)
            .Select(d => d.MarketingPlanRegistrationNumber)
            .FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && int.TryParse(last[(pattern.Length)..], out var n))
            seq = n + 1;
        return $"{pattern}{seq:D4}";
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

    private async Task<List<Guid>> GetMarketingSectionDepartmentIdsAsync(CancellationToken ct)
    {
        var section = await db.Departments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Code == HoMktMkt, ct);
        return section is null ? [] : [section.Id];
    }

    private Task PersistStepCommentAsync(
        ProcurementRequestDetail detail, Guid authorId, ProcurementWorkflowPhase phase, int stepNumber,
        string body, ProcurementStepCommentKind kind, CancellationToken ct)
    {
        var comment = new ProcurementStepComment
        {
            Id = Guid.NewGuid(),
            DocumentId = detail.DocumentId,
            Phase = phase,
            StepNumber = stepNumber,
            AuthorId = authorId,
            Body = body,
            Kind = kind,
        };
        db.ProcurementStepComments.Add(comment);
        detail.StepComments.Add(comment);
        return Task.CompletedTask;
    }

    private static ProcurementContractsPermissionsDto BuildContractsPermissions(User actor, ProcurementRequestDetail detail)
    {
        if (detail.Phase != ProcurementRequestPhase.Contracts)
            return new ProcurementContractsPermissionsDto(false, false);

        return new ProcurementContractsPermissionsDto(
            detail.ContractsSubPhase == ProcurementContractsSubPhase.WaitingAccept
                && detail.ContractsSpecialistId == actor.Id,
            detail.ContractsSubPhase == ProcurementContractsSubPhase.Pending
                && CanAssignContracts(actor, detail));
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
            detail.MarketingSubPhase == ProcurementMarketingSubPhase.WaitingAccept
                && detail.MarketingSpecialistId == actor.Id,
            detail.MarketingCurrentStep == 1
                && detail.MarketingSubPhase == ProcurementMarketingSubPhase.Pending
                && CanAssignMarketing(actor, detail),
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
        if (detail.MarketingCurrentStep == 1)
        {
            detail.MarketingSubPhase = detail.MarketingSpecialistId switch
            {
                null => ProcurementMarketingSubPhase.Pending,
                _ when detail.MarketingAcceptedAt is null => ProcurementMarketingSubPhase.WaitingAccept,
                _ => ProcurementMarketingSubPhase.InProgress,
            };
            return;
        }
        detail.MarketingSubPhase = ProcurementMarketingSubPhase.InProgress;
    }

    private static MarketingBranchType? BranchForStep(int step) => step switch
    {
        2 => MarketingBranchType.TzEscalation,
        5 => MarketingBranchType.ResponseFollowUp,
        6 => MarketingBranchType.KpNegotiation,
        7 => MarketingBranchType.ManagementRevision,
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
            1 => false,
            9 => false,
            8 => CanCompleteMarketing(actor, detail),
            _ => IsMarketingEngineer(actor, detail),
        };
    }

    private static ProcurementMarketingPlanPermissionsDto BuildMarketingPlanPermissions(
        User actor, ProcurementRequestDetail detail)
    {
        if (detail.Phase != ProcurementRequestPhase.Marketing)
            return new ProcurementMarketingPlanPermissionsDto(false, false, false);

        var step = detail.MarketingCurrentStep;
        var nextPlan = GetNextPendingPlanApprover(detail);
        var engineer = IsMarketingEngineer(actor, detail);
        var hasRejection = detail.MarketingPlanApprovers.Any(a => a.Status == ProcurementApproverStatus.Rejected);
        var allApproved = detail.MarketingPlanApprovers.Count > 0
            && detail.MarketingPlanApprovers.All(a => a.Status == ProcurementApproverStatus.Approved);

        return new ProcurementMarketingPlanPermissionsDto(
            step == 8 && engineer && (detail.MarketingPlanApprovers.Count == 0 || hasRejection),
            step == 8 && nextPlan?.UserId == actor.Id,
            step == 9 && engineer && allApproved && detail.MarketingPlanRegisteredAt is null);
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

    private static bool CanAssignMarketing(User actor, ProcurementRequestDetail detail)
    {
        if (IsPlatformAdmin(actor)) return true;
        if (detail.Document.AssigneeId == actor.Id && IsMarketingStaff(actor) && IsDeptManager(actor)) return true;
        return IsMarketingDeptCode(actor.Department?.Code) && IsDeptManager(actor);
    }

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

    private static bool IsContractsDeptCode(string? code) =>
        code is not null && (code == HoCproc || code.StartsWith("HO-CPROC-", StringComparison.Ordinal));

    private static bool IsContractsStaff(User u) =>
        IsPlatformAdmin(u) || IsContractsDeptCode(u.Department?.Code);

    private static bool CanAssignContracts(User actor, ProcurementRequestDetail detail)
    {
        if (IsPlatformAdmin(actor)) return true;
        if (detail.Document.AssigneeId == actor.Id && IsContractsStaff(actor) && IsDeptManager(actor)) return true;
        return IsContractsDeptCode(actor.Department?.Code) && IsDeptManager(actor);
    }

    private static bool CanView(User actor, ProcurementRequestDetail detail)
    {
        if (IsPlatformAdmin(actor)) return true;
        if (detail.Document.AuthorId == actor.Id) return true;
        if (detail.Document.AssigneeId == actor.Id) return true;
        if (detail.MarketingSpecialistId == actor.Id) return true;
        if (detail.ContractsSpecialistId == actor.Id) return true;
        if (detail.InitiatorId == actor.Id) return true;
        if (detail.Approvers.Any(a => a.UserId == actor.Id)) return true;
        if (detail.Phase == ProcurementRequestPhase.Marketing && IsMarketingStaff(actor)) return true;
        if (detail.Phase == ProcurementRequestPhase.Contracts && IsContractsStaff(actor)) return true;
        if (actor.DepartmentId == detail.Document.DepartmentId && IsDeptManager(actor)) return true;
        return false;
    }

    private static bool CanWorkAsResponsible(User actor, ProcurementRequestDetail detail) =>
        detail.Document.AssigneeId == actor.Id || IsPlatformAdmin(actor);

    private static bool CanForwardToContracts(User actor, ProcurementRequestDetail detail)
    {
        if (IsPlatformAdmin(actor)) return true;
        if (detail.Document.AssigneeId == actor.Id) return true;
        return IsMarketingDeptCode(actor.Department?.Code) && IsDeptManager(actor);
    }

    private static bool IsDeptManager(User u) =>
        u.Role is UserRole.HONachalnik or UserRole.BMGMCNachalnikiOtdeli or UserRole.BMGMCManager;

    private static (ProcurementRegion region, string labelRu, string labelEn) ResolveRegion(Organization org)
    {
        if (org.Code == HoMasterData.OrganizationCode)
            return (ProcurementRegion.HeadOffice, "Ташкент — головной офис", "Tashkent Head Office");
        if (org.Code == BmgmcMasterData.OrganizationCode)
            return (ProcurementRegion.Bmgmc, "BMGMC", "BMGMC");
        return (ProcurementRegion.Station, org.Name, org.Name);
    }

    private async Task<(ProcurementRegion region, string labelRu, string labelEn)> ResolveRegionForUserAsync(
        User actor, CancellationToken ct)
    {
        if (actor.DepartmentId is null)
            return (ProcurementRegion.Bmgmc, "BMGMC", "BMGMC");
        var dept = await db.Departments.AsNoTracking()
            .Include(d => d.Organization)
            .FirstOrDefaultAsync(d => d.Id == actor.DepartmentId, ct);
        return dept is null ? (ProcurementRegion.Bmgmc, "BMGMC", "BMGMC") : ResolveRegion(dept.Organization);
    }

    private async Task NotifyNextApproverAsync(ProcurementRequestDetail detail, CancellationToken ct)
    {
        var next = GetNextPendingApprover(detail);
        if (next is null) return;
        await notifications.NotifyDcsApprovalRequiredAsync(
            next.UserId, detail.Document.Number, detail.Document.Title, detail.DocumentId, ct);
    }

    private async Task NotifyNextPlanApproverAsync(ProcurementRequestDetail detail, CancellationToken ct)
    {
        var next = GetNextPendingPlanApprover(detail);
        if (next is null) return;
        await notifications.NotifyMarketingPlanApprovalRequiredAsync(
            next.UserId, detail.Document.Number, detail.DocumentId, ct);
    }

    private async Task SetWorkTaskStatusAsync(Guid? taskId, WorkTaskStatus status, CancellationToken ct)
    {
        if (taskId is null) return;
        var task = await db.WorkTasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);
        if (task is null || task.Status == status) return;

        task.Status = status;
        task.UpdatedAt = DateTime.UtcNow;
        if (status == WorkTaskStatus.InProgress && task.StartedAt is null)
            task.StartedAt = DateTime.UtcNow;
        if (status == WorkTaskStatus.Done)
            task.CompletedAt = DateTime.UtcNow;
    }

    private Task NotifyLinkedTaskAsync(WorkTask task, CancellationToken ct) =>
        notifications.NotifyTaskAssignedAsync(
            task.AssigneeId, task.Number, task.Title, task.Id, task.Source, task.ExternalId, ct);

    private async Task NotifyLinkedTaskByIdAsync(Guid taskId, CancellationToken ct)
    {
        var task = await db.WorkTasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId, ct);
        if (task is not null)
            await NotifyLinkedTaskAsync(task, ct);
    }
}
