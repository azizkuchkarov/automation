using System.Security.Claims;
using ATG.Platform.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATG.Platform.API.Controllers;

[ApiController]
[Route("api/platform")]
[Authorize]
public class PlatformController(IPlatformHomeService platformHome) : ControllerBase
{
    [HttpGet("module-counts")]
    public async Task<IActionResult> GetModuleCounts(CancellationToken ct)
    {
        var result = await platformHome.GetModuleCountsAsync(GetUserId()!.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    private Guid? GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
