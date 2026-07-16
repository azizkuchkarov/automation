using ATG.Platform.Application.Common;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Data;
using ATG.Platform.Infrastructure.Dcs;
using Microsoft.EntityFrameworkCore;

namespace ATG.Platform.Infrastructure.Services;

public class MarketingService(AppDbContext db, IAuditService audit, MarketingRfqChannelService rfqChannels, IFileStorageService files) : IMarketingService
{
    private const string HoMkt = "HO-MKT";

    public async Task<Result<MarketingRecordDto>> CreateFromProcurementAsync(Guid documentId, CancellationToken ct = default)
    {
        var existing = await LoadRecordAsync(documentId, ct);
        if (existing is not null)
            return Result<MarketingRecordDto>.Ok(await MapRecordAsync(existing, ct));

        var detail = await db.ProcurementRequestDetails
            .Include(d => d.Document)
            .Include(d => d.Initiator)
            .Include(d => d.InitiatorDepartment)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId, ct);
        if (detail is null) return Result<MarketingRecordDto>.Fail("Procurement request not found");

        var portalNumber = detail.Document.Number;
        if (!string.IsNullOrWhiteSpace(portalNumber) &&
            await db.MarketingRecords.AnyAsync(r => r.PortalNumber == portalNumber, ct))
        {
            portalNumber = null;
        }

        var received = DateOnly.FromDateTime((detail.Document.RegisteredAt ?? detail.Document.CreatedAt).Date);
        var record = new MarketingRecord
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            PortalNumber = portalNumber,
            RegisteredDate = detail.Document.RegisteredAt is not null
                ? DateOnly.FromDateTime(detail.Document.RegisteredAt.Value)
                : null,
            InitiatorDepartment = detail.InitiatorDepartment?.Name,
            InitiatorFullName = detail.Initiator?.FullName,
            ReceivedDate = received,
            DeadlineBaseDate = received,
            RequestTitle = detail.Document.Title,
            Status = MarketingRecordStatus.WaitingExecutor,
        };

        db.MarketingRecords.Add(record);
        return Result<MarketingRecordDto>.Ok(await MapRecordAsync(record, ct));
    }

    public async Task<Result<MarketingRecordDto>> GetByDocumentIdAsync(
        Guid documentId, Guid actorId, CancellationToken ct = default)
    {
        var record = await LoadRecordAsync(documentId, ct);
        if (record is null) return Result<MarketingRecordDto>.Fail("Marketing record not found");
        if (actorId != Guid.Empty)
        {
            var actor = await GetActorAsync(actorId, ct);
            if (actor is null || !CanView(actor, record))
                return Result<MarketingRecordDto>.Fail("Access denied");
        }

        return Result<MarketingRecordDto>.Ok(await MapRecordAsync(record, ct));
    }

    public async Task<Result<PagedResult<MarketingRecordListItemDto>>> GetRecordsAsync(
        Guid actorId, MarketingRecordStatus? status, Guid? executorId, MarketingRequestCategory? category,
        int page, int pageSize, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsMarketingStaff(actor))
            return Result<PagedResult<MarketingRecordListItemDto>>.Fail("Access denied");

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = BuildMarketingRecordsQuery(actor, status, executorId, category);
        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderByDescending(r => r.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new { Record = r, OfferCount = r.Offers.Count })
            .ToListAsync(ct);

        var items = rows.Select(x => MapListItem(x.Record, x.OfferCount)).ToList();
        return Result<PagedResult<MarketingRecordListItemDto>>.Ok(
            new PagedResult<MarketingRecordListItemDto>(items, total, page, pageSize));
    }

    private IQueryable<MarketingRecord> BuildMarketingRecordsQuery(
        User actor,
        MarketingRecordStatus? status,
        Guid? executorId,
        MarketingRequestCategory? category)
    {
        var query = db.MarketingRecords.AsNoTracking()
            .Include(r => r.MarketingExecutor)
            .Where(r => r.Status != MarketingRecordStatus.Cancelled);

        if (!IsMarketingManager(actor))
            query = query.Where(r => r.MarketingExecutorId == actor.Id || r.AssignedByManagerId == actor.Id);

        if (status is not null) query = query.Where(r => r.Status == status);
        if (executorId is not null) query = query.Where(r => r.MarketingExecutorId == executorId);
        if (category is not null) query = query.Where(r => r.RequestCategory == category);
        return query;
    }

    public async Task<Result<MarketingRecordDto>> SetCategoryAsync(
        Guid documentId, SetMarketingCategoryRequest request, Guid actorId, CancellationToken ct = default)
    {
        var record = await LoadRecordTrackedAsync(documentId, ct);
        if (record is null) return Result<MarketingRecordDto>.Fail("Marketing record not found");
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanManage(actor, record))
            return Result<MarketingRecordDto>.Fail("Access denied");

        var baseDate = request.DeadlineBaseDate ?? record.DeadlineBaseDate ?? record.ReceivedDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        record.RequestCategory = request.Category;
        record.DeadlineBaseDate = baseDate;
        record.DeadlineWorkingDays = MarketingDeadlineService.GetWorkingDaysForCategory(request.Category);
        record.DeadlineDate = MarketingDeadlineService.CalculateDeadline(baseDate, request.Category);
        record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetByDocumentIdAsync(documentId, actorId, ct);
    }

    public async Task<Result<MarketingRecordDto>> AssignExecutorAsync(
        Guid documentId, AssignMarketingExecutorRequest request, Guid actorId, CancellationToken ct = default)
    {
        var record = await LoadRecordTrackedAsync(documentId, ct);
        if (record is null) return Result<MarketingRecordDto>.Fail("Marketing record not found");
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanManage(actor, record))
            return Result<MarketingRecordDto>.Fail("Access denied");

        var executor = await GetMarketingWorkerAsync(request.ExecutorId, ct);
        if (executor is null) return Result<MarketingRecordDto>.Fail("Executor must be HO Marketing staff");

        record.MarketingExecutorId = executor.Id;
        record.AssignedByManagerId = actor.Id;
        record.Status = MarketingRecordStatus.WaitingAccept;
        record.UpdatedAt = DateTime.UtcNow;

        var detail = await db.ProcurementRequestDetails.Include(d => d.Document)
            .FirstAsync(d => d.DocumentId == documentId, ct);
        detail.MarketingSpecialistId = executor.Id;
        detail.Document.AssigneeId = executor.Id;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "MarketingExecutorAssigned", "MarketingRecord", record.Id, executor.FullName, null, ct);
        return await GetByDocumentIdAsync(documentId, actorId, ct);
    }

    public async Task<Result<MarketingRecordDto>> AcceptAsync(Guid documentId, Guid actorId, CancellationToken ct = default)
    {
        var record = await LoadRecordTrackedAsync(documentId, ct);
        if (record is null) return Result<MarketingRecordDto>.Fail("Marketing record not found");
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || record.MarketingExecutorId != actor.Id)
            return Result<MarketingRecordDto>.Fail("Only assigned executor can accept");

        record.AcceptedAt = DateTime.UtcNow;
        record.HandoverDate = DateOnly.FromDateTime(DateTime.UtcNow);
        record.Status = MarketingRecordStatus.StudyingDocuments;
        record.UpdatedAt = DateTime.UtcNow;

        var detail = await db.ProcurementRequestDetails.Include(d => d.Document)
            .FirstAsync(d => d.DocumentId == documentId, ct);
        detail.MarketingAcceptedAt = record.AcceptedAt;
        if (detail.MarketingCurrentStep < 2) detail.MarketingCurrentStep = 2;
        detail.MarketingSubPhase = ProcurementMarketingSubPhase.InProgress;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return await GetByDocumentIdAsync(documentId, actorId, ct);
    }

    public async Task<Result<MarketingRecordDto>> ReportTzIssueAsync(
        Guid documentId, MarketingTzIssueRequest request, Guid actorId, CancellationToken ct = default)
    {
        var record = await LoadRecordTrackedAsync(documentId, ct);
        if (record is null) return Result<MarketingRecordDto>.Fail("Marketing record not found");
        if (!await CanWorkRecord(actorId, record, ct)) return Result<MarketingRecordDto>.Fail("Access denied");

        record.TzIssueFound = true;
        record.TzIssueDescription = request.IssueDescription.Trim();
        record.TzIssueResolvedAt = null;
        record.Status = MarketingRecordStatus.TzIssue;
        record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetByDocumentIdAsync(documentId, actorId, ct);
    }

    public async Task<Result<MarketingRecordDto>> ResolveTzIssueAsync(Guid documentId, Guid actorId, CancellationToken ct = default)
    {
        var record = await LoadRecordTrackedAsync(documentId, ct);
        if (record is null) return Result<MarketingRecordDto>.Fail("Marketing record not found");
        if (!await CanWorkRecord(actorId, record, ct)) return Result<MarketingRecordDto>.Fail("Access denied");

        record.TzIssueResolvedAt = DateTime.UtcNow;
        record.Status = MarketingRecordStatus.StudyingDocuments;
        record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetByDocumentIdAsync(documentId, actorId, ct);
    }

    public async Task<Result<MarketingRecordDto>> MarkRfqPreparedAsync(Guid documentId, Guid actorId, CancellationToken ct = default)
    {
        var record = await LoadRecordTrackedAsync(documentId, ct);
        if (record is null) return Result<MarketingRecordDto>.Fail("Marketing record not found");
        if (!await CanWorkRecord(actorId, record, ct)) return Result<MarketingRecordDto>.Fail("Access denied");

        record.RfqPreparedAt = DateTime.UtcNow;
        record.Status = MarketingRecordStatus.RfqPreparation;
        record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetByDocumentIdAsync(documentId, actorId, ct);
    }

    public async Task<Result<MarketingRecordDto>> UploadRfqDocumentAsync(
        Guid documentId, UploadRfqDocumentRequest request, Guid actorId, CancellationToken ct = default)
    {
        var record = await LoadRecordTrackedAsync(documentId, ct);
        if (record is null) return Result<MarketingRecordDto>.Fail("Marketing record not found");
        if (!await CanWorkRecord(actorId, record, ct)) return Result<MarketingRecordDto>.Fail("Access denied");
        if (record.Request.MarketingCurrentStep != 3)
            return Result<MarketingRecordDto>.Fail("RFQ document can only be uploaded at marketing step 3");
        if (string.IsNullOrWhiteSpace(request.StorageKey) || string.IsNullOrWhiteSpace(request.FileName))
            return Result<MarketingRecordDto>.Fail("RFQ file is required");

        record.RfqDocumentStorageKey = request.StorageKey.Trim();
        record.RfqDocumentFileName = request.FileName.Trim();
        record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetByDocumentIdAsync(documentId, actorId, ct);
    }

    public async Task<Result<MarketingRecordDto>> RegisterAndGenerateRfqAsync(
        Guid documentId, RegisterRfqStep3Request request, Guid actorId, CancellationToken ct = default)
    {
        var record = await LoadRecordTrackedAsync(documentId, ct);
        if (record is null) return Result<MarketingRecordDto>.Fail("Marketing record not found");
        if (!await CanWorkRecord(actorId, record, ct)) return Result<MarketingRecordDto>.Fail("Access denied");
        if (record.Request.MarketingCurrentStep != 3)
            return Result<MarketingRecordDto>.Fail("RFQ registration is only available at marketing step 3");
        if (request.CommercialProposalDeadline == default)
            return Result<MarketingRecordDto>.Fail("Commercial proposal deadline is required");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<MarketingRecordDto>.Fail("User not found");

        var detail = record.Request;
        var requisitionType = detail.TasRequisitionType ?? TasRequisitionType.MaterialRequest;
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        if (!MarketingRegistrationNumberGenerator.IsCurrentRfqNumber(record.PortalNumber))
        {
            record.PortalNumber = await MarketingRegistrationNumberGenerator.GenerateNextAsync(db, ct);
            record.RegisteredDate = today;
        }

        record.RfqCommercialProposalDeadline = request.CommercialProposalDeadline;

        var fill = new RfqDocumentFillRequest(
            requisitionType,
            record.PortalNumber,
            today,
            request.CommercialProposalDeadline,
            detail.Document.TitleRu ?? detail.Document.Title ?? "",
            detail.Document.Title,
            actor.Email,
            actor.FullName);

        var bytes = RfqWordDocumentGenerator.Generate(fill);
        await using var stream = new MemoryStream(bytes);
        var fileName = $"{record.PortalNumber}.docx";
        var storageKey = await files.UploadAsync("marketing/rfq", fileName, stream, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", ct);

        record.RfqDocumentStorageKey = storageKey;
        record.RfqDocumentFileName = fileName;
        record.RfqPreparedAt = DateTime.UtcNow;
        record.Status = MarketingRecordStatus.RfqPreparation;
        record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        if (!record.RfqChannelRequests.Any(c =>
                c.Channel == MarketingRfqChannelType.AtgWebsite && c.Status == MarketingRfqChannelStatus.Open))
        {
            await rfqChannels.CreateAtgWebsiteChannelAsync(record, actor, ct);
        }

        if (!record.RfqChannelRequests.Any(c =>
                c.Channel == MarketingRfqChannelType.Tenderweek && c.Status == MarketingRfqChannelStatus.Open))
        {
            await rfqChannels.CreateTenderChannelAsync(record, actor, ct);
        }

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "MarketingRfqRegistered", "Document", documentId, record.PortalNumber, null, ct);
        return await GetByDocumentIdAsync(documentId, actorId, ct);
    }

    public async Task<Result<MarketingRecordDto>> RegisterAndGeneratePlanAsync(
        Guid documentId, RegisterMarketingPlanRequest request, Guid actorId, CancellationToken ct = default)
    {
        var record = await LoadRecordTrackedAsync(documentId, ct);
        if (record is null) return Result<MarketingRecordDto>.Fail("Marketing record not found");
        if (!await CanWorkRecord(actorId, record, ct)) return Result<MarketingRecordDto>.Fail("Access denied");
        if (record.Request.MarketingCurrentStep != 6)
            return Result<MarketingRecordDto>.Fail("Plan registration is only available at marketing step 6");

        var plan = record.Plans.OrderByDescending(p => p.Version).FirstOrDefault();
        if (plan is null || plan.Status != MarketingPlanStatus.Draft)
        {
            var version = record.Plans.Count == 0 ? 1 : record.Plans.Max(p => p.Version) + 1;
            plan = new MarketingProcurementPlan
            {
                Id = Guid.NewGuid(),
                MarketingRecordId = record.Id,
                Version = version,
                Status = MarketingPlanStatus.Draft,
            };
            db.MarketingProcurementPlans.Add(plan);
            record.Plans.Add(plan);
        }

        var procurementMethod = MarketingPlanRegistrationNumberGenerator.ToProcurementMethod(request.RegistrationMethod);
        var methodChanged = plan.RegistrationMethod != request.RegistrationMethod;

        plan.RegistrationMethod = request.RegistrationMethod;
        plan.ProcurementMethod = procurementMethod;
        plan.UpdatedAt = DateTime.UtcNow;

        if (methodChanged || string.IsNullOrWhiteSpace(plan.RegistrationNumber))
        {
            plan.RegistrationNumber = await MarketingPlanRegistrationNumberGenerator.GenerateNextAsync(
                db, request.RegistrationMethod, ct);
            plan.RegisteredAt = DateTime.UtcNow;
            plan.AttachmentKey = null;
        }

        record.ProcurementMethod = procurementMethod;
        record.StrategyNumber = plan.RegistrationNumber;
        record.PlanPreparedAt = DateTime.UtcNow;
        record.Status = MarketingRecordStatus.PlanPreparation;
        record.UpdatedAt = DateTime.UtcNow;

        if (PlanWordDocumentGenerator.HasTemplate(request.RegistrationMethod))
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var fill = new PlanDocumentFillRequest(request.RegistrationMethod, plan.RegistrationNumber, today);
            var bytes = PlanWordDocumentGenerator.Generate(fill);
            await using var stream = new MemoryStream(bytes);
            var fileName = $"{plan.RegistrationNumber}.docx";
            var storageKey = await files.UploadAsync(
                "marketing/plan", fileName, stream,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document", ct);
            plan.TemplateStorageKey = storageKey;
            plan.TemplateFileName = fileName;
        }
        else
        {
            plan.TemplateStorageKey = null;
            plan.TemplateFileName = null;
        }

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "MarketingPlanRegistered", "Document", documentId, plan.RegistrationNumber, null, ct);
        return await GetByDocumentIdAsync(documentId, actorId, ct);
    }

    public async Task<Result<MarketingRecordDto>> UploadPlanDocumentAsync(
        Guid documentId, UploadMarketingPlanDocumentRequest request, Guid actorId, CancellationToken ct = default)
    {
        var record = await LoadRecordTrackedAsync(documentId, ct);
        if (record is null) return Result<MarketingRecordDto>.Fail("Marketing record not found");
        if (!await CanWorkRecord(actorId, record, ct)) return Result<MarketingRecordDto>.Fail("Access denied");
        if (record.Request.MarketingCurrentStep != 6)
            return Result<MarketingRecordDto>.Fail("Plan document upload is only available at marketing step 6");

        var plan = record.Plans.OrderByDescending(p => p.Version).FirstOrDefault();
        if (plan is null || string.IsNullOrWhiteSpace(plan.RegistrationNumber))
            return Result<MarketingRecordDto>.Fail("Register the procurement plan first");

        plan.AttachmentKey = request.StorageKey.Trim();
        plan.UpdatedAt = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetByDocumentIdAsync(documentId, actorId, ct);
    }

    public async Task<Result<(byte[] Bytes, string FileName)>> DownloadPlanTemplateAsync(
        Guid documentId, Guid actorId, CancellationToken ct = default)
    {
        var record = await LoadRecordAsync(documentId, ct);
        if (record is null) return Result<(byte[], string)>.Fail("Marketing record not found");
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanView(actor, record))
            return Result<(byte[], string)>.Fail("Access denied");

        var plan = record.Plans.OrderByDescending(p => p.Version).FirstOrDefault();
        if (plan?.RegistrationMethod is null || string.IsNullOrWhiteSpace(plan.RegistrationNumber))
            return Result<(byte[], string)>.Fail("Register the procurement plan first");
        if (!PlanWordDocumentGenerator.HasTemplate(plan.RegistrationMethod.Value))
            return Result<(byte[], string)>.Fail("This procurement method has no Word template");

        var fill = new PlanDocumentFillRequest(
            plan.RegistrationMethod.Value,
            plan.RegistrationNumber,
            DateOnly.FromDateTime(plan.RegisteredAt?.Date ?? DateTime.UtcNow.Date));

        var bytes = PlanWordDocumentGenerator.Generate(fill);
        var fileName = plan.TemplateFileName ?? $"{plan.RegistrationNumber}.docx";
        return Result<(byte[], string)>.Ok((bytes, fileName));
    }

    public async Task<Result<MarketingRecordDto>> OpenRfqAtgWebsiteChannelAsync(
        Guid documentId, Guid actorId, CancellationToken ct = default)
    {
        var record = await LoadRecordTrackedAsync(documentId, ct);
        if (record is null) return Result<MarketingRecordDto>.Fail("Marketing record not found");
        if (!await CanWorkRecord(actorId, record, ct)) return Result<MarketingRecordDto>.Fail("Access denied");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<MarketingRecordDto>.Fail("User not found");

        try
        {
            await rfqChannels.CreateAtgWebsiteChannelAsync(record, actor, ct);
        }
        catch (InvalidOperationException ex)
        {
            return Result<MarketingRecordDto>.Fail(ex.Message);
        }

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "MarketingRfqAtgWebsiteOpened", "Document", documentId, null, null, ct);
        return await GetByDocumentIdAsync(documentId, actorId, ct);
    }

    public async Task<Result<MarketingRecordDto>> OpenRfqTenderChannelAsync(
        Guid documentId, Guid actorId, CancellationToken ct = default)
    {
        var record = await LoadRecordTrackedAsync(documentId, ct);
        if (record is null) return Result<MarketingRecordDto>.Fail("Marketing record not found");
        if (!await CanWorkRecord(actorId, record, ct)) return Result<MarketingRecordDto>.Fail("Access denied");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<MarketingRecordDto>.Fail("User not found");

        try
        {
            await rfqChannels.CreateTenderChannelAsync(record, actor, ct);
        }
        catch (InvalidOperationException ex)
        {
            return Result<MarketingRecordDto>.Fail(ex.Message);
        }

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "MarketingRfqTenderOpened", "Document", documentId, null, null, ct);
        return await GetByDocumentIdAsync(documentId, actorId, ct);
    }

    public async Task<Result<MarketingRecordDto>> CompleteRfqTenderChannelAsync(
        Guid documentId, Guid actorId, CancellationToken ct = default)
    {
        var record = await LoadRecordTrackedAsync(documentId, ct);
        if (record is null) return Result<MarketingRecordDto>.Fail("Marketing record not found");
        if (!await CanWorkRecord(actorId, record, ct)) return Result<MarketingRecordDto>.Fail("Access denied");

        try
        {
            await rfqChannels.CompleteTenderChannelByEngineerAsync(documentId, actorId, ct);
        }
        catch (InvalidOperationException ex)
        {
            return Result<MarketingRecordDto>.Fail(ex.Message);
        }

        await audit.LogAsync(actorId, "MarketingRfqTenderCompleted", "Document", documentId, null, null, ct);
        return await GetByDocumentIdAsync(documentId, actorId, ct);
    }

    public async Task<Result<MarketingRecordDto>> AddRfqDispatchAsync(
        Guid documentId, AddRfqDispatchRequest request, Guid actorId, CancellationToken ct = default)
    {
        var record = await LoadRecordTrackedAsync(documentId, ct);
        if (record is null) return Result<MarketingRecordDto>.Fail("Marketing record not found");
        if (!await CanWorkRecord(actorId, record, ct)) return Result<MarketingRecordDto>.Fail("Access denied");

        var dispatch = new RfqDispatch
        {
            Id = Guid.NewGuid(),
            MarketingRecordId = record.Id,
            DispatchType = request.DispatchType,
            RecipientName = request.RecipientName,
            RecipientEmail = request.RecipientEmail,
            RecipientPhone = request.RecipientPhone,
            Notes = request.Notes,
        };
        db.RfqDispatches.Add(dispatch);
        record.RfqDispatches.Add(dispatch);

        record.Status = MarketingRecordStatus.RfqSent;
        record.RfqPublishedAtgSite |= request.DispatchType == RfqDispatchType.AtgSite;
        record.RfqPublishedTenderweek |= request.DispatchType == RfqDispatchType.Tenderweek;
        record.RfqSentToVendor |= request.DispatchType == RfqDispatchType.Vendor;
        record.RfqSentToDistributor |= request.DispatchType == RfqDispatchType.Distributor;
        record.RfqOpenSearchDone |= request.DispatchType == RfqDispatchType.OpenSource;
        record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetByDocumentIdAsync(documentId, actorId, ct);
    }

    public async Task<Result<MarketingRecordDto>> MarkFollowupSentAsync(
        Guid dispatchId, MarkRfqFollowupRequest request, Guid actorId, CancellationToken ct = default)
    {
        var dispatch = await db.RfqDispatches.Include(d => d.Record).FirstOrDefaultAsync(d => d.Id == dispatchId, ct);
        if (dispatch is null) return Result<MarketingRecordDto>.Fail("Dispatch not found");
        if (!await CanWorkRecord(actorId, dispatch.Record, ct)) return Result<MarketingRecordDto>.Fail("Access denied");

        dispatch.FollowupSentAt = DateTime.UtcNow;
        dispatch.FollowupPhoneCalled = request.PhoneCallMade;
        dispatch.Notes = string.IsNullOrWhiteSpace(request.Notes) ? dispatch.Notes : request.Notes.Trim();
        dispatch.Record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetByDocumentIdAsync(dispatch.Record.DocumentId, actorId, ct);
    }

    public async Task<Result<MarketingOfferDto>> AddOfferAsync(
        Guid documentId, AddMarketingOfferRequest request, Guid actorId, CancellationToken ct = default)
    {
        var record = await LoadRecordTrackedAsync(documentId, ct);
        if (record is null) return Result<MarketingOfferDto>.Fail("Marketing record not found");
        if (!await CanWorkRecord(actorId, record, ct)) return Result<MarketingOfferDto>.Fail("Access denied");

        if (record.Request.MarketingCurrentStep != 4)
            return Result<MarketingOfferDto>.Fail("Commercial proposals can only be added at marketing step 4");

        if (string.IsNullOrWhiteSpace(request.CompanyName))
            return Result<MarketingOfferDto>.Fail("Company name is required");

        var offer = new MarketingOffer
        {
            Id = Guid.NewGuid(),
            MarketingRecordId = record.Id,
            CompanyName = request.CompanyName.Trim(),
            OfferAmount = request.OfferAmount,
            Currency = request.Currency ?? "UZS",
            VatIncluded = request.VatIncluded,
            DeliveryIncluded = request.DeliveryIncluded,
            WarrantyTerms = request.WarrantyTerms,
            OfferDate = request.OfferDate,
            OfferValidityDate = request.OfferValidityDate,
            ContactInfo = request.ContactInfo,
            Source = request.Source,
            AttachmentKey = request.AttachmentKey,
            InitiatorReviewStatus = MarketingInitiatorReviewStatus.Pending,
            EngineerReviewStatus = MarketingInitiatorReviewStatus.Pending,
        };
        db.MarketingOffers.Add(offer);
        record.Offers.Add(offer);
        record.Status = MarketingRecordStatus.KpAnalysis;
        record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Result<MarketingOfferDto>.Ok(MapOffer(offer));
    }

    public async Task<Result<MarketingOfferDto>> ReviewOfferByInitiatorAsync(
        Guid offerId, ReviewMarketingOfferRequest request, Guid actorId, CancellationToken ct = default)
    {
        var offer = await db.MarketingOffers
            .Include(o => o.Record).ThenInclude(r => r.Request)
            .FirstOrDefaultAsync(o => o.Id == offerId, ct);
        if (offer is null) return Result<MarketingOfferDto>.Fail("Offer not found");

        var detail = offer.Record.Request;
        if (detail is null || detail.Phase != ProcurementRequestPhase.Marketing || detail.MarketingCurrentStep != 4)
            return Result<MarketingOfferDto>.Fail("TAS review is only available at marketing step 4");

        if (!await CanReviewProposalAsTasAsync(detail, actorId, ct))
            return Result<MarketingOfferDto>.Fail("Only the TAS responsible can review proposals");

        var action = request.Action?.Trim().ToLowerInvariant();
        if (action is not ("approve" or "reject"))
            return Result<MarketingOfferDto>.Fail("Action must be Approve or Reject");
        if (action == "reject" && string.IsNullOrWhiteSpace(request.Comment))
            return Result<MarketingOfferDto>.Fail("Rejection comment is required");

        offer.InitiatorReviewStatus = action == "approve"
            ? MarketingInitiatorReviewStatus.Approved
            : MarketingInitiatorReviewStatus.Rejected;
        offer.InitiatorReviewedById = actorId;
        offer.InitiatorReviewedAt = DateTime.UtcNow;
        offer.InitiatorReviewComment = request.Comment?.Trim();
        offer.MeetsTzRequirements = action == "approve";
        offer.RejectionReason = action == "reject" ? request.Comment?.Trim() : null;
        offer.Record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var reviewer = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == actorId, ct);
        return Result<MarketingOfferDto>.Ok(MapOffer(offer, initiatorReviewerName: reviewer?.FullName));
    }

    public async Task<Result<MarketingOfferDto>> ReviewOfferByEngineerAsync(
        Guid offerId, ReviewMarketingOfferRequest request, Guid actorId, CancellationToken ct = default)
    {
        var offer = await db.MarketingOffers
            .Include(o => o.Record).ThenInclude(r => r.Request).ThenInclude(req => req!.Document)
            .FirstOrDefaultAsync(o => o.Id == offerId, ct);
        if (offer is null) return Result<MarketingOfferDto>.Fail("Offer not found");

        var detail = offer.Record.Request;
        if (detail is null || detail.Phase != ProcurementRequestPhase.Marketing || detail.MarketingCurrentStep != 4)
            return Result<MarketingOfferDto>.Fail("Marketing engineer review is only available at marketing step 4");
        if (!await IsMarketingEngineerForRecordAsync(actorId, offer.Record, ct))
            return Result<MarketingOfferDto>.Fail("Only the assigned marketing engineer can review proposals");

        var action = request.Action?.Trim().ToLowerInvariant();
        if (action is not ("approve" or "reject"))
            return Result<MarketingOfferDto>.Fail("Action must be Approve or Reject");
        if (action == "reject" && string.IsNullOrWhiteSpace(request.Comment))
            return Result<MarketingOfferDto>.Fail("Rejection comment is required");

        offer.EngineerReviewStatus = action == "approve"
            ? MarketingInitiatorReviewStatus.Approved
            : MarketingInitiatorReviewStatus.Rejected;
        offer.EngineerReviewedById = actorId;
        offer.EngineerReviewedAt = DateTime.UtcNow;
        offer.EngineerReviewComment = request.Comment?.Trim();
        offer.Record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var reviewer = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == actorId, ct);
        return Result<MarketingOfferDto>.Ok(MapOffer(offer, engineerReviewerName: reviewer?.FullName));
    }

    public async Task<(bool Ok, string? Error)> ValidateStep4ProposalsAsync(Guid documentId, CancellationToken ct = default)
    {
        var record = await db.MarketingRecords.AsNoTracking()
            .Include(r => r.Offers)
            .FirstOrDefaultAsync(r => r.DocumentId == documentId, ct);
        if (record is null) return (false, "Marketing record not found");
        if (record.Offers.Count == 0)
            return (false, "Add at least one commercial proposal before completing step 4");
        if (record.Offers.Any(o => o.EngineerReviewStatus == MarketingInitiatorReviewStatus.Pending))
            return (false, "Marketing engineer must approve or reject every commercial proposal");
        if (record.Offers.Any(o => o.InitiatorReviewStatus == MarketingInitiatorReviewStatus.Pending))
            return (false, "TAS responsible must approve or reject every commercial proposal");
        if (!record.Offers.Any(o =>
                o.EngineerReviewStatus == MarketingInitiatorReviewStatus.Approved
                && o.InitiatorReviewStatus == MarketingInitiatorReviewStatus.Approved))
            return (false, "At least one commercial proposal must be approved by both marketing engineer and TAS responsible");

        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> ValidateStep6PlanAsync(Guid documentId, CancellationToken ct = default)
    {
        var record = await db.MarketingRecords.AsNoTracking()
            .Include(r => r.Plans)
            .FirstOrDefaultAsync(r => r.DocumentId == documentId, ct);
        if (record is null) return (false, "Marketing record not found");

        var plan = record.Plans.OrderByDescending(p => p.Version).FirstOrDefault();
        if (plan is null || plan.RegistrationMethod is null)
            return (false, "Select a procurement method and register the plan");
        if (string.IsNullOrWhiteSpace(plan.RegistrationNumber))
            return (false, "Procurement plan registration number is required");

        if (PlanWordDocumentGenerator.HasTemplate(plan.RegistrationMethod.Value)
            && string.IsNullOrWhiteSpace(plan.AttachmentKey))
            return (false, "Upload the completed procurement plan document");

        return (true, null);
    }

    public async Task<Result<MarketingOfferDto>> UpdateOfferComplianceAsync(
        Guid offerId, UpdateOfferComplianceRequest request, Guid actorId, CancellationToken ct = default)
    {
        var offer = await db.MarketingOffers.Include(o => o.Record).FirstOrDefaultAsync(o => o.Id == offerId, ct);
        if (offer is null) return Result<MarketingOfferDto>.Fail("Offer not found");
        if (!await CanWorkRecord(actorId, offer.Record, ct)) return Result<MarketingOfferDto>.Fail("Access denied");

        offer.MeetsTzRequirements = request.MeetsTz;
        offer.RejectionReason = request.MeetsTz ? null : request.RejectionReason?.Trim();
        if (!request.MeetsTz) offer.Record.Status = MarketingRecordStatus.KpNegotiation;
        offer.Record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Result<MarketingOfferDto>.Ok(MapOffer(offer));
    }

    public async Task<Result<MarketingOfferDto>> UpdateOfferAffiliationAsync(
        Guid offerId, UpdateOfferAffiliationRequest request, Guid actorId, CancellationToken ct = default)
    {
        var offer = await db.MarketingOffers.Include(o => o.Record).FirstOrDefaultAsync(o => o.Id == offerId, ct);
        if (offer is null) return Result<MarketingOfferDto>.Fail("Offer not found");
        if (!await CanWorkRecord(actorId, offer.Record, ct)) return Result<MarketingOfferDto>.Fail("Access denied");

        offer.IsAffiliated = request.IsAffiliated;
        offer.AffiliationNote = request.Note?.Trim();
        offer.Record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Result<MarketingOfferDto>.Ok(MapOffer(offer));
    }

    public async Task<Result<MarketingProcurementPlanDto>> CreateProcurementPlanAsync(
        Guid documentId, CreateMarketingPlanRequest request, Guid actorId, CancellationToken ct = default)
    {
        var record = await LoadRecordTrackedAsync(documentId, ct);
        if (record is null) return Result<MarketingProcurementPlanDto>.Fail("Marketing record not found");
        if (!await CanWorkRecord(actorId, record, ct)) return Result<MarketingProcurementPlanDto>.Fail("Access denied");

        var version = record.Plans.Count == 0 ? 1 : record.Plans.Max(p => p.Version) + 1;
        var plan = new MarketingProcurementPlan
        {
            Id = Guid.NewGuid(),
            MarketingRecordId = record.Id,
            Version = version,
            ProcurementMethod = request.ProcurementMethod,
            StartPrice = request.StartPrice,
            StartPriceCurrency = request.StartPriceCurrency,
            VatConsidered = request.VatConsidered,
            Incoterms = request.Incoterms,
            CompetitionCriteria = request.CompetitionCriteria,
            EvaluationGroupMembers = request.EvaluationGroupMembers,
            NdsNote = request.NdsNote,
            AttachmentKey = request.AttachmentKey,
        };
        db.MarketingProcurementPlans.Add(plan);
        record.Plans.Add(plan);
        record.ProcurementMethod = request.ProcurementMethod;
        record.PlanPreparedAt = DateTime.UtcNow;
        record.Status = MarketingRecordStatus.PlanPreparation;
        record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Result<MarketingProcurementPlanDto>.Ok(MapPlan(plan));
    }

    public async Task<Result<MarketingProcurementPlanDto>> SubmitPlanToManagementAsync(Guid planId, Guid actorId, CancellationToken ct = default)
    {
        var plan = await db.MarketingProcurementPlans.Include(p => p.Record).FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null) return Result<MarketingProcurementPlanDto>.Fail("Plan not found");
        if (!await CanWorkRecord(actorId, plan.Record, ct)) return Result<MarketingProcurementPlanDto>.Fail("Access denied");

        plan.Status = MarketingPlanStatus.SentToMgmt;
        plan.SubmittedAt = DateTime.UtcNow;
        plan.Record.PlanSentToManagementAt = DateTime.UtcNow;
        plan.Record.Status = MarketingRecordStatus.PlanManagementReview;
        plan.Record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Result<MarketingProcurementPlanDto>.Ok(MapPlan(plan));
    }

    public async Task<Result<MarketingProcurementPlanDto>> RejectPlanByManagementAsync(
        Guid planId, string notes, Guid actorId, CancellationToken ct = default)
    {
        var plan = await db.MarketingProcurementPlans.Include(p => p.Record).FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null) return Result<MarketingProcurementPlanDto>.Fail("Plan not found");
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanManage(actor, plan.Record))
            return Result<MarketingProcurementPlanDto>.Fail("Access denied");

        plan.Status = MarketingPlanStatus.Rejected;
        plan.RejectionNotes = notes.Trim();
        plan.Record.Status = MarketingRecordStatus.PlanPreparation;
        plan.Record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Result<MarketingProcurementPlanDto>.Ok(MapPlan(plan));
    }

    public async Task<Result<MarketingRecordDto>> SubmitToPortalAsync(
        Guid documentId, SubmitPortalRequest request, Guid actorId, CancellationToken ct = default)
    {
        var record = await LoadRecordTrackedAsync(documentId, ct);
        if (record is null) return Result<MarketingRecordDto>.Fail("Marketing record not found");
        if (!await CanWorkRecord(actorId, record, ct)) return Result<MarketingRecordDto>.Fail("Access denied");

        var plan = record.Plans.FirstOrDefault(p => p.Id == request.PlanId);
        if (plan is null) return Result<MarketingRecordDto>.Fail("Plan not found");

        plan.Status = MarketingPlanStatus.PortalSubmitted;
        var approval = new MarketingPortalApproval
        {
            Id = Guid.NewGuid(),
            MarketingRecordId = record.Id,
            ProcurementPlanId = plan.Id,
            ApprovalType = request.ApprovalType,
            SubmittedAt = DateTime.UtcNow,
            Notes = request.Notes,
        };
        db.MarketingPortalApprovals.Add(approval);
        record.PortalApprovals.Add(approval);
        record.PortalApprovalStartedAt = DateTime.UtcNow;
        record.PortalApprovalType = request.ApprovalType;
        record.PlanSubmittedToPortalAt = DateTime.UtcNow;
        record.Status = MarketingRecordStatus.PlanPortalApproval;
        record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetByDocumentIdAsync(documentId, actorId, ct);
    }

    public async Task<Result<MarketingRecordDto>> CompletePortalApprovalAsync(
        Guid portalApprovalId, CompletePortalApprovalRequest request, Guid actorId, CancellationToken ct = default)
    {
        var pa = await db.MarketingPortalApprovals.Include(p => p.Record).ThenInclude(r => r.Plans)
            .FirstOrDefaultAsync(p => p.Id == portalApprovalId, ct);
        if (pa is null) return Result<MarketingRecordDto>.Fail("Portal approval not found");
        if (!await CanWorkRecord(actorId, pa.Record, ct)) return Result<MarketingRecordDto>.Fail("Access denied");

        pa.ApprovedAt = DateTime.UtcNow;
        pa.BudgetNumber = request.BudgetNumber.Trim();
        pa.Notes = request.Notes;
        pa.Record.PortalBudgetNumber = pa.BudgetNumber;
        pa.Record.PlanApprovedAt = DateTime.UtcNow;
        pa.Record.Status = MarketingRecordStatus.PlanMonitoring;
        if (pa.ProcurementPlanId is Guid planId)
        {
            var plan = pa.Record.Plans.FirstOrDefault(p => p.Id == planId);
            if (plan is not null)
            {
                plan.Status = MarketingPlanStatus.Approved;
                plan.ApprovedAt = DateTime.UtcNow;
            }
        }
        pa.Record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetByDocumentIdAsync(pa.Record.DocumentId, actorId, ct);
    }

    public async Task<Result<MarketingRecordDto>> CompleteToContractAsync(Guid documentId, Guid actorId, CancellationToken ct = default)
    {
        var record = await LoadRecordTrackedAsync(documentId, ct);
        if (record is null) return Result<MarketingRecordDto>.Fail("Marketing record not found");
        if (!await CanWorkRecord(actorId, record, ct) && !IsMarketingManager(await GetActorAsync(actorId, ct)!))
            return Result<MarketingRecordDto>.Fail("Access denied");

        record.PlanRegisteredAt = DateTime.UtcNow;
        record.Status = MarketingRecordStatus.CompletedToContract;
        record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetByDocumentIdAsync(documentId, actorId, ct);
    }

    public async Task<Result<MarketingRecordDto>> CancelAsync(
        Guid documentId, MarketingCancelRequest request, Guid actorId, CancellationToken ct = default)
    {
        var record = await LoadRecordTrackedAsync(documentId, ct);
        if (record is null) return Result<MarketingRecordDto>.Fail("Marketing record not found");
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanManage(actor, record))
            return Result<MarketingRecordDto>.Fail("Access denied");

        record.Status = MarketingRecordStatus.Cancelled;
        record.Notes = request.Reason.Trim();
        record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetByDocumentIdAsync(documentId, actorId, ct);
    }

    public async Task SyncStatusFromWorkflowAsync(Guid documentId, CancellationToken ct = default)
    {
        var record = await LoadRecordTrackedAsync(documentId, ct);
        var detail = await db.ProcurementRequestDetails.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DocumentId == documentId, ct);
        if (record is null || detail is null || detail.Phase != ProcurementRequestPhase.Marketing) return;

        record.Status = MapWorkflowStatus(detail);
        if (detail.MarketingSpecialistId is not null && record.MarketingExecutorId is null)
            record.MarketingExecutorId = detail.MarketingSpecialistId;
        if (detail.MarketingAcceptedAt is not null) record.AcceptedAt ??= detail.MarketingAcceptedAt;
        if (detail.MarketingSubPhase == ProcurementMarketingSubPhase.Completed)
            record.Status = MarketingRecordStatus.CompletedToContract;
        record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<Result<MarketingStatsDto>> GetStatsAsync(Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsMarketingStaff(actor))
            return Result<MarketingStatsDto>.Fail("Access denied");

        var baseQuery = db.MarketingRecords.AsNoTracking()
            .Where(r => r.Status != MarketingRecordStatus.Cancelled);

        if (!IsMarketingManager(actor))
            baseQuery = baseQuery.Where(r => r.MarketingExecutorId == actor.Id || r.AssignedByManagerId == actor.Id);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var total = await baseQuery.CountAsync(ct);
        var completed = await baseQuery.CountAsync(r => r.Status == MarketingRecordStatus.CompletedToContract, ct);
        var overdue = await baseQuery.CountAsync(r =>
            r.DeadlineDate != null && r.DeadlineDate < today
            && r.Status != MarketingRecordStatus.CompletedToContract, ct);
        var inProgress = total - completed;

        var byCategory = await baseQuery
            .Where(r => r.RequestCategory != null)
            .GroupBy(r => r.RequestCategory!.Value)
            .Select(g => new MarketingCategoryStatDto(g.Key, g.Count()))
            .ToListAsync(ct);

        var byExecutorRows = await baseQuery
            .Where(r => r.MarketingExecutorId != null)
            .GroupBy(r => r.MarketingExecutorId!.Value)
            .Select(g => new
            {
                ExecutorId = g.Key,
                Count = g.Count(),
                Overdue = g.Count(r => r.DeadlineDate != null && r.DeadlineDate < today
                    && r.Status != MarketingRecordStatus.CompletedToContract),
            })
            .ToListAsync(ct);

        var executorIds = byExecutorRows.Select(x => x.ExecutorId).ToList();
        var executorNames = executorIds.Count == 0
            ? new Dictionary<Guid, string>()
            : (await db.Users.AsNoTracking()
                .Where(u => executorIds.Contains(u.Id))
                .ToListAsync(ct))
                .ToDictionary(u => u.Id, u => u.FullName);

        var byExecutor = byExecutorRows
            .Select(x => new MarketingExecutorStatDto(
                executorNames.GetValueOrDefault(x.ExecutorId, "—"),
                x.Count,
                x.Overdue))
            .OrderBy(x => x.ExecutorName)
            .ToList();

        var byMethod = await baseQuery
            .Where(r => r.ProcurementMethod != null)
            .GroupBy(r => r.ProcurementMethod!.Value)
            .Select(g => new MarketingMethodStatDto(g.Key, g.Count()))
            .ToListAsync(ct);

        return Result<MarketingStatsDto>.Ok(new MarketingStatsDto(
            total, inProgress, overdue, completed, byCategory, byExecutor, byMethod));
    }

    public async Task<Result<IReadOnlyList<MarketingLeadershipRowDto>>> GetLeadershipOverviewAsync(Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsMarketingManager(actor))
            return Result<IReadOnlyList<MarketingLeadershipRowDto>>.Fail("Access denied");

        var records = await db.MarketingRecords.AsNoTracking()
            .Include(r => r.Request)
            .Where(r => r.Status != MarketingRecordStatus.Cancelled && r.Status != MarketingRecordStatus.CompletedToContract)
            .OrderBy(r => r.InitiatorDepartment).ThenBy(r => r.InitiatorFullName)
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rows = records
            .GroupBy(r => (r.InitiatorDepartment ?? "—", r.InitiatorFullName ?? "—"))
            .Select(g => new MarketingLeadershipRowDto(
                g.Key.Item1,
                g.Key.Item2,
                g.Select(r =>
                {
                    var remaining = r.DeadlineDate is not null
                        ? MarketingDeadlineService.GetRemainingWorkingDays(r.DeadlineDate.Value)
                        : (int?)null;
                    var color = r.DeadlineDate is not null
                        ? MarketingDeadlineService.GetDeadlineColor(r.DeadlineDate.Value)
                        : null;
                    var overdue = r.DeadlineDate is not null && r.DeadlineDate < today;
                    return new MarketingLeadershipItemDto(
                        r.DocumentId, r.PortalNumber, r.RequestTitle, r.Status,
                        r.Request?.MarketingCurrentStep ?? 1, remaining, color, overdue);
                }).ToList()))
            .ToList();

        return Result<IReadOnlyList<MarketingLeadershipRowDto>>.Ok(rows);
    }

    public async Task ProcessPortalApprovalRemindersAsync(CancellationToken ct = default)
    {
        var pending = await db.MarketingPortalApprovals
            .Include(p => p.Record).ThenInclude(r => r.Request)
            .Where(p => p.ApprovedAt == null && p.ReminderSentAt == null)
            .ToListAsync(ct);

        foreach (var approval in pending)
        {
            var workingDays = MarketingDeadlineService.GetWorkingDaysSince(approval.SubmittedAt);
            if (workingDays < 2) continue;

            approval.ReminderSentAt = DateTime.UtcNow;
            var docId = approval.Record.DocumentId;
            await audit.LogAsync(null, "Marketing.PortalReminder",
                "MarketingPortalApproval", approval.Id,
                $"Portal approval pending {workingDays} working days for {approval.Record.PortalNumber}", null, ct);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task ProcessDeadlineWarningsAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var active = await db.MarketingRecords.AsNoTracking()
            .Where(r => r.Status != MarketingRecordStatus.Cancelled
                && r.Status != MarketingRecordStatus.CompletedToContract
                && r.DeadlineDate != null)
            .ToListAsync(ct);

        foreach (var record in active)
        {
            var remaining = MarketingDeadlineService.GetRemainingWorkingDays(record.DeadlineDate!.Value, today);
            if (remaining > 3) continue;

            var level = remaining <= 0 ? "overdue" : remaining <= 3 ? "critical" : "warning";
            await audit.LogAsync(null, $"Marketing.Deadline.{level}",
                "MarketingRecord", record.Id,
                $"{record.PortalNumber}: {remaining} working days left (category {record.RequestCategory})", null, ct);
        }
    }

    public async Task<Result<IReadOnlyList<MarketingBoardColumnDto>>> GetBoardAsync(Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsMarketingStaff(actor))
            return Result<IReadOnlyList<MarketingBoardColumnDto>>.Fail("Access denied");

        var rows = await BuildMarketingRecordsQuery(actor, null, null, null)
            .OrderByDescending(r => r.UpdatedAt)
            .Select(r => new { Record = r, OfferCount = r.Offers.Count })
            .ToListAsync(ct);

        var items = rows.Select(x => MapListItem(x.Record, x.OfferCount)).ToList();

        var columns = new[]
        {
            (MarketingRecordStatus.WaitingAccept, "Ожидание исполнителя", "Awaiting executor"),
            (MarketingRecordStatus.RfqSent, "RFQ / запрос цен", "RFQ & price request"),
            (MarketingRecordStatus.KpAnalysis, "Анализ КП", "Commercial offer analysis"),
            (MarketingRecordStatus.PlanPreparation, "План закупки", "Procurement plan"),
            (MarketingRecordStatus.PlanPortalApproval, "Согласование на портале", "Portal approval"),
            (MarketingRecordStatus.CompletedToContract, "Передано в контракты", "Handed to contracts"),
        };

        var result = columns.Select(c => new MarketingBoardColumnDto(
            c.Item1,
            c.Item2,
            c.Item3,
            items.Where(i => MapBoardStatus(i.Status) == c.Item1).ToList())).ToList();

        return Result<IReadOnlyList<MarketingBoardColumnDto>>.Ok(result);
    }

    private static MarketingRecordStatus MapBoardStatus(MarketingRecordStatus status) => status switch
    {
        MarketingRecordStatus.WaitingExecutor or MarketingRecordStatus.WaitingAccept or MarketingRecordStatus.StudyingDocuments or MarketingRecordStatus.TzIssue => MarketingRecordStatus.WaitingAccept,
        MarketingRecordStatus.RfqPreparation or MarketingRecordStatus.RfqSent => MarketingRecordStatus.RfqSent,
        MarketingRecordStatus.KpAnalysis or MarketingRecordStatus.KpNegotiation => MarketingRecordStatus.KpAnalysis,
        MarketingRecordStatus.PlanPreparation or MarketingRecordStatus.PlanManagementReview => MarketingRecordStatus.PlanPreparation,
        MarketingRecordStatus.PlanPortalApproval or MarketingRecordStatus.PlanMonitoring => MarketingRecordStatus.PlanPortalApproval,
        MarketingRecordStatus.CompletedToContract => MarketingRecordStatus.CompletedToContract,
        _ => MarketingRecordStatus.WaitingAccept,
    };

    private static MarketingRecordStatus MapWorkflowStatus(ProcurementRequestDetail detail)
    {
        if (detail.MarketingActiveBranch == MarketingBranchType.TzEscalation) return MarketingRecordStatus.TzIssue;
        if (detail.MarketingActiveBranch == MarketingBranchType.KpNegotiation) return MarketingRecordStatus.KpNegotiation;
        if (detail.MarketingActiveBranch == MarketingBranchType.ManagementRevision) return MarketingRecordStatus.PlanManagementReview;

        return detail.MarketingCurrentStep switch
        {
            1 when detail.MarketingSpecialistId is null => MarketingRecordStatus.WaitingExecutor,
            1 => MarketingRecordStatus.WaitingAccept,
            2 => MarketingRecordStatus.StudyingDocuments,
            3 => MarketingRecordStatus.RfqSent,
            4 => MarketingRecordStatus.KpAnalysis,
            5 => MarketingRecordStatus.KpAnalysis,
            6 => MarketingRecordStatus.PlanPreparation,
            7 => MarketingRecordStatus.PlanPortalApproval,
            8 => MarketingRecordStatus.PlanMonitoring,
            _ => MarketingRecordStatus.WaitingExecutor,
        };
    }

    private async Task<MarketingRecord?> LoadRecordAsync(Guid documentId, CancellationToken ct) =>
        await db.MarketingRecords.AsNoTracking()
            .Include(r => r.MarketingExecutor)
            .Include(r => r.AssignedByManager)
            .Include(r => r.Offers)
            .Include(r => r.RfqDispatches)
            .Include(r => r.RfqChannelRequests).ThenInclude(c => c.AssignedUser)
            .Include(r => r.Plans)
            .Include(r => r.PortalApprovals)
            .Include(r => r.Request).ThenInclude(req => req!.Initiator)
            .Include(r => r.Request).ThenInclude(req => req!.TasResponsible)
            .FirstOrDefaultAsync(r => r.DocumentId == documentId, ct);

    private async Task<MarketingRecord?> LoadRecordTrackedAsync(Guid documentId, CancellationToken ct) =>
        await db.MarketingRecords
            .Include(r => r.Offers)
            .Include(r => r.RfqDispatches)
            .Include(r => r.RfqChannelRequests).ThenInclude(c => c.AssignedUser)
            .Include(r => r.Plans)
            .Include(r => r.PortalApprovals)
            .Include(r => r.Request).ThenInclude(req => req!.Document)
            .Include(r => r.Request).ThenInclude(req => req!.Initiator)
            .Include(r => r.Request).ThenInclude(req => req!.TasResponsible)
            .FirstOrDefaultAsync(r => r.DocumentId == documentId, ct);

    private async Task<MarketingRecordDto> MapRecordAsync(MarketingRecord r, CancellationToken ct)
    {
        var remaining = r.DeadlineDate is not null ? MarketingDeadlineService.GetRemainingWorkingDays(r.DeadlineDate.Value) : (int?)null;
        var color = r.DeadlineDate is not null ? MarketingDeadlineService.GetDeadlineColor(r.DeadlineDate.Value) : null;
        var compliant = r.Offers.Where(o => o.MeetsTzRequirements == true).ToList();
        var avg = compliant.Count > 0 ? compliant.Average(o => (double)(o.OfferAmount ?? 0)) : (double?)null;

        var reviewerIds = r.Offers
            .SelectMany(o => new Guid?[] { o.InitiatorReviewedById, o.EngineerReviewedById })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        var reviewerNames = reviewerIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await db.Users.AsNoTracking()
                .Where(u => reviewerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        string? NameOf(Guid? id) =>
            id is Guid uid && reviewerNames.TryGetValue(uid, out var name) ? name : null;

        var tasResponsibleName = r.Request?.TasResponsible?.FullName
            ?? r.Request?.Initiator?.FullName
            ?? r.InitiatorFullName;

        return new MarketingRecordDto(
            r.Id, r.DocumentId, r.PortalNumber, r.RegisteredDate, r.InitiatorDepartment, r.InitiatorFullName,
            r.ReceivedDate, r.DeadlineBaseDate, r.RequestCategory, r.DeadlineWorkingDays, r.DeadlineDate,
            remaining, color, r.MarketingExecutorId, r.MarketingExecutor?.FullName,
            tasResponsibleName,
            r.AssignedByManagerId, r.AssignedByManager?.FullName, r.HandoverDate, r.AcceptedAt,
            r.RequestTitle, r.ProcurementMethod, r.StrategyNumber, r.StrategyNumberManual,
            r.BudgetAmount, r.BudgetCurrency, r.LegalBasis, r.RfqPreparedAt,
            r.RfqDocumentStorageKey, r.RfqDocumentFileName, r.RfqCommercialProposalDeadline,
            r.RfqPublishedAtgSite, r.RfqPublishedTenderweek, r.RfqSentToVendor, r.RfqSentToDistributor, r.RfqOpenSearchDone,
            r.TzIssueFound, r.TzIssueDescription, r.TzIssueResolvedAt, r.Status,
            r.Request?.MarketingCurrentStep ?? 1, r.Notes,
            r.Offers.OrderByDescending(o => o.CreatedAt)
                .Select(o => MapOffer(o, NameOf(o.InitiatorReviewedById), NameOf(o.EngineerReviewedById)))
                .ToList(),
            r.RfqDispatches.OrderByDescending(d => d.SentAt).Select(MapDispatch).ToList(),
            r.RfqChannelRequests.OrderBy(c => c.CreatedAt).Select(MapChannelRequest).ToList(),
            r.Plans.OrderByDescending(p => p.Version).Select(MapPlan).ToList(),
            r.PortalApprovals.OrderByDescending(p => p.SubmittedAt).Select(MapPortal).ToList(),
            compliant.Count > 0 ? new MarketingOffersSummaryDto(compliant.Count, (decimal?)avg, r.Offers.Count(o => o.IsAffiliated)) : null,
            r.CreatedAt, r.UpdatedAt);
    }

    private static MarketingRecordListItemDto MapListItem(MarketingRecord r, int offerCount)
    {
        var remaining = r.DeadlineDate is not null ? MarketingDeadlineService.GetRemainingWorkingDays(r.DeadlineDate.Value) : (int?)null;
        return new MarketingRecordListItemDto(
            r.Id, r.DocumentId, r.PortalNumber, r.RequestTitle, r.Status, r.RequestCategory,
            r.DeadlineDate, remaining,
            r.DeadlineDate is not null ? MarketingDeadlineService.GetDeadlineColor(r.DeadlineDate.Value) : null,
            r.MarketingExecutor?.FullName, r.InitiatorDepartment, r.BudgetAmount, r.BudgetCurrency,
            offerCount, r.UpdatedAt);
    }

    private static MarketingOfferDto MapOffer(
        MarketingOffer o,
        string? initiatorReviewerName = null,
        string? engineerReviewerName = null) => new(
        o.Id, o.CompanyName, o.OfferAmount, o.Currency, o.VatIncluded, o.DeliveryIncluded,
        o.WarrantyTerms, o.OfferDate, o.OfferValidityDate, o.ContactInfo, o.MeetsTzRequirements,
        o.RejectionReason, o.IsAffiliated, o.AffiliationNote, o.Source, o.AttachmentKey,
        o.InitiatorReviewStatus, o.InitiatorReviewedById, initiatorReviewerName, o.InitiatorReviewedAt,
        o.InitiatorReviewComment,
        o.EngineerReviewStatus, o.EngineerReviewedById, engineerReviewerName, o.EngineerReviewedAt,
        o.EngineerReviewComment,
        o.CreatedAt);

    private static RfqDispatchDto MapDispatch(RfqDispatch d) => new(
        d.Id, d.DispatchType, d.RecipientName, d.RecipientEmail, d.RecipientPhone,
        d.SentAt, d.ResponseReceivedAt, d.FollowupSentAt, d.FollowupPhoneCalled, d.Notes);

    private static MarketingRfqChannelRequestDto MapChannelRequest(MarketingRfqChannelRequest c) => new(
        c.Id, c.Channel, c.Status, c.ExternalNumber, c.HelpDeskTicketId, c.WorkTaskId,
        c.AssignedUser?.FullName, c.CreatedAt, c.CompletedAt);

    private static MarketingProcurementPlanDto MapPlan(MarketingProcurementPlan p) => new(
        p.Id, p.Version, p.ProcurementMethod, p.RegistrationMethod, p.RegistrationNumber,
        p.TemplateStorageKey, p.TemplateFileName, p.RegisteredAt,
        p.StartPrice, p.StartPriceCurrency, p.VatConsidered,
        p.Incoterms, p.CompetitionCriteria, p.EvaluationGroupMembers, p.NdsNote, p.Status,
        p.RejectionNotes, p.SubmittedAt, p.ApprovedAt, p.AttachmentKey, p.CreatedAt);

    private static MarketingPortalApprovalDto MapPortal(MarketingPortalApproval p) => new(
        p.Id, p.ApprovalType, p.SubmittedAt, p.ApprovedAt, p.BudgetNumber, p.ReminderSentAt, p.Notes);

    private async Task<User?> GetActorAsync(Guid actorId, CancellationToken ct) =>
        await db.Users.AsNoTracking().Include(u => u.Organization).Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == actorId && u.IsActive, ct);

    private async Task<User?> GetMarketingWorkerAsync(Guid userId, CancellationToken ct)
    {
        return await db.Users.Include(u => u.Department).FirstOrDefaultAsync(
            u => u.Id == userId && u.IsActive && u.Department != null && u.Department.Code == "HO-MKT-MKT", ct);
    }

    private async Task<List<Guid>> GetMarketingDeptIdsAsync(CancellationToken ct)
    {
        var mkt = await db.Departments.AsNoTracking().FirstOrDefaultAsync(d => d.Code == HoMkt, ct);
        if (mkt is null) return [];
        return await db.Departments.AsNoTracking()
            .Where(d => d.Id == mkt.Id || d.ParentId == mkt.Id)
            .Select(d => d.Id).ToListAsync(ct);
    }

    private static bool IsMarketingStaff(User u) =>
        u.Role is UserRole.SuperAdmin or UserRole.HOTopManager ||
        u.Department?.Code is HoMkt or "HO-MKT-MKT" or "HO-MKT-TND";

    private static bool IsMarketingManager(User u) =>
        u.Role is UserRole.SuperAdmin or UserRole.HOTopManager or UserRole.HONachalnik;

    private static bool CanManage(User actor, MarketingRecord record) =>
        IsMarketingManager(actor) || record.AssignedByManagerId == actor.Id;

    private async Task<bool> CanWorkRecord(Guid actorId, MarketingRecord record, CancellationToken ct)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return false;
        if (IsMarketingManager(actor)) return true;
        return record.MarketingExecutorId == actor.Id;
    }

    private async Task<bool> IsMarketingEngineerForRecordAsync(Guid actorId, MarketingRecord record, CancellationToken ct)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return false;
        if (IsMarketingManager(actor)) return true;

        var detail = record.Request;
        if (detail?.MarketingSpecialistId == actorId) return true;
        if (detail?.MarketingSpecialistId is null && detail?.Document.AssigneeId == actorId
            && actor.Department?.Code is "HO-MKT" or "HO-MKT-MKT" or "HO-MKT-TND")
            return true;

        return record.MarketingExecutorId == actorId;
    }

    private async Task<bool> CanReviewProposalAsTasAsync(ProcurementRequestDetail detail, Guid actorId, CancellationToken ct)
    {
        if (detail.Flow != ProcurementRequestFlow.TechnicalAffairs)
            return detail.InitiatorId == actorId;

        var tasResponsibleId = detail.TasResponsibleId;
        if (tasResponsibleId is null && detail.ResponsibleTaskId is not null)
        {
            tasResponsibleId = await db.WorkTasks.AsNoTracking()
                .Where(t => t.Id == detail.ResponsibleTaskId)
                .Select(t => t.AssigneeId)
                .FirstOrDefaultAsync(ct);
        }

        return tasResponsibleId == actorId;
    }

    private static bool CanView(User actor, MarketingRecord record) =>
        IsMarketingStaff(actor)
        || record.MarketingExecutorId == actor.Id
        || record.Request?.TasResponsibleId == actor.Id
        || (record.Request?.Flow != ProcurementRequestFlow.TechnicalAffairs
            && record.Request?.InitiatorId == actor.Id);
}
