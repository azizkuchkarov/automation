using System.Security.Claims;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATG.Platform.API.Controllers;

[ApiController]
[Route("api/dcs/incoming-letters")]
[Authorize]
public class IncomingLetterController(IIncomingLetterService letters) : ControllerBase
{
    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissions([FromQuery] Guid? documentId, CancellationToken ct)
    {
        var result = await letters.GetPermissionsAsync(GetUserId()!.Value, documentId, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("top-managers")]
    public async Task<IActionResult> GetTopManagers(CancellationToken ct)
    {
        var result = await letters.GetTopManagersAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments(CancellationToken ct)
    {
        var result = await letters.GetDepartmentsAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:guid}/workers")]
    public async Task<IActionResult> GetWorkers(Guid id, CancellationToken ct)
    {
        var result = await letters.GetDepartmentWorkersAsync(id, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await letters.GetByIdAsync(id, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIncomingLetterRequest request, CancellationToken ct)
    {
        var result = await letters.CreateAsync(request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/send-to-translation")]
    public async Task<IActionResult> SendToTranslation(Guid id, [FromBody] SendToTranslationRequest request, CancellationToken ct)
    {
        var result = await letters.SendToTranslationAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("translation-languages")]
    public IActionResult GetTranslationLanguages() =>
        Ok(TranslationLanguageOptions.Codes);

    [HttpPost("{id:guid}/complete-translation")]
    public async Task<IActionResult> CompleteTranslation(Guid id, CancellationToken ct)
    {
        var result = await letters.CompleteTranslationAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/register-in-eds")]
    public async Task<IActionResult> RegisterInEds(Guid id, CancellationToken ct)
    {
        var result = await letters.RegisterInEdsAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/send-for-resolution")]
    public async Task<IActionResult> SendForResolution(Guid id, [FromBody] SendForResolutionRequest request, CancellationToken ct)
    {
        var result = await letters.SendForResolutionAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/inform-additional")]
    public async Task<IActionResult> InformAdditional(Guid id, [FromBody] InformTopManagersRequest request, CancellationToken ct)
    {
        var result = await letters.InformAdditionalManagersAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/route")]
    public async Task<IActionResult> Route(Guid id, [FromBody] RouteIncomingLetterRequest request, CancellationToken ct)
    {
        var result = await letters.RouteToDepartmentAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignIncomingLetterRequest request, CancellationToken ct)
    {
        var result = await letters.AssignWorkerAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, [FromBody] AcceptExecutionRequest request, CancellationToken ct)
    {
        var result = await letters.AcceptExecutionAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/report")]
    public async Task<IActionResult> Report(Guid id, CancellationToken ct)
    {
        var result = await letters.ReportCompletionAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/request-revision")]
    public async Task<IActionResult> RequestRevision(Guid id, [FromBody] IncomingLetterCommentRequest request, CancellationToken ct)
    {
        var result = await letters.RequestRevisionAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/accept-completion")]
    public async Task<IActionResult> AcceptCompletion(Guid id, CancellationToken ct)
    {
        var result = await letters.AcceptCompletionAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    {
        var result = await letters.ArchiveAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] IncomingLetterCommentRequest request, CancellationToken ct)
    {
        var result = await letters.AddCommentAsync(id, request, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim is not null && Guid.TryParse(claim, out var id) ? id : null;
    }

    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
