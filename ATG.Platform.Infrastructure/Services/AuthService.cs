using ATG.Platform.Application.Common;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Application.Mappings;
using ATG.Platform.Application.Options;
using ATG.Platform.Domain.Entities;
using ATG.Platform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATG.Platform.Infrastructure.Services;

public class AuthService(
    AppDbContext db,
    IJwtService jwt,
    IAuditService audit,
    ILdapService ldap,
    IOptions<LdapOptions> ldapOptions) : IAuthService
{
    private readonly LdapOptions _ldapOptions = ldapOptions.Value;

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default)
    {
        var loginId = request.Email.Trim();
        var normalizedEmail = NormalizeLoginEmail(loginId);

        var user = await FindUserAsync(normalizedEmail, loginId, ct);

        if (HasLocalPassword(user))
        {
            if (user!.IsActive && BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return await IssueLoginAsync(user, ipAddress, ct);

            return Result<LoginResponse>.Fail("Invalid email or password");
        }

        if (!_ldapOptions.Enabled)
            return Result<LoginResponse>.Fail("Invalid email or password");

        var ldapResult = await ldap.AuthenticateAsync(loginId, request.Password, ct);
        if (!ldapResult.IsSuccess)
            return Result<LoginResponse>.Fail(ldapResult.Error ?? "Invalid email or password");

        var ldapEmail = ldapResult.Data!.ToLowerInvariant();
        user = await db.Users
            .Include(u => u.Organization)
            .Include(u => u.Department)
            .Include(u => u.Position)
            .FirstOrDefaultAsync(u => u.Email == ldapEmail, ct);

        if (user is null || !user.IsActive)
            return Result<LoginResponse>.Fail("User is not registered in the platform");

        return await IssueLoginAsync(user, ipAddress, ct);
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

    private async Task<User?> FindUserAsync(string normalizedEmail, string loginId, CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.Organization)
            .Include(u => u.Department)
            .Include(u => u.Position)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        if (user is not null || loginId.Contains('@'))
            return user;

        var domainEmail = $"{loginId}@{_ldapOptions.Domain}".ToLowerInvariant();
        return await db.Users
            .Include(u => u.Organization)
            .Include(u => u.Department)
            .Include(u => u.Position)
            .FirstOrDefaultAsync(u => u.Email == domainEmail, ct);
    }

    private static bool HasLocalPassword(User? user) =>
        user?.PasswordHash.StartsWith("$2") == true;

    private static string NormalizeLoginEmail(string loginId) =>
        loginId.Contains('@') ? loginId.ToLowerInvariant() : loginId.ToLowerInvariant();

    private async Task<Result<LoginResponse>> IssueLoginAsync(User user, string? ipAddress, CancellationToken ct)
    {
        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        var refreshToken = CreateRefreshToken(user.Id);
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(user.Id, "LOGIN", "User", user.Id, null, ipAddress, ct);

        var accessToken = jwt.GenerateAccessToken(user);
        return Result<LoginResponse>.Ok(new LoginResponse(accessToken, refreshToken.Token, user.ToDto()));
    }

    private RefreshToken CreateRefreshToken(Guid userId) => new()
    {
        UserId = userId,
        Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
        ExpiresAt = DateTime.UtcNow.AddDays(7)
    };
}
