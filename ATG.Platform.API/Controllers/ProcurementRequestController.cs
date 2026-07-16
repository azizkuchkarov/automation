using System.Security.Claims;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Domain.Enums;
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
    public async Task<IActionResult> CompleteStep(Guid id, int step, [FromBody] CompleteProcurementStepRequest? request, CancellationToken ct)
    {
        var result = await requests.CompleteStepAsync(id, step, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/step9/submit")]
    public async Task<IActionResult> SubmitStep9(Guid id, [FromBody] SubmitStep9Request request, CancellationToken ct)
    {
        var result = await requests.SubmitStep9Async(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/step6/submit")]
    public async Task<IActionResult> SubmitStep6(Guid id, [FromBody] SubmitStep9Request request, CancellationToken ct)
    {
        var result = await requests.SubmitStep9Async(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/tas/reject")]
    public async Task<IActionResult> RejectTas(Guid id, [FromBody] CompleteProcurementStepRequest request, CancellationToken ct)
    {
        var result = await requests.RejectTasAsync(id, request, GetUserId()!.Value, GetIp(), ct);
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

    [HttpGet("contracts/workers")]
    public async Task<IActionResult> GetContractsWorkers(CancellationToken ct)
    {
        var result = await requests.GetContractsWorkersAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/contracts/route-section")]
    public async Task<IActionResult> RouteContractsSection(
        Guid id, [FromBody] RouteContractsSectionRequest request, CancellationToken ct)
    {
        var result = await requests.RouteContractsSectionAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/contracts/accept")]
    public async Task<IActionResult> AcceptContracts(Guid id, [FromBody] AcceptContractsRequest request, CancellationToken ct)
    {
        var result = await requests.AcceptContractsAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/contracts/int/select-variant")]
    public async Task<IActionResult> SelectContractsIntVariant(
        Guid id, [FromBody] SelectContractsIntVariantRequest request, CancellationToken ct)
    {
        var result = await requests.SelectContractsIntVariantAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/contracts/int/steps/{step:int}/complete")]
    public async Task<IActionResult> CompleteContractsIntStep(
        Guid id, int step, [FromBody] CompleteContractsIntStepRequest request, CancellationToken ct)
    {
        var result = await requests.CompleteContractsIntStepAsync(id, step, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/contracts/int/steps/{step:int}/files")]
    public async Task<IActionResult> AddContractsIntStepFile(
        Guid id, int step, [FromBody] ContractsIntStepFileInput request, CancellationToken ct)
    {
        var result = await requests.AddContractsIntStepFileAsync(id, step, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/contracts/int/steps/{step:int}/approvers")]
    public async Task<IActionResult> SubmitContractsIntStepApprovers(
        Guid id, int step, [FromBody] SubmitContractsIntStepApproversRequest request, CancellationToken ct)
    {
        var result = await requests.SubmitContractsIntStepApproversAsync(id, step, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/contracts/int/steps/{step:int}/approvers/decide")]
    public async Task<IActionResult> DecideContractsIntStepApproval(
        Guid id, int step, [FromBody] DecideContractsIntStepApprovalRequest request, CancellationToken ct)
    {
        var result = await requests.DecideContractsIntStepApprovalAsync(id, step, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/contracts/int/steps/{step:int}/send-secretariat")]
    public async Task<IActionResult> SendContractsIntToSecretariat(
        Guid id, int step, [FromBody] SendContractsIntToSecretariatRequest request, CancellationToken ct)
    {
        var result = await requests.SendContractsIntToSecretariatAsync(id, step, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/contracts/dom/select-variant")]
    public async Task<IActionResult> SelectContractsDomVariant(
        Guid id, [FromBody] SelectContractsDomVariantRequest request, CancellationToken ct)
    {
        var result = await requests.SelectContractsDomVariantAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/contracts/dom/steps/{step:int}/complete")]
    public async Task<IActionResult> CompleteContractsDomStep(
        Guid id, int step, [FromBody] CompleteContractsDomStepRequest request, CancellationToken ct)
    {
        var result = await requests.CompleteContractsDomStepAsync(id, step, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/contracts/dom/steps/{step:int}/schedule")]
    public async Task<IActionResult> ScheduleContractsDomStep(
        Guid id, int step, [FromBody] ScheduleContractsDomStepRequest request, CancellationToken ct)
    {
        var result = await requests.ScheduleContractsDomStepAsync(id, step, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/contracts/dom/steps/{step:int}/files")]
    public async Task<IActionResult> AddContractsDomStepFile(
        Guid id, int step, [FromBody] ContractsDomStepFileInput request, CancellationToken ct)
    {
        var result = await requests.AddContractsDomStepFileAsync(id, step, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/contracts/dom/steps/{step:int}/approvers")]
    public async Task<IActionResult> SubmitContractsDomStepApprovers(
        Guid id, int step, [FromBody] SubmitContractsDomStepApproversRequest request, CancellationToken ct)
    {
        var result = await requests.SubmitContractsDomStepApproversAsync(id, step, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/contracts/dom/steps/{step:int}/approvers/decide")]
    public async Task<IActionResult> DecideContractsDomStepApproval(
        Guid id, int step, [FromBody] DecideContractsDomStepApprovalRequest request, CancellationToken ct)
    {
        var result = await requests.DecideContractsDomStepApprovalAsync(id, step, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/contracts/dom/steps/{step:int}/send-contracts-admin")]
    public async Task<IActionResult> SendContractsDomToContractsAdmin(
        Guid id, int step, [FromBody] SendContractsDomToContractsAdminRequest request, CancellationToken ct)
    {
        var result = await requests.SendContractsDomToContractsAdminAsync(id, step, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/contracts/dom/steps/{step:int}/return-marketing")]
    public async Task<IActionResult> ReturnContractsDomToMarketing(
        Guid id, int step, [FromBody] ReturnContractsDomToMarketingRequest request, CancellationToken ct)
    {
        var result = await requests.ReturnContractsDomToMarketingAsync(id, step, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/contracts/dom/steps/{step:int}/rollback")]
    public async Task<IActionResult> RollbackContractsDomStep(
        Guid id, int step, [FromBody] RollbackContractsDomStepRequest request, CancellationToken ct)
    {
        var result = await requests.RollbackContractsDomStepAsync(id, step, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("payment/workers")]
    public async Task<IActionResult> GetPaymentWorkers(CancellationToken ct)
    {
        var result = await requests.GetPaymentWorkersAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/payment/assign")]
    public async Task<IActionResult> AssignPaymentSpecialist(
        Guid id, [FromBody] AssignContractsSpecialistRequest request, CancellationToken ct)
    {
        var result = await requests.AssignPaymentSpecialistAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/payment/accept")]
    public async Task<IActionResult> AcceptPayment(Guid id, [FromBody] AcceptContractsRequest request, CancellationToken ct)
    {
        var result = await requests.AcceptPaymentAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/contracts/assign")]
    public async Task<IActionResult> AssignContractsSpecialist(Guid id, [FromBody] AssignContractsSpecialistRequest request, CancellationToken ct)
    {
        var result = await requests.AssignContractsSpecialistAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("marketing/steps")]
    public IActionResult GetMarketingSteps() => Ok(requests.GetMarketingSteps());

    [HttpGet("marketing/queue")]
    public async Task<IActionResult> GetMarketingQueue(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] ProcurementMarketingSubPhase? subPhase = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await requests.GetMarketingQueueAsync(GetUserId()!.Value, page, pageSize, subPhase, search, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("marketing/queue/summary")]
    public async Task<IActionResult> GetMarketingQueueSummary(CancellationToken ct)
    {
        var result = await requests.GetMarketingQueueSummaryAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("contracts/queue")]
    public async Task<IActionResult> GetContractsQueue(
        [FromQuery] ContractsProcurementSectionType? section = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await requests.GetContractsQueueAsync(
            GetUserId()!.Value, section, page, pageSize, search, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("contracts/board")]
    public async Task<IActionResult> GetContractsBoard(
        [FromQuery] ContractsProcurementSectionType section,
        CancellationToken ct = default)
    {
        var result = await requests.GetContractsBoardAsync(GetUserId()!.Value, section, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("marketing/workers")]
    public async Task<IActionResult> GetMarketingWorkers(CancellationToken ct)
    {
        var result = await requests.GetMarketingWorkersAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/marketing/accept")]
    public async Task<IActionResult> AcceptMarketing(Guid id, [FromBody] AcceptMarketingRequest request, CancellationToken ct)
    {
        var result = await requests.AcceptMarketingAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/marketing/assign")]
    public async Task<IActionResult> AssignMarketingSpecialist(Guid id, [FromBody] AssignMarketingSpecialistRequest request, CancellationToken ct)
    {
        var result = await requests.AssignMarketingSpecialistAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/marketing/return-to-initiator")]
    public async Task<IActionResult> ReturnMarketingToInitiator(Guid id, [FromBody] ReturnMarketingToInitiatorRequest request, CancellationToken ct)
    {
        var result = await requests.ReturnMarketingToInitiatorAsync(id, request, GetUserId()!.Value, GetIp(), ct);
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

    [HttpGet("marketing/plan-approver-users")]
    public async Task<IActionResult> GetMarketingPlanApproverUsers([FromQuery] string? search, CancellationToken ct)
    {
        var result = await requests.GetMarketingPlanApproverUsersAsync(GetUserId()!.Value, search, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/marketing/plan/submit")]
    public async Task<IActionResult> SubmitMarketingPlanApproval(Guid id, [FromBody] SubmitMarketingPlanApprovalRequest request, CancellationToken ct)
    {
        var result = await requests.SubmitMarketingPlanApprovalAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/marketing/plan/approve")]
    public async Task<IActionResult> ApproveMarketingPlan(Guid id, [FromBody] ProcurementApprovalRequest request, CancellationToken ct)
    {
        var result = await requests.ApproveMarketingPlanAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/marketing/plan/reject")]
    public async Task<IActionResult> RejectMarketingPlan(Guid id, [FromBody] ProcurementApprovalRequest request, CancellationToken ct)
    {
        var result = await requests.RejectMarketingPlanAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/marketing/register")]
    public async Task<IActionResult> ConfirmMarketingRegistration(Guid id, [FromBody] ConfirmMarketingRegistrationRequest request, CancellationToken ct)
    {
        var result = await requests.ConfirmMarketingRegistrationAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddStepComment(Guid id, [FromBody] AddProcurementStepCommentRequest request, CancellationToken ct)
    {
        var result = await requests.AddStepCommentAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("admin/workflow-roles")]
    [Authorize(Roles = "SuperAdmin,HOTopManager")]
    public async Task<IActionResult> GetWorkflowRolesAdmin(CancellationToken ct)
    {
        var result = await requests.GetWorkflowRolesAdminAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPut("admin/workflow-roles/{roleKey}")]
    [Authorize(Roles = "SuperAdmin,HOTopManager")]
    public async Task<IActionResult> UpdateWorkflowRole(
        string roleKey, [FromBody] UpdateProcurementWorkflowRoleRequest request, CancellationToken ct)
    {
        var result = await requests.UpdateWorkflowRoleAsync(GetUserId()!.Value, roleKey, request, GetIp(), ct);
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

    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
