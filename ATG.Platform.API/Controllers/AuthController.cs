using System.Security.Claims;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATG.Platform.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await auth.LoginAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        if (!result.IsSuccess) return Unauthorized(new { error = result.Error });

        Response.Cookies.Append("refreshToken", result.Data!.RefreshToken, CookieOptions());
        return Ok(new { accessToken = result.Data.AccessToken, user = result.Data.User });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var token = Request.Cookies["refreshToken"] ?? Request.Headers["X-Refresh-Token"].FirstOrDefault();
        if (string.IsNullOrEmpty(token)) return Unauthorized(new { error = "No refresh token" });

        var result = await auth.RefreshTokenAsync(token, ct);
        if (!result.IsSuccess) return Unauthorized(new { error = result.Error });

        Response.Cookies.Append("refreshToken", result.Data!.RefreshToken, CookieOptions());
        return Ok(new { accessToken = result.Data.AccessToken });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var token = Request.Cookies["refreshToken"];
        if (!string.IsNullOrEmpty(token)) await auth.LogoutAsync(token, ct);
        Response.Cookies.Delete("refreshToken");
        return Ok();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var result = await auth.GetCurrentUserAsync(userId.Value, ct);
        return result.IsSuccess ? Ok(result.Data) : NotFound(new { error = result.Error });
    }

    private Guid? GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(id, out var guid) ? guid : null;
    }

    private CookieOptions CookieOptions() => new()
    {
        HttpOnly = true,
        Secure = false,
        SameSite = SameSiteMode.Lax,
        Expires = DateTimeOffset.UtcNow.AddDays(7),
        Path = "/"
    };
}
