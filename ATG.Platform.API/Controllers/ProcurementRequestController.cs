using System.Security.Claims;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATG.Platform.API.Controllers;

[ApiController]
[Route("api/dcs/procurement-requests")]
[Authorize]
public class ProcurementRequestController(IProcurementRequestService requests, IFileStorageService files) : ControllerBase
{
    [HttpGet("steps")]
    public IActionResult GetSteps() => Ok(requests.GetSteps());

    [HttpGet("create-options")]
    public async Task<IActionResult> GetCreateOptions(CancellationToken ct)
    {
        var result = await requests.GetCreateOptionsAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("initiator-departments")]
    public async Task<IActionResult> GetInitiatorDepartments(CancellationToken ct)
    {
        var result = await requests.GetInitiatorDepartmentsAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("initiators")]
    public async Task<IActionResult> GetInitiators([FromQuery] Guid departmentId, CancellationToken ct)
    {
        var result = await requests.GetInitiatorsAsync(GetUserId()!.Value, departmentId, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("responsible-users")]
    public async Task<IActionResult> GetResponsibleUsers(CancellationToken ct)
    {
        var result = await requests.GetResponsibleUsersAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await requests.GetByIdAsync(id, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    [HttpPost("tas")]
    public async Task<IActionResult> CreateTas([FromBody] CreateTasProcurementRequest request, CancellationToken ct)
    {
        var result = await requests.CreateTasAsync(request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("express")]
    public async Task<IActionResult> CreateExpress([FromBody] CreateExpressProcurementRequest request, CancellationToken ct)
    {
        var result = await requests.CreateExpressAsync(request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/steps/{step:int}/complete")]
    public async Task<IActionResult> CompleteStep(Guid id, int step, CancellationToken ct)
    {
        var result = await requests.CompleteStepAsync(id, step, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/step9/submit")]
    public async Task<IActionResult> SubmitStep9(Guid id, [FromBody] SubmitStep9Request request, CancellationToken ct)
    {
        var result = await requests.SubmitStep9Async(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ProcurementApprovalRequest request, CancellationToken ct)
    {
        var result = await requests.ApproveAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ProcurementApprovalRequest request, CancellationToken ct)
    {
        var result = await requests.RejectAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/forward-contracts")]
    public async Task<IActionResult> ForwardToContracts(Guid id, CancellationToken ct)
    {
        var result = await requests.ForwardToContractsAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("marketing/steps")]
    public IActionResult GetMarketingSteps() => Ok(requests.GetMarketingSteps());

    [HttpGet("marketing/queue")]
    public async Task<IActionResult> GetMarketingQueue(CancellationToken ct)
    {
        var result = await requests.GetMarketingQueueAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("marketing/workers")]
    public async Task<IActionResult> GetMarketingWorkers(CancellationToken ct)
    {
        var result = await requests.GetMarketingWorkersAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/marketing/accept")]
    public async Task<IActionResult> AcceptMarketing(Guid id, CancellationToken ct)
    {
        var result = await requests.AcceptMarketingAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/marketing/assign")]
    public async Task<IActionResult> AssignMarketingSpecialist(Guid id, [FromBody] AssignMarketingSpecialistRequest request, CancellationToken ct)
    {
        var result = await requests.AssignMarketingSpecialistAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/marketing/complete")]
    public async Task<IActionResult> CompleteMarketing(Guid id, [FromBody] MarketingActionRequest request, CancellationToken ct)
    {
        var result = await requests.CompleteMarketingAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/marketing/steps/{step:int}/complete")]
    public async Task<IActionResult> CompleteMarketingStep(Guid id, int step, [FromBody] CompleteMarketingStepRequest request, CancellationToken ct)
    {
        var result = await requests.CompleteMarketingStepAsync(id, step, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/marketing/branch")]
    public async Task<IActionResult> RecordMarketingBranch(Guid id, [FromBody] MarketingBranchRequest request, CancellationToken ct)
    {
        var result = await requests.RecordMarketingBranchAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("files/upload")]
    [RequestSizeLimit(52_428_800)]
    public async Task<IActionResult> UploadFile(IFormFile file, [FromQuery] string folder = "documents", CancellationToken ct = default)
    {
        if (file.Length == 0) return BadRequest(new { error = "Empty file" });
        await using var stream = file.OpenReadStream();
        var key = await files.UploadAsync(folder, file.FileName, stream, file.ContentType, ct);
        return Ok(new FileUploadResultDto(key, files.GetPublicUrl(key)));
    }

    [HttpGet("files/{*key}")]
    public async Task<IActionResult> DownloadFile(string key, CancellationToken ct)
    {
        var result = await files.DownloadAsync(key, ct);
        if (result is null) return NotFound();
        return File(result.Value.Stream, result.Value.ContentType, Path.GetFileName(key));
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim is not null && Guid.TryParse(claim, out var id) ? id : null;
    }

    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
