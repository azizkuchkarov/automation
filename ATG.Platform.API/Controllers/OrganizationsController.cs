using ATG.Platform.Application.Interfaces;
using ATG.Platform.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATG.Platform.API.Controllers;

[ApiController]
[Route("api/organizations")]
[Authorize]
public class OrganizationsController(IOrganizationService orgs) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetTree(CancellationToken ct)
    {
        var result = await orgs.GetTreeAsync(ct);
        return Ok(result.Data);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await orgs.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] OrgRequest request, CancellationToken ct)
    {
        var result = await orgs.CreateAsync(request.Name, request.Code, request.ParentId, request.OrgType, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,HOTopManager")]
    public async Task<IActionResult> Update(Guid id, [FromBody] OrgUpdateRequest request, CancellationToken ct)
    {
        var result = await orgs.UpdateAsync(id, request.Name, request.Code, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }
}

public record OrgRequest(string Name, string Code, Guid? ParentId, OrgType OrgType);
public record OrgUpdateRequest(string Name, string Code);

[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController(IDepartmentService depts) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? orgId, CancellationToken ct)
    {
        var result = await depts.GetAllAsync(orgId, ct);
        return Ok(result.Data);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,HOTopManager")]
    public async Task<IActionResult> Create([FromBody] DeptRequest request, CancellationToken ct)
    {
        var result = await depts.CreateAsync(request.OrganizationId, request.Name, request.Code, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,HOTopManager")]
    public async Task<IActionResult> Update(Guid id, [FromBody] DeptUpdateRequest request, CancellationToken ct)
    {
        var result = await depts.UpdateAsync(id, request.Name, request.Code, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,HOTopManager")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await depts.DeleteAsync(id, ct);
        return result.IsSuccess ? Ok() : BadRequest(new { error = result.Error });
    }
}

public record DeptRequest(Guid OrganizationId, string Name, string Code);
public record DeptUpdateRequest(string Name, string Code);

[ApiController]
[Route("api/positions")]
[Authorize]
public class PositionsController(IPositionService positions) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await positions.GetAllAsync(ct);
        return Ok(result.Data);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,HOTopManager")]
    public async Task<IActionResult> Create([FromBody] PositionRequest request, CancellationToken ct)
    {
        var result = await positions.CreateAsync(request.Name, request.Code, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,HOTopManager")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PositionRequest request, CancellationToken ct)
    {
        var result = await positions.UpdateAsync(id, request.Name, request.Code, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }
}

public record PositionRequest(string Name, string Code);

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = "SuperAdmin,HOTopManager")]
public class AuditController(IAuditService audit) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var result = await audit.GetLogsAsync(page, pageSize, userId, action, from, to, ct);
        return Ok(result.Data);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var result = await audit.GetDashboardStatsAsync(ct);
        return Ok(result.Data);
    }
}
