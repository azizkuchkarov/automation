using System.Security.Claims;
using ATG.Platform.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATG.Platform.API.Controllers;

[ApiController]
[Route("api/hr/business-trips/admin/workflow")]
[Authorize(Roles = "SuperAdmin,HOTopManager")]
public class HrBusinessTripWorkflowController(IHrBusinessTripWorkflowService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await service.GetAdminAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : Forbid();
    }

    private Guid? GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
