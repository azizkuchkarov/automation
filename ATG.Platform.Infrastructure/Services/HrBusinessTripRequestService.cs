using ATG.Platform.Application.Common;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Application.Options;
using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Data;
using ATG.Platform.Infrastructure.Hr;
using ATG.Platform.Infrastructure.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace ATG.Platform.Infrastructure.Services;

public class HrBusinessTripRequestService(
    AppDbContext db,
    IAuditService audit,
    INotificationService notifications,
    IFileStorageService files,
    IEimzoServerClient eimzo,
    IHrBusinessTripWorkflowService workflowService,
    IOptions<HrLeaveOptions> hrLeaveOptions) : IHrBusinessTripRequestService
{
    private readonly HrLeaveOptions _hrOptions = hrLeaveOptions.Value;
    private const string HoFirstDeputyGdEmail = "m.azizov@atg.uz";

    public async Task<Result<IReadOnlyList<HrBusinessTripListItemDto>>> GetMyRequestsAsync(
        Guid actorId, CancellationToken ct = default)
    {
        var asAuthor = await db.HrBusinessTripRequestDetails.AsNoTracking()
            .Include(d => d.Document).ThenInclude(doc => doc.Department)
            .Include(d => d.Travelers)
            .Where(d => d.Document.AuthorId == actorId)
            .ToListAsync(ct);

        var asTraveler = await db.HrBusinessTripRequestDetails.AsNoTracking()
            .Include(d => d.Document).ThenInclude(doc => doc.Department)
            .Include(d => d.Travelers)
            .Where(d => d.Travelers.Any(t =>
                t.UserId == actorId && !string.IsNullOrWhiteSpace(t.CertificateStorageKey)))
            .ToListAsync(ct);

        var items = asAuthor
            .Concat(asTraveler)
            .GroupBy(d => d.DocumentId)
            .Select(g => g.First())
            .OrderByDescending(d => d.Document.CreatedAt)
            .Select(d => MapListItemForViewer(d, actorId))
            .ToList();

        return Result<IReadOnlyList<HrBusinessTripListItemDto>>.Ok(items);
    }

    public async Task<Result<IReadOnlyList<HrBusinessTripListItemDto>>> GetApprovalQueueAsync(
        Guid actorId, CancellationToken ct = default)
    {
        var items = new List<HrBusinessTripListItemDto>();

        var hrReviewItems = await db.HrBusinessTripRequestDetails.AsNoTracking()
            .Include(d => d.Document).ThenInclude(doc => doc.Department)
            .Include(d => d.Travelers)
            .Where(d => d.Phase == HrBusinessTripPhase.HrReview && d.Document.AssigneeId == actorId)
            .OrderByDescending(d => d.Document.UpdatedAt)
            .ToListAsync(ct);
        items.AddRange(hrReviewItems.Select(d => MapListItem(d)));

        var details = await db.HrBusinessTripRequestDetails.AsNoTracking()
            .Include(d => d.Document).ThenInclude(doc => doc.Department)
            .Include(d => d.Travelers)
            .Include(d => d.Approvers)
            .Where(d => d.Phase == HrBusinessTripPhase.AwaitingApproval)
            .Where(d => d.Approvers.Any(a =>
                a.UserId == actorId
                && a.Status == HrLeaveApproverStatus.Pending
                && !d.Approvers.Any(b =>
                    b.Status == HrLeaveApproverStatus.Pending && b.SortOrder < a.SortOrder)))
            .OrderByDescending(d => d.Document.UpdatedAt)
            .ToListAsync(ct);

        foreach (var item in details.Select(d => MapListItem(d)))
        {
            if (items.All(i => i.Id != item.Id))
                items.Add(item);
        }

        var actor = await GetActorAsync(actorId, ct);
        if (actor is not null && await IsOrderResponsibleAsync(actor, ct))
        {
            var orderItems = await db.HrBusinessTripRequestDetails.AsNoTracking()
                .Include(d => d.Document).ThenInclude(doc => doc.Department)
                .Include(d => d.Travelers)
                .Where(d => d.Phase == HrBusinessTripPhase.OrderPending && d.Document.AssigneeId == actorId)
                .OrderByDescending(d => d.Document.UpdatedAt)
                .ToListAsync(ct);
            foreach (var item in orderItems.Select(d => MapListItem(d)))
            {
                if (items.All(i => i.Id != item.Id))
                    items.Add(item);
            }

            var certificateItems = await db.HrBusinessTripRequestDetails.AsNoTracking()
                .Include(d => d.Document).ThenInclude(doc => doc.Department)
                .Include(d => d.Travelers)
                .Where(d => d.Phase == HrBusinessTripPhase.CertificatePending && d.Document.AssigneeId == actorId)
                .OrderByDescending(d => d.Document.UpdatedAt)
                .ToListAsync(ct);
            foreach (var item in certificateItems.Select(d => MapListItem(d)))
            {
                if (items.All(i => i.Id != item.Id))
                    items.Add(item);
            }
        }

        if (actor is not null)
        {
            var gdOrderItems = await db.HrBusinessTripRequestDetails.AsNoTracking()
                .Include(d => d.Document).ThenInclude(doc => doc.Department)
                .Include(d => d.Travelers)
                .Where(d => d.Phase == HrBusinessTripPhase.AwaitingOrderEimzo && d.Document.AssigneeId == actorId)
                .OrderByDescending(d => d.Document.UpdatedAt)
                .ToListAsync(ct);
            foreach (var item in gdOrderItems.Select(d => MapListItem(d)))
            {
                if (items.All(i => i.Id != item.Id))
                    items.Add(item);
            }
        }

        return Result<IReadOnlyList<HrBusinessTripListItemDto>>.Ok(
            items.OrderByDescending(i => i.CreatedAt).ToList());
    }

    public async Task<Result<IReadOnlyList<HrBusinessTripListItemDto>>> GetOrderQueueAsync(
        Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<IReadOnlyList<HrBusinessTripListItemDto>>.Fail("User not found");
        if (!await IsOrderResponsibleAsync(actor, ct))
            return Result<IReadOnlyList<HrBusinessTripListItemDto>>.Ok([]);

        var items = await db.HrBusinessTripRequestDetails.AsNoTracking()
            .Include(d => d.Document).ThenInclude(doc => doc.Department)
            .Include(d => d.Travelers)
            .Where(d => d.Phase == HrBusinessTripPhase.OrderPending && d.Document.AssigneeId == actorId)
            .OrderByDescending(d => d.Document.UpdatedAt)
            .Select(d => MapListItem(d))
            .ToListAsync(ct);

        return Result<IReadOnlyList<HrBusinessTripListItemDto>>.Ok(items);
    }

    public async Task<Result<IReadOnlyList<HrBusinessTripListItemDto>>> GetCertificateQueueAsync(
        Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<IReadOnlyList<HrBusinessTripListItemDto>>.Fail("User not found");
        if (!await IsOrderResponsibleAsync(actor, ct))
            return Result<IReadOnlyList<HrBusinessTripListItemDto>>.Ok([]);

        var items = await db.HrBusinessTripRequestDetails.AsNoTracking()
            .Include(d => d.Document).ThenInclude(doc => doc.Department)
            .Include(d => d.Travelers)
            .Where(d => d.Phase == HrBusinessTripPhase.CertificatePending && d.Document.AssigneeId == actorId)
            .OrderByDescending(d => d.Document.UpdatedAt)
            .Select(d => MapListItem(d))
            .ToListAsync(ct);

        return Result<IReadOnlyList<HrBusinessTripListItemDto>>.Ok(items);
    }

    public async Task<Result<IReadOnlyList<HrBusinessTripColleagueDto>>> GetDepartmentColleaguesAsync(
        Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<IReadOnlyList<HrBusinessTripColleagueDto>>.Fail("User not found");
        if (actor.DepartmentId is null)
            return Result<IReadOnlyList<HrBusinessTripColleagueDto>>.Fail("Your profile has no department");

        var colleagues = await db.Users.AsNoTracking()
            .Include(u => u.Position)
            .Where(u => u.IsActive && u.DepartmentId == actor.DepartmentId && u.Id != actorId)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync(ct);

        var result = colleagues.Select(u => new HrBusinessTripColleagueDto(
            u.Id,
            u.FullName,
            string.IsNullOrWhiteSpace(u.FullNameEn) ? null : u.FullNameEn,
            u.GetJobTitle("ru") ?? u.Position?.Name ?? "",
            u.GetJobTitle("en") ?? u.Position?.Name)).ToList();

        return Result<IReadOnlyList<HrBusinessTripColleagueDto>>.Ok(result);
    }

    public async Task<Result<HrBusinessTripRequestDto>> GetByIdAsync(Guid id, Guid actorId, CancellationToken ct = default)
    {
        var detail = await LoadDetailAsync(id, ct);
        if (detail is null) return Result<HrBusinessTripRequestDto>.Fail("Request not found");
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<HrBusinessTripRequestDto>.Fail("Access denied");
        if (!CanView(actor, detail) && !await HasActedAsync(id, actorId, ct))
            return Result<HrBusinessTripRequestDto>.Fail("Access denied");
        return Result<HrBusinessTripRequestDto>.Ok(await MapDetailAsync(detail, actor, ct));
    }

    public async Task<Result<HrBusinessTripRequestDto>> CreateAsync(
        CreateHrBusinessTripRequestRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<HrBusinessTripRequestDto>.Fail("User not found");
        if (actor.DepartmentId is null) return Result<HrBusinessTripRequestDto>.Fail("Your profile has no department");

        var validation = ValidateRequest(request);
        if (validation is not null) return Result<HrBusinessTripRequestDto>.Fail(validation);

        var number = await GenerateNumberAsync(ct);
        var days = HrBusinessTripTextBuilder.ComputeDaysInclusive(request.DateFrom, request.DateTo);
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            Number = number,
            Title = $"Командировка — {request.PlaceRu.Trim()}",
            Type = DocumentType.HrBusinessTripRequest,
            Status = DocumentStatus.Draft,
            AuthorId = actorId,
            OrganizationId = actor.OrganizationId,
            DepartmentId = actor.DepartmentId.Value,
            AssigneeId = actorId,
        };

        var detail = new HrBusinessTripRequestDetail
        {
            DocumentId = doc.Id,
            Document = doc,
            Phase = HrBusinessTripPhase.Draft,
            RequestDate = DateTimeNormalization.ToUtc(request.RequestDate),
            PurposeRu = request.PurposeRu.Trim(),
            PurposeEn = request.PurposeEn?.Trim(),
            DateFrom = DateTimeNormalization.ToUtc(request.DateFrom),
            DateTo = DateTimeNormalization.ToUtc(request.DateTo),
            DaysCount = days,
            PlaceRu = request.PlaceRu.Trim(),
            PlaceEn = request.PlaceEn?.Trim(),
        };

        AddTravelers(detail, request.Travelers);
        db.Documents.Add(doc);
        db.HrBusinessTripRequestDetails.Add(detail);
        await AddActivityAsync(doc, actorId, "created", null, DocumentStatus.Draft, null, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "HrBusinessTripCreated", "Document", doc.Id, number, ip, ct);

        var loaded = await LoadDetailAsync(doc.Id, ct);
        return Result<HrBusinessTripRequestDto>.Ok(await MapDetailAsync(loaded!, actor, ct));
    }

    public async Task<Result<HrBusinessTripRequestDto>> UpdateAsync(
        Guid id, UpdateHrBusinessTripRequestRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<HrBusinessTripRequestDto>.Fail("Request not found");
        if (detail.Phase != HrBusinessTripPhase.Draft)
            return Result<HrBusinessTripRequestDto>.Fail("Only draft requests can be edited");
        if (detail.Document.AuthorId != actorId)
            return Result<HrBusinessTripRequestDto>.Fail("Access denied");

        var validation = ValidateRequest(request);
        if (validation is not null) return Result<HrBusinessTripRequestDto>.Fail(validation);

        detail.RequestDate = DateTimeNormalization.ToUtc(request.RequestDate);
        detail.PurposeRu = request.PurposeRu.Trim();
        detail.PurposeEn = request.PurposeEn?.Trim();
        detail.DateFrom = DateTimeNormalization.ToUtc(request.DateFrom);
        detail.DateTo = DateTimeNormalization.ToUtc(request.DateTo);
        detail.DaysCount = HrBusinessTripTextBuilder.ComputeDaysInclusive(request.DateFrom, request.DateTo);
        detail.PlaceRu = request.PlaceRu.Trim();
        detail.PlaceEn = request.PlaceEn?.Trim();
        detail.Document.Title = $"Командировка — {detail.PlaceRu}";
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await db.HrBusinessTripTravelers.Where(t => t.DocumentId == id).ExecuteDeleteAsync(ct);
        detail.Travelers.Clear();
        AddTravelers(detail, request.Travelers);

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "HrBusinessTripUpdated", "Document", id, null, ip, ct);

        var actor = await GetActorAsync(actorId, ct);
        return Result<HrBusinessTripRequestDto>.Ok(await MapDetailAsync(detail, actor!, ct));
    }

    public async Task<Result<HrBusinessTripRequestDto>> SubmitAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<HrBusinessTripRequestDto>.Fail("Request not found");
        if (detail.Phase != HrBusinessTripPhase.Draft)
            return Result<HrBusinessTripRequestDto>.Fail("Request is already submitted");
        if (detail.Document.AuthorId != actorId)
            return Result<HrBusinessTripRequestDto>.Fail("Access denied");
        if (detail.Travelers.Count == 0)
            return Result<HrBusinessTripRequestDto>.Fail("Add at least one traveler");

        await BuildApproversAsync(detail, ct);
        if (detail.Approvers.Count == 0)
            return Result<HrBusinessTripRequestDto>.Fail("Approval chain could not be built");

        var next = GetNextPendingApprover(detail);
        if (next is null)
            return Result<HrBusinessTripRequestDto>.Fail("Approval chain could not be built");

        detail.Phase = HrBusinessTripPhase.AwaitingApproval;
        detail.Document.Status = DocumentStatus.InReview;
        detail.Document.AssigneeId = next.UserId;
        detail.Document.UpdatedAt = DateTime.UtcNow;
        await EnsurePdfAsync(detail, ct);

        await CreateLinkedTaskAsync(
            next.UserId,
            actorId,
            detail.Document.DepartmentId,
            detail.Document.OrganizationId,
            $"Business trip approval — {detail.Document.Number}",
            detail.Document.Title,
            detail.Document.Id,
            ct);

        await AddActivityAsync(detail.Document, actorId, "submitted", DocumentStatus.Draft, DocumentStatus.InReview, null, ct);
        await SaveChangesWithTaskNumberRetryAsync(ct);
        await audit.LogAsync(actorId, "HrBusinessTripSubmitted", "Document", id, null, ip, ct);
        await notifications.NotifyDcsApprovalRequiredAsync(
            next.UserId, detail.Document.Number, detail.Document.Title, detail.DocumentId, ct);

        var actor = await GetActorAsync(actorId, ct);
        return Result<HrBusinessTripRequestDto>.Ok(await MapDetailAsync(detail, actor!, ct));
    }

    public async Task<Result<HrBusinessTripRequestDto>> HrReviewAsync(
        Guid id, HrBusinessTripApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<HrBusinessTripRequestDto>.Fail("Request not found");
        if (detail.Phase != HrBusinessTripPhase.HrReview)
            return Result<HrBusinessTripRequestDto>.Fail("Request is not in HR review");
        if (detail.Document.AssigneeId != actorId)
            return Result<HrBusinessTripRequestDto>.Fail("You are not the HR reviewer for this request");

        await BuildApproversAsync(detail, ct);
        var next = GetNextPendingApprover(detail);
        detail.Phase = HrBusinessTripPhase.AwaitingApproval;
        detail.Document.AssigneeId = next?.UserId ?? actorId;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "hr_reviewed", null, detail.Document.Status, null, ct);
        await db.SaveChangesAsync(ct);
        await db.Entry(detail).Collection(d => d.Approvers).Query()
            .Include(a => a.User).ThenInclude(u => u.Position)
            .LoadAsync(ct);
        await audit.LogAsync(actorId, "HrBusinessTripHrReviewed", "Document", id, null, ip, ct);
        if (next is not null)
            await notifications.NotifyDcsApprovalRequiredAsync(
                next.UserId, detail.Document.Number, detail.Document.Title, detail.DocumentId, ct);

        var actor = await GetActorAsync(actorId, ct);
        return Result<HrBusinessTripRequestDto>.Ok(await MapDetailAsync(detail, actor!, ct));
    }

    public async Task<Result<HrBusinessTripRequestDto>> ApproveAsync(
        Guid id, HrBusinessTripApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<HrBusinessTripRequestDto>.Fail("Request not found");
        if (detail.Phase != HrBusinessTripPhase.AwaitingApproval)
            return Result<HrBusinessTripRequestDto>.Fail("Request is not awaiting approval");

        var approver = GetNextPendingApprover(detail);
        if (approver is null) return Result<HrBusinessTripRequestDto>.Fail("No pending approvers");
        if (approver.UserId != actorId)
            return Result<HrBusinessTripRequestDto>.Fail("Wait for the previous approver in the chain");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<HrBusinessTripRequestDto>.Fail("User not found");

        if (RequiresEimzoSignature(detail, approver, actor))
        {
            var eimzoResult = await ProcessEimzoApprovalAsync(detail, approver, actor, request, ip, ct);
            if (eimzoResult is not null)
                return eimzoResult;
        }

        approver.Status = HrLeaveApproverStatus.Approved;
        approver.DecidedAt = DateTime.UtcNow;
        approver.Comment = request.Comment?.Trim();
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "approved", null, detail.Document.Status,
            approver.Role.ToString(), ct);

        if (detail.Approvers.All(a => a.Status == HrLeaveApproverStatus.Approved))
        {
            detail.ApprovedAt = DateTime.UtcNow;
            detail.Document.Status = DocumentStatus.Approved;
            await AddActivityAsync(detail.Document, actorId, "fully_approved", null,
                DocumentStatus.Approved, null, ct);
            if (!detail.EimzoCompletedAt.HasValue)
                await EnsurePdfAsync(detail, ct);

            var orderResponsible = await GetOrderResponsibleAsync(detail, ct);
            if (orderResponsible is not null)
            {
                detail.Phase = HrBusinessTripPhase.OrderPending;
                detail.Document.AssigneeId = orderResponsible.Id;
                await CreateLinkedTaskAsync(
                    orderResponsible.Id,
                    actorId,
                    orderResponsible.DepartmentId ?? detail.Document.DepartmentId,
                    detail.Document.OrganizationId,
                    $"Prepare business trip order — {detail.Document.Number}",
                    detail.Document.Title,
                    detail.Document.Id,
                    ct);
                await notifications.NotifyDcsApprovalRequiredAsync(
                    orderResponsible.Id, detail.Document.Number, detail.Document.Title, detail.DocumentId, ct);
            }
            else
            {
                detail.Phase = HrBusinessTripPhase.Completed;
                detail.Document.AssigneeId = detail.Document.AuthorId;
            }
        }
        else
        {
            var next = GetNextPendingApprover(detail);
            if (next is not null)
            {
                detail.Document.AssigneeId = next.UserId;
                await notifications.NotifyDcsApprovalRequiredAsync(
                    next.UserId, detail.Document.Number, detail.Document.Title, detail.DocumentId, ct);
            }
        }

        await SaveChangesWithTaskNumberRetryAsync(ct);
        await audit.LogAsync(actorId, "HrBusinessTripApproved", "Document", id, null, ip, ct);

        return Result<HrBusinessTripRequestDto>.Ok(await MapDetailAsync(detail, actor, ct));
    }

    public async Task<Result<HrBusinessTripSigningPackageDto>> GetSigningPackageAsync(
        Guid id, Guid actorId, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<HrBusinessTripSigningPackageDto>.Fail("Request not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanView(actor, detail))
            return Result<HrBusinessTripSigningPackageDto>.Fail("Access denied");

        var approver = GetNextPendingApprover(detail);
        if (approver is null || approver.UserId != actorId)
            return Result<HrBusinessTripSigningPackageDto>.Fail("You are not the pending approver");
        if (!RequiresEimzoSignature(detail, approver, actor))
            return Result<HrBusinessTripSigningPackageDto>.Fail("E-IMZO is not required for this approval step");

        await EnsureSigningArtifactsAsync(detail, ct);
        await db.SaveChangesAsync(ct);
        var canonicalJson = HrBusinessTripSigningPayloadBuilder.BuildCanonicalJson(detail);
        var pdfBytes = await GetPdfBytesAsync(detail, ct);

        return Result<HrBusinessTripSigningPackageDto>.Ok(new HrBusinessTripSigningPackageDto(
            HrBusinessTripSigningPayloadBuilder.ToBase64(canonicalJson),
            Convert.ToBase64String(pdfBytes),
            detail.SigningPayloadHash!,
            detail.Document.Number));
    }

    public async Task<Result<HrBusinessTripSigningPackageDto>> GetOrderSigningPackageAsync(
        Guid id, Guid actorId, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<HrBusinessTripSigningPackageDto>.Fail("Request not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanView(actor, detail))
            return Result<HrBusinessTripSigningPackageDto>.Fail("Access denied");
        if (!RequiresOrderEimzoSignature(detail, actor))
            return Result<HrBusinessTripSigningPackageDto>.Fail("E-IMZO is not required for this order");

        var batch = await LoadOrderBatchTrackedAsync(detail, ct);
        var orderNumber = detail.OrderNumber!;
        var orderDate = detail.OrderIssuedAt ?? DateTime.UtcNow;
        var canonicalJson = HrBusinessTripOrderSigningPayloadBuilder.BuildCanonicalJson(orderNumber, orderDate, batch);
        detail.SigningPayloadHash = HrBusinessTripOrderSigningPayloadBuilder.ComputeSha256Hex(canonicalJson);
        await db.SaveChangesAsync(ct);

        var pdfBytes = HrBusinessTripOrderDocumentGenerator.Generate(
            batch, orderNumber, orderDate, BuildBusinessTripVerificationUrl(batch[0]));
        return Result<HrBusinessTripSigningPackageDto>.Ok(new HrBusinessTripSigningPackageDto(
            HrBusinessTripOrderSigningPayloadBuilder.ToBase64(canonicalJson),
            Convert.ToBase64String(pdfBytes),
            detail.SigningPayloadHash,
            orderNumber));
    }

    public async Task<Result<HrBusinessTripRequestDto>> SignOrderWithEimzoAsync(
        Guid id, HrBusinessTripApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<HrBusinessTripRequestDto>.Fail("Request not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<HrBusinessTripRequestDto>.Fail("User not found");
        if (!RequiresOrderEimzoSignature(detail, actor))
            return Result<HrBusinessTripRequestDto>.Fail("You are not authorized to sign this order");

        var batch = await LoadOrderBatchTrackedAsync(detail, ct);
        if (batch.Any(d => d.EimzoCompletedAt.HasValue))
        {
            foreach (var item in batch.Where(d => d.Phase == HrBusinessTripPhase.AwaitingOrderEimzo))
                await TransitionToCertificatePendingAsync(item, actorId, ct);
            await SaveChangesWithTaskNumberRetryAsync(ct);
            return Result<HrBusinessTripRequestDto>.Ok(await MapDetailAsync(detail, actor, ct));
        }

        var eimzoResult = await ProcessOrderEimzoAsync(batch, actor, request, ip, ct);
        if (eimzoResult is not null)
            return eimzoResult;

        foreach (var item in batch)
        {
            await TransitionToCertificatePendingAsync(item, actorId, ct);
            await AddActivityAsync(item.Document, actorId, "order_signed", null,
                item.Document.Status, item.OrderNumber, ct);
        }

        await SaveChangesWithTaskNumberRetryAsync(ct);
        await audit.LogAsync(actorId, "HrBusinessTripOrderEimzoSigned", "Document", id, detail.OrderNumber, ip, ct);
        return Result<HrBusinessTripRequestDto>.Ok(await MapDetailAsync(detail, actor, ct));
    }

    public async Task<Result<(Stream Stream, string ContentType, string FileName)>> DownloadSignedPdfAsync(
        Guid id, Guid actorId, string? clientIp, CancellationToken ct = default)
    {
        var detail = await LoadDetailAsync(id, ct);
        if (detail is null) return Result<(Stream, string, string)>.Fail("Request not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanView(actor, detail))
            return Result<(Stream, string, string)>.Fail("Access denied");

        if (!string.IsNullOrWhiteSpace(detail.PdfPresentationStorageKey))
            return await OpenStoredFileAsync(
                detail.PdfPresentationStorageKey,
                "application/pdf",
                $"{detail.OrderNumber ?? detail.Document.Number}_signed.pdf",
                ct);

        var pkcs7Key = GetSignedPdfStorageKey(detail);
        if (string.IsNullOrWhiteSpace(pkcs7Key))
            return Result<(Stream, string, string)>.Fail("Signed PDF is not available yet");

        return await ExtractPdfFromSignedPkcs7Async(
            pkcs7Key,
            detail.OrderNumber ?? detail.Document.Number,
            clientIp,
            ct);
    }

    public async Task<Result<(Stream Stream, string ContentType, string FileName)>> DownloadPresentationPdfAsync(
        Guid id, Guid actorId, string? clientIp, CancellationToken ct = default) =>
        await DownloadSignedPdfAsync(id, actorId, clientIp, ct);

    public async Task<Result<HrBusinessTripRequestDto>> RejectAsync(
        Guid id, HrBusinessTripApprovalRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<HrBusinessTripRequestDto>.Fail("Request not found");

        if (detail.Phase == HrBusinessTripPhase.HrReview)
        {
            if (detail.Document.AssigneeId != actorId)
                return Result<HrBusinessTripRequestDto>.Fail("You cannot reject this request now");
            if (string.IsNullOrWhiteSpace(request.Comment))
                return Result<HrBusinessTripRequestDto>.Fail("Rejection reason is required");

            detail.Phase = HrBusinessTripPhase.Rejected;
            detail.Document.Status = DocumentStatus.Rejected;
            detail.Document.AssigneeId = detail.Document.AuthorId;
            detail.Document.UpdatedAt = DateTime.UtcNow;

            await AddActivityAsync(detail.Document, actorId, "rejected", null, DocumentStatus.Rejected,
                request.Comment.Trim(), ct);
            await db.SaveChangesAsync(ct);
            await audit.LogAsync(actorId, "HrBusinessTripRejected", "Document", id, null, ip, ct);
            await notifications.NotifyDcsApprovalRejectedAsync(
                detail.Document.AuthorId, detail.Document.Number, detail.DocumentId, ct);

            var rejectActor = await GetActorAsync(actorId, ct);
            return Result<HrBusinessTripRequestDto>.Ok(await MapDetailAsync(detail, rejectActor!, ct));
        }

        if (detail.Phase != HrBusinessTripPhase.AwaitingApproval)
            return Result<HrBusinessTripRequestDto>.Fail("Request cannot be rejected in this phase");
        if (string.IsNullOrWhiteSpace(request.Comment))
            return Result<HrBusinessTripRequestDto>.Fail("Rejection reason is required");

        var approver = GetNextPendingApprover(detail);
        if (approver is null || approver.UserId != actorId)
            return Result<HrBusinessTripRequestDto>.Fail("You cannot reject this request now");

        approver.Status = HrLeaveApproverStatus.Rejected;
        approver.DecidedAt = DateTime.UtcNow;
        approver.Comment = request.Comment.Trim();
        detail.Phase = HrBusinessTripPhase.Rejected;
        detail.Document.Status = DocumentStatus.Rejected;
        detail.Document.AssigneeId = detail.Document.AuthorId;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        await AddActivityAsync(detail.Document, actorId, "rejected", null, DocumentStatus.Rejected,
            request.Comment.Trim(), ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "HrBusinessTripRejected", "Document", id, null, ip, ct);
        await notifications.NotifyDcsApprovalRejectedAsync(
            detail.Document.AuthorId, detail.Document.Number, detail.DocumentId, ct);

        var actor = await GetActorAsync(actorId, ct);
        return Result<HrBusinessTripRequestDto>.Ok(await MapDetailAsync(detail, actor!, ct));
    }

    public async Task<Result<(Stream Stream, string ContentType, string FileName)>> DownloadPdfAsync(
        Guid id, Guid actorId, CancellationToken ct = default)
    {
        var detail = await LoadDetailAsync(id, ct);
        if (detail is null) return Result<(Stream, string, string)>.Fail("Request not found");
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanView(actor, detail))
            return Result<(Stream, string, string)>.Fail("Access denied");

        await EnsurePdfAsync(detail, ct);
        if (string.IsNullOrWhiteSpace(detail.PdfStorageKey))
            return Result<(Stream, string, string)>.Fail("PDF is not available yet");

        var stored = await files.DownloadAsync(detail.PdfStorageKey, ct);
        if (stored is null) return Result<(Stream, string, string)>.Fail("File not found in storage");
        return Result<(Stream, string, string)>.Ok((
            stored.Value.Stream, "application/pdf", $"{detail.Document.Number}.pdf"));
    }

    public async Task<Result<HrBusinessTripRequestDto>> IssueOrderAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var batch = await IssueOrdersAsync(new IssueHrBusinessTripOrderRequest([id]), actorId, ip, ct);
        if (!batch.IsSuccess) return Result<HrBusinessTripRequestDto>.Fail(batch.Error!);

        var detail = await LoadDetailAsync(id, ct);
        if (detail is null) return Result<HrBusinessTripRequestDto>.Fail("Request not found");
        var actor = await GetActorAsync(actorId, ct);
        return Result<HrBusinessTripRequestDto>.Ok(await MapDetailAsync(detail, actor!, ct));
    }

    public async Task<Result<HrBusinessTripOrderResultDto>> IssueOrdersAsync(
        IssueHrBusinessTripOrderRequest request, Guid actorId, string? ip, CancellationToken ct = default)
    {
        if (request.RequestIds.Count == 0)
            return Result<HrBusinessTripOrderResultDto>.Fail("Select at least one memorandum");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<HrBusinessTripOrderResultDto>.Fail("Access denied");
        if (!await IsOrderResponsibleAsync(actor, ct))
            return Result<HrBusinessTripOrderResultDto>.Fail("You are not authorized to issue orders");

        var ids = request.RequestIds.Distinct().ToList();
        var details = new List<HrBusinessTripRequestDetail>();
        foreach (var id in ids)
        {
            var detail = await LoadDetailTrackedAsync(id, ct);
            if (detail is null)
                return Result<HrBusinessTripOrderResultDto>.Fail($"Request not found: {id}");
            if (detail.Phase != HrBusinessTripPhase.OrderPending)
                return Result<HrBusinessTripOrderResultDto>.Fail($"Request {detail.Document.Number} is not pending order issuance");
            if (detail.Document.AssigneeId != actorId)
                return Result<HrBusinessTripOrderResultDto>.Fail($"Request {detail.Document.Number} is not assigned to you");
            details.Add(detail);
        }

        var orgId = details[0].Document.OrganizationId;
        if (details.Any(d => d.Document.OrganizationId != orgId))
            return Result<HrBusinessTripOrderResultDto>.Fail("All memoranda must belong to the same organization");

        var orderNumber = await GenerateOrderNumberAsync(ct);
        var orderDate = DateTime.UtcNow;
        var pdfBytes = HrBusinessTripOrderDocumentGenerator.Generate(
            details, orderNumber, orderDate, BuildBusinessTripVerificationUrl(details[0]));
        await using var pdfStream = new MemoryStream(pdfBytes);
        var storageKey = await files.UploadAsync(
            "hr-business-trip-orders",
            $"{orderNumber}.pdf",
            pdfStream,
            "application/pdf",
            ct);

        var gd = await GetGeneralDirectorAsync(details[0].Document.OrganizationId, ct);
        if (gd is null)
            return Result<HrBusinessTripOrderResultDto>.Fail("General Director is not configured");

        foreach (var detail in details)
        {
            detail.OrderNumber = orderNumber;
            detail.OrderIssuedAt = orderDate;
            detail.OrderDocxStorageKey = storageKey;
            detail.Phase = HrBusinessTripPhase.AwaitingOrderEimzo;
            detail.Document.AssigneeId = gd.Id;
            detail.Document.UpdatedAt = orderDate;
            await AddActivityAsync(detail.Document, actorId, "order_issued", null,
                detail.Document.Status, orderNumber, ct);
        }

        await CreateLinkedTaskAsync(
            gd.Id,
            actorId,
            gd.DepartmentId ?? details[0].Document.DepartmentId,
            details[0].Document.OrganizationId,
            $"Sign business trip order — {orderNumber}",
            $"E-IMZO signature required for order {orderNumber}",
            details[0].Document.Id,
            ct);

        await notifications.NotifyDcsApprovalRequiredAsync(
            gd.Id, orderNumber, $"Business trip order {orderNumber}", details[0].DocumentId, ct);

        await SaveChangesWithTaskNumberRetryAsync(ct);
        await audit.LogAsync(actorId, "HrBusinessTripOrderIssued", "Document",
            details[0].DocumentId, $"{orderNumber}:{string.Join(",", ids)}", ip, ct);

        return Result<HrBusinessTripOrderResultDto>.Ok(new HrBusinessTripOrderResultDto(
            orderNumber, orderDate, ids));
    }

    public async Task<Result<(Stream Stream, string ContentType, string FileName)>> DownloadOrderDocxAsync(
        Guid id, Guid actorId, CancellationToken ct = default)
    {
        var detail = await LoadDetailAsync(id, ct);
        if (detail is null) return Result<(Stream, string, string)>.Fail("Request not found");
        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanView(actor, detail))
            return Result<(Stream, string, string)>.Fail("Access denied");
        if (string.IsNullOrWhiteSpace(detail.OrderDocxStorageKey))
            return Result<(Stream, string, string)>.Fail("Order document is not available yet");

        var fileName = $"order-{detail.OrderNumber ?? detail.Document.Number}.pdf";
        return await OpenStoredFileAsync(
            detail.OrderDocxStorageKey,
            "application/pdf",
            fileName,
            ct);
    }

    public async Task<Result<(Stream Stream, string ContentType, string FileName)>> DownloadOrderPdfPublicAsync(
        Guid id, CancellationToken ct = default)
    {
        var detail = await LoadDetailAsync(id, ct);
        if (detail is null) return Result<(Stream, string, string)>.Fail("Request not found");
        if (!detail.OrderIssuedAt.HasValue)
            return Result<(Stream, string, string)>.Fail("Order document is not available yet");

        // Prefer signed order PDF when present; otherwise the issued (unsigned) order PDF.
        var key = !string.IsNullOrWhiteSpace(detail.PdfSignedStorageKey)
            ? detail.PdfSignedStorageKey!
            : detail.OrderDocxStorageKey;
        if (string.IsNullOrWhiteSpace(key))
            return Result<(Stream, string, string)>.Fail("Order document is not available yet");

        var suffix = !string.IsNullOrWhiteSpace(detail.PdfSignedStorageKey) ? "-signed" : "";
        return await OpenStoredFileAsync(
            key,
            "application/pdf",
            $"order-{detail.OrderNumber ?? detail.Document.Number}{suffix}.pdf",
            ct);
    }

    public async Task<Result<HrBusinessTripRequestDto>> GenerateCertificatesAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<HrBusinessTripRequestDto>.Fail("Request not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<HrBusinessTripRequestDto>.Fail("User not found");
        if (!await CanManageCertificatesAsync(detail, actor, ct))
            return Result<HrBusinessTripRequestDto>.Fail("You are not authorized to generate certificates");
        if (!detail.OrderIssuedAt.HasValue || string.IsNullOrWhiteSpace(detail.OrderNumber))
            return Result<HrBusinessTripRequestDto>.Fail("Order must be issued before certificates");

        foreach (var traveler in detail.Travelers.OrderBy(t => t.SortOrder))
        {
            User? linkedUser = null;
            if (traveler.UserId.HasValue)
            {
                linkedUser = await db.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == traveler.UserId.Value, ct);
            }

            var certNumber = HrBusinessTripCertificateGenerator.BuildCertificateNumber(detail, traveler);
            var bytes = HrBusinessTripCertificateGenerator.Generate(
                detail,
                traveler,
                linkedUser?.PassportSeries,
                linkedUser?.PassportNumber);
            await using var stream = new MemoryStream(bytes);
            var safeName = $"{detail.OrderNumber}-{traveler.SortOrder + 1}.xlsx";
            traveler.CertificateNumber = certNumber;
            traveler.CertificateStorageKey = await files.UploadAsync(
                "hr-business-trip-certificates",
                safeName,
                stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ct);
            traveler.CertificateDeliveredAt = null;
        }

        detail.Document.UpdatedAt = DateTime.UtcNow;
        await AddActivityAsync(detail.Document, actorId, "certificates_generated", null,
            detail.Document.Status, detail.OrderNumber, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "HrBusinessTripCertificatesGenerated", "Document", id, detail.OrderNumber, ip, ct);

        foreach (var traveler in detail.Travelers.Where(t =>
                     t.UserId.HasValue && !string.IsNullOrWhiteSpace(t.CertificateStorageKey)))
        {
            await notifications.NotifyHrBusinessTripCertificateAvailableAsync(
                traveler.UserId!.Value,
                detail.Document.Number,
                detail.OrderNumber ?? detail.Document.Number,
                detail.DocumentId,
                ct);
        }

        return Result<HrBusinessTripRequestDto>.Ok(await MapDetailAsync(detail, actor, ct));
    }

    public async Task<Result<HrBusinessTripRequestDto>> DeliverCertificatesAsync(
        Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var detail = await LoadDetailTrackedAsync(id, ct);
        if (detail is null) return Result<HrBusinessTripRequestDto>.Fail("Request not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null) return Result<HrBusinessTripRequestDto>.Fail("User not found");
        if (!await CanManageCertificatesAsync(detail, actor, ct))
            return Result<HrBusinessTripRequestDto>.Fail("You are not authorized to deliver certificates");
        if (detail.Travelers.Any(t => string.IsNullOrWhiteSpace(t.CertificateStorageKey)))
            return Result<HrBusinessTripRequestDto>.Fail("Generate certificates before delivery");

        var deliveredAt = DateTime.UtcNow;
        foreach (var traveler in detail.Travelers)
            traveler.CertificateDeliveredAt = deliveredAt;

        detail.Phase = HrBusinessTripPhase.Completed;
        detail.Document.AssigneeId = detail.Document.AuthorId;
        detail.Document.UpdatedAt = deliveredAt;
        await AddActivityAsync(detail.Document, actorId, "certificates_delivered", null,
            detail.Document.Status, detail.OrderNumber, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "HrBusinessTripCertificatesDelivered", "Document", id, detail.OrderNumber, ip, ct);
        return Result<HrBusinessTripRequestDto>.Ok(await MapDetailAsync(detail, actor, ct));
    }

    public async Task<Result<(Stream Stream, string ContentType, string FileName)>> DownloadCertificateAsync(
        Guid id, Guid travelerId, Guid actorId, CancellationToken ct = default)
    {
        var detail = await LoadDetailAsync(id, ct);
        if (detail is null) return Result<(Stream, string, string)>.Fail("Request not found");

        var actor = await GetActorAsync(actorId, ct);
        if (actor is null || !CanView(actor, detail))
            return Result<(Stream, string, string)>.Fail("Access denied");

        var traveler = detail.Travelers.FirstOrDefault(t => t.Id == travelerId);
        if (traveler is null) return Result<(Stream, string, string)>.Fail("Traveler not found");
        if (string.IsNullOrWhiteSpace(traveler.CertificateStorageKey))
            return Result<(Stream, string, string)>.Fail("Certificate is not available yet");
        if (IsTravelerOnlyView(actor, detail) && traveler.UserId != actor.Id)
            return Result<(Stream, string, string)>.Fail("Access denied");

        var fileName = $"certificate-{traveler.CertificateNumber ?? (traveler.SortOrder + 1).ToString()}.xlsx";
        return await OpenStoredFileAsync(
            traveler.CertificateStorageKey,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName,
            ct);
    }

    private async Task EnsurePdfAsync(HrBusinessTripRequestDetail detail, CancellationToken ct)
    {
        // Keep signed PDF intact; otherwise always rebuild so template updates apply.
        if (!string.IsNullOrWhiteSpace(detail.PdfSignedStorageKey)
            && !string.IsNullOrWhiteSpace(detail.PdfStorageKey))
            return;

        var userIds = detail.Approvers.Select(a => a.UserId).Distinct().ToList();
        IReadOnlyDictionary<Guid, User> approverUsers = userIds.Count == 0
            ? new Dictionary<Guid, User>()
            : await db.Users.AsNoTracking()
                .Include(u => u.Position)
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, ct);

        var bytes = HrBusinessTripPdfGenerator.Generate(detail, approverUsers);
        await using var stream = new MemoryStream(bytes);
        detail.PdfStorageKey = await files.UploadAsync(
            "hr-business-trip",
            $"{detail.Document.Number}.pdf",
            stream,
            "application/pdf",
            ct);

        var tracked = await db.HrBusinessTripRequestDetails
            .FirstOrDefaultAsync(d => d.DocumentId == detail.DocumentId, ct);
        if (tracked is not null)
            tracked.PdfStorageKey = detail.PdfStorageKey;

        await db.SaveChangesAsync(ct);
    }

    private async Task BuildApproversAsync(HrBusinessTripRequestDetail detail, CancellationToken ct)
    {
        var author = await db.Users.AsNoTracking()
            .Include(u => u.Department)
            .Include(u => u.Organization)
            .FirstAsync(u => u.Id == detail.Document.AuthorId, ct);

        IReadOnlyList<HrBusinessTripWorkflowApproverStep> steps;
        if (author.Organization?.Code == HoMasterData.OrganizationCode)
        {
            var workflowDept = HrBusinessTripWorkflowResolver.ResolveWorkflowDepartmentCode(author.Department?.Code);
            if (workflowDept is not null)
            {
                steps = await workflowService.BuildApprovalChainAsync(
                    author.Id,
                    workflowDept,
                    author.Department?.Code,
                    detail.Document.OrganizationId,
                    ct);
            }
            else
            {
                steps = await BuildLegacyApprovalChainAsync(author, detail.Document.DepartmentId, ct);
            }
        }
        else
        {
            steps = await BuildLegacyApprovalChainAsync(author, detail.Document.DepartmentId, ct);
        }

        var order = 0;
        foreach (var step in steps)
        {
            var approver = new HrBusinessTripApprover
            {
                Id = Guid.NewGuid(),
                DocumentId = detail.DocumentId,
                UserId = step.UserId,
                Role = step.Role,
                Status = HrLeaveApproverStatus.Pending,
                SortOrder = order++,
            };
            detail.Approvers.Add(approver);
            db.HrBusinessTripApprovers.Add(approver);
        }
    }

    private async Task<IReadOnlyList<HrBusinessTripWorkflowApproverStep>> BuildLegacyApprovalChainAsync(
        User author, Guid deptId, CancellationToken ct)
    {
        var chain = new List<HrBusinessTripWorkflowApproverStep>();

        var head = await GetDepartmentHeadAsync(deptId, ct);
        if (head is not null && head.Id != author.Id)
            chain.Add(new HrBusinessTripWorkflowApproverStep(HrBusinessTripApprovalRole.DepartmentHead, head.Id));

        var hrManager = await GetBusinessTripResponsibleAsync(author.OrganizationId, ct);
        if (hrManager is not null
            && chain.All(c => c.UserId != hrManager.Id)
            && hrManager.Id != author.Id)
            chain.Add(new HrBusinessTripWorkflowApproverStep(HrBusinessTripApprovalRole.HrManager, hrManager.Id));

        var fdgd = await GetUserByEmailAsync(HoFirstDeputyGdEmail, ct)
            ?? await GetSupervisingDeputyAsync(author.OrganizationId, ct);
        if (fdgd is not null
            && chain.All(c => c.UserId != fdgd.Id)
            && fdgd.Id != author.Id)
            chain.Add(new HrBusinessTripWorkflowApproverStep(HrBusinessTripApprovalRole.FirstDeputyGeneralDirector, fdgd.Id));

        return chain;
    }

    private static void AddTravelers(
        HrBusinessTripRequestDetail detail, IReadOnlyList<CreateHrBusinessTripTravelerRequest> travelers)
    {
        var order = 0;
        foreach (var t in travelers)
        {
            detail.Travelers.Add(new HrBusinessTripTraveler
            {
                Id = Guid.NewGuid(),
                DocumentId = detail.DocumentId,
                UserId = t.UserId,
                FullNameRu = t.FullNameRu.Trim(),
                FullNameEn = t.FullNameEn?.Trim(),
                PositionRu = t.PositionRu.Trim(),
                PositionEn = t.PositionEn?.Trim(),
                SortOrder = order++,
            });
        }
    }

    private static string? ValidateRequest(UpdateHrBusinessTripRequestRequest request) =>
        ValidateCore(request.PurposeRu, request.PlaceRu, request.DateFrom, request.DateTo, request.Travelers);

    private static string? ValidateRequest(CreateHrBusinessTripRequestRequest request) =>
        ValidateCore(request.PurposeRu, request.PlaceRu, request.DateFrom, request.DateTo, request.Travelers);

    private static string? ValidateCore(
        string purposeRu,
        string placeRu,
        DateTime dateFrom,
        DateTime dateTo,
        IReadOnlyList<CreateHrBusinessTripTravelerRequest> travelers)
    {
        if (string.IsNullOrWhiteSpace(purposeRu)) return "Purpose is required";
        if (string.IsNullOrWhiteSpace(placeRu)) return "Place is required";
        if (travelers.Count == 0) return "Add at least one traveler";
        if (dateTo.Date < dateFrom.Date) return "End date must be on or after start date";
        foreach (var t in travelers)
        {
            if (string.IsNullOrWhiteSpace(t.FullNameRu)) return "Traveler name is required";
            if (string.IsNullOrWhiteSpace(t.PositionRu)) return "Traveler position is required";
        }
        return null;
    }

    private static HrBusinessTripApprover? GetNextPendingApprover(HrBusinessTripRequestDetail detail) =>
        detail.Approvers
            .Where(a => a.Status == HrLeaveApproverStatus.Pending)
            .OrderBy(a => a.SortOrder)
            .FirstOrDefault();

    private static bool CanView(User actor, HrBusinessTripRequestDetail detail) =>
        detail.Document.AuthorId == actor.Id
        || detail.Document.AssigneeId == actor.Id
        || detail.Approvers.Any(a => a.UserId == actor.Id)
        || detail.Travelers.Any(t =>
            t.UserId == actor.Id && !string.IsNullOrWhiteSpace(t.CertificateStorageKey))
        || actor.Role is UserRole.SuperAdmin or UserRole.HOTopManager or UserRole.BMGMCManager;

    private static bool IsTravelerOnlyView(User actor, HrBusinessTripRequestDetail detail) =>
        detail.Document.AuthorId != actor.Id
        && detail.Document.AssigneeId != actor.Id
        && !detail.Approvers.Any(a => a.UserId == actor.Id)
        && actor.Role is not (UserRole.SuperAdmin or UserRole.HOTopManager or UserRole.BMGMCManager)
        && detail.Travelers.Any(t =>
            t.UserId == actor.Id && !string.IsNullOrWhiteSpace(t.CertificateStorageKey));

    private static Guid? GetMyTravelerId(User actor, HrBusinessTripRequestDetail detail) =>
        detail.Travelers.FirstOrDefault(t => t.UserId == actor.Id)?.Id;

    private static bool HasMyCertificate(HrBusinessTripRequestDetail detail, Guid actorId) =>
        detail.Travelers.Any(t =>
            t.UserId == actorId && !string.IsNullOrWhiteSpace(t.CertificateStorageKey));

    private async Task<bool> HasActedAsync(Guid documentId, Guid actorId, CancellationToken ct) =>
        await db.DocumentActivities.AsNoTracking()
            .AnyAsync(a => a.DocumentId == documentId && a.ActorId == actorId, ct);

    private async Task<HrBusinessTripRequestDetail?> LoadDetailAsync(Guid id, CancellationToken ct) =>
        await db.HrBusinessTripRequestDetails.AsNoTracking()
            .Include(d => d.Document).ThenInclude(doc => doc.Author).ThenInclude(a => a.Position)
            .Include(d => d.Document).ThenInclude(doc => doc.Department)
            .Include(d => d.Document).ThenInclude(doc => doc.Organization)
            .Include(d => d.Travelers)
            .Include(d => d.Approvers).ThenInclude(a => a.User).ThenInclude(u => u.Position)
            .Include(d => d.Signatures).ThenInclude(s => s.Signer)
            .FirstOrDefaultAsync(d => d.DocumentId == id, ct);

    private async Task<HrBusinessTripRequestDetail?> LoadDetailTrackedAsync(Guid id, CancellationToken ct) =>
        await db.HrBusinessTripRequestDetails
            .Include(d => d.Document).ThenInclude(doc => doc.Author).ThenInclude(a => a.Position)
            .Include(d => d.Document).ThenInclude(doc => doc.Department)
            .Include(d => d.Document).ThenInclude(doc => doc.Organization)
            .Include(d => d.Travelers)
            .Include(d => d.Approvers).ThenInclude(a => a.User).ThenInclude(u => u.Position)
            .Include(d => d.Signatures).ThenInclude(s => s.Signer)
            .FirstOrDefaultAsync(d => d.DocumentId == id, ct);

    private async Task<User?> GetActorAsync(Guid actorId, CancellationToken ct) =>
        await db.Users.AsNoTracking()
            .Include(u => u.Department)
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == actorId, ct);

    private async Task<User?> GetDepartmentHeadAsync(Guid departmentId, CancellationToken ct) =>
        await db.Users.AsNoTracking()
            .Include(u => u.Position)
            .Where(u => u.IsActive && u.DepartmentId == departmentId
                && (u.Role == UserRole.HONachalnik || u.Role == UserRole.BMGMCNachalnikiOtdeli))
            .OrderBy(u => u.LastName)
            .FirstOrDefaultAsync(ct);

    private async Task<User?> GetDepartmentDeputyAsync(Guid departmentId, CancellationToken ct)
    {
        var users = await db.Users.AsNoTracking()
            .Include(u => u.Position)
            .Where(u => u.IsActive && u.DepartmentId == departmentId
                && (u.Role == UserRole.HONachalnik || u.Role == UserRole.BMGMCNachalnikiOtdeli))
            .OrderBy(u => u.LastName)
            .ToListAsync(ct);
        return users.Count > 1 ? users[1] : null;
    }

    private async Task<User?> GetSupervisingDeputyAsync(Guid organizationId, CancellationToken ct) =>
        await db.Users.AsNoTracking()
            .Include(u => u.Position)
            .Where(u => u.IsActive && u.OrganizationId == organizationId && u.Role == UserRole.HOTopManager)
            .OrderBy(u => u.LastName)
            .Skip(1)
            .FirstOrDefaultAsync(ct);

    private async Task<User?> GetUserByEmailAsync(string email, CancellationToken ct) =>
        await db.Users.AsNoTracking()
            .Include(u => u.Position)
            .FirstOrDefaultAsync(u => u.IsActive && u.Email == email.ToLower(), ct);

    private async Task<User?> GetBusinessTripResponsibleAsync(Guid organizationId, CancellationToken ct)
    {
        var org = await db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == organizationId, ct);
        if (org?.Code == HoMasterData.OrganizationCode
            && !string.IsNullOrWhiteSpace(_hrOptions.HoBusinessTripResponsibleEmail))
        {
            var preferred = await GetUserByEmailAsync(_hrOptions.HoBusinessTripResponsibleEmail, ct);
            if (preferred is not null) return preferred;
        }

        return await db.Users.AsNoTracking()
            .Include(u => u.Department)
            .Where(u => u.IsActive && u.Department != null && u.Department.Code == HrLeaveRouting.HoHrDepartmentCode)
            .OrderBy(u => u.LastName)
            .FirstOrDefaultAsync(ct);
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
        return $"{prefix}{seq:D4}";
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

    private async Task<HrBusinessTripRequestDto> MapDetailAsync(
        HrBusinessTripRequestDetail detail, User actor, CancellationToken ct)
    {
        var activities = await db.DocumentActivities.AsNoTracking()
            .Include(a => a.Actor)
            .Where(a => a.DocumentId == detail.DocumentId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(ct);

        var next = GetNextPendingApprover(detail);
        var canEimzo = RequiresOrderEimzoSignature(detail, actor);
        var canIssueOrder = detail.Phase == HrBusinessTripPhase.OrderPending
            && detail.Document.AssigneeId == actor.Id
            && await IsOrderResponsibleAsync(actor, ct);
        var canManageCertificates = await CanManageCertificatesAsync(detail, actor, ct);
        var hasCertificates = detail.Travelers.Any(t => !string.IsNullOrWhiteSpace(t.CertificateStorageKey));
        var isTravelerView = IsTravelerOnlyView(actor, detail);
        var myTravelerId = GetMyTravelerId(actor, detail);
        var visibleTravelers = isTravelerView && myTravelerId.HasValue
            ? detail.Travelers.Where(t => t.Id == myTravelerId.Value)
            : detail.Travelers;
        var permissions = new HrBusinessTripPermissionsDto(
            CanCreate: !isTravelerView,
            CanEdit: !isTravelerView && detail.Phase == HrBusinessTripPhase.Draft && detail.Document.AuthorId == actor.Id,
            CanSubmit: !isTravelerView && detail.Phase == HrBusinessTripPhase.Draft && detail.Document.AuthorId == actor.Id,
            CanHrReview: !isTravelerView && detail.Phase == HrBusinessTripPhase.HrReview && detail.Document.AssigneeId == actor.Id,
            CanApprove: !isTravelerView && detail.Phase == HrBusinessTripPhase.AwaitingApproval && next?.UserId == actor.Id,
            CanEimzoApprove: !isTravelerView && canEimzo,
            CanIssueOrder: !isTravelerView && canIssueOrder,
            CanGenerateCertificates: !isTravelerView && canManageCertificates,
            CanDeliverCertificates: !isTravelerView && canManageCertificates && hasCertificates,
            CanReject: !isTravelerView && ((detail.Phase == HrBusinessTripPhase.HrReview && detail.Document.AssigneeId == actor.Id)
                || (detail.Phase == HrBusinessTripPhase.AwaitingApproval && next?.UserId == actor.Id)));

        var signatures = detail.Signatures
            .OrderBy(s => s.SignedAt)
            .Select(s => new HrBusinessTripSignatureDto(
                s.Id,
                s.Kind.ToString(),
                s.Signer?.FullName ?? s.SignerCn ?? "",
                s.SignerPinpp,
                s.SignedAt,
                s.CertificateSerial))
            .ToList();

        return new HrBusinessTripRequestDto(
            detail.DocumentId,
            detail.Document.Number,
            detail.Document.Status,
            detail.Phase,
            detail.RequestDate,
            detail.PurposeRu,
            detail.PurposeEn,
            detail.DateFrom,
            detail.DateTo,
            detail.DaysCount,
            detail.PlaceRu,
            detail.PlaceEn,
            detail.Document.Author.FullName,
            detail.Document.Department?.Name ?? "",
            detail.Document.Department?.GetName("en") ?? "",
            detail.Document.Organization?.Name ?? "",
            detail.Document.CreatedAt,
            detail.Document.UpdatedAt,
            detail.OrderNumber,
            detail.OrderIssuedAt,
            HasMemoPdf: !string.IsNullOrWhiteSpace(detail.PdfStorageKey)
                || detail.Phase != HrBusinessTripPhase.Draft,
            HasOrderPdf: !string.IsNullOrWhiteSpace(detail.OrderDocxStorageKey)
                || detail.OrderIssuedAt.HasValue,
            HasOrderSigned: detail.EimzoCompletedAt.HasValue
                || detail.Signatures.Any(s => s.ApproverRole == HrBusinessTripApprovalRole.GeneralDirector),
            HasCertificates: visibleTravelers.Any(t => !string.IsNullOrWhiteSpace(t.CertificateStorageKey)),
            AllCertificatesDelivered: detail.Travelers.Count > 0
                && detail.Travelers.All(t => t.CertificateDeliveredAt.HasValue),
            IsTravelerView: isTravelerView,
            MyTravelerId: myTravelerId,
            visibleTravelers.OrderBy(t => t.SortOrder).Select(MapTraveler).ToList(),
            detail.Approvers.OrderBy(a => a.SortOrder).Select(MapApprover).ToList(),
            activities.Select(a => new HrBusinessTripTimelineEventDto(
                a.Id, a.Action, a.Actor?.FullName ?? "", a.Details, a.CreatedAt)).ToList(),
            signatures,
            permissions);
    }

    private static HrBusinessTripTravelerDto MapTraveler(HrBusinessTripTraveler t) => new(
        t.Id,
        t.FullNameRu,
        t.FullNameEn,
        t.PositionRu,
        t.PositionEn,
        t.SortOrder,
        HrBusinessTripTextBuilder.BuildTravelerLineRu(t.FullNameRu, t.PositionRu),
        HrBusinessTripTextBuilder.BuildTravelerLineEn(t.FullNameEn, t.PositionEn, t.FullNameRu, t.PositionRu),
        t.CertificateNumber,
        !string.IsNullOrWhiteSpace(t.CertificateStorageKey),
        t.CertificateDeliveredAt,
        t.UserId);

    private static HrBusinessTripApproverDto MapApprover(HrBusinessTripApprover a) => new(
        a.Id,
        a.UserId,
        a.User?.FullName ?? "",
        a.User?.GetJobTitle("ru") ?? a.User?.Position?.Name,
        a.User?.GetJobTitle("en") ?? a.User?.Position?.Name,
        a.Role,
        a.Status,
        a.SortOrder,
        a.DecidedAt,
        a.Comment);

    private static HrBusinessTripListItemDto MapListItem(HrBusinessTripRequestDetail d) =>
        MapListItemForViewer(d, null);

    private static HrBusinessTripListItemDto MapListItemForViewer(HrBusinessTripRequestDetail d, Guid? viewerId) => new(
        d.DocumentId,
        d.Document.Number,
        d.Phase,
        d.Document.Department?.Name ?? "",
        d.Document.Department?.GetName("en") ?? "",
        d.RequestDate,
        d.DateFrom,
        d.DateTo,
        d.PlaceRu,
        d.Travelers.Count,
        d.Document.CreatedAt,
        viewerId.HasValue && HasMyCertificate(d, viewerId.Value));

    private async Task<string> GenerateNumberAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"HBT-{year}-";
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

    private async Task<string> GenerateOrderNumberAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"HBO-{year}-";
        var last = await db.HrBusinessTripRequestDetails.AsNoTracking()
            .Where(d => d.OrderNumber != null && d.OrderNumber.StartsWith(prefix))
            .OrderByDescending(d => d.OrderNumber)
            .Select(d => d.OrderNumber!)
            .FirstOrDefaultAsync(ct);
        var seq = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var n))
            seq = n + 1;
        return $"{prefix}{seq:D3}";
    }

    private async Task<User?> GetOrderResponsibleAsync(HrBusinessTripRequestDetail detail, CancellationToken ct)
    {
        if (detail.Document.Organization.Code != HoMasterData.OrganizationCode
            || string.IsNullOrWhiteSpace(_hrOptions.HoOrderResponsibleEmail))
            return null;

        return await GetUserByEmailAsync(_hrOptions.HoOrderResponsibleEmail, ct);
    }

    private Task<bool> IsOrderResponsibleAsync(User actor, CancellationToken ct) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(_hrOptions.HoOrderResponsibleEmail)
            && string.Equals(actor.Email, _hrOptions.HoOrderResponsibleEmail, StringComparison.OrdinalIgnoreCase));

    private async Task<bool> CanManageCertificatesAsync(
        HrBusinessTripRequestDetail detail, User actor, CancellationToken ct) =>
        detail.Phase == HrBusinessTripPhase.CertificatePending
        && detail.Document.AssigneeId == actor.Id
        && await IsOrderResponsibleAsync(actor, ct);

    private async Task TransitionToCertificatePendingAsync(
        HrBusinessTripRequestDetail detail, Guid actorId, CancellationToken ct)
    {
        if (detail.Phase is HrBusinessTripPhase.CertificatePending or HrBusinessTripPhase.Completed)
            return;

        var hr = await GetBusinessTripResponsibleAsync(detail.Document.OrganizationId, ct);
        if (hr is null)
        {
            detail.Phase = HrBusinessTripPhase.Completed;
            detail.Document.AssigneeId = detail.Document.AuthorId;
            detail.Document.UpdatedAt = DateTime.UtcNow;
            return;
        }

        detail.Phase = HrBusinessTripPhase.CertificatePending;
        detail.Document.AssigneeId = hr.Id;
        detail.Document.UpdatedAt = DateTime.UtcNow;

        if (!await HasOpenHrTaskAsync(detail.Document.Id, hr.Id, ct))
        {
            await CreateLinkedTaskAsync(
                hr.Id,
                actorId,
                hr.DepartmentId ?? detail.Document.DepartmentId,
                detail.Document.OrganizationId,
                $"Issue travel certificates — {detail.Document.Number}",
                $"Prepare and deliver business trip certificates for order {detail.OrderNumber}",
                detail.Document.Id,
                ct);
            await notifications.NotifyDcsApprovalRequiredAsync(
                hr.Id, detail.Document.Number, detail.Document.Title, detail.DocumentId, ct);
        }
    }

    private Task<bool> HasOpenHrTaskAsync(Guid documentId, Guid assigneeId, CancellationToken ct) =>
        db.WorkTasks.AsNoTracking().AnyAsync(t =>
            t.ExternalId == documentId
            && t.Source == TaskSource.HR
            && t.AssigneeId == assigneeId
            && t.Status != WorkTaskStatus.Done
            && t.Status != WorkTaskStatus.Cancelled,
            ct);

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

    private async Task<Result<HrBusinessTripRequestDto>?> ProcessEimzoApprovalAsync(
        HrBusinessTripRequestDetail detail,
        HrBusinessTripApprover approver,
        User actor,
        HrBusinessTripApprovalRequest request,
        string? ip,
        CancellationToken ct)
    {
        if (detail.EimzoCompletedAt.HasValue
            && detail.Signatures.Any(s =>
                s.ApproverRole == approver.Role && s.Kind == HrLeaveSignatureKind.JsonDetached))
            return null;

        if (string.IsNullOrWhiteSpace(request.JsonPkcs7) || string.IsNullOrWhiteSpace(request.PdfPkcs7))
            return Result<HrBusinessTripRequestDto>.Fail("E-IMZO signatures for JSON and PDF are required");

        if (string.IsNullOrWhiteSpace(actor.Pinpp))
            return Result<HrBusinessTripRequestDto>.Fail("PINPP is not set in your employee profile");

        await EnsureSigningArtifactsAsync(detail, ct);
        var canonicalJson = HrBusinessTripSigningPayloadBuilder.BuildCanonicalJson(detail);
        var jsonBase64 = HrBusinessTripSigningPayloadBuilder.ToBase64(canonicalJson);
        var clientIp = ip ?? "127.0.0.1";

        var jsonVerify = await eimzo.VerifyDetachedAsync(jsonBase64, request.JsonPkcs7.Trim(), clientIp, ct);
        if (!jsonVerify.IsSuccess)
            return Result<HrBusinessTripRequestDto>.Fail(jsonVerify.Error ?? "JSON signature verification failed");

        var pdfVerify = await eimzo.VerifyAttachedAsync(request.PdfPkcs7.Trim(), clientIp, ct);
        if (!pdfVerify.IsSuccess)
            return Result<HrBusinessTripRequestDto>.Fail(pdfVerify.Error ?? "PDF signature verification failed");

        if (!PinppMatches(actor.Pinpp, jsonVerify.Data!.SignerPinpp)
            || !PinppMatches(actor.Pinpp, pdfVerify.Data!.SignerPinpp))
            return Result<HrBusinessTripRequestDto>.Fail("Certificate PINPP does not match your profile");

        var payloadHash = detail.SigningPayloadHash!;
        var signedAt = DateTime.UtcNow;

        db.HrBusinessTripSignatures.Add(new HrBusinessTripSignature
        {
            Id = Guid.NewGuid(),
            DocumentId = detail.DocumentId,
            SignerUserId = actor.Id,
            Kind = HrLeaveSignatureKind.JsonDetached,
            ApproverRole = approver.Role,
            Pkcs7Base64 = request.JsonPkcs7.Trim(),
            PayloadSha256 = payloadHash,
            CertificateSerial = jsonVerify.Data.CertificateSerial,
            SignerPinpp = jsonVerify.Data.SignerPinpp,
            SignerCn = jsonVerify.Data.SignerFullName,
            SignerTin = jsonVerify.Data.SignerTin,
            SignedAt = signedAt,
        });

        await using var signedPdfStream = new MemoryStream(Convert.FromBase64String(request.PdfPkcs7.Trim()));
        var signedKey = await files.UploadAsync(
            "hr-business-trip",
            $"{detail.Document.Number}_signed.pdf",
            signedPdfStream,
            "application/pkcs7",
            ct);

        db.HrBusinessTripSignatures.Add(new HrBusinessTripSignature
        {
            Id = Guid.NewGuid(),
            DocumentId = detail.DocumentId,
            SignerUserId = actor.Id,
            Kind = HrLeaveSignatureKind.PdfAttached,
            ApproverRole = approver.Role,
            Pkcs7Base64 = request.PdfPkcs7.Trim(),
            PayloadSha256 = payloadHash,
            CertificateSerial = pdfVerify.Data.CertificateSerial,
            SignerPinpp = pdfVerify.Data.SignerPinpp,
            SignerCn = pdfVerify.Data.SignerFullName,
            SignerTin = pdfVerify.Data.SignerTin,
            SignedAt = signedAt,
            StorageKey = signedKey,
        });

        detail.PdfSignedStorageKey = signedKey;
        detail.EimzoCompletedAt = signedAt;

        var stamps = BuildPresentationStamps(detail, pdfVerify.Data, signedAt, ip);
        var presentationBytes = HrBusinessTripPresentationPdfGenerator.Generate(
            detail, stamps, BuildBusinessTripPageUrl(detail));
        await using var presentationStream = new MemoryStream(presentationBytes);
        detail.PdfPresentationStorageKey = await files.UploadAsync(
            "hr-business-trip",
            $"{detail.Document.Number}_presentation.pdf",
            presentationStream,
            "application/pdf",
            ct);

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actor.Id, "HrBusinessTripEimzoSigned", "Document", detail.DocumentId, approver.Role.ToString(), ip, ct);
        return null;
    }

    private async Task<User?> GetGeneralDirectorAsync(Guid organizationId, CancellationToken ct)
    {
        var org = await db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == organizationId, ct);
        if (org?.Code != HoMasterData.OrganizationCode
            || string.IsNullOrWhiteSpace(_hrOptions.HoGeneralDirectorEmail))
            return null;

        return await GetUserByEmailAsync(_hrOptions.HoGeneralDirectorEmail, ct);
    }

    private async Task<List<HrBusinessTripRequestDetail>> LoadOrderBatchTrackedAsync(
        HrBusinessTripRequestDetail detail, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(detail.OrderNumber))
            return [detail];

        var batch = await db.HrBusinessTripRequestDetails
            .Include(d => d.Document).ThenInclude(doc => doc.Author)
            .Include(d => d.Document).ThenInclude(doc => doc.Department)
            .Include(d => d.Document).ThenInclude(doc => doc.Organization)
            .Include(d => d.Travelers)
            .Where(d => d.OrderNumber == detail.OrderNumber
                && d.Phase == HrBusinessTripPhase.AwaitingOrderEimzo)
            .OrderBy(d => d.Document.Number)
            .ToListAsync(ct);

        return batch.Count > 0 ? batch : [detail];
    }

    private bool RequiresOrderEimzoSignature(HrBusinessTripRequestDetail detail, User actor) =>
        detail.Phase == HrBusinessTripPhase.AwaitingOrderEimzo
        && detail.Document.AssigneeId == actor.Id
        && detail.Document.Organization?.Code == HoMasterData.OrganizationCode
        && !string.IsNullOrWhiteSpace(_hrOptions.HoGeneralDirectorEmail)
        && string.Equals(actor.Email, _hrOptions.HoGeneralDirectorEmail, StringComparison.OrdinalIgnoreCase);

    private async Task<Result<HrBusinessTripRequestDto>?> ProcessOrderEimzoAsync(
        IReadOnlyList<HrBusinessTripRequestDetail> batch,
        User actor,
        HrBusinessTripApprovalRequest request,
        string? ip,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.JsonPkcs7) || string.IsNullOrWhiteSpace(request.PdfPkcs7))
            return Result<HrBusinessTripRequestDto>.Fail("E-IMZO signatures for JSON and PDF are required");

        if (string.IsNullOrWhiteSpace(actor.Pinpp))
            return Result<HrBusinessTripRequestDto>.Fail("PINPP is not set in your employee profile");

        var primary = batch[0];
        var orderNumber = primary.OrderNumber!;
        var orderDate = primary.OrderIssuedAt ?? DateTime.UtcNow;
        var canonicalJson = HrBusinessTripOrderSigningPayloadBuilder.BuildCanonicalJson(orderNumber, orderDate, batch);
        var payloadHash = HrBusinessTripOrderSigningPayloadBuilder.ComputeSha256Hex(canonicalJson);
        var jsonBase64 = HrBusinessTripOrderSigningPayloadBuilder.ToBase64(canonicalJson);
        var clientIp = ip ?? "127.0.0.1";

        var jsonVerify = await eimzo.VerifyDetachedAsync(jsonBase64, request.JsonPkcs7.Trim(), clientIp, ct);
        if (!jsonVerify.IsSuccess)
            return Result<HrBusinessTripRequestDto>.Fail(jsonVerify.Error ?? "JSON signature verification failed");

        var pdfVerify = await eimzo.VerifyAttachedAsync(request.PdfPkcs7.Trim(), clientIp, ct);
        if (!pdfVerify.IsSuccess)
            return Result<HrBusinessTripRequestDto>.Fail(pdfVerify.Error ?? "PDF signature verification failed");

        if (!PinppMatches(actor.Pinpp, jsonVerify.Data!.SignerPinpp)
            || !PinppMatches(actor.Pinpp, pdfVerify.Data!.SignerPinpp))
            return Result<HrBusinessTripRequestDto>.Fail("Certificate PINPP does not match your profile");

        var signedAt = DateTime.UtcNow;
        await using var signedPdfStream = new MemoryStream(Convert.FromBase64String(request.PdfPkcs7.Trim()));
        var signedKey = await files.UploadAsync(
            "hr-business-trip-orders",
            $"{orderNumber}_signed.pdf",
            signedPdfStream,
            "application/pkcs7",
            ct);

        foreach (var detail in batch)
        {
            detail.SigningPayloadHash = payloadHash;
            detail.EimzoCompletedAt = signedAt;
            detail.PdfSignedStorageKey = signedKey;

            db.HrBusinessTripSignatures.Add(new HrBusinessTripSignature
            {
                Id = Guid.NewGuid(),
                DocumentId = detail.DocumentId,
                SignerUserId = actor.Id,
                Kind = HrLeaveSignatureKind.JsonDetached,
                ApproverRole = HrBusinessTripApprovalRole.GeneralDirector,
                Pkcs7Base64 = request.JsonPkcs7.Trim(),
                PayloadSha256 = payloadHash,
                CertificateSerial = jsonVerify.Data.CertificateSerial,
                SignerPinpp = jsonVerify.Data.SignerPinpp,
                SignerCn = jsonVerify.Data.SignerFullName,
                SignerTin = jsonVerify.Data.SignerTin,
                SignedAt = signedAt,
            });

            db.HrBusinessTripSignatures.Add(new HrBusinessTripSignature
            {
                Id = Guid.NewGuid(),
                DocumentId = detail.DocumentId,
                SignerUserId = actor.Id,
                Kind = HrLeaveSignatureKind.PdfAttached,
                ApproverRole = HrBusinessTripApprovalRole.GeneralDirector,
                Pkcs7Base64 = request.PdfPkcs7.Trim(),
                PayloadSha256 = payloadHash,
                CertificateSerial = pdfVerify.Data.CertificateSerial,
                SignerPinpp = pdfVerify.Data.SignerPinpp,
                SignerCn = pdfVerify.Data.SignerFullName,
                SignerTin = pdfVerify.Data.SignerTin,
                SignedAt = signedAt,
                StorageKey = signedKey,
            });
        }

        await db.SaveChangesAsync(ct);
        return null;
    }

    private static bool RequiresEimzoSignature(
        HrBusinessTripRequestDetail detail, HrBusinessTripApprover approver, User actor) =>
        approver.Role == HrBusinessTripApprovalRole.GeneralDirector
        && actor.Role == UserRole.HOTopManager
        && detail.Document.Organization.Code == HoMasterData.OrganizationCode;

    private static bool PinppMatches(string? profilePinpp, string? certPinpp) =>
        !string.IsNullOrWhiteSpace(profilePinpp)
        && !string.IsNullOrWhiteSpace(certPinpp)
        && string.Equals(profilePinpp.Trim(), certPinpp.Trim(), StringComparison.Ordinal);

    private async Task EnsureSigningArtifactsAsync(HrBusinessTripRequestDetail detail, CancellationToken ct)
    {
        var canonicalJson = HrBusinessTripSigningPayloadBuilder.BuildCanonicalJson(detail);
        detail.SigningPayloadHash = HrBusinessTripSigningPayloadBuilder.ComputeSha256Hex(canonicalJson);

        if (!string.IsNullOrWhiteSpace(detail.PdfStorageKey)) return;

        var pdfBytes = HrBusinessTripPdfGenerator.Generate(detail);
        await using var stream = new MemoryStream(pdfBytes);
        detail.PdfStorageKey = await files.UploadAsync(
            "hr-business-trip",
            $"{detail.Document.Number}.pdf",
            stream,
            "application/pdf",
            ct);
    }

    private async Task<byte[]> GetPdfBytesAsync(HrBusinessTripRequestDetail detail, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(detail.PdfStorageKey))
        {
            var stored = await files.DownloadAsync(detail.PdfStorageKey, ct);
            if (stored is not null)
            {
                await using var s = stored.Value.Stream;
                using var ms = new MemoryStream();
                await s.CopyToAsync(ms, ct);
                return ms.ToArray();
            }
        }

        return HrBusinessTripPdfGenerator.Generate(detail);
    }

    private string BuildBusinessTripVerificationUrl(HrBusinessTripRequestDetail detail) =>
        $"{_hrOptions.PublicAppBaseUrl.TrimEnd('/')}/api/hr/business-trips/{detail.DocumentId}/public/order-pdf";

    private string BuildBusinessTripPageUrl(HrBusinessTripRequestDetail detail) =>
        $"{_hrOptions.PublicAppBaseUrl.TrimEnd('/')}/ru/hr/business-trip/{detail.DocumentId}";

    private static List<HrLeavePdfStamp> BuildPresentationStamps(
        HrBusinessTripRequestDetail detail,
        EimzoVerifyResultDto eimzoVerify,
        DateTime signedAtUtc,
        string? ip)
    {
        var number = detail.Document.Number;
        var stamps = new List<HrLeavePdfStamp>
        {
            new(
                "ЮБОРИЛГАН",
                HrLeaveStampStyle.Sent,
                number,
                detail.Document.CreatedAt,
                detail.Document.Author.FullName,
                null,
                "ATG Platform",
                null),
        };

        var hrActivity = detail.Document.UpdatedAt;
        stamps.Add(new HrLeavePdfStamp(
            "TEKSHIRILGAN",
            HrLeaveStampStyle.Reviewed,
            number,
            hrActivity,
            "HR Department",
            null,
            "ATG Platform / HR",
            null));

        var eimzoSignedAt = ParseEimzoSigningTime(eimzoVerify.SigningTime) ?? signedAtUtc;
        stamps.Add(new HrLeavePdfStamp(
            "ТАСДИКЛАНГАН",
            HrLeaveStampStyle.Approved,
            number,
            eimzoSignedAt,
            eimzoVerify.SignerFullName ?? "",
            eimzoVerify.SignerPinpp,
            "ATG Platform / E-IMZO",
            ip));

        return stamps;
    }

    private static DateTime? ParseEimzoSigningTime(string? signingTime)
    {
        if (string.IsNullOrWhiteSpace(signingTime)) return null;
        return DateTime.TryParse(signingTime, out var parsed) ? parsed : null;
    }

    private static string? GetSignedPdfStorageKey(HrBusinessTripRequestDetail detail) =>
        detail.Signatures.FirstOrDefault(s => s.Kind == HrLeaveSignatureKind.PdfAttached)?.StorageKey
        ?? detail.PdfSignedStorageKey;

    private async Task<Result<(Stream Stream, string ContentType, string FileName)>> OpenStoredFileAsync(
        string key, string contentType, string fileName, CancellationToken ct)
    {
        var stored = await files.DownloadAsync(key, ct);
        if (stored is null)
            return Result<(Stream, string, string)>.Fail("File not found in storage");

        return Result<(Stream, string, string)>.Ok((stored.Value.Stream, contentType, fileName));
    }

    private async Task<Result<(Stream Stream, string ContentType, string FileName)>> ExtractPdfFromSignedPkcs7Async(
        string pkcs7StorageKey, string documentNumber, string? clientIp, CancellationToken ct)
    {
        var stored = await files.DownloadAsync(pkcs7StorageKey, ct);
        if (stored is null)
            return Result<(Stream, string, string)>.Fail("File not found in storage");

        await using var pkcs7Stream = stored.Value.Stream;
        using var buffer = new MemoryStream();
        await pkcs7Stream.CopyToAsync(buffer, ct);
        var pkcs7Base64 = Convert.ToBase64String(buffer.ToArray());

        var verify = await eimzo.VerifyAttachedAsync(pkcs7Base64, clientIp ?? "127.0.0.1", ct);
        if (!verify.IsSuccess)
            return Result<(Stream, string, string)>.Fail(verify.Error ?? "Signature verification failed");

        if (string.IsNullOrWhiteSpace(verify.Data?.DocumentBase64))
            return Result<(Stream, string, string)>.Fail("PDF could not be extracted from the signature file");

        byte[] pdfBytes;
        try
        {
            pdfBytes = Convert.FromBase64String(verify.Data.DocumentBase64.Trim());
        }
        catch
        {
            return Result<(Stream, string, string)>.Fail("Invalid PDF data in signature file");
        }

        return Result<(Stream, string, string)>.Ok((
            new MemoryStream(pdfBytes),
            "application/pdf",
            $"{documentNumber}_signed.pdf"));
    }
}
