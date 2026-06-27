using System.Security.Claims;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATG.Platform.API.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
public class TasksController(ITaskService tasks) : ControllerBase
{
    [HttpGet("navigation")]
    public async Task<IActionResult> Navigation(CancellationToken ct)
    {
        var result = await tasks.GetNavigationAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> Analytics(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? departmentId,
        CancellationToken ct)
    {
        var result = await tasks.GetAnalyticsAsync(GetUserId()!.Value, organizationId, departmentId, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string view = "mine",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] WorkTaskStatus? status = null,
        [FromQuery] TaskSource? source = null,
        [FromQuery] Guid? organizationId = null,
        [FromQuery] Guid? departmentId = null,
        CancellationToken ct = default)
    {
        var result = await tasks.GetTasksAsync(
            GetUserId()!.Value, view, page, pageSize, status, source, organizationId, departmentId, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        var result = await tasks.CreateAsync(request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateTaskStatusRequest request, CancellationToken ct)
    {
        var result = await tasks.UpdateStatusAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("assignees")]
    [HttpGet("assignable-users")]
    public async Task<IActionResult> AssignableUsers(CancellationToken ct)
    {
        var result = await tasks.GetAssignableUsersAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    private Guid? GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(id, out var guid) ? guid : null;
    }

    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
