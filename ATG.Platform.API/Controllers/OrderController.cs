using System.Security.Claims;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATG.Platform.API.Controllers;

[ApiController]
[Route("api/dcs/orders")]
[Authorize]
public class OrderController(IOrderService orders) : ControllerBase
{
    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissions([FromQuery] Guid? documentId, CancellationToken ct)
    {
        var result = await orders.GetPermissionsAsync(GetUserId()!.Value, documentId, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("dept-heads")]
    public async Task<IActionResult> GetDeptHeads(CancellationToken ct)
    {
        var result = await orders.GetDeptHeadsAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("top-managers")]
    public async Task<IActionResult> GetTopManagers(CancellationToken ct)
    {
        var result = await orders.GetTopManagersAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("coordinators")]
    public async Task<IActionResult> GetCoordinators(CancellationToken ct)
    {
        var result = await orders.GetCoordinatorsAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:guid}/distribution-targets")]
    public async Task<IActionResult> GetDistributionTargets(Guid id, CancellationToken ct)
    {
        var result = await orders.GetDistributionTargetsAsync(id, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await orders.GetByIdAsync(id, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var result = await orders.CreateAsync(request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateDraft(Guid id, [FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var result = await orders.UpdateDraftAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/submit-for-approval")]
    public async Task<IActionResult> SubmitForApproval(Guid id, [FromBody] SubmitOrderRequest request, CancellationToken ct)
    {
        var result = await orders.SubmitForApprovalAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/coordinators")]
    public async Task<IActionResult> AddCoordinators(Guid id, [FromBody] OrderCoordinatorRequest request, CancellationToken ct)
    {
        var result = await orders.AddCoordinatorsAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/complete-specialist-coordination")]
    public async Task<IActionResult> CompleteSpecialistCoordination(Guid id, CancellationToken ct)
    {
        var result = await orders.CompleteSpecialistCoordinationAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/complete-department-coordination")]
    public async Task<IActionResult> CompleteDepartmentCoordination(Guid id, CancellationToken ct)
    {
        var result = await orders.CompleteDepartmentCoordinationAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/approve-dept-head")]
    public async Task<IActionResult> ApproveDeptHead(Guid id, [FromBody] OrderApprovalRequest request, CancellationToken ct)
    {
        var result = await orders.ApproveDeptHeadAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/reject-dept-head")]
    public async Task<IActionResult> RejectDeptHead(Guid id, [FromBody] OrderRevisionRequest request, CancellationToken ct)
    {
        var result = await orders.RejectDeptHeadAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/approve-legal")]
    public async Task<IActionResult> ApproveLegal(Guid id, [FromBody] OrderApprovalRequest request, CancellationToken ct)
    {
        var result = await orders.ApproveLegalAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/reject-legal")]
    public async Task<IActionResult> RejectLegal(Guid id, [FromBody] OrderRevisionRequest request, CancellationToken ct)
    {
        var result = await orders.RejectLegalAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/approve-supervising-deputy")]
    public async Task<IActionResult> ApproveSupervisingDeputy(Guid id, [FromBody] OrderApprovalRequest request, CancellationToken ct)
    {
        var result = await orders.ApproveSupervisingDeputyAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/approve-first-deputy")]
    public async Task<IActionResult> ApproveFirstDeputy(Guid id, [FromBody] OrderApprovalRequest request, CancellationToken ct)
    {
        var result = await orders.ApproveFirstDeputyAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/approve-general-director")]
    public async Task<IActionResult> ApproveGeneralDirector(Guid id, [FromBody] OrderApprovalRequest request, CancellationToken ct)
    {
        var result = await orders.ApproveGeneralDirectorAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/reject-approval")]
    public async Task<IActionResult> RejectApproval(Guid id, [FromBody] OrderRevisionRequest request, CancellationToken ct)
    {
        var result = await orders.RejectApprovalAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/finalize-eds")]
    public async Task<IActionResult> FinalizeEds(Guid id, CancellationToken ct)
    {
        var result = await orders.FinalizeEdsAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/send-to-registrar")]
    public async Task<IActionResult> SendToRegistrar(Guid id, CancellationToken ct)
    {
        var result = await orders.SendToRegistrarAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/register")]
    public async Task<IActionResult> Register(Guid id, CancellationToken ct)
    {
        var result = await orders.RegisterAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/confirm-paper-signature")]
    public async Task<IActionResult> ConfirmPaperSignature(Guid id, CancellationToken ct)
    {
        var result = await orders.ConfirmPaperSignatureAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/upload-scan")]
    public async Task<IActionResult> UploadScan(Guid id, [FromBody] OrderScanUploadRequest request, CancellationToken ct)
    {
        var result = await orders.UploadScanAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/distribute")]
    public async Task<IActionResult> Distribute(Guid id, [FromBody] OrderDistributionRequest request, CancellationToken ct)
    {
        var result = await orders.DistributeAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    {
        var result = await orders.ArchiveAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] OrderCommentRequest request, CancellationToken ct)
    {
        var result = await orders.AddCommentAsync(id, request, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim is not null && Guid.TryParse(claim, out var id) ? id : null;
    }

    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
