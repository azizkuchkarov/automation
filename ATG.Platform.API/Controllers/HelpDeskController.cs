using System.Security.Claims;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATG.Platform.API.Controllers;

[ApiController]
[Route("api/helpdesk")]
[Authorize]
public class HelpDeskController(IHelpDeskService helpDesk) : ControllerBase
{
    [HttpGet("categories")]
    public IActionResult GetCategories() => Ok(helpDesk.GetCategories());

    [HttpGet("tickets")]
    public async Task<IActionResult> GetTickets(
        [FromQuery] string view = "mine",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] TicketCategory? category = null,
        [FromQuery] TicketStatus? status = null,
        CancellationToken ct = default)
    {
        var result = await helpDesk.GetTicketsAsync(GetUserId()!.Value, view, page, pageSize, category, status, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("board")]
    public async Task<IActionResult> GetBoard([FromQuery] TicketCategory? category, CancellationToken ct)
    {
        var result = await helpDesk.GetBoardAsync(GetUserId()!.Value, category, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("tickets/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await helpDesk.GetByIdAsync(id, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    [HttpPost("tickets")]
    public async Task<IActionResult> Create([FromBody] CreateTicketRequest request, CancellationToken ct)
    {
        var result = await helpDesk.CreateAsync(request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("tickets/{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignTicketRequest request, CancellationToken ct)
    {
        var result = await helpDesk.AssignAsync(id, request, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("tickets/{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct)
    {
        var result = await helpDesk.AcceptAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("tickets/{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct)
    {
        var result = await helpDesk.StartAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("tickets/{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var result = await helpDesk.CompleteAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("tickets/{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct)
    {
        var result = await helpDesk.CloseAsync(id, GetUserId()!.Value, GetIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("tickets/{id:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] AddTicketCommentRequest request, CancellationToken ct)
    {
        var result = await helpDesk.AddCommentAsync(id, request, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("tickets/{id:guid}/assignees")]
    public async Task<IActionResult> GetAssignees(Guid id, CancellationToken ct)
    {
        var result = await helpDesk.GetAssigneesAsync(id, GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("admin/control")]
    [Authorize(Roles = "SuperAdmin,HOTopManager")]
    public async Task<IActionResult> AdminControl(CancellationToken ct)
    {
        var result = await helpDesk.GetAdminControlAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpGet("admin/dashboard")]
    [Authorize(Roles = "SuperAdmin,HOTopManager")]
    public async Task<IActionResult> AdminDashboard(CancellationToken ct)
    {
        var result = await helpDesk.GetAdminDashboardAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    private Guid? GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(id, out var guid) ? guid : null;
    }

    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
