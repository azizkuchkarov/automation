using ATG.Platform.Application.Common;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Application.Mappings;
using ATG.Platform.Domain.Entities;
using ATG.Platform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ATG.Platform.Infrastructure.Services;

public class AuthService(AppDbContext db, IJwtService jwt, IAuditService audit) : IAuthService
{
    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default)
    {
        var user = await db.Users
            .Include(u => u.Organization)
            .Include(u => u.Department)
            .Include(u => u.Position)
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower(), ct);

        if (user is null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Result<LoginResponse>.Fail("Invalid email or password");

        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        var refreshToken = CreateRefreshToken(user.Id);
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(user.Id, "LOGIN", "User", user.Id, null, ipAddress, ct);

        var accessToken = jwt.GenerateAccessToken(user);
        return Result<LoginResponse>.Ok(new LoginResponse(accessToken, refreshToken.Token, user.ToDto()));
    }

    public async Task<Result<(string AccessToken, string RefreshToken)>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var stored = await db.RefreshTokens
            .Include(r => r.User).ThenInclude(u => u.Organization)
            .Include(r => r.User).ThenInclude(u => u.Department)
            .Include(r => r.User).ThenInclude(u => u.Position)
            .FirstOrDefaultAsync(r => r.Token == refreshToken && !r.IsRevoked, ct);

        if (stored is null || stored.ExpiresAt < DateTime.UtcNow || !stored.User.IsActive)
            return Result<(string, string)>.Fail("Invalid refresh token");

        stored.IsRevoked = true;
        var newRefresh = CreateRefreshToken(stored.UserId);
        db.RefreshTokens.Add(newRefresh);
        await db.SaveChangesAsync(ct);

        return Result<(string, string)>.Ok((jwt.GenerateAccessToken(stored.User), newRefresh.Token));
    }

    public async Task<Result<bool>> LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == refreshToken, ct);
        if (stored is not null)
        {
            stored.IsRevoked = true;
            await db.SaveChangesAsync(ct);
        }
        return Result<bool>.Ok(true);
    }

    public async Task<Result<UserDto>> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users
            .Include(u => u.Organization)
            .Include(u => u.Department)
            .Include(u => u.Position)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        return user is null
            ? Result<UserDto>.Fail("User not found")
            : Result<UserDto>.Ok(user.ToDto());
    }

    private RefreshToken CreateRefreshToken(Guid userId) => new()
    {
        UserId = userId,
        Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
        ExpiresAt = DateTime.UtcNow.AddDays(7)
    };
}
