using ATG.Platform.Application.Common;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Data;
using ATG.Platform.Infrastructure.Dcs;
using ATG.Platform.Infrastructure.Seeds;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ATG.Platform.Infrastructure.Services;

public partial class ProcurementRequestService(AppDbContext db, IAuditService audit, IMarketingService marketing, IMarketingRfqChannelService rfqChannels, INotificationService notifications) : IProcurementRequestService
{
    private const string BmgmcTech = "BMGMC-TECH";
    private const string BmgmcAdm = "BMGMC-ADM";
    private const string BmgmcTrans = "BMGMC-TRANS";
    private const string HoMkt = "HO-MKT";
    private const string HoMktMkt = "HO-MKT-MKT";
    private const string HoMktTnd = "HO-MKT-TND";
    private const string HoCproc = "HO-CPROC";
    private const string HoCprocInt = "HO-CPROC-INT";
    private const string HoCprocDom = "HO-CPROC-DOM";
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
        Guid? tasResponsibleId = detail.TasResponsibleId;
        string? tasResponsibleName = detail.TasResponsible?.FullName;
        if (tasResponsibleId is null && detail.ResponsibleTaskId is not null)
        {
            var responsibleTask = await db.WorkTasks.AsNoTracking()
                .Include(t => t.Assignee)
                .FirstOrDefaultAsync(t => t.Id == detail.ResponsibleTaskId, ct);
            tasResponsibleId = responsibleTask?.AssigneeId;
            tasResponsibleName = responsibleTask?.Assignee?.FullName;
        }
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

        string? marketingRfqRegistrationNumber = null;
        DateTime? marketingRfqRegisteredAt = null;
        string? marketingProcurementPlanRegistrationNumber = null;
        DateTime? marketingProcurementPlanRegisteredAt = null;
        string? marketingProcurementPlanRegistrationMethod = null;
        var marketingRecord = await db.MarketingRecords.AsNoTracking()
            .Include(r => r.Offers)
            .Include(r => r.Plans)
            .FirstOrDefaultAsync(r => r.DocumentId == id, ct);

        string? mktDeptName = null;
        string? mktDeptNameEn = null;
        string? mktUserName = detail.MarketingSpecialist?.FullName;
        if (detail.MarketingSpecialistId is Guid mktId)
        {
            var mktUser = await db.Users.AsNoTracking()
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Id == mktId, ct);
            mktUserName = mktUser?.FullName ?? mktUserName;
            mktDeptName = mktUser?.Department?.Name;
            mktDeptNameEn = mktUser?.Department?.NameEn;
        }

        if (marketingRecord is not null)
        {
            if (marketingRecord.PortalNumber is not null
                && MarketingRegistrationNumberGenerator.IsRfqRegistrationNumber(marketingRecord.PortalNumber))
            {
                marketingRfqRegistrationNumber = marketingRecord.PortalNumber;
                marketingRfqRegisteredAt = marketingRecord.RfqPreparedAt
                    ?? marketingRecord.RegisteredDate?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            }

            var latestPlan = marketingRecord.Plans
                .Where(p => p.RegistrationNumber != null)
                .OrderByDescending(p => p.Version)
                .FirstOrDefault();
            if (latestPlan is not null)
            {
                marketingProcurementPlanRegistrationNumber = latestPlan.RegistrationNumber;
                marketingProcurementPlanRegisteredAt = latestPlan.RegisteredAt;
                marketingProcurementPlanRegistrationMethod = latestPlan.RegistrationMethod?.ToString();
            }
        }

        IReadOnlyList<ProcurementProcessDocumentDto> processDocuments;
        try
        {
            processDocuments = BuildProcessDocuments(
                detail, marketingRecord, mktDeptName, mktDeptNameEn, mktUserName);
        }
        catch
        {
            processDocuments = Array.Empty<ProcurementProcessDocumentDto>();
        }

        return Result<ProcurementRequestDto>.Ok(dto with
        {
            TasResponsibleId = tasResponsibleId,
            TasResponsibleName = tasResponsibleName,
            MarketingTaskNumber = marketingTaskNumber,
            ContractsTaskNumber = contractsTaskNumber,
            MarketingRfqRegistrationNumber = marketingRfqRegistrationNumber,
            MarketingRfqRegisteredAt = marketingRfqRegisteredAt,
            MarketingProcurementPlanRegistrationNumber = marketingProcurementPlanRegistrationNumber,
            MarketingProcurementPlanRegisteredAt = marketingProcurementPlanRegisteredAt,
            MarketingProcurementPlanRegistrationMethod = marketingProcurementPlanRegistrationMethod,
            MarketingPermissions = BuildMarketingPermissions(actor, detail, tasResponsibleId),
            ContractsPermissions = BuildContractsPermissions(actor, detail),
            PaymentPermissions = BuildPaymentPermissions(actor, detail),
            MarketingPlanPermissions = BuildMarketingPlanPermissions(actor, detail),
            ProcessDocuments = processDocuments,
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

        var subjectEn = request.SubjectEn?.Trim();
        var subjectRu = request.SubjectRu?.Trim();
        var legacySubject = request.ProcurementName?.Trim();
        if (string.IsNullOrWhiteSpace(subjectEn))
            subjectEn = legacySubject;
        if (string.IsNullOrWhiteSpace(subjectRu))
            subjectRu = legacySubject;

        if (string.IsNullOrWhiteSpace(subjectEn))
            return Result<ProcurementRequestDto>.Fail("Subject (English) is required");
        if (string.IsNullOrWhiteSpace(subjectRu))
            return Result<ProcurementRequestDto>.Fail("Subject (Russian) is required");
        if (subjectEn.Length > 500 || subjectRu.Length > 500)
            return Result<ProcurementRequestDto>.Fail("Subject must be 500 characters or less");
        if (request.Deadline == default)
            return Result<ProcurementRequestDto>.Fail("Deadline is required");
        if (request.TasRequisitionType is not (TasRequisitionType.MaterialRequest or TasRequisitionType.ServiceRequest))
            return Result<ProcurementRequestDto>.Fail("Request type (Material or Service) is required");

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
            Title = subjectEn,
            TitleRu = subjectRu,
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
            TasRequisitionType = request.TasRequisitionType,
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
            subjectEn,
            doc.Id, request.Priority, ct);
        task.DueDate = doc.DueDate;
        detail.ResponsibleTaskId = task.Id;
        detail.TasResponsibleId = responsible.Id;

        await SaveChangesWithTaskNumberRetryAsync(ct);
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

        var (region, regionRu, regionEn) = await ResolveRegionForUserAsync(actor, ct);
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
        try
        {
            await AddApproversAsync(detail, request.Approvers, ct);
        }
        catch (InvalidOperationException ex)
        {
            return Result<ProcurementRequestDto>.Fail(ex.Message);
        }
        await AddAttachmentsAsync(detail, request.Attachments ?? [], actorId, ct);
        await AddDocumentActivityAsync(doc, actorId, "created", null, DocumentStatus.InReview,
            "Express procurement request", ct);

        await SaveChangesWithTaskNumberRetryAsync(ct);
        await audit.LogAsync(actorId, "ProcurementRequestCreated", "Document", doc.Id, number, ip, ct);
        await NotifyNextApproverAsync(detail, ct);
        await NotifyStakeholdersOfPhaseMoveAsync(detail, "Approval", ct);

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
        await SaveChangesWithTaskNumberRetryAsync(ct);
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
        await SaveChangesWithTaskNumberRetryAsync(ct);
        await audit.LogAsync(actorId, "ProcurementSubmittedForApproval", "Document", id, "step6", ip, ct);
        await NotifyNextApproverAsync(detail, ct);
        await NotifyStakeholdersOfPhaseMoveAsync(detail, "Approval", ct);

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

        var deptId = await ResolveEngineerDepartmentIdForActorAsync(actor, ct)
            ?? actor.DepartmentId;
        if (deptId is null)
            return Result<IReadOnlyList<ProcurementRequestUserDto>>.Fail("Department is required");

        // Engineers in the configured section department + section head may self-assign.
        // Email constants (not helper methods) so EF can translate the filter to SQL.
        var intHeadEmail = DevTestAccounts.ContractsIntSectionHeadEmail;
        var domHeadEmail = DevTestAccounts.ContractsDomSectionHeadEmail;
        var users = await db.Users.AsNoTracking()
            .Include(u => u.Department)
            .Include(u => u.Organization)
            .Where(u => u.IsActive
                && u.DepartmentId == deptId
                && (u.Role == UserRole.HOEngineer
                    || u.Email == intHeadEmail
                    || u.Email == domHeadEmail
                    || u.Id == actor.Id))
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);

        return Result<IReadOnlyList<ProcurementRequestUserDto>>.Ok(users.Select(MapUser).ToList());
    }

    public async Task<Result<ProcurementRequestDto>> RouteContractsSectionAsync(
        Guid id, RouteContractsSectionRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Contracts)
            return Result<ProcurementRequestDto>.Fail("Request is not at Contracts Department");
        // Only Contracts Department Head distributes to Local / International — never skip this step.
        if (detail.ContractsSubPhase != ProcurementContractsSubPhase.Pending)
            return Result<ProcurementRequestDto>.Fail("Section has already been routed by Contracts Department");
        if (detail.ContractsProcurementSection is not null)
            return Result<ProcurementRequestDto>.Fail("Procurement section is already selected");
        if (detail.MarketingSubPhase != ProcurementMarketingSubPhase.Completed)
            return Result<ProcurementRequestDto>.Fail("Marketing must be completed before Contracts distribution");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanRouteContractsSection(actor, detail))
            return Result<ProcurementRequestDto>.Fail("Access denied");

        if (string.IsNullOrWhiteSpace(request.Comment))
            return Result<ProcurementRequestDto>.Fail("Comment is required when routing to a section");

        try
        {
            await RouteContractsSectionInternalAsync(detail, request.Section, actorId, ct);
        }
        catch (InvalidOperationException ex)
        {
            return Result<ProcurementRequestDto>.Fail(ex.Message);
        }

        await PersistStepCommentAsync(detail, actorId, ProcurementWorkflowPhase.Contracts, 1,
            request.Comment.Trim(), ProcurementStepCommentKind.Assignment, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementContractsSectionRouted", "Document", id,
            request.Section.ToString(), ip, ct);
        if (detail.ContractsTaskId is Guid contractsTaskId)
            await NotifyLinkedTaskByIdAsync(contractsTaskId, ct);

        return await GetByIdAsync(id, actorId, ct);
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

    public async Task<Result<ProcurementRequestDto>> SelectContractsIntVariantAsync(
        Guid id, SelectContractsIntVariantRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Contracts)
            return Result<ProcurementRequestDto>.Fail("Request is not at Contracts");
        if (detail.ContractsProcurementSection != ContractsProcurementSectionType.International)
            return Result<ProcurementRequestDto>.Fail("Variant selection is only for International Procurement Section");
        if (detail.ContractsSubPhase != ProcurementContractsSubPhase.InProgress)
            return Result<ProcurementRequestDto>.Fail("Engineer must accept the request before selecting a variant");
        if (detail.ContractsIntVariant is not null)
            return Result<ProcurementRequestDto>.Fail("Procurement variant is already selected");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanSelectContractsIntVariant(actor, detail))
            return Result<ProcurementRequestDto>.Fail("Access denied");

        if (string.IsNullOrWhiteSpace(request.Comment))
            return Result<ProcurementRequestDto>.Fail("Comment is required when selecting a variant");

        if (!InternationalContractsIntSteps.IsSupported(request.Variant))
            return Result<ProcurementRequestDto>.Fail("This procurement variant is not available yet");

        detail.ContractsIntVariant = request.Variant;
        detail.ContractsIntVariantSelectedAt = DateTime.UtcNow;
        detail.ContractsIntCurrentStep = InternationalContractsIntSteps.FirstOperationalStep(request.Variant);
        detail.Document.UpdatedAt = DateTime.UtcNow;

        var variantLabel = InternationalContractsIntSteps.VariantLabelRu(request.Variant);
        await PersistStepCommentAsync(detail, actorId, ProcurementWorkflowPhase.Contracts, 0,
            $"Variant: {variantLabel} — {request.Comment.Trim()}", ProcurementStepCommentKind.StepCompletion, ct);
        await AddDocumentActivityAsync(detail.Document, actorId, "contracts_int_variant_selected", null,
            detail.Document.Status, variantLabel, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementContractsIntVariantSelected", "Document", id,
            request.Variant.ToString(), ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> CompleteContractsIntStepAsync(
        Guid id, int step, CompleteContractsIntStepRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Contracts)
            return Result<ProcurementRequestDto>.Fail("Request is not at Contracts");
        if (detail.ContractsIntVariant is not { } intVariant
            || !InternationalContractsIntSteps.IsSupported(intVariant))
            return Result<ProcurementRequestDto>.Fail("INT procurement workflow is not active");
        if (detail.ContractsSubPhase != ProcurementContractsSubPhase.InProgress)
            return Result<ProcurementRequestDto>.Fail("Contracts workflow is not in progress");
        if (detail.ContractsIntCurrentStep != step)
            return Result<ProcurementRequestDto>.Fail($"Expected step {detail.ContractsIntCurrentStep}, not {step}");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<ProcurementRequestDto>.Fail("Access denied");

        var stepDef = InternationalContractsIntSteps.GetDefinitions(intVariant)
            .FirstOrDefault(s => s.Number == step);
        if (stepDef is null) return Result<ProcurementRequestDto>.Fail("Invalid step");

        if (stepDef.RequiresSecretariat)
        {
            if (!detail.ContractsIntSecretariatPending || detail.ContractsIntSecretariatUserId != actorId)
                return Result<ProcurementRequestDto>.Fail("Only Tender Secretariat can complete this step");
        }
        else if (!CanCompleteContractsIntStep(actor, detail))
        {
            return Result<ProcurementRequestDto>.Fail("Access denied");
        }

        if (string.IsNullOrWhiteSpace(request.Comment))
            return Result<ProcurementRequestDto>.Fail("Comment is required when completing a step");

        var totalSteps = InternationalContractsIntSteps.TotalSteps(intVariant);
        if (step < InternationalContractsIntSteps.FirstOperationalStep(intVariant) || step > totalSteps)
            return Result<ProcurementRequestDto>.Fail("Invalid step number");

        if (stepDef.RequiresUpload
            && !detail.ContractsIntStepFiles.Any(f => f.StepNumber == step))
            return Result<ProcurementRequestDto>.Fail("Upload at least one document for this step");

        if (stepDef.RequiresApprovers)
        {
            var stepApprovers = detail.ContractsIntStepApprovers.Where(a => a.StepNumber == step).ToList();
            if (stepApprovers.Count == 0)
                return Result<ProcurementRequestDto>.Fail("Submit approvers for this step first");
            if (stepApprovers.Any(a => a.Status != ProcurementApproverStatus.Approved))
                return Result<ProcurementRequestDto>.Fail("All approvers must approve before completing this step");
        }

        if (stepDef.RequiresRegistration)
        {
            if (string.IsNullOrWhiteSpace(detail.ContractsIntContractRegistrationNumber))
            {
                detail.ContractsIntContractRegistrationNumber =
                    await ContractsIntRegistrationNumberGenerator.GenerateNextAsync(db, intVariant, ct);
            }
            detail.ContractsIntContractRegisteredAt ??= DateTime.UtcNow;
        }

        await PersistStepCommentAsync(detail, actorId, ProcurementWorkflowPhase.Contracts, step,
            request.Comment.Trim(), ProcurementStepCommentKind.StepCompletion, ct);

        if (stepDef.RequiresSecretariat)
        {
            detail.ContractsIntSecretariatPending = false;
            if (detail.ContractsSpecialistId is Guid specialistId)
            {
                detail.Document.AssigneeId = specialistId;
                if (detail.ContractsTaskId is Guid contractsTaskId)
                {
                    var task = await db.WorkTasks.FirstOrDefaultAsync(t => t.Id == contractsTaskId, ct);
                    if (task is not null)
                    {
                        task.AssigneeId = specialistId;
                        task.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }
        }

        if (step >= totalSteps)
        {
            detail.ContractsIntCurrentStep = totalSteps + 1;
            detail.ContractsIntCompletedAt = DateTime.UtcNow;
            detail.ContractsSubPhase = ProcurementContractsSubPhase.Completed;
            await SetWorkTaskStatusAsync(detail.ContractsTaskId, WorkTaskStatus.Done, ct);
            var completedLabel = InternationalContractsIntSteps.VariantLabelEn(intVariant);
            await AddDocumentActivityAsync(detail.Document, actorId, "contracts_int_completed", null,
                detail.Document.Status, $"{completedLabel} workflow completed", ct);
            try
            {
                await HandoffToPaymentAsync(detail, actorId, ct);
            }
            catch (InvalidOperationException ex)
            {
                return Result<ProcurementRequestDto>.Fail(ex.Message);
            }
        }
        else
        {
            detail.ContractsIntCurrentStep = step + 1;
            await AddDocumentActivityAsync(detail.Document, actorId, $"contracts_int_step_{step}_completed", null,
                detail.Document.Status, request.Comment.Trim(), ct);
        }

        detail.Document.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementContractsIntStepCompleted", "Document", id, $"step={step}", ip, ct);

        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> AddContractsIntStepFileAsync(
        Guid id, int step, ContractsIntStepFileInput request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Contracts || detail.ContractsIntCurrentStep != step)
            return Result<ProcurementRequestDto>.Fail("Step is not active");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanCompleteContractsIntStep(actor, detail) || detail.ContractsIntSecretariatPending)
            return Result<ProcurementRequestDto>.Fail("Access denied");

        if (string.IsNullOrWhiteSpace(request.FileName) || string.IsNullOrWhiteSpace(request.StorageKey))
            return Result<ProcurementRequestDto>.Fail("File is required");

        var file = new ProcurementContractsIntStepFile
        {
            Id = Guid.NewGuid(),
            DocumentId = id,
            StepNumber = step,
            FileName = Path.GetFileName(request.FileName.Trim()),
            StorageKey = request.StorageKey.Trim(),
            UploadedById = actorId,
            UploadedAt = DateTime.UtcNow,
        };
        db.ProcurementContractsIntStepFiles.Add(file);
        detail.Document.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementContractsIntStepFileUploaded", "Document", id, $"step={step}", ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> SubmitContractsIntStepApproversAsync(
        Guid id, int step, SubmitContractsIntStepApproversRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Contracts || detail.ContractsIntCurrentStep != step)
            return Result<ProcurementRequestDto>.Fail("Step is not active");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanCompleteContractsIntStep(actor, detail) || detail.ContractsIntSecretariatPending)
            return Result<ProcurementRequestDto>.Fail("Access denied");

        if (request.UserIds is null || request.UserIds.Count == 0)
            return Result<ProcurementRequestDto>.Fail("Select at least one approver");

        var existing = detail.ContractsIntStepApprovers.Where(a => a.StepNumber == step).ToList();
        if (existing.Count > 0)
            return Result<ProcurementRequestDto>.Fail("Approvers already submitted for this step");

        var order = 0;
        foreach (var userId in request.UserIds.Distinct())
        {
            var exists = await db.Users.AsNoTracking().AnyAsync(u => u.Id == userId && u.IsActive, ct);
            if (!exists) return Result<ProcurementRequestDto>.Fail("Approver not found");
            db.ProcurementContractsIntStepApprovers.Add(new ProcurementContractsIntStepApprover
            {
                Id = Guid.NewGuid(),
                DocumentId = id,
                StepNumber = step,
                UserId = userId,
                SortOrder = order++,
                Status = ProcurementApproverStatus.Pending,
            });
        }

        detail.Document.UpdatedAt = DateTime.UtcNow;
        await AddDocumentActivityAsync(detail.Document, actorId, "contracts_int_approvers_submitted", null,
            detail.Document.Status, $"Step {step}: {request.UserIds.Count} approver(s)", ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementContractsIntApproversSubmitted", "Document", id, $"step={step}", ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> DecideContractsIntStepApprovalAsync(
        Guid id, int step, DecideContractsIntStepApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Contracts || detail.ContractsIntCurrentStep != step)
            return Result<ProcurementRequestDto>.Fail("Step is not active");

        var pending = detail.ContractsIntStepApprovers
            .Where(a => a.StepNumber == step && a.Status == ProcurementApproverStatus.Pending)
            .OrderBy(a => a.SortOrder)
            .FirstOrDefault();
        if (pending is null) return Result<ProcurementRequestDto>.Fail("No pending approval");
        if (pending.UserId != actorId)
            return Result<ProcurementRequestDto>.Fail("Wait for the previous approver");

        if (!request.Approve && string.IsNullOrWhiteSpace(request.Comment))
            return Result<ProcurementRequestDto>.Fail("Comment is required when rejecting");

        pending.Status = request.Approve ? ProcurementApproverStatus.Approved : ProcurementApproverStatus.Rejected;
        pending.DecidedAt = DateTime.UtcNow;
        pending.Comment = request.Comment?.Trim();
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddDocumentActivityAsync(detail.Document, actorId,
            request.Approve ? "contracts_int_step_approved" : "contracts_int_step_rejected",
            null, detail.Document.Status, $"Step {step}", ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementContractsIntStepApproval", "Document", id,
            $"step={step};approve={request.Approve}", ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> SendContractsIntToSecretariatAsync(
        Guid id, int step, SendContractsIntToSecretariatRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Contracts || detail.ContractsIntCurrentStep != step)
            return Result<ProcurementRequestDto>.Fail("Step is not active");
        if (detail.ContractsIntSecretariatPending)
            return Result<ProcurementRequestDto>.Fail("Already sent to Tender Secretariat");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanCompleteContractsIntStep(actor, detail))
            return Result<ProcurementRequestDto>.Fail("Access denied");

        if (string.IsNullOrWhiteSpace(request.Comment))
            return Result<ProcurementRequestDto>.Fail("Comment is required");

        var secretariat = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == DevTestAccounts.TenderSecretariatEmail && u.IsActive, ct);
        if (secretariat is null)
            return Result<ProcurementRequestDto>.Fail("Tender Secretariat user not found (user8@atg.uz)");

        detail.ContractsIntSecretariatPending = true;
        detail.ContractsIntSecretariatUserId = secretariat.Id;
        detail.Document.AssigneeId = secretariat.Id;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        if (detail.ContractsTaskId is Guid taskId)
        {
            var task = await db.WorkTasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);
            if (task is not null)
            {
                task.AssigneeId = secretariat.Id;
                if (secretariat.DepartmentId is Guid secDeptId)
                    task.DepartmentId = secDeptId;
                task.UpdatedAt = DateTime.UtcNow;
            }
        }

        await PersistStepCommentAsync(detail, actorId, ProcurementWorkflowPhase.Contracts, step,
            request.Comment.Trim(), ProcurementStepCommentKind.Assignment, ct);
        await AddDocumentActivityAsync(detail.Document, actorId, "contracts_int_sent_to_secretariat", null,
            detail.Document.Status, secretariat.FullName, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementContractsIntSentToSecretariat", "Document", id, null, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<IReadOnlyList<ProcurementRequestUserDto>>> GetPaymentWorkersAsync(
        Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || (!IsPaymentSectionHead(actor) && !IsPlatformAdmin(actor)))
            return Result<IReadOnlyList<ProcurementRequestUserDto>>.Fail("Access denied");

        var deptId = actor.DepartmentId;
        if (deptId is null)
            return Result<IReadOnlyList<ProcurementRequestUserDto>>.Fail("Department is required");

        var paymentHeadEmail = DevTestAccounts.PaymentSectionHeadEmail;
        var users = await db.Users.AsNoTracking()
            .Include(u => u.Department)
            .Include(u => u.Organization)
            .Where(u => u.IsActive && u.DepartmentId == deptId
                && (u.Role == UserRole.HOEngineer || u.Email == paymentHeadEmail || u.Id == actor.Id))
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);

        return Result<IReadOnlyList<ProcurementRequestUserDto>>.Ok(users.Select(MapUser).ToList());
    }

    public async Task<Result<ProcurementRequestDto>> AssignPaymentSpecialistAsync(
        Guid id, AssignContractsSpecialistRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Payment)
            return Result<ProcurementRequestDto>.Fail("Request is not at Payment");
        if (detail.PaymentSubPhase != ProcurementPaymentSubPhase.Pending)
            return Result<ProcurementRequestDto>.Fail("Specialist already assigned");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || (!IsPaymentSectionHead(actor) && !IsPlatformAdmin(actor) && detail.Document.AssigneeId != actor.Id))
            return Result<ProcurementRequestDto>.Fail("Access denied");

        if (string.IsNullOrWhiteSpace(request.Comment))
            return Result<ProcurementRequestDto>.Fail("Comment is required when assigning");
        if (request.SpecialistId == Guid.Empty)
            return Result<ProcurementRequestDto>.Fail("Select a specialist");

        var specialist = await db.Users.Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == request.SpecialistId && u.IsActive, ct);
        if (specialist is null)
            return Result<ProcurementRequestDto>.Fail("Specialist not found");

        detail.PaymentSpecialistId = specialist.Id;
        detail.PaymentAssignedAt = DateTime.UtcNow;
        detail.PaymentAcceptedAt = null;
        detail.PaymentSubPhase = ProcurementPaymentSubPhase.WaitingAccept;
        detail.Document.AssigneeId = specialist.Id;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        if (detail.PaymentTaskId is Guid taskId)
        {
            var task = await db.WorkTasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);
            if (task is not null)
            {
                task.AssigneeId = specialist.Id;
                task.UpdatedAt = DateTime.UtcNow;
            }
        }

        await PersistStepCommentAsync(detail, actorId, ProcurementWorkflowPhase.Contracts, 0,
            request.Comment.Trim(), ProcurementStepCommentKind.Assignment, ct);
        await AddDocumentActivityAsync(detail.Document, actorId, "payment_assigned", null,
            detail.Document.Status, specialist.FullName, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementPaymentAssigned", "Document", id, null, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    public async Task<Result<ProcurementRequestDto>> AcceptPaymentAsync(
        Guid id, AcceptContractsRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Payment)
            return Result<ProcurementRequestDto>.Fail("Request is not at Payment");
        if (detail.PaymentSubPhase != ProcurementPaymentSubPhase.WaitingAccept)
            return Result<ProcurementRequestDto>.Fail("Request is not awaiting acceptance");
        if (detail.PaymentSpecialistId != actorId)
            return Result<ProcurementRequestDto>.Fail("Only the assigned specialist can accept");

        if (string.IsNullOrWhiteSpace(request.Comment))
            return Result<ProcurementRequestDto>.Fail("Comment is required when accepting");

        detail.PaymentAcceptedAt = DateTime.UtcNow;
        detail.PaymentSubPhase = ProcurementPaymentSubPhase.InProgress;
        detail.Document.UpdatedAt = DateTime.UtcNow;
        await SetWorkTaskStatusAsync(detail.PaymentTaskId, WorkTaskStatus.InProgress, ct);
        await PersistStepCommentAsync(detail, actorId, ProcurementWorkflowPhase.Contracts, 0,
            request.Comment.Trim(), ProcurementStepCommentKind.Acceptance, ct);
        await AddDocumentActivityAsync(detail.Document, actorId, "payment_accepted", null,
            detail.Document.Status, request.Comment.Trim(), ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementPaymentAccepted", "Document", id, null, ip, ct);
        return await GetByIdAsync(id, actorId, ct);
    }

    private async Task HandoffToPaymentAsync(ProcurementRequestDetail detail, Guid actorId, CancellationToken ct)
    {
        var paymentHead = await db.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Email == DevTestAccounts.PaymentSectionHeadEmail && u.IsActive, ct)
            ?? throw new InvalidOperationException("Payment section head not found");

        var paymentDept = paymentHead.DepartmentId
            ?? throw new InvalidOperationException("Payment department not found");
        var hoOrg = await db.Organizations.FirstAsync(o => o.Code == HoMasterData.OrganizationCode, ct);

        var task = await CreateLinkedTaskAsync(
            paymentHead.Id, actorId, paymentDept, hoOrg.Id,
            $"Payment — {detail.Document.Number}",
            detail.Document.Title,
            detail.Document.Id, detail.Priority, ct);

        detail.Phase = ProcurementRequestPhase.Payment;
        detail.PaymentTaskId = task.Id;
        detail.PaymentSubPhase = ProcurementPaymentSubPhase.Pending;
        detail.PaymentSpecialistId = null;
        detail.PaymentAssignedAt = null;
        detail.PaymentAcceptedAt = null;
        detail.Document.DepartmentId = paymentDept;
        detail.Document.AssigneeId = paymentHead.Id;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddDocumentActivityAsync(detail.Document, actorId, "handed_off_to_payment", null,
            detail.Document.Status, paymentHead.FullName, ct);

        await NotifyStakeholdersOfPhaseMoveAsync(detail, "Payment", ct);
    }

    public async Task<Result<IReadOnlyList<ProcurementRequestUserDto>>> GetMarketingPlanApproverUsersAsync(
        Guid actorId, string? search, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<IReadOnlyList<ProcurementRequestUserDto>>.Fail("User not found");
        if (!IsMarketingStaff(actor) && !IsContractsStaff(actor) && !IsPlatformAdmin(actor))
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
        if (detail.MarketingCurrentStep != 7)
            return Result<ProcurementRequestDto>.Fail("Plan approval is only available on marketing step 7");

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
        if (detail.Phase != ProcurementRequestPhase.Marketing || detail.MarketingCurrentStep != 7)
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
        if (detail.Phase != ProcurementRequestPhase.Marketing || detail.MarketingCurrentStep != 7)
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
        if (detail.MarketingCurrentStep != 8)
            return Result<ProcurementRequestDto>.Fail("Registration is only available on marketing step 8");
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

        await PersistStepCommentAsync(detail, actorId, ProcurementWorkflowPhase.Marketing, 8,
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
        if (detail.ContractsSubPhase != ProcurementContractsSubPhase.SectionPending)
            return Result<ProcurementRequestDto>.Fail("Section must be routed before assigning an engineer");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanAssignContracts(actor, detail))
            return Result<ProcurementRequestDto>.Fail("Access denied");

        var engineerDeptId = await ResolveEngineerDepartmentIdForActorAsync(actor, ct)
            ?? actor.DepartmentId;
        if (engineerDeptId is null)
            return Result<ProcurementRequestDto>.Fail("Department is required");

        if (string.IsNullOrWhiteSpace(request.Comment))
            return Result<ProcurementRequestDto>.Fail("Comment is required when assigning");

        if (request.SpecialistId == Guid.Empty)
            return Result<ProcurementRequestDto>.Fail("Select an engineer from your department");

        try
        {
            await AssignContractsSpecialistInternalAsync(detail, request.SpecialistId, engineerDeptId.Value, actorId, ct);
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

    public async Task<Result<PagedResult<ProcurementMarketingQueueItemDto>>> GetMarketingQueueAsync(
        Guid actorId, int page, int pageSize, ProcurementMarketingSubPhase? subPhase, string? search, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsMarketingStaff(actor))
            return Result<PagedResult<ProcurementMarketingQueueItemDto>>.Fail("Access denied");

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = db.ProcurementRequestDetails.AsNoTracking()
            .Where(d => d.Phase == ProcurementRequestPhase.Marketing);

        if (subPhase.HasValue)
            query = query.Where(d => d.MarketingSubPhase == subPhase);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(d =>
                EF.Functions.ILike(d.Document.Number, term)
                || EF.Functions.ILike(d.Document.Title, term)
                || (d.Document.TitleRu != null && EF.Functions.ILike(d.Document.TitleRu, term)));
        }

        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderByDescending(d => d.Document.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new
            {
                d.DocumentId,
                d.Document.Number,
                d.Document.Title,
                d.Document.TitleRu,
                d.MarketingSubPhase,
                d.MarketingCurrentStep,
                AssigneeName = d.Document.Assignee != null ? d.Document.Assignee.FullName : null,
                SpecialistName = d.MarketingSpecialist != null ? d.MarketingSpecialist.FullName : null,
                UpdatedAt = d.Document.UpdatedAt,
                RegisteredAt = d.Document.RegisteredAt ?? d.Document.CreatedAt,
            })
            .ToListAsync(ct);

        var items = rows.Select(d =>
        {
            var stepDef = MarketingRequestSteps.Definitions
                .First(s => s.Number == Math.Min(d.MarketingCurrentStep, MarketingRequestSteps.TotalSteps));
            return new ProcurementMarketingQueueItemDto(
                d.DocumentId,
                d.Number,
                d.Title,
                d.TitleRu,
                d.MarketingSubPhase,
                d.MarketingCurrentStep,
                stepDef.TitleRu,
                stepDef.TitleEn,
                d.AssigneeName,
                d.SpecialistName,
                d.RegisteredAt,
                d.UpdatedAt);
        }).ToList();

        return Result<PagedResult<ProcurementMarketingQueueItemDto>>.Ok(
            new PagedResult<ProcurementMarketingQueueItemDto>(items, total, page, pageSize));
    }

    public async Task<Result<ProcurementMarketingQueueSummaryDto>> GetMarketingQueueSummaryAsync(
        Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsMarketingStaff(actor))
            return Result<ProcurementMarketingQueueSummaryDto>.Fail("Access denied");

        var baseQuery = db.ProcurementRequestDetails.AsNoTracking()
            .Where(d => d.Phase == ProcurementRequestPhase.Marketing);

        var total = await baseQuery.CountAsync(ct);
        var pending = await baseQuery.CountAsync(d => d.MarketingSubPhase == ProcurementMarketingSubPhase.Pending, ct);
        var inProgress = await baseQuery.CountAsync(d => d.MarketingSubPhase == ProcurementMarketingSubPhase.InProgress, ct);
        var completed = await baseQuery.CountAsync(d => d.MarketingSubPhase == ProcurementMarketingSubPhase.Completed, ct);

        return Result<ProcurementMarketingQueueSummaryDto>.Ok(
            new ProcurementMarketingQueueSummaryDto(total, pending, inProgress, completed));
    }

    public async Task<Result<PagedResult<ProcurementContractsQueueItemDto>>> GetContractsQueueAsync(
        Guid actorId,
        ContractsProcurementSectionType? section,
        int page,
        int pageSize,
        string? search,
        CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsContractsStaff(actor))
            return Result<PagedResult<ProcurementContractsQueueItemDto>>.Fail("Access denied");

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = db.ProcurementRequestDetails.AsNoTracking()
            .Where(d => d.Phase == ProcurementRequestPhase.Contracts);

        if (section.HasValue)
        {
            // Include unrouted items so dept head can pick them up from either section menu.
            query = query.Where(d =>
                d.ContractsProcurementSection == section
                || d.ContractsProcurementSection == null);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(d =>
                EF.Functions.ILike(d.Document.Number, term)
                || EF.Functions.ILike(d.Document.Title, term)
                || (d.Document.TitleRu != null && EF.Functions.ILike(d.Document.TitleRu, term)));
        }

        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderByDescending(d => d.Document.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new ProcurementContractsQueueItemDto(
                d.DocumentId,
                d.Document.Number,
                d.Document.Title,
                d.Document.TitleRu,
                d.ContractsProcurementSection,
                d.ContractsSubPhase,
                d.Document.Assignee != null ? d.Document.Assignee.FullName : null,
                d.ContractsSpecialist != null ? d.ContractsSpecialist.FullName : null,
                d.Document.UpdatedAt))
            .ToListAsync(ct);

        return Result<PagedResult<ProcurementContractsQueueItemDto>>.Ok(
            new PagedResult<ProcurementContractsQueueItemDto>(rows, total, page, pageSize));
    }

    public async Task<Result<IReadOnlyList<ProcurementContractsBoardColumnDto>>> GetContractsBoardAsync(
        Guid actorId,
        ContractsProcurementSectionType section,
        CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsContractsStaff(actor))
            return Result<IReadOnlyList<ProcurementContractsBoardColumnDto>>.Fail("Access denied");

        var rows = await db.ProcurementRequestDetails.AsNoTracking()
            .Where(d => d.Phase == ProcurementRequestPhase.Contracts)
            .Where(d => d.ContractsProcurementSection == section || d.ContractsProcurementSection == null)
            .OrderByDescending(d => d.Document.UpdatedAt)
            .Select(d => new ProcurementContractsBoardItemDto(
                d.DocumentId,
                d.Document.Number,
                d.Document.Title,
                d.Document.TitleRu,
                d.ContractsProcurementSection,
                d.ContractsSubPhase,
                d.Document.Assignee != null ? d.Document.Assignee.FullName : null,
                d.ContractsSpecialist != null ? d.ContractsSpecialist.FullName : null,
                d.ContractsDomVariant != null ? d.ContractsDomVariant.ToString() : null,
                d.ContractsIntVariant != null ? d.ContractsIntVariant.ToString() : null,
                d.ContractsDomCurrentStep,
                d.ContractsIntCurrentStep,
                d.Document.UpdatedAt))
            .ToListAsync(ct);

        var columns = new[]
        {
            (ProcurementContractsSubPhase.Pending, "Ожидает маршрутизации", "Awaiting routing"),
            (ProcurementContractsSubPhase.SectionPending, "Назначение инженера", "Engineer assignment"),
            (ProcurementContractsSubPhase.WaitingAccept, "Ожидает принятия", "Awaiting acceptance"),
            (ProcurementContractsSubPhase.InProgress, "В работе", "In progress"),
            (ProcurementContractsSubPhase.Completed, "Завершено", "Completed"),
        };

        var result = columns.Select(c => new ProcurementContractsBoardColumnDto(
            c.Item1,
            c.Item2,
            c.Item3,
            rows.Where(r => r.ContractsSubPhase == c.Item1).ToList())).ToList();

        return Result<IReadOnlyList<ProcurementContractsBoardColumnDto>>.Ok(result);
    }

    public async Task<Result<IReadOnlyList<ProcurementRequestUserDto>>> GetMarketingWorkersAsync(
        Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsMarketingStaff(actor))
            return Result<IReadOnlyList<ProcurementRequestUserDto>>.Fail("Access denied");

        var configuredDeptId = await ResolveEngineerDepartmentIdForRoleAsync(
            ProcurementWorkflowRoleKey.MarketingSectionHead, ct);
        List<Guid> deptIds;
        if (configuredDeptId is Guid cfgDept)
            deptIds = [cfgDept];
        else
            deptIds = await GetMarketingSectionDepartmentIdsAsync(ct);

        var users = await db.Users.AsNoTracking()
            .Include(u => u.Department)
            .Include(u => u.Organization)
            .Where(u => u.IsActive && u.DepartmentId != null && deptIds.Contains(u.DepartmentId.Value)
                && (u.Role == UserRole.HOEngineer
                    || u.Email == DevTestAccounts.MarketingSectionHeadEmail
                    || u.Id == actor.Id))
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);

        return Result<IReadOnlyList<ProcurementRequestUserDto>>.Ok(users.Select(MapUser).ToList());
    }

    public async Task<Result<ProcurementWorkflowRolesAdminDto>> GetWorkflowRolesAdminAsync(
        Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsPlatformAdmin(actor))
            return Result<ProcurementWorkflowRolesAdminDto>.Fail("Access denied");

        await EnsureWorkflowRolesSeededAsync(ct);

        var roles = new List<ProcurementWorkflowRoleDto>();
        foreach (var key in Enum.GetValues<ProcurementWorkflowRoleKey>())
            roles.Add(await MapWorkflowRoleAsync(key, ct));

        var managers = await db.Users.AsNoTracking()
            .Include(u => u.Department)
            .Include(u => u.Organization)
            .Where(u => u.IsActive && (
                u.Role == UserRole.HONachalnik
                || u.Role == UserRole.HOTopManager
                || u.Role == UserRole.SuperAdmin
                || u.Role == UserRole.HOEngineer))
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Take(300)
            .ToListAsync(ct);

        var departments = await db.Departments.AsNoTracking()
            .Where(d => d.IsActive && d.Organization.Code == HoMasterData.OrganizationCode)
            .OrderBy(d => d.Code)
            .Select(d => new ProcurementDepartmentOptionDto(d.Id, d.Code, d.Name, d.NameEn))
            .ToListAsync(ct);

        return Result<ProcurementWorkflowRolesAdminDto>.Ok(new ProcurementWorkflowRolesAdminDto(
            roles, managers.Select(MapUser).ToList(), departments));
    }

    public async Task<Result<ProcurementWorkflowRoleDto>> UpdateWorkflowRoleAsync(
        Guid actorId, string roleKey, UpdateProcurementWorkflowRoleRequest request, string? ip, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsPlatformAdmin(actor))
            return Result<ProcurementWorkflowRoleDto>.Fail("Access denied");

        if (!Enum.TryParse<ProcurementWorkflowRoleKey>(roleKey, ignoreCase: true, out var key))
            return Result<ProcurementWorkflowRoleDto>.Fail("Unknown role key");

        await EnsureWorkflowRolesSeededAsync(ct);

        if (request.ManagerUserId is Guid managerId)
        {
            var managerExists = await db.Users.AnyAsync(u => u.Id == managerId && u.IsActive, ct);
            if (!managerExists) return Result<ProcurementWorkflowRoleDto>.Fail("Manager user not found");
        }

        if (request.EngineerDepartmentId is Guid deptId)
        {
            var deptExists = await db.Departments.AnyAsync(d => d.Id == deptId && d.IsActive, ct);
            if (!deptExists) return Result<ProcurementWorkflowRoleDto>.Fail("Department not found");
        }

        var assignment = await db.ProcurementWorkflowRoleAssignments
            .FirstOrDefaultAsync(a => a.RoleKey == key, ct);
        if (assignment is null)
        {
            assignment = new ProcurementWorkflowRoleAssignment { RoleKey = key };
            db.ProcurementWorkflowRoleAssignments.Add(assignment);
        }

        assignment.ManagerUserId = request.ManagerUserId;
        assignment.EngineerDepartmentId = request.EngineerDepartmentId;
        assignment.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementWorkflowRoleUpdated", "ProcurementWorkflowRole", null,
            $"{key}: manager={request.ManagerUserId}, dept={request.EngineerDepartmentId}", ip, ct);

        return Result<ProcurementWorkflowRoleDto>.Ok(await MapWorkflowRoleAsync(key, ct));
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

    public async Task<Result<ProcurementRequestDto>> ReturnMarketingToInitiatorAsync(
        Guid id, ReturnMarketingToInitiatorRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<ProcurementRequestDto>.Fail("Request not found");
        if (detail.Phase != ProcurementRequestPhase.Marketing || detail.MarketingCurrentStep != 1)
            return Result<ProcurementRequestDto>.Fail("Return for revision is only available at marketing step 1");
        if (detail.MarketingSubPhase == ProcurementMarketingSubPhase.Completed)
            return Result<ProcurementRequestDto>.Fail("Marketing is already completed");
        if (detail.MarketingSubPhase is not (ProcurementMarketingSubPhase.Pending or ProcurementMarketingSubPhase.WaitingAccept))
            return Result<ProcurementRequestDto>.Fail("Return for revision is not available after the engineer has accepted");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanAssignMarketing(actor, detail))
            return Result<ProcurementRequestDto>.Fail("Access denied");

        if (string.IsNullOrWhiteSpace(request.Comment))
            return Result<ProcurementRequestDto>.Fail("Comment is required when returning for revision");

        var initiatorId = detail.InitiatorId ?? detail.Document.AuthorId;
        var initiator = await db.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == initiatorId, ct);
        if (initiator is null)
            return Result<ProcurementRequestDto>.Fail("Initiator not found");

        await SetWorkTaskStatusAsync(detail.MarketingTaskId, WorkTaskStatus.Done, ct);

        detail.Phase = ProcurementRequestPhase.InProgress;
        detail.MarketingSubPhase = ProcurementMarketingSubPhase.Pending;
        detail.MarketingCurrentStep = 1;
        detail.MarketingSpecialistId = null;
        detail.MarketingAssignedAt = null;
        detail.MarketingAcceptedAt = null;
        detail.MarketingActiveBranch = null;
        detail.MarketingBranchStartedAt = null;
        detail.MarketingTaskId = null;

        detail.Document.AssigneeId = initiator.Id;
        if (initiator.DepartmentId is Guid deptId)
            detail.Document.DepartmentId = deptId;
        detail.Document.OrganizationId = initiator.OrganizationId;
        detail.Document.Status = DocumentStatus.InReview;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        var revisionTask = await CreateLinkedTaskAsync(
            initiator.Id, actorId, initiator.DepartmentId!.Value, initiator.OrganizationId,
            $"Revise procurement request — {detail.Document.Number}",
            detail.Document.Title,
            detail.Document.Id, detail.Priority, ct);
        detail.ResponsibleTaskId = revisionTask.Id;

        await PersistStepCommentAsync(detail, actorId, ProcurementWorkflowPhase.Marketing, 1,
            request.Comment.Trim(), ProcurementStepCommentKind.StepCompletion, ct);
        await AddDocumentActivityAsync(detail.Document, actorId, "marketing_returned_to_initiator", null,
            detail.Document.Status, request.Comment.Trim(), ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ProcurementMarketingReturnedToInitiator", "Document", id, null, ip, ct);

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

        if (step == 3)
        {
            var step3 = await rfqChannels.ValidateStep3RfqCompletionAsync(id, ct);
            if (!step3.Ok)
                return Result<ProcurementRequestDto>.Fail(step3.Error ?? "Step 3 requirements not met");
        }

        if (step == 4)
        {
            var step4 = await marketing.ValidateStep4ProposalsAsync(id, ct);
            if (!step4.Ok)
                return Result<ProcurementRequestDto>.Fail(step4.Error ?? "Step 4 requirements not met");
        }

        if (step == 6)
        {
            var step6 = await marketing.ValidateStep6PlanAsync(id, ct);
            if (!step6.Ok)
                return Result<ProcurementRequestDto>.Fail(step6.Error ?? "Step 6 requirements not met");
        }

        if (step == 8)
            return Result<ProcurementRequestDto>.Fail("Use registration confirmation for marketing step 8");

        if (step == 7)
        {
            if (detail.MarketingPlanApprovalSubmittedAt is null || detail.MarketingPlanApprovers.Count == 0)
                return Result<ProcurementRequestDto>.Fail("Submit procurement plan for approval first");
            if (!detail.MarketingPlanApprovers.All(a => a.Status == ProcurementApproverStatus.Approved))
                return Result<ProcurementRequestDto>.Fail("All plan approvers must approve before completing step 7");
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

    private async Task RouteContractsSectionInternalAsync(
        ProcurementRequestDetail detail, ContractsProcurementSectionType section, Guid actorId, CancellationToken ct)
    {
        var sectionDeptCode = SectionDeptCode(section);
        var sectionDept = await GetDepartmentAsync(sectionDeptCode, HoMasterData.OrganizationCode, ct)
            ?? throw new InvalidOperationException("Procurement section department not found");

        var sectionHead = await ResolveContractsSectionHeadForTypeAsync(section, ct);

        detail.ContractsProcurementSection = section;
        detail.ContractsSectionRoutedAt = DateTime.UtcNow;
        detail.ContractsSubPhase = ProcurementContractsSubPhase.SectionPending;
        detail.ContractsSpecialistId = null;
        detail.ContractsAssignedAt = null;
        detail.ContractsAcceptedAt = null;
        detail.Document.DepartmentId = sectionDept.Id;
        detail.Document.AssigneeId = sectionHead.Id;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        if (detail.ContractsTaskId is Guid taskId)
        {
            var task = await db.WorkTasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);
            if (task is not null)
            {
                task.AssigneeId = sectionHead.Id;
                task.DepartmentId = sectionDept.Id;
                task.UpdatedAt = DateTime.UtcNow;
            }
        }

        var sectionLabel = section == ContractsProcurementSectionType.International
            ? "International Procurement Section"
            : "Domestic Procurement Section";
        var sectionLabelRu = section == ContractsProcurementSectionType.International
            ? "Международный закуп"
            : "Местный закуп";
        await AddDocumentActivityAsync(detail.Document, actorId, "contracts_section_routed", null,
            detail.Document.Status, $"{sectionLabel} → {sectionHead.FullName}", ct);

        // Section head receives the request automatically after department head routes it.
        await notifications.NotifyContractsSectionAssignedAsync(
            sectionHead.Id, detail.Document.Number, sectionLabelRu, detail.DocumentId, ct);

        var sectionKey = section == ContractsProcurementSectionType.International
            ? "ContractsInt"
            : "ContractsDom";
        await NotifyStakeholdersOfPhaseMoveAsync(detail, sectionKey, ct);
    }

    private async Task AssignContractsSpecialistInternalAsync(
        ProcurementRequestDetail detail, Guid specialistId, Guid actorDeptId, Guid actorId, CancellationToken ct)
    {
        var specialist = await db.Users.Include(u => u.Department).FirstOrDefaultAsync(
            u => u.Id == specialistId && u.IsActive && u.DepartmentId == actorDeptId, ct)
            ?? throw new InvalidOperationException("Engineer must be from your department");

        if (specialist.Role == UserRole.HONachalnik
            && !DevTestAccounts.IsContractsSectionHeadEmail(specialist.Email))
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

        await notifications.NotifyContractsEngineerAssignedAsync(
            specialist.Id, detail.Document.Number, detail.DocumentId, ct);
    }

    /// <summary>
    /// Unified pipeline: every request (HO / BMGMC / TAS) enters Contracts Department first.
    /// Section (Local / International) is chosen only by Contracts Department Head afterwards.
    /// </summary>
    private async Task HandoffToContractsAsync(ProcurementRequestDetail detail, Guid actorId, CancellationToken ct)
    {
        if (detail.Phase == ProcurementRequestPhase.Contracts) return;
        if (detail.Phase != ProcurementRequestPhase.Marketing)
            throw new InvalidOperationException("Only Marketing can hand off to Contracts Department");
        if (detail.MarketingSubPhase != ProcurementMarketingSubPhase.Completed)
            throw new InvalidOperationException("Marketing workflow must be completed before forwarding to Contracts");

        var cprocDept = await GetDepartmentAsync(HoCproc, HoMasterData.OrganizationCode, ct)
            ?? throw new InvalidOperationException("Contracts department not found");

        var assignee = await ResolveContractsDepartmentHeadAsync(ct);
        var hoOrg = await db.Organizations.FirstAsync(o => o.Code == HoMasterData.OrganizationCode, ct);
        var task = await CreateLinkedTaskAsync(
            assignee.Id, actorId, cprocDept.Id, hoOrg.Id,
            $"Contracts review — {detail.Document.Number}",
            detail.Document.Title,
            detail.Document.Id, detail.Priority, ct);

        detail.Phase = ProcurementRequestPhase.Contracts;
        detail.ContractsTaskId = task.Id;
        // Always land on Contracts Department queue — never skip to INT/DOM.
        detail.ContractsSubPhase = ProcurementContractsSubPhase.Pending;
        detail.ContractsProcurementSection = null;
        detail.ContractsSectionRoutedAt = null;
        detail.ContractsSpecialistId = null;
        detail.ContractsAssignedAt = null;
        detail.ContractsAcceptedAt = null;
        detail.ContractsIntVariant = null;
        detail.ContractsIntCurrentStep = 0;
        detail.ContractsIntVariantSelectedAt = null;
        detail.ContractsIntCompletedAt = null;
        detail.ContractsDomVariant = null;
        detail.ContractsDomCurrentStep = 0;
        detail.ContractsDomVariantSelectedAt = null;
        detail.ContractsDomCompletedAt = null;
        detail.ContractsDomContractsAdminPending = false;
        detail.ContractsDomContractsAdminUserId = null;
        detail.ContractsDomPriceRequestDate = null;
        detail.ContractsDomPriceResponseDueDate = null;
        detail.ContractsDomDeliveryDueDate = null;
        detail.ContractsDomActualDeliveryDate = null;
        detail.ContractsDomLastTerminationAt = null;
        detail.Document.DepartmentId = cprocDept.Id;
        detail.Document.OrganizationId = hoOrg.Id;
        detail.Document.AssigneeId = assignee.Id;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddDocumentActivityAsync(detail.Document, actorId, "handoff_contracts", null,
            detail.Document.Status,
            $"Contracts Department (HO-CPROC) → {assignee.FullName}; section routing pending", ct);

        // Explicit inbox notification for Contracts Department Head (Local/International routing).
        await notifications.NotifyContractsRoutingRequiredAsync(
            assignee.Id, detail.Document.Number, detail.Document.Title, detail.DocumentId, ct);

        await NotifyStakeholdersOfPhaseMoveAsync(detail, "Contracts", ct);
    }

    private async Task<User> ResolveContractsDepartmentHeadAsync(CancellationToken ct)
    {
        var configured = await ResolveConfiguredManagerAsync(ProcurementWorkflowRoleKey.ContractsDepartmentHead, ct);
        if (configured is not null) return configured;

        var devHead = await db.Users
            .FirstOrDefaultAsync(u => u.IsActive && u.Email == DevTestAccounts.ContractsDepartmentHeadEmail, ct);
        if (devHead is not null)
            return devHead;

        var dept = await GetDepartmentAsync(HoCproc, HoMasterData.OrganizationCode, ct)
            ?? throw new InvalidOperationException("Contracts department not found");

        return await db.Users
            .Where(u => u.IsActive && u.DepartmentId == dept.Id && u.Role == UserRole.HONachalnik)
            .OrderBy(u => u.LastName)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Contracts department head not found");
    }

    private async Task<User> ResolveContractsSectionHeadForTypeAsync(
        ContractsProcurementSectionType section, CancellationToken ct)
    {
        var roleKey = section == ContractsProcurementSectionType.International
            ? ProcurementWorkflowRoleKey.ContractsIntSectionHead
            : ProcurementWorkflowRoleKey.ContractsDomSectionHead;
        var configured = await ResolveConfiguredManagerAsync(roleKey, ct);
        if (configured is not null) return configured;

        var email = section == ContractsProcurementSectionType.International
            ? DevTestAccounts.ContractsIntSectionHeadEmail
            : DevTestAccounts.ContractsDomSectionHeadEmail;
        var devHead = await db.Users
            .FirstOrDefaultAsync(u => u.IsActive && u.Email == email, ct);
        if (devHead is not null)
            return devHead;

        var sectionCode = SectionDeptCode(section);
        var sectionDept = await GetDepartmentAsync(sectionCode, HoMasterData.OrganizationCode, ct)
            ?? throw new InvalidOperationException("Procurement section not found");

        return await db.Users
            .Where(u => u.IsActive && u.DepartmentId == sectionDept.Id && u.Role == UserRole.HONachalnik)
            .OrderBy(u => u.LastName)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Procurement section head not found");
    }

    private async Task<User?> ResolveConfiguredManagerAsync(ProcurementWorkflowRoleKey roleKey, CancellationToken ct)
    {
        await EnsureWorkflowRolesSeededAsync(ct);
        var assignment = await db.ProcurementWorkflowRoleAssignments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.RoleKey == roleKey, ct);
        if (assignment?.ManagerUserId is not Guid managerId) return null;
        return await db.Users.FirstOrDefaultAsync(u => u.Id == managerId && u.IsActive, ct);
    }

    private async Task<Guid?> ResolveEngineerDepartmentIdForRoleAsync(
        ProcurementWorkflowRoleKey roleKey, CancellationToken ct)
    {
        await EnsureWorkflowRolesSeededAsync(ct);
        return await db.ProcurementWorkflowRoleAssignments.AsNoTracking()
            .Where(a => a.RoleKey == roleKey)
            .Select(a => a.EngineerDepartmentId)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<Guid?> ResolveEngineerDepartmentIdForActorAsync(User actor, CancellationToken ct)
    {
        await EnsureWorkflowRolesSeededAsync(ct);
        var managed = await db.ProcurementWorkflowRoleAssignments.AsNoTracking()
            .Where(a => a.ManagerUserId == actor.Id && a.EngineerDepartmentId != null)
            .Select(a => a.EngineerDepartmentId)
            .FirstOrDefaultAsync(ct);
        if (managed is not null) return managed;

        var code = actor.Department?.Code;
        var roleKey = code switch
        {
            HoMktMkt or HoMkt => ProcurementWorkflowRoleKey.MarketingSectionHead,
            HoCprocInt => ProcurementWorkflowRoleKey.ContractsIntSectionHead,
            HoCprocDom => ProcurementWorkflowRoleKey.ContractsDomSectionHead,
            HoCproc => ProcurementWorkflowRoleKey.ContractsDepartmentHead,
            _ => (ProcurementWorkflowRoleKey?)null,
        };
        if (roleKey is null) return actor.DepartmentId;
        return await ResolveEngineerDepartmentIdForRoleAsync(roleKey.Value, ct) ?? actor.DepartmentId;
    }

    private async Task EnsureWorkflowRolesSeededAsync(CancellationToken ct)
    {
        var existing = await db.ProcurementWorkflowRoleAssignments.Select(a => a.RoleKey).ToListAsync(ct);
        var defaults = new (ProcurementWorkflowRoleKey Key, string Email, string DeptCode)[]
        {
            (ProcurementWorkflowRoleKey.MarketingSectionHead, DevTestAccounts.MarketingSectionHeadEmail, HoMktMkt),
            (ProcurementWorkflowRoleKey.ContractsDepartmentHead, DevTestAccounts.ContractsDepartmentHeadEmail, HoCproc),
            (ProcurementWorkflowRoleKey.ContractsIntSectionHead, DevTestAccounts.ContractsIntSectionHeadEmail, HoCprocInt),
            (ProcurementWorkflowRoleKey.ContractsDomSectionHead, DevTestAccounts.ContractsDomSectionHeadEmail, HoCprocDom),
        };

        var added = false;
        foreach (var (key, email, deptCode) in defaults)
        {
            if (existing.Contains(key)) continue;
            var managerId = await db.Users.AsNoTracking()
                .Where(u => u.Email == email && u.IsActive)
                .Select(u => (Guid?)u.Id)
                .FirstOrDefaultAsync(ct);
            var deptId = await db.Departments.AsNoTracking()
                .Where(d => d.Code == deptCode && d.Organization.Code == HoMasterData.OrganizationCode)
                .Select(d => (Guid?)d.Id)
                .FirstOrDefaultAsync(ct);
            db.ProcurementWorkflowRoleAssignments.Add(new ProcurementWorkflowRoleAssignment
            {
                RoleKey = key,
                ManagerUserId = managerId,
                EngineerDepartmentId = deptId,
                UpdatedAt = DateTime.UtcNow,
            });
            added = true;
        }

        if (added) await db.SaveChangesAsync(ct);
    }

    private async Task<ProcurementWorkflowRoleDto> MapWorkflowRoleAsync(
        ProcurementWorkflowRoleKey key, CancellationToken ct)
    {
        var (titleRu, titleEn, descRu, descEn) = key switch
        {
            ProcurementWorkflowRoleKey.MarketingSectionHead => (
                "Маркетинг — начальник отдела",
                "Marketing — section head",
                "Назначает инженеров маркетинга (HO-MKT-MKT)",
                "Assigns marketing engineers (HO-MKT-MKT)"),
            ProcurementWorkflowRoleKey.ContractsDepartmentHead => (
                "Контракты — начальник департамента",
                "Contracts — department head",
                "Направляет заявки в Local или International",
                "Routes requests to Local or International"),
            ProcurementWorkflowRoleKey.ContractsIntSectionHead => (
                "International — начальник отдела",
                "International — section head",
                "Назначает инженеров международных закупок",
                "Assigns international procurement engineers"),
            ProcurementWorkflowRoleKey.ContractsDomSectionHead => (
                "Local — начальник отдела",
                "Local — section head",
                "Назначает инженеров локальных закупок",
                "Assigns domestic procurement engineers"),
            _ => (key.ToString(), key.ToString(), "", ""),
        };

        var assignment = await db.ProcurementWorkflowRoleAssignments.AsNoTracking()
            .Include(a => a.ManagerUser)
            .Include(a => a.EngineerDepartment)
            .FirstOrDefaultAsync(a => a.RoleKey == key, ct);

        var engineers = new List<ProcurementRequestUserDto>();
        if (assignment?.EngineerDepartmentId is Guid deptId)
        {
            var engUsers = await db.Users.AsNoTracking()
                .Include(u => u.Department)
                .Include(u => u.Organization)
                .Where(u => u.IsActive && u.DepartmentId == deptId
                    && (u.Role == UserRole.HOEngineer || u.Id == assignment.ManagerUserId))
                .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
                .ToListAsync(ct);
            engineers = engUsers.Select(MapUser).ToList();
        }

        return new ProcurementWorkflowRoleDto(
            key.ToString(),
            titleRu, titleEn, descRu, descEn,
            assignment?.ManagerUserId,
            assignment?.ManagerUser?.FullName,
            assignment?.ManagerUser?.Email,
            assignment?.EngineerDepartmentId,
            assignment?.EngineerDepartment?.Name,
            assignment?.EngineerDepartment?.NameEn,
            assignment?.EngineerDepartment?.Code,
            engineers);
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

    /// <summary>
    /// Unified pipeline: every request (HO Express, BMGMC Express, BMGMC TAS) enters Marketing first.
    /// Origin organization does not change the destination — always HO Marketing.
    /// </summary>
    private async Task HandoffToMarketingAsync(ProcurementRequestDetail detail, Guid actorId, CancellationToken ct)
    {
        if (detail.Phase is ProcurementRequestPhase.Marketing or ProcurementRequestPhase.Contracts
            or ProcurementRequestPhase.Completed)
            throw new InvalidOperationException("Request has already left initiation/approval");

        // Prefer marketing section (HO-MKT-MKT); fall back to parent HO-MKT.
        var mktSection = await GetDepartmentAsync(HoMktMkt, HoMasterData.OrganizationCode, ct);
        var mktDept = mktSection
            ?? await GetDepartmentAsync(HoMkt, HoMasterData.OrganizationCode, ct)
            ?? throw new InvalidOperationException("HO Marketing department not found");

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
        detail.MarketingSpecialistId = null;
        detail.MarketingAssignedAt = null;
        detail.MarketingAcceptedAt = null;
        detail.MarketingCompletedAt = null;
        detail.MarketingTaskId = task.Id;
        // Never carry initiator org into Marketing — always HO Marketing.
        detail.Document.Status = DocumentStatus.InReview;
        detail.Document.DepartmentId = mktDept.Id;
        detail.Document.OrganizationId = hoOrg.Id;
        detail.Document.AssigneeId = assignee.Id;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddDocumentActivityAsync(detail.Document, actorId, "handoff_marketing", DocumentStatus.Registered,
            DocumentStatus.InReview,
            $"Marketing ({mktDept.Code}) → {assignee.FullName}; origin={detail.Flow}", ct);

        var marketingResult = await marketing.CreateFromProcurementAsync(detail.DocumentId, ct);
        if (!marketingResult.IsSuccess)
            throw new InvalidOperationException(marketingResult.Error ?? "Failed to create marketing record");

        await NotifyStakeholdersOfPhaseMoveAsync(detail, "Marketing", ct);
    }

    private async Task<User> ResolveMarketingSectionHeadAsync(CancellationToken ct)
    {
        var configured = await ResolveConfiguredManagerAsync(ProcurementWorkflowRoleKey.MarketingSectionHead, ct);
        if (configured is not null) return configured;

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
            .Include(d => d.TasResponsible)
            .Include(d => d.InitiatorDepartment)
            .Include(d => d.MarketingSpecialist)
            .Include(d => d.ContractsSpecialist)
            .Include(d => d.ContractsIntSecretariatUser)
            .Include(d => d.ContractsDomContractsAdminUser)
            .Include(d => d.PaymentSpecialist)
            .Include(d => d.Approvers).ThenInclude(a => a.User).ThenInclude(u => u.Department)
            .Include(d => d.Approvers).ThenInclude(a => a.User).ThenInclude(u => u.Organization)
            .Include(d => d.MarketingPlanApprovers).ThenInclude(a => a.User).ThenInclude(u => u.Department)
            .Include(d => d.Attachments).ThenInclude(a => a.UploadedBy).ThenInclude(u => u.Department)
            .Include(d => d.ContractsIntStepFiles).ThenInclude(f => f.UploadedBy)
            .Include(d => d.ContractsIntStepApprovers).ThenInclude(a => a.User)
            .Include(d => d.ContractsDomStepFiles).ThenInclude(f => f.UploadedBy)
            .Include(d => d.ContractsDomStepApprovers).ThenInclude(a => a.User)
            .Include(d => d.StepComments).ThenInclude(c => c.Author)
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.DocumentId == id, ct);

    private async Task<ProcurementRequestDetail?> LoadDetailTrackedAsync(Guid id, CancellationToken ct) =>
        await db.ProcurementRequestDetails
            .Include(d => d.Document)
            .Include(d => d.Approvers)
            .Include(d => d.MarketingPlanApprovers)
            .Include(d => d.Attachments)
            .Include(d => d.ContractsIntStepFiles)
            .Include(d => d.ContractsIntStepApprovers)
            .Include(d => d.ContractsDomStepFiles)
            .Include(d => d.ContractsDomStepApprovers)
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
        d.TasRequisitionType,
        d.Document.DueDate,
        d.Document.OrganizationId,
        d.Document.Organization.Name,
        d.Document.DepartmentId,
        d.Document.Department.Name,
        d.Document.Department.NameEn,
        d.ResponsibleTaskId,
        d.TasResponsibleId,
        d.TasResponsible?.FullName,
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
        d.ContractsProcurementSection,
        d.ContractsSectionRoutedAt,
        d.ContractsSpecialistId,
        d.ContractsSpecialist?.FullName,
        d.ContractsAssignedAt,
        d.ContractsAcceptedAt,
        d.ContractsIntVariant,
        d.ContractsIntCurrentStep,
        d.ContractsIntVariantSelectedAt,
        d.ContractsIntCompletedAt,
        d.ContractsIntContractRegistrationNumber,
        d.ContractsIntContractRegisteredAt,
        d.ContractsIntSecretariatPending,
        d.ContractsIntSecretariatUserId,
        d.ContractsIntSecretariatUser?.FullName,
        d.ContractsDomVariant,
        d.ContractsDomCurrentStep,
        d.ContractsDomVariantSelectedAt,
        d.ContractsDomCompletedAt,
        d.ContractsDomContractRegistrationNumber,
        d.ContractsDomContractRegisteredAt,
        d.ContractsDomContractsAdminPending,
        d.ContractsDomContractsAdminUserId,
        d.ContractsDomContractsAdminUser?.FullName,
        d.ContractsDomPriceRequestDate,
        d.ContractsDomPriceResponseDueDate,
        d.ContractsDomDeliveryDueDate,
        d.ContractsDomActualDeliveryDate,
        d.ContractsDomLastTerminationAt,
        d.PaymentSubPhase,
        d.PaymentTaskId,
        d.PaymentSpecialistId,
        d.PaymentSpecialist?.FullName,
        d.PaymentAssignedAt,
        d.PaymentAcceptedAt,
        null,
        null,
        null,
        BuildContractsIntSteps(d),
        BuildContractsDomSteps(d),
        d.MarketingPlanApprovalSubmittedAt,
        d.MarketingPlanRegistrationNumber,
        d.MarketingPlanRegisteredAt,
        null,
        null,
        null,
        null,
        null,
        null,
        d.MarketingPlanApprovers.OrderBy(a => a.SortOrder).Select(a => new ProcurementMarketingPlanApproverDto(
            a.Id, a.UserId, a.User?.FullName ?? "—", a.Role, a.Status, a.SortOrder, a.DecidedAt, a.Comment,
            a.User?.Department?.Name, a.User?.Department?.NameEn, a.User?.Email ?? "")).ToList(),
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
            a.Id, a.UserId, a.User?.FullName ?? "—", a.Role, a.Status, a.SortOrder, a.DecidedAt, a.Comment,
            a.User?.Department?.Name, a.User?.Department?.NameEn,
            a.User?.Organization?.Name, null,
            a.User?.JobTitleRu, a.User?.JobTitleEn,
            a.User?.Email ?? "", a.User?.EmployeeId)).ToList(),
        d.Attachments.OrderByDescending(a => a.UploadedAt).Select(a => new ProcurementAttachmentDto(
            a.Id, a.Kind, a.FileName, a.StorageKey, a.UploadedBy?.FullName ?? "—", a.UploadedAt)).ToList(),
        Array.Empty<ProcurementProcessDocumentDto>(),
        d.Document.RegisteredAt,
        d.Document.Activities.OrderBy(a => a.CreatedAt).Select(a => new ProcurementTimelineEventDto(
            a.Id, a.Action, a.Actor?.FullName ?? "—", a.Details, a.CreatedAt)).ToList(),
        d.StepComments.OrderBy(c => c.CreatedAt).Select(c => new ProcurementStepCommentDto(
            c.Id, c.Phase, c.StepNumber, c.AuthorId, c.Author?.FullName ?? "—", c.Body, c.Kind, c.CreatedAt)).ToList(),
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
        u.Department?.Name ?? "", u.Department?.NameEn ?? "", u.Organization?.Name ?? "");

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
            Number = GenerateConflictSafeTaskNumber(),
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

    private async Task SaveChangesWithTaskNumberRetryAsync(CancellationToken ct)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await db.SaveChangesAsync(ct);
                return;
            }
            catch (DbUpdateException ex) when (attempt < maxAttempts && IsWorkTaskNumberConflict(ex))
            {
                var pendingTasks = db.ChangeTracker.Entries<WorkTask>()
                    .Where(e => e.State == EntityState.Added)
                    .Select(e => e.Entity)
                    .ToList();

                foreach (var task in pendingTasks)
                    task.Number = attempt == 1
                        ? await GenerateTaskNumberAsync(ct)
                        : GenerateConflictSafeTaskNumber();
            }
        }
    }

    private static bool IsWorkTaskNumberConflict(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg
        && pg.SqlState == PostgresErrorCodes.UniqueViolation
        && string.Equals(pg.ConstraintName, "IX_work_tasks_Number", StringComparison.Ordinal);

    private static string GenerateConflictSafeTaskNumber() =>
        $"TSK-{DateTime.UtcNow:yyMMddHHmmssfff}";

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
        await db.Users.AsNoTracking()
            .Include(u => u.Organization).ThenInclude(o => o.Parent)
            .Include(u => u.Department).ThenInclude(d => d!.Organization)
            .FirstOrDefaultAsync(u => u.Id == actorId && u.IsActive, ct);

    private static bool IsPlatformAdmin(User u) =>
        u.Role is UserRole.SuperAdmin or UserRole.HOTopManager;

    private static bool IsTasStaff(User u) => u.Department?.Code == BmgmcTech;

    private static bool CanCreateExpress(User u)
    {
        if (u.Organization?.Code == HoMasterData.OrganizationCode) return true;
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

    private static IReadOnlyList<ProcurementContractsIntStepDto>? BuildContractsIntSteps(ProcurementRequestDetail d)
    {
        if (d.ContractsProcurementSection != ContractsProcurementSectionType.International)
            return null;
        if (d.ContractsIntVariant is not { } variant || !InternationalContractsIntSteps.IsSupported(variant))
            return null;

        var files = d.ContractsIntStepFiles ?? [];
        var approvers = d.ContractsIntStepApprovers ?? [];

        return InternationalContractsIntSteps.GetDefinitions(variant)
            .Select(s =>
            {
                var stepFiles = files.Where(f => f.StepNumber == s.Number)
                    .OrderBy(f => f.UploadedAt)
                    .Select(f => new ProcurementContractsIntStepFileDto(
                        f.Id, f.StepNumber, f.FileName, f.StorageKey,
                        f.UploadedBy?.FullName ?? "—", f.UploadedAt))
                    .ToList();
                var stepApprovers = approvers.Where(a => a.StepNumber == s.Number)
                    .OrderBy(a => a.SortOrder)
                    .Select(a => new ProcurementContractsIntStepApproverDto(
                        a.Id, a.StepNumber, a.UserId, a.User?.FullName ?? "—", a.User?.Email ?? "",
                        a.Status, a.SortOrder, a.DecidedAt, a.Comment))
                    .ToList();
                var submitted = stepApprovers.Count > 0;
                var allApproved = submitted && stepApprovers.All(a => a.Status == ProcurementApproverStatus.Approved);
                var secretariatPending = s.RequiresSecretariat
                    && d.ContractsIntCurrentStep == s.Number
                    && d.ContractsIntSecretariatPending;
                return new ProcurementContractsIntStepDto(
                    s.Number, s.TitleRu, s.TitleEn, s.HintRu, s.HintEn,
                    s.HasBranch, s.BranchHintRu, s.BranchHintEn,
                    s.RequiresUpload, s.RequiresApprovers, s.RequiresSecretariat, s.RequiresRegistration,
                    stepFiles, stepApprovers, submitted, allApproved, secretariatPending);
            })
            .ToList();
    }

    private static bool CanCompleteContractsIntStepNow(ProcurementRequestDetail detail) =>
        detail.ContractsIntVariant is { } v
        && InternationalContractsIntSteps.IsSupported(v)
        && detail.ContractsSubPhase == ProcurementContractsSubPhase.InProgress
        && detail.ContractsIntCurrentStep >= InternationalContractsIntSteps.FirstOperationalStep(v)
        && detail.ContractsIntCurrentStep <= InternationalContractsIntSteps.TotalSteps(v);

    private static ProcurementContractsPermissionsDto BuildContractsPermissions(User actor, ProcurementRequestDetail detail)
    {
        if (detail.Phase != ProcurementRequestPhase.Contracts)
            return new ProcurementContractsPermissionsDto(false, false, false, false, false, false, false, false, false, false, 0,
                false, false, false, false, false, false, false, false, false, false, 0);

        var intStep = detail.ContractsIntCurrentStep;
        var intStepDef = GetCurrentIntStepDefinition(detail);
        var isIntEngineer = CanCompleteContractsIntStep(actor, detail) && !detail.ContractsIntSecretariatPending;
        var isSecretariat = detail.ContractsIntSecretariatPending
            && detail.ContractsIntSecretariatUserId == actor.Id;
        var intPendingApprover = detail.ContractsIntStepApprovers
            .Where(a => a.StepNumber == intStep && a.Status == ProcurementApproverStatus.Pending)
            .OrderBy(a => a.SortOrder)
            .FirstOrDefault();

        var domStep = detail.ContractsDomCurrentStep;
        var domStepDef = GetCurrentDomStepDefinition(detail);
        var isDomEngineer = CanCompleteContractsDomStep(actor, detail) && !detail.ContractsDomContractsAdminPending;
        var isContractsAdmin = detail.ContractsDomContractsAdminPending
            && detail.ContractsDomContractsAdminUserId == actor.Id;
        var domPendingApprover = detail.ContractsDomStepApprovers
            .Where(a => a.StepNumber == domStep && a.Status == ProcurementApproverStatus.Pending)
            .OrderBy(a => a.SortOrder)
            .FirstOrDefault();

        return new ProcurementContractsPermissionsDto(
            detail.ContractsSubPhase == ProcurementContractsSubPhase.WaitingAccept
                && detail.ContractsSpecialistId == actor.Id,
            detail.ContractsSubPhase == ProcurementContractsSubPhase.SectionPending
                && CanAssignContracts(actor, detail),
            detail.ContractsSubPhase == ProcurementContractsSubPhase.Pending
                && detail.ContractsProcurementSection is null
                && CanRouteContractsSection(actor, detail),
            detail.ContractsProcurementSection == ContractsProcurementSectionType.International
                && detail.ContractsSubPhase == ProcurementContractsSubPhase.InProgress
                && detail.ContractsIntVariant is null
                && CanSelectContractsIntVariant(actor, detail),
            CanCompleteContractsIntStepNow(detail)
                && ((isIntEngineer && intStepDef is not { RequiresSecretariat: true })
                    || (isSecretariat && intStepDef is { RequiresSecretariat: true })),
            CanCompleteContractsIntStepNow(detail) && isIntEngineer && intStepDef is { RequiresUpload: true },
            CanCompleteContractsIntStepNow(detail) && isIntEngineer && intStepDef is { RequiresApprovers: true }
                && !detail.ContractsIntStepApprovers.Any(a => a.StepNumber == intStep),
            intPendingApprover?.UserId == actor.Id,
            CanCompleteContractsIntStepNow(detail) && isIntEngineer && intStepDef is { RequiresSecretariat: true }
                && !detail.ContractsIntSecretariatPending,
            isSecretariat && intStepDef is { RequiresSecretariat: true },
            intStep,
            detail.ContractsProcurementSection == ContractsProcurementSectionType.Domestic
                && detail.ContractsSubPhase == ProcurementContractsSubPhase.InProgress
                && detail.ContractsDomVariant is null
                && CanSelectContractsDomVariant(actor, detail),
            CanCompleteContractsDomStepNow(detail)
                && ((isDomEngineer && domStepDef is not { RequiresContractsAdmin: true })
                    || (isContractsAdmin && domStepDef is { RequiresContractsAdmin: true })),
            CanCompleteContractsDomStepNow(detail) && isDomEngineer && domStepDef is { RequiresUpload: true },
            CanCompleteContractsDomStepNow(detail) && isDomEngineer && domStepDef is { RequiresApprovers: true }
                && !detail.ContractsDomStepApprovers.Any(a => a.StepNumber == domStep),
            domPendingApprover?.UserId == actor.Id,
            CanCompleteContractsDomStepNow(detail) && isDomEngineer && domStepDef is { RequiresContractsAdmin: true }
                && !detail.ContractsDomContractsAdminPending,
            isContractsAdmin && domStepDef is { RequiresContractsAdmin: true },
            CanCompleteContractsDomStepNow(detail) && isDomEngineer && domStepDef is { RequiresScheduleDate: true },
            CanCompleteContractsDomStepNow(detail) && isDomEngineer && domStepDef is { AllowsReturnToMarketing: true },
            CanCompleteContractsDomStepNow(detail) && isDomEngineer && domStepDef is { AllowsTerminationRollback: true },
            domStep);
    }

    private static ProcurementPaymentPermissionsDto BuildPaymentPermissions(User actor, ProcurementRequestDetail detail)
    {
        if (detail.Phase != ProcurementRequestPhase.Payment)
            return new ProcurementPaymentPermissionsDto(false, false);

        var isPaymentHead = IsPaymentSectionHead(actor);
        return new ProcurementPaymentPermissionsDto(
            detail.PaymentSubPhase == ProcurementPaymentSubPhase.Pending
                && (isPaymentHead || IsPlatformAdmin(actor) || detail.Document.AssigneeId == actor.Id),
            detail.PaymentSubPhase == ProcurementPaymentSubPhase.WaitingAccept
                && detail.PaymentSpecialistId == actor.Id);
    }

    private static ContractsIntStepDefinition? GetCurrentIntStepDefinition(ProcurementRequestDetail detail)
    {
        if (detail.ContractsIntVariant is not { } variant || !InternationalContractsIntSteps.IsSupported(variant))
            return null;
        return InternationalContractsIntSteps.GetDefinitions(variant)
            .FirstOrDefault(s => s.Number == detail.ContractsIntCurrentStep);
    }

    private static bool IsPaymentSectionHead(User actor) =>
        string.Equals(actor.Email, DevTestAccounts.PaymentSectionHeadEmail, StringComparison.OrdinalIgnoreCase)
        || (string.Equals(actor.Department?.Code, "HO-FINPLAN", StringComparison.Ordinal)
            && IsDeptManager(actor));

    private static bool IsTenderSecretariat(User actor) =>
        string.Equals(actor.Email, DevTestAccounts.TenderSecretariatEmail, StringComparison.OrdinalIgnoreCase);

    private static ProcurementMarketingPermissionsDto BuildMarketingPermissions(
        User actor, ProcurementRequestDetail detail, Guid? tasResponsibleId = null)
    {
        if (detail.Phase != ProcurementRequestPhase.Marketing)
            return new ProcurementMarketingPermissionsDto(false, false, false, false, false, false, false, false, false, false, 1);

        var step = detail.MarketingCurrentStep;
        var stepDef = MarketingRequestSteps.Definitions.FirstOrDefault(s => s.Number == step);
        var hasBranch = stepDef?.HasBranch == true;
        var activeBranch = detail.MarketingActiveBranch;
        var canAssign = detail.MarketingCurrentStep == 1
            && detail.MarketingSubPhase == ProcurementMarketingSubPhase.Pending
            && CanAssignMarketing(actor, detail);
        var resolvedTasResponsibleId = detail.TasResponsibleId ?? tasResponsibleId;
        var canReviewAsTas = detail.Flow == ProcurementRequestFlow.TechnicalAffairs
            ? resolvedTasResponsibleId == actor.Id
            : detail.InitiatorId == actor.Id;
        var isEngineer = IsMarketingEngineer(actor, detail);

        return new ProcurementMarketingPermissionsDto(
            detail.MarketingSubPhase == ProcurementMarketingSubPhase.WaitingAccept
                && detail.MarketingSpecialistId == actor.Id,
            canAssign,
            detail.MarketingCurrentStep == 1
                && (detail.MarketingSubPhase is ProcurementMarketingSubPhase.Pending or ProcurementMarketingSubPhase.WaitingAccept)
                && CanAssignMarketing(actor, detail),
            step == MarketingRequestSteps.TotalSteps && detail.MarketingSubPhase == ProcurementMarketingSubPhase.InProgress && CanCompleteMarketing(actor, detail),
            detail.MarketingSubPhase == ProcurementMarketingSubPhase.Completed && CanForwardToContracts(actor, detail),
            CanCompleteMarketingStep(actor, detail, step),
            hasBranch && activeBranch is null && CanRecordMarketingBranch(actor, detail),
            activeBranch is not null && CanResolveMarketingBranch(actor, detail),
            step == 4 && canReviewAsTas,
            step == 4 && isEngineer,
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
        6 => MarketingBranchType.ManagementRevision,
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
            8 => false,
            7 => CanCompleteMarketing(actor, detail),
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
            step == 7 && engineer && (detail.MarketingPlanApprovers.Count == 0 || hasRejection),
            step == 7 && nextPlan?.UserId == actor.Id,
            step == 8 && engineer && allApproved && detail.MarketingPlanRegisteredAt is null);
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

    private static string SectionDeptCode(ContractsProcurementSectionType section) => section switch
    {
        ContractsProcurementSectionType.International => HoCprocInt,
        ContractsProcurementSectionType.Domestic => HoCprocDom,
        _ => throw new ArgumentOutOfRangeException(nameof(section)),
    };

    private static bool CanSelectContractsIntVariant(User actor, ProcurementRequestDetail detail)
    {
        if (IsPlatformAdmin(actor)) return true;
        if (detail.ContractsSpecialistId == actor.Id) return true;
        return detail.Document.AssigneeId == actor.Id && IsContractsIntStaff(actor);
    }

    private static bool CanCompleteContractsIntStep(User actor, ProcurementRequestDetail detail)
    {
        if (IsPlatformAdmin(actor)) return true;
        if (detail.ContractsSpecialistId == actor.Id) return true;
        return detail.Document.AssigneeId == actor.Id && IsContractsIntStaff(actor);
    }

    private static bool IsContractsIntStaff(User u) =>
        IsPlatformAdmin(u) || string.Equals(u.Department?.Code, HoCprocInt, StringComparison.Ordinal);

    private static bool CanRouteContractsSection(User actor, ProcurementRequestDetail detail)
    {
        if (IsPlatformAdmin(actor)) return true;
        // Contracts Department Head after handoff (assignee), regardless of profile quirks.
        if (detail.Document.AssigneeId == actor.Id
            && detail.ContractsSubPhase == ProcurementContractsSubPhase.Pending
            && detail.ContractsProcurementSection is null)
            return true;
        // HO-CPROC department managers.
        if (!string.Equals(actor.Department?.Code, HoCproc, StringComparison.Ordinal)) return false;
        return IsDeptManager(actor);
    }

    private static bool CanAssignContracts(User actor, ProcurementRequestDetail detail)
    {
        if (detail.ContractsProcurementSection is null) return false;
        if (IsPlatformAdmin(actor)) return true;

        var expectedCode = SectionDeptCode(detail.ContractsProcurementSection.Value);
        if (!string.Equals(actor.Department?.Code, expectedCode, StringComparison.Ordinal)) return false;
        return IsDeptManager(actor) || detail.Document.AssigneeId == actor.Id;
    }

    private static bool CanView(User actor, ProcurementRequestDetail detail)
    {
        if (IsPlatformAdmin(actor)) return true;
        if (detail.Document.AuthorId == actor.Id) return true;
        if (detail.Document.AssigneeId == actor.Id) return true;
        if (detail.MarketingSpecialistId == actor.Id) return true;
        if (detail.ContractsSpecialistId == actor.Id) return true;
        if (detail.InitiatorId == actor.Id) return true;
        if (detail.TasResponsibleId == actor.Id) return true;
        if (detail.Approvers.Any(a => a.UserId == actor.Id)) return true;
        if (detail.Phase == ProcurementRequestPhase.Marketing && IsMarketingStaff(actor)) return true;
        if (detail.Phase == ProcurementRequestPhase.Contracts && IsContractsStaff(actor)) return true;
        if (detail.Phase == ProcurementRequestPhase.Contracts
            && detail.ContractsIntSecretariatUserId == actor.Id) return true;
        if (detail.Phase == ProcurementRequestPhase.Contracts
            && detail.ContractsDomContractsAdminUserId == actor.Id) return true;
        if (detail.Phase == ProcurementRequestPhase.Payment
            && (detail.PaymentSpecialistId == actor.Id
                || IsPaymentSectionHead(actor)
                || IsPlatformAdmin(actor))) return true;
        if (detail.ContractsIntStepApprovers.Any(a => a.UserId == actor.Id)) return true;
        if (detail.ContractsDomStepApprovers.Any(a => a.UserId == actor.Id)) return true;
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

    private static IReadOnlyList<ProcurementProcessDocumentDto> BuildProcessDocuments(
        ProcurementRequestDetail detail,
        MarketingRecord? marketingRecord,
        string? mktDeptName,
        string? mktDeptNameEn,
        string? mktUserName)
    {
        var docs = new List<ProcurementProcessDocumentDto>();

        foreach (var a in detail.Attachments.OrderByDescending(x => x.UploadedAt))
        {
            if (string.IsNullOrWhiteSpace(a.FileName)) continue;
            docs.Add(new ProcurementProcessDocumentDto(
                a.Id.ToString(),
                a.FileName,
                a.StorageKey,
                "Uploaded",
                "Initiation",
                a.Kind.ToString(),
                a.UploadedBy?.Department?.Name,
                a.UploadedBy?.Department?.NameEn,
                a.UploadedBy?.FullName,
                a.UploadedAt));
        }

        if (marketingRecord is not null)
        {
            if (!string.IsNullOrWhiteSpace(marketingRecord.RfqDocumentFileName)
                || !string.IsNullOrWhiteSpace(marketingRecord.RfqDocumentStorageKey))
            {
                docs.Add(new ProcurementProcessDocumentDto(
                    $"rfq-{marketingRecord.Id}",
                    marketingRecord.RfqDocumentFileName ?? "RFQ.docx",
                    marketingRecord.RfqDocumentStorageKey,
                    "Generated",
                    "Marketing",
                    "RfqDocument",
                    mktDeptName,
                    mktDeptNameEn,
                    mktUserName,
                    marketingRecord.RfqPreparedAt ?? marketingRecord.UpdatedAt));
            }

            foreach (var offer in marketingRecord.Offers.OrderByDescending(o => o.CreatedAt))
            {
                if (string.IsNullOrWhiteSpace(offer.AttachmentKey)) continue;
                var offerFile = offer.AttachmentKey.Contains('/')
                    ? offer.AttachmentKey[(offer.AttachmentKey.LastIndexOf('/') + 1)..]
                    : offer.AttachmentKey;
                docs.Add(new ProcurementProcessDocumentDto(
                    $"offer-{offer.Id}",
                    string.IsNullOrWhiteSpace(offerFile) ? $"{offer.CompanyName}.pdf" : offerFile,
                    offer.AttachmentKey,
                    "Uploaded",
                    "Marketing",
                    "CommercialOffer",
                    mktDeptName,
                    mktDeptNameEn,
                    mktUserName,
                    offer.CreatedAt));
            }

            foreach (var plan in marketingRecord.Plans.OrderByDescending(p => p.Version))
            {
                if (!string.IsNullOrWhiteSpace(plan.TemplateStorageKey) || !string.IsNullOrWhiteSpace(plan.TemplateFileName))
                {
                    docs.Add(new ProcurementProcessDocumentDto(
                        $"plan-template-{plan.Id}",
                        plan.TemplateFileName ?? $"{plan.RegistrationNumber ?? "plan"}.docx",
                        plan.TemplateStorageKey,
                        "Generated",
                        "Marketing",
                        "PlanTemplate",
                        mktDeptName,
                        mktDeptNameEn,
                        mktUserName,
                        plan.RegisteredAt ?? plan.CreatedAt));
                }

                if (!string.IsNullOrWhiteSpace(plan.AttachmentKey))
                {
                    var planFile = plan.AttachmentKey.Contains('/')
                        ? plan.AttachmentKey[(plan.AttachmentKey.LastIndexOf('/') + 1)..]
                        : plan.AttachmentKey;
                    docs.Add(new ProcurementProcessDocumentDto(
                        $"plan-doc-{plan.Id}",
                        string.IsNullOrWhiteSpace(planFile)
                            ? $"{plan.RegistrationNumber ?? "plan"}-signed.docx"
                            : planFile,
                        plan.AttachmentKey,
                        "Uploaded",
                        "Marketing",
                        "PlanDocument",
                        mktDeptName,
                        mktDeptNameEn,
                        mktUserName,
                        plan.UpdatedAt));
                }
            }
        }

        foreach (var f in detail.ContractsIntStepFiles.OrderBy(x => x.StepNumber).ThenBy(x => x.UploadedAt))
        {
            if (string.IsNullOrWhiteSpace(f.FileName)) continue;
            docs.Add(new ProcurementProcessDocumentDto(
                f.Id.ToString(),
                f.FileName,
                f.StorageKey,
                "Uploaded",
                "Contracts",
                $"IntStep{f.StepNumber}",
                f.UploadedBy?.Department?.Name,
                f.UploadedBy?.Department?.NameEn,
                f.UploadedBy?.FullName,
                f.UploadedAt));
        }

        // Process order: Request (Initiation) → Marketing → Contracts (oldest first within phase)
        static int PhaseOrder(string phase) => phase switch
        {
            "Initiation" => 0,
            "Marketing" => 1,
            "Contracts" => 2,
            _ => 9,
        };

        return docs
            .OrderBy(d => PhaseOrder(d.Phase))
            .ThenBy(d => d.At ?? DateTime.MaxValue)
            .ToList();
    }

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

    private async Task NotifyStakeholdersOfPhaseMoveAsync(
        ProcurementRequestDetail detail, string departmentKey, CancellationToken ct)
    {
        var recipients = new HashSet<Guid>();
        if (detail.InitiatorId is Guid initiatorId)
            recipients.Add(initiatorId);
        if (detail.Document.AuthorId != Guid.Empty)
            recipients.Add(detail.Document.AuthorId);
        if (detail.TasResponsibleId is Guid tasId)
            recipients.Add(tasId);

        foreach (var recipientId in recipients)
        {
            await notifications.NotifyProcurementPhaseMovedAsync(
                recipientId,
                detail.Document.Number,
                detail.Document.Title,
                detail.DocumentId,
                departmentKey,
                ct);
        }
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
