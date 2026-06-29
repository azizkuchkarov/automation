using ATG.Platform.Application.Common;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Data;
using ATG.Platform.Infrastructure.Dcs;
using Microsoft.EntityFrameworkCore;

namespace ATG.Platform.Infrastructure.Services;

public class MarketingService(AppDbContext db, IAuditService audit, MarketingRfqChannelService rfqChannels) : IMarketingService
{
    private const string HoMkt = "HO-MKT";

    public async Task<Result<MarketingRecordDto>> CreateFromProcurementAsync(Guid documentId, CancellationToken ct = default)
    {
        var existing = await LoadRecordAsync(documentId, ct);
        if (existing is not null)
            return Result<MarketingRecordDto>.Ok(MapRecord(existing));

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
        return Result<MarketingRecordDto>.Ok(MapRecord(record));
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

        return Result<MarketingRecordDto>.Ok(MapRecord(record));
    }

    public async Task<Result<IReadOnlyList<MarketingRecordListItemDto>>> GetRecordsAsync(
        Guid actorId, MarketingRecordStatus? status, Guid? executorId, MarketingRequestCategory? category, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !IsMarketingStaff(actor))
            return Result<IReadOnlyList<MarketingRecordListItemDto>>.Fail("Access denied");

        var query = db.MarketingRecords.AsNoTracking()
            .Include(r => r.MarketingExecutor)
            .Include(r => r.Offers)
            .Where(r => r.Status != MarketingRecordStatus.Cancelled);

        if (!IsMarketingManager(actor))
            query = query.Where(r => r.MarketingExecutorId == actor.Id || r.AssignedByManagerId == actor.Id);

        if (status is not null) query = query.Where(r => r.Status == status);
        if (executorId is not null) query = query.Where(r => r.MarketingExecutorId == executorId);
        if (category is not null) query = query.Where(r => r.RequestCategory == category);

        var items = await query.OrderByDescending(r => r.UpdatedAt).ToListAsync(ct);
        return Result<IReadOnlyList<MarketingRecordListItemDto>>.Ok(items.Select(MapListItem).ToList());
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
        if (record.Request.MarketingCurrentStep != 4)
            return Result<MarketingRecordDto>.Fail("RFQ document can only be uploaded at marketing step 4");
        if (string.IsNullOrWhiteSpace(request.StorageKey) || string.IsNullOrWhiteSpace(request.FileName))
            return Result<MarketingRecordDto>.Fail("RFQ file is required");

        record.RfqDocumentStorageKey = request.StorageKey.Trim();
        record.RfqDocumentFileName = request.FileName.Trim();
        record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetByDocumentIdAsync(documentId, actorId, ct);
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
        };
        db.MarketingOffers.Add(offer);
        record.Offers.Add(offer);
        record.Status = MarketingRecordStatus.KpAnalysis;
        record.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Result<MarketingOfferDto>.Ok(MapOffer(offer));
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

        var records = await db.MarketingRecords.AsNoTracking()
            .Include(r => r.MarketingExecutor)
            .Where(r => r.Status != MarketingRecordStatus.Cancelled)
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var overdue = records.Count(r => r.DeadlineDate is not null && r.DeadlineDate < today
            && r.Status != MarketingRecordStatus.CompletedToContract);
        var completed = records.Count(r => r.Status == MarketingRecordStatus.CompletedToContract);
        var inProgress = records.Count - completed;

        return Result<MarketingStatsDto>.Ok(new MarketingStatsDto(
            records.Count,
            inProgress,
            overdue,
            completed,
            records.Where(r => r.RequestCategory is not null)
                .GroupBy(r => r.RequestCategory!.Value)
                .Select(g => new MarketingCategoryStatDto(g.Key, g.Count())).ToList(),
            records.Where(r => r.MarketingExecutor is not null)
                .GroupBy(r => r.MarketingExecutor!.FullName)
                .Select(g => new MarketingExecutorStatDto(g.Key, g.Count(),
                    g.Count(r => r.DeadlineDate < today && r.Status != MarketingRecordStatus.CompletedToContract)))
                .ToList(),
            records.Where(r => r.ProcurementMethod is not null)
                .GroupBy(r => r.ProcurementMethod!.Value)
                .Select(g => new MarketingMethodStatDto(g.Key, g.Count()))
                .ToList()));
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
        var list = await GetRecordsAsync(actorId, null, null, null, ct);
        if (!list.IsSuccess) return Result<IReadOnlyList<MarketingBoardColumnDto>>.Fail(list.Error!);

        var columns = new[]
        {
            (MarketingRecordStatus.WaitingAccept, "Executor kutilmoqda", "Awaiting executor"),
            (MarketingRecordStatus.RfqSent, "RFQ jarayoni", "RFQ process"),
            (MarketingRecordStatus.KpAnalysis, "KP tahlil", "KP analysis"),
            (MarketingRecordStatus.PlanPreparation, "Reja", "Plan"),
            (MarketingRecordStatus.PlanPortalApproval, "Portal", "Portal"),
            (MarketingRecordStatus.CompletedToContract, "Tugallandi", "Completed"),
        };

        var items = list.Data!;
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
            3 => MarketingRecordStatus.RfqPreparation,
            4 or 5 => MarketingRecordStatus.RfqSent,
            6 => MarketingRecordStatus.KpAnalysis,
            7 => MarketingRecordStatus.PlanPreparation,
            8 => MarketingRecordStatus.PlanPortalApproval,
            9 => MarketingRecordStatus.PlanMonitoring,
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
            .Include(r => r.Request)
            .FirstOrDefaultAsync(r => r.DocumentId == documentId, ct);

    private async Task<MarketingRecord?> LoadRecordTrackedAsync(Guid documentId, CancellationToken ct) =>
        await db.MarketingRecords
            .Include(r => r.Offers)
            .Include(r => r.RfqDispatches)
            .Include(r => r.RfqChannelRequests).ThenInclude(c => c.AssignedUser)
            .Include(r => r.Plans)
            .Include(r => r.PortalApprovals)
            .Include(r => r.Request).ThenInclude(req => req.Document)
            .FirstOrDefaultAsync(r => r.DocumentId == documentId, ct);

    private MarketingRecordDto MapRecord(MarketingRecord r)
    {
        var remaining = r.DeadlineDate is not null ? MarketingDeadlineService.GetRemainingWorkingDays(r.DeadlineDate.Value) : (int?)null;
        var color = r.DeadlineDate is not null ? MarketingDeadlineService.GetDeadlineColor(r.DeadlineDate.Value) : null;
        var compliant = r.Offers.Where(o => o.MeetsTzRequirements == true).ToList();
        var avg = compliant.Count > 0 ? compliant.Average(o => (double)(o.OfferAmount ?? 0)) : (double?)null;

        return new MarketingRecordDto(
            r.Id, r.DocumentId, r.PortalNumber, r.RegisteredDate, r.InitiatorDepartment, r.InitiatorFullName,
            r.ReceivedDate, r.DeadlineBaseDate, r.RequestCategory, r.DeadlineWorkingDays, r.DeadlineDate,
            remaining, color, r.MarketingExecutorId, r.MarketingExecutor?.FullName,
            r.AssignedByManagerId, r.AssignedByManager?.FullName, r.HandoverDate, r.AcceptedAt,
            r.RequestTitle, r.ProcurementMethod, r.StrategyNumber, r.StrategyNumberManual,
            r.BudgetAmount, r.BudgetCurrency, r.LegalBasis, r.RfqPreparedAt,
            r.RfqDocumentStorageKey, r.RfqDocumentFileName,
            r.RfqPublishedAtgSite, r.RfqPublishedTenderweek, r.RfqSentToVendor, r.RfqSentToDistributor, r.RfqOpenSearchDone,
            r.TzIssueFound, r.TzIssueDescription, r.TzIssueResolvedAt, r.Status,
            r.Request?.MarketingCurrentStep ?? 1, r.Notes,
            r.Offers.OrderByDescending(o => o.CreatedAt).Select(MapOffer).ToList(),
            r.RfqDispatches.OrderByDescending(d => d.SentAt).Select(MapDispatch).ToList(),
            r.RfqChannelRequests.OrderBy(c => c.CreatedAt).Select(MapChannelRequest).ToList(),
            r.Plans.OrderByDescending(p => p.Version).Select(MapPlan).ToList(),
            r.PortalApprovals.OrderByDescending(p => p.SubmittedAt).Select(MapPortal).ToList(),
            compliant.Count > 0 ? new MarketingOffersSummaryDto(compliant.Count, (decimal?)avg, r.Offers.Count(o => o.IsAffiliated)) : null,
            r.CreatedAt, r.UpdatedAt);
    }

    private static MarketingRecordListItemDto MapListItem(MarketingRecord r)
    {
        var remaining = r.DeadlineDate is not null ? MarketingDeadlineService.GetRemainingWorkingDays(r.DeadlineDate.Value) : (int?)null;
        return new MarketingRecordListItemDto(
            r.Id, r.DocumentId, r.PortalNumber, r.RequestTitle, r.Status, r.RequestCategory,
            r.DeadlineDate, remaining,
            r.DeadlineDate is not null ? MarketingDeadlineService.GetDeadlineColor(r.DeadlineDate.Value) : null,
            r.MarketingExecutor?.FullName, r.InitiatorDepartment, r.BudgetAmount, r.BudgetCurrency,
            r.Offers.Count, r.UpdatedAt);
    }

    private static MarketingOfferDto MapOffer(MarketingOffer o) => new(
        o.Id, o.CompanyName, o.OfferAmount, o.Currency, o.VatIncluded, o.DeliveryIncluded,
        o.WarrantyTerms, o.OfferDate, o.OfferValidityDate, o.ContactInfo, o.MeetsTzRequirements,
        o.RejectionReason, o.IsAffiliated, o.AffiliationNote, o.Source, o.AttachmentKey, o.CreatedAt);

    private static RfqDispatchDto MapDispatch(RfqDispatch d) => new(
        d.Id, d.DispatchType, d.RecipientName, d.RecipientEmail, d.RecipientPhone,
        d.SentAt, d.ResponseReceivedAt, d.FollowupSentAt, d.FollowupPhoneCalled, d.Notes);

    private static MarketingRfqChannelRequestDto MapChannelRequest(MarketingRfqChannelRequest c) => new(
        c.Id, c.Channel, c.Status, c.ExternalNumber, c.HelpDeskTicketId, c.WorkTaskId,
        c.AssignedUser?.FullName, c.CreatedAt, c.CompletedAt);

    private static MarketingProcurementPlanDto MapPlan(MarketingProcurementPlan p) => new(
        p.Id, p.Version, p.ProcurementMethod, p.StartPrice, p.StartPriceCurrency, p.VatConsidered,
        p.Incoterms, p.CompetitionCriteria, p.EvaluationGroupMembers, p.NdsNote, p.Status,
        p.RejectionNotes, p.SubmittedAt, p.ApprovedAt, p.AttachmentKey, p.CreatedAt);

    private static MarketingPortalApprovalDto MapPortal(MarketingPortalApproval p) => new(
        p.Id, p.ApprovalType, p.SubmittedAt, p.ApprovedAt, p.BudgetNumber, p.ReminderSentAt, p.Notes);

    private async Task<User?> GetActorAsync(Guid actorId, CancellationToken ct) =>
        await db.Users.Include(u => u.Organization).Include(u => u.Department)
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

    private static bool CanView(User actor, MarketingRecord record) =>
        IsMarketingStaff(actor) || record.MarketingExecutorId == actor.Id;
}
