using System.Security.Claims;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATG.Platform.API.Controllers;

[ApiController]
[Route("api/it-automation")]
[Authorize]
public class ItAutomationController(IItAutomationService service) : ControllerBase
{
    [HttpGet("hub")]
    public async Task<IActionResult> Hub(CancellationToken ct)
    {
        var result = await service.GetHubAsync(ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("assets")]
    public async Task<IActionResult> List([FromQuery] string? category, [FromQuery] int? planYear, CancellationToken ct)
    {
        var result = await service.ListAsync(category, planYear, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("assets/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await service.GetAsync(id, ct);
        return result.IsSuccess ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    [HttpPost("assets")]
    public async Task<IActionResult> Create([FromBody] CreateItAssetRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(GetUserId()!.Value, request, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPut("assets/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateItAssetRequest request, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, GetUserId()!.Value, request, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpDelete("assets/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(new { ok = true }) : BadRequest(new { error = result.Error });
    }

    [HttpGet("admin/roles")]
    [Authorize(Roles = "SuperAdmin,HOTopManager")]
    public async Task<IActionResult> GetRoles(CancellationToken ct)
    {
        var result = await service.GetRolesAdminAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : Forbid();
    }

    [HttpPut("admin/roles/{category}")]
    [Authorize(Roles = "SuperAdmin,HOTopManager")]
    public async Task<IActionResult> UpdateRole(string category, [FromBody] UpdateItAutomationRoleRequest request, CancellationToken ct)
    {
        var result = await service.UpdateRoleAsync(GetUserId()!.Value, category, request, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    private Guid? GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
