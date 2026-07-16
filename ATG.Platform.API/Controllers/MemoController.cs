using System.Security.Claims;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATG.Platform.API.Controllers;

[ApiController]
[Route("api/dcs/memos")]
[Authorize]
public class MemoController(IMemoService memos) : ControllerBase
{
    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissions([FromQuery] Guid? documentId, CancellationToken ct)
    {
        var result = await memos.GetPermissionsAsync(GetUserId()!.Value, documentId, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("top-managers")]
    public async Task<IActionResult> GetTopManagers(CancellationToken ct)
    {
        var result = await memos.GetTopManagersAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("dept-heads")]
    public async Task<IActionResult> GetDeptHeads(CancellationToken ct)
    {
        var result = await memos.GetDeptHeadsAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("coordinators")]
    public async Task<IActionResult> GetCoordinators(CancellationToken ct)
    {
        var result = await memos.GetCoordinatorsAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments(CancellationToken ct)
    {
        var result = await memos.GetDepartmentsAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:guid}/workers")]
    public async Task<IActionResult> GetWorkers(Guid id, CancellationToken ct)
    {
        var result = await memos.GetDepartmentWorkersAsync(id, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("translation-languages")]
    public IActionResult GetTranslationLanguages() => Ok(TranslationLanguageOptions.Codes);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await memos.GetByIdAsync(id, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMemoRequest request, CancellationToken ct)
    {
        var result = await memos.CreateAsync(request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateDraft(Guid id, [FromBody] CreateMemoRequest request, CancellationToken ct)
    {
        var result = await memos.UpdateDraftAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/send-to-translation")]
    public async Task<IActionResult> SendToTranslation(Guid id, [FromBody] SendMemoToTranslationRequest request, CancellationToken ct)
    {
        var result = await memos.SendToTranslationAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/submit-for-approval")]
    public async Task<IActionResult> SubmitForApproval(Guid id, [FromBody] SubmitMemoForApprovalRequest request, CancellationToken ct)
    {
        var result = await memos.SubmitForApprovalAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/coordinators")]
    public async Task<IActionResult> AddCoordinators(Guid id, [FromBody] MemoCoordinatorRequest request, CancellationToken ct)
    {
        var result = await memos.AddCoordinatorsAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/complete-specialist-coordination")]
    public async Task<IActionResult> CompleteSpecialistCoordination(Guid id, CancellationToken ct)
    {
        var result = await memos.CompleteSpecialistCoordinationAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/approve-dept-head")]
    public async Task<IActionResult> ApproveDeptHead(Guid id, [FromBody] MemoApprovalRequest request, CancellationToken ct)
    {
        var result = await memos.ApproveDeptHeadAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/reject-dept-head")]
    public async Task<IActionResult> RejectDeptHead(Guid id, [FromBody] MemoRevisionRequest request, CancellationToken ct)
    {
        var result = await memos.RejectDeptHeadAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/register-and-distribute")]
    public async Task<IActionResult> RegisterAndDistribute(Guid id, [FromBody] RegisterMemoRequest request, CancellationToken ct)
    {
        var result = await memos.RegisterAndDistributeAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/inform-recipients")]
    public async Task<IActionResult> InformRecipients(Guid id, [FromBody] InformMemoRecipientsRequest request, CancellationToken ct)
    {
        var result = await memos.InformRecipientsAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/route-to-department")]
    public async Task<IActionResult> RouteToDepartment(Guid id, [FromBody] RouteMemoRequest request, CancellationToken ct)
    {
        var result = await memos.RouteToDepartmentAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/assign")]
    public async Task<IActionResult> AssignWorker(Guid id, [FromBody] AssignMemoRequest request, CancellationToken ct)
    {
        var result = await memos.AssignWorkerAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/accept-execution")]
    public async Task<IActionResult> AcceptExecution(Guid id, CancellationToken ct)
    {
        var result = await memos.AcceptExecutionAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/report-completion")]
    public async Task<IActionResult> ReportCompletion(Guid id, CancellationToken ct)
    {
        var result = await memos.ReportCompletionAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/request-revision")]
    public async Task<IActionResult> RequestRevision(Guid id, [FromBody] MemoCommentRequest request, CancellationToken ct)
    {
        var result = await memos.RequestExecutionRevisionAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/accept-completion")]
    public async Task<IActionResult> AcceptCompletion(Guid id, CancellationToken ct)
    {
        var result = await memos.AcceptCompletionAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    {
        var result = await memos.ArchiveAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] MemoCommentRequest request, CancellationToken ct)
    {
        var result = await memos.AddCommentAsync(id, request, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim is not null && Guid.TryParse(claim, out var id) ? id : null;
    }

    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
