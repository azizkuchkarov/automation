using System.Security.Claims;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATG.Platform.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "SuperAdmin,HOTopManager")]
public class UsersController(IUserService users) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? orgId = null,
        [FromQuery] UserRole? role = null,
        [FromQuery] bool? isActive = null,
        CancellationToken ct = default)
    {
        var result = await users.GetUsersAsync(page, pageSize, search, orgId, role, isActive, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await users.GetUserByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var actorId = GetUserId()!.Value;
        var result = await users.CreateUserAsync(request, actorId, GetIp(), ct);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var result = await users.UpdateUserAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var result = await users.DeactivateUserAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        var result = await users.ActivateUserAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    [HttpPatch("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var result = await users.ResetPasswordAsync(id, request.NewPassword, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }

    [HttpGet("check-email")]
    public async Task<IActionResult> CheckEmail([FromQuery] string email, [FromQuery] Guid? excludeId, CancellationToken ct)
    {
        var result = await users.IsEmailUniqueAsync(email, excludeId, ct);
        return Ok(new { isUnique = result.Data });
    }

    [HttpGet("check-employee-id")]
    public async Task<IActionResult> CheckEmployeeId([FromQuery] string employeeId, [FromQuery] Guid? excludeId, CancellationToken ct)
    {
        var result = await users.IsEmployeeIdUniqueAsync(employeeId, excludeId, ct);
        return Ok(new { isUnique = result.Data });
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        var data = await users.ExportUsersCsvAsync(ct);
        return File(data, "text/csv", "users.csv");
    }

    private Guid? GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(id, out var guid) ? guid : null;
    }

    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}

public record ResetPasswordRequest(string NewPassword);
