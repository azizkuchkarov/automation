using System.Security.Claims;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATG.Platform.API.Controllers;

[ApiController]
[Route("api/dcs/outgoing-letters")]
[Authorize]
public class OutgoingLetterController(IOutgoingLetterService letters) : ControllerBase
{
    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissions([FromQuery] Guid? documentId, CancellationToken ct)
    {
        var result = await letters.GetPermissionsAsync(GetUserId()!.Value, documentId, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("dept-heads")]
    public async Task<IActionResult> GetDeptHeads(CancellationToken ct)
    {
        var result = await letters.GetDeptHeadsAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("top-managers")]
    public async Task<IActionResult> GetTopManagers(CancellationToken ct)
    {
        var result = await letters.GetTopManagersAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("coordinators")]
    public async Task<IActionResult> GetCoordinators(CancellationToken ct)
    {
        var result = await letters.GetCoordinatorsAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("translation-languages")]
    public IActionResult GetTranslationLanguages() =>
        Ok(TranslationLanguageOptions.Codes);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await letters.GetByIdAsync(id, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOutgoingLetterRequest request, CancellationToken ct)
    {
        var result = await letters.CreateAsync(request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateDraft(Guid id, [FromBody] CreateOutgoingLetterRequest request, CancellationToken ct)
    {
        var result = await letters.UpdateDraftAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/send-to-translation")]
    public async Task<IActionResult> SendToTranslation(Guid id, [FromBody] SendOutgoingToTranslationRequest request, CancellationToken ct)
    {
        var result = await letters.SendToTranslationAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/submit-to-eds")]
    public async Task<IActionResult> SubmitToEds(Guid id, [FromBody] SubmitOutgoingToEdsRequest request, CancellationToken ct)
    {
        var result = await letters.SubmitToEdsAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/approve-dept-head")]
    public async Task<IActionResult> ApproveDeptHead(Guid id, [FromBody] OutgoingApprovalRequest request, CancellationToken ct)
    {
        var result = await letters.ApproveDeptHeadAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/reject-dept-head")]
    public async Task<IActionResult> RejectDeptHead(Guid id, [FromBody] OutgoingRevisionRequest request, CancellationToken ct)
    {
        var result = await letters.RejectDeptHeadAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/coordinators")]
    public async Task<IActionResult> AddCoordinators(Guid id, [FromBody] OutgoingCoordinatorRequest request, CancellationToken ct)
    {
        var result = await letters.AddCoordinatorsAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/complete-specialist-coordination")]
    public async Task<IActionResult> CompleteSpecialistCoordination(Guid id, CancellationToken ct)
    {
        var result = await letters.CompleteSpecialistCoordinationAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/complete-department-coordination")]
    public async Task<IActionResult> CompleteDepartmentCoordination(Guid id, CancellationToken ct)
    {
        var result = await letters.CompleteDepartmentCoordinationAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/approve-supervising-deputy")]
    public async Task<IActionResult> ApproveSupervisingDeputy(Guid id, [FromBody] OutgoingApprovalRequest request, CancellationToken ct)
    {
        var result = await letters.ApproveSupervisingDeputyAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/approve-first-deputy")]
    public async Task<IActionResult> ApproveFirstDeputy(Guid id, [FromBody] OutgoingApprovalRequest request, CancellationToken ct)
    {
        var result = await letters.ApproveFirstDeputyAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/approve-general-director")]
    public async Task<IActionResult> ApproveGeneralDirector(Guid id, [FromBody] OutgoingApprovalRequest request, CancellationToken ct)
    {
        var result = await letters.ApproveGeneralDirectorAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/reject-approval")]
    public async Task<IActionResult> RejectApproval(Guid id, [FromBody] OutgoingRevisionRequest request, CancellationToken ct)
    {
        var result = await letters.RejectApprovalAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/finalize-eds")]
    public async Task<IActionResult> FinalizeEds(Guid id, CancellationToken ct)
    {
        var result = await letters.FinalizeEdsAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/send-to-registrar")]
    public async Task<IActionResult> SendToRegistrar(Guid id, CancellationToken ct)
    {
        var result = await letters.SendToRegistrarAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/register")]
    public async Task<IActionResult> Register(Guid id, CancellationToken ct)
    {
        var result = await letters.RegisterAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/confirm-paper-signature")]
    public async Task<IActionResult> ConfirmPaperSignature(Guid id, CancellationToken ct)
    {
        var result = await letters.ConfirmPaperSignatureAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/confirm-dispatch")]
    public async Task<IActionResult> ConfirmDispatch(Guid id, CancellationToken ct)
    {
        var result = await letters.ConfirmDispatchAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    {
        var result = await letters.ArchiveAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim is not null && Guid.TryParse(claim, out var id) ? id : null;
    }

    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
