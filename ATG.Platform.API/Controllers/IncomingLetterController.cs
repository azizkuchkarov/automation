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

    [HttpPost("{id:guid}/inform")]
    public async Task<IActionResult> Inform(Guid id, [FromBody] InformTopManagersRequest request, CancellationToken ct)
    {
        var result = await letters.InformTopManagersAsync(id, request, GetUserId()!.Value, GetIp(), ct);
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

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var result = await letters.CompleteAsync(id, GetUserId()!.Value, GetIp(), ct);
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
