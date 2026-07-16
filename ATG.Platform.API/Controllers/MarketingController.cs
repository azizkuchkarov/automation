using System.Security.Claims;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATG.Platform.API.Controllers;

[ApiController]
[Route("api/marketing")]
[Authorize]
public class MarketingController(IMarketingService marketing, IFileStorageService files) : ControllerBase
{
    [HttpGet("records")]
    public async Task<IActionResult> GetRecords(
        [FromQuery] MarketingRecordStatus? status,
        [FromQuery] Guid? executorId,
        [FromQuery] MarketingRequestCategory? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await marketing.GetRecordsAsync(GetUserId()!.Value, status, executorId, category, page, pageSize, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("records/by-document/{documentId:guid}")]
    public async Task<IActionResult> GetByDocument(Guid documentId, CancellationToken ct)
    {
        var result = await marketing.GetByDocumentIdAsync(documentId, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    [HttpPut("records/by-document/{documentId:guid}/category")]
    public async Task<IActionResult> SetCategory(Guid documentId, [FromBody] SetMarketingCategoryRequest request, CancellationToken ct)
    {
        var result = await marketing.SetCategoryAsync(documentId, request, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("records/by-document/{documentId:guid}/assign-executor")]
    public async Task<IActionResult> AssignExecutor(Guid documentId, [FromBody] AssignMarketingExecutorRequest request, CancellationToken ct)
    {
        var result = await marketing.AssignExecutorAsync(documentId, request, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("records/by-document/{documentId:guid}/accept")]
    public async Task<IActionResult> Accept(Guid documentId, CancellationToken ct)
    {
        var result = await marketing.AcceptAsync(documentId, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("records/by-document/{documentId:guid}/tz-issue")]
    public async Task<IActionResult> ReportTzIssue(Guid documentId, [FromBody] MarketingTzIssueRequest request, CancellationToken ct)
    {
        var result = await marketing.ReportTzIssueAsync(documentId, request, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("records/by-document/{documentId:guid}/tz-issue/resolve")]
    public async Task<IActionResult> ResolveTzIssue(Guid documentId, CancellationToken ct)
    {
        var result = await marketing.ResolveTzIssueAsync(documentId, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("records/by-document/{documentId:guid}/rfq/prepare")]
    public async Task<IActionResult> MarkRfqPrepared(Guid documentId, CancellationToken ct)
    {
        var result = await marketing.MarkRfqPreparedAsync(documentId, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("records/by-document/{documentId:guid}/rfq/document")]
    public async Task<IActionResult> UploadRfqDocument(Guid documentId, [FromBody] UploadRfqDocumentRequest request, CancellationToken ct)
    {
        var result = await marketing.UploadRfqDocumentAsync(documentId, request, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("records/by-document/{documentId:guid}/rfq/register-generate")]
    public async Task<IActionResult> RegisterAndGenerateRfq(Guid documentId, [FromBody] RegisterRfqStep3Request request, CancellationToken ct)
    {
        var result = await marketing.RegisterAndGenerateRfqAsync(documentId, request, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("records/by-document/{documentId:guid}/rfq/channels/atg-website")]
    public async Task<IActionResult> OpenAtgWebsiteChannel(Guid documentId, CancellationToken ct)
    {
        var result = await marketing.OpenRfqAtgWebsiteChannelAsync(documentId, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("records/by-document/{documentId:guid}/rfq/channels/tenderweek")]
    public async Task<IActionResult> OpenTenderChannel(Guid documentId, CancellationToken ct)
    {
        var result = await marketing.OpenRfqTenderChannelAsync(documentId, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("records/by-document/{documentId:guid}/rfq/channels/tenderweek/complete")]
    public async Task<IActionResult> CompleteTenderChannel(Guid documentId, CancellationToken ct)
    {
        var result = await marketing.CompleteRfqTenderChannelAsync(documentId, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("records/by-document/{documentId:guid}/rfq/dispatch")]
    public async Task<IActionResult> AddRfqDispatch(Guid documentId, [FromBody] AddRfqDispatchRequest request, CancellationToken ct)
    {
        var result = await marketing.AddRfqDispatchAsync(documentId, request, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("rfq/dispatches/{dispatchId:guid}/followup")]
    public async Task<IActionResult> MarkFollowup(Guid dispatchId, [FromBody] MarkRfqFollowupRequest request, CancellationToken ct)
    {
        var result = await marketing.MarkFollowupSentAsync(dispatchId, request, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("records/by-document/{documentId:guid}/offers")]
    public async Task<IActionResult> AddOffer(Guid documentId, [FromBody] AddMarketingOfferRequest request, CancellationToken ct)
    {
        var result = await marketing.AddOfferAsync(documentId, request, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("offers/{offerId:guid}/initiator-review")]
    public async Task<IActionResult> ReviewOfferByInitiator(Guid offerId, [FromBody] ReviewMarketingOfferRequest request, CancellationToken ct)
    {
        var result = await marketing.ReviewOfferByInitiatorAsync(offerId, request, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("offers/{offerId:guid}/engineer-review")]
    public async Task<IActionResult> ReviewOfferByEngineer(Guid offerId, [FromBody] ReviewMarketingOfferRequest request, CancellationToken ct)
    {
        var result = await marketing.ReviewOfferByEngineerAsync(offerId, request, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPut("offers/{offerId:guid}/compliance")]
    public async Task<IActionResult> UpdateOfferCompliance(Guid offerId, [FromBody] UpdateOfferComplianceRequest request, CancellationToken ct)
    {
        var result = await marketing.UpdateOfferComplianceAsync(offerId, request, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPut("offers/{offerId:guid}/affiliation")]
    public async Task<IActionResult> UpdateOfferAffiliation(Guid offerId, [FromBody] UpdateOfferAffiliationRequest request, CancellationToken ct)
    {
        var result = await marketing.UpdateOfferAffiliationAsync(offerId, request, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("records/by-document/{documentId:guid}/plan/register-generate")]
    public async Task<IActionResult> RegisterAndGeneratePlan(Guid documentId, [FromBody] RegisterMarketingPlanRequest request, CancellationToken ct)
    {
        var result = await marketing.RegisterAndGeneratePlanAsync(documentId, request, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("records/by-document/{documentId:guid}/plan/download-template")]
    public async Task<IActionResult> DownloadPlanTemplate(Guid documentId, CancellationToken ct)
    {
        var result = await marketing.DownloadPlanTemplateAsync(documentId, GetUserId()!.Value, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return File(result.Data!.Bytes,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            result.Data!.FileName);
    }

    [HttpPost("records/by-document/{documentId:guid}/plan/document")]
    public async Task<IActionResult> UploadPlanDocument(Guid documentId, [FromBody] UploadMarketingPlanDocumentRequest request, CancellationToken ct)
    {
        var result = await marketing.UploadPlanDocumentAsync(documentId, request, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("records/by-document/{documentId:guid}/plans")]
    public async Task<IActionResult> CreatePlan(Guid documentId, [FromBody] CreateMarketingPlanRequest request, CancellationToken ct)
    {
        var result = await marketing.CreateProcurementPlanAsync(documentId, request, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("plans/{planId:guid}/submit-to-mgmt")]
    public async Task<IActionResult> SubmitPlanToMgmt(Guid planId, CancellationToken ct)
    {
        var result = await marketing.SubmitPlanToManagementAsync(planId, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("plans/{planId:guid}/mgmt-reject")]
    public async Task<IActionResult> RejectPlan(Guid planId, [FromBody] MarketingCancelRequest request, CancellationToken ct)
    {
        var result = await marketing.RejectPlanByManagementAsync(planId, request.Reason, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("records/by-document/{documentId:guid}/portal/submit")]
    public async Task<IActionResult> SubmitToPortal(Guid documentId, [FromBody] SubmitPortalRequest request, CancellationToken ct)
    {
        var result = await marketing.SubmitToPortalAsync(documentId, request, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("portal/{portalApprovalId:guid}/complete")]
    public async Task<IActionResult> CompletePortal(Guid portalApprovalId, [FromBody] CompletePortalApprovalRequest request, CancellationToken ct)
    {
        var result = await marketing.CompletePortalApprovalAsync(portalApprovalId, request, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("records/by-document/{documentId:guid}/complete")]
    public async Task<IActionResult> CompleteToContract(Guid documentId, CancellationToken ct)
    {
        var result = await marketing.CompleteToContractAsync(documentId, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("records/by-document/{documentId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid documentId, [FromBody] MarketingCancelRequest request, CancellationToken ct)
    {
        var result = await marketing.CancelAsync(documentId, request, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("board")]
    public async Task<IActionResult> GetBoard(CancellationToken ct)
    {
        var result = await marketing.GetBoardAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var result = await marketing.GetStatsAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("leadership")]
    public async Task<IActionResult> GetLeadership(CancellationToken ct)
    {
        var result = await marketing.GetLeadershipOverviewAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("files/upload")]
    [RequestSizeLimit(52_428_800)]
    public async Task<IActionResult> UploadFile(IFormFile file, [FromQuery] string folder = "marketing", CancellationToken ct = default)
    {
        if (file.Length == 0) return BadRequest(new { error = "Empty file" });
        await using var stream = file.OpenReadStream();
        var key = await files.UploadAsync(folder, file.FileName, stream, file.ContentType, ct);
        return Ok(new FileUploadResultDto(key, files.GetPublicUrl(key)));
    }

    [HttpGet("files/{*key}")]
    public async Task<IActionResult> DownloadFile(string key, [FromQuery] string? fileName, CancellationToken ct)
    {
        var result = await files.DownloadAsync(key, ct);
        if (result is null) return NotFound();
        var downloadName = ATG.Platform.Infrastructure.Storage.FileDownloadNames.Resolve(key, fileName);
        return File(result.Value.Stream, result.Value.ContentType, downloadName);
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim is not null && Guid.TryParse(claim, out var id) ? id : null;
    }
}
