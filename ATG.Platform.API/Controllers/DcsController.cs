using System.Security.Claims;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATG.Platform.API.Controllers;

[ApiController]
[Route("api/dcs")]
[Authorize]
public class DcsController(IDcsService dcs) : ControllerBase
{
    [HttpGet("types")]
    public IActionResult GetTypes() => Ok(dcs.GetTypes());

    [HttpGet("documents")]
    public async Task<IActionResult> GetDocuments(
        [FromQuery] DocumentType type,
        [FromQuery] string view = "all",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] DocumentStatus? status = null,
        CancellationToken ct = default)
    {
        var result = await dcs.GetDocumentsAsync(GetUserId()!.Value, type, view, page, pageSize, status, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("documents/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await dcs.GetByIdAsync(id, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    [HttpPost("documents")]
    public async Task<IActionResult> Create([FromBody] CreateDocumentRequest request, CancellationToken ct)
    {
        var result = await dcs.CreateAsync(request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPatch("documents/{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id, [FromBody] UpdateDocumentStatusRequest request, CancellationToken ct)
    {
        var result = await dcs.UpdateStatusAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("admin/control")]
    [Authorize(Roles = "SuperAdmin,HOTopManager")]
    public async Task<IActionResult> AdminControl(CancellationToken ct)
    {
        var result = await dcs.GetAdminControlAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("admin/dashboard")]
    [Authorize(Roles = "SuperAdmin,HOTopManager")]
    public async Task<IActionResult> AdminDashboard(CancellationToken ct)
    {
        var result = await dcs.GetAdminDashboardAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim is not null && Guid.TryParse(claim, out var id) ? id : null;
    }

    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
