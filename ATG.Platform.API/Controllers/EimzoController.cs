using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Application.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ATG.Platform.API.Controllers;

[ApiController]
[Route("api/eimzo")]
public class EimzoController(IEimzoServerClient eimzo, IOptions<EimzoOptions> options) : ControllerBase
{
    [HttpGet("status")]
    [Authorize(Roles = "SuperAdmin,HOTopManager")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var status = await eimzo.GetStatusAsync(ct);
        return Ok(status);
    }

    [HttpGet("config")]
    [Authorize]
    public IActionResult GetFrontendConfig()
    {
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.FrontendApiKey))
            return BadRequest(new { error = "E-IMZO frontend API key is not configured" });

        return Ok(new EimzoFrontendConfigDto(
            opts.SiteHost,
            opts.FrontendApiKey,
            "/api/eimzo/timestamp"));
    }

    [HttpPost("timestamp")]
    [Authorize]
    public async Task<IActionResult> AttachTimestamp([FromBody] EimzoTimestampRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Pkcs7Base64))
            return BadRequest(new { error = "pkcs7 is required" });

        var result = await eimzo.AttachTimestampAsync(request.Pkcs7Base64, GetClientIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("verify/attached")]
    [Authorize(Roles = "SuperAdmin,HOTopManager")]
    public async Task<IActionResult> VerifyAttached([FromBody] EimzoVerifyAttachedRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Pkcs7Base64))
            return BadRequest(new { error = "pkcs7 is required" });

        var result = await eimzo.VerifyAttachedAsync(request.Pkcs7Base64, GetClientIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    [HttpPost("verify/detached")]
    [Authorize(Roles = "SuperAdmin,HOTopManager")]
    public async Task<IActionResult> VerifyDetached([FromBody] EimzoVerifyDetachedRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Pkcs7Base64) || string.IsNullOrWhiteSpace(request.DetachedDataBase64))
            return BadRequest(new { error = "pkcs7 and detachedData are required" });

        var result = await eimzo.VerifyDetachedAsync(request.DetachedDataBase64, request.Pkcs7Base64, GetClientIp(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    private string GetClientIp() =>
        HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
        ?? HttpContext.Connection.RemoteIpAddress?.ToString()
        ?? "127.0.0.1";
}
