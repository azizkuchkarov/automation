using System.Globalization;
using System.Text;
using System.Text.Json;
using ATG.Platform.Application.Common;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Application.Mappings;
using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ATG.Platform.Infrastructure.Services;

public class UserService(AppDbContext db, IAuditService audit) : IUserService
{
    public async Task<Result<PagedResult<UserDto>>> GetUsersAsync(int page, int pageSize, string? search, Guid? orgId, UserRole? role, bool? isActive, CancellationToken ct = default)
    {
        var query = db.Users
            .Include(u => u.Organization)
            .Include(u => u.Department)
            .Include(u => u.Position)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(s) ||
                u.LastName.ToLower().Contains(s) ||
                u.Email.ToLower().Contains(s) ||
                (u.EmployeeId != null && u.EmployeeId.ToLower().Contains(s)));
        }
        if (orgId.HasValue) query = query.Where(u => u.OrganizationId == orgId);
        if (role.HasValue) query = query.Where(u => u.Role == role);
        if (isActive.HasValue) query = query.Where(u => u.IsActive == isActive);

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return Result<PagedResult<UserDto>>.Ok(new PagedResult<UserDto>(
            items.Select(i => i.ToDto()).ToList(), total, page, pageSize));
    }

    public async Task<Result<UserDto>> GetUserByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await GetUserQuery().FirstOrDefaultAsync(u => u.Id == id, ct);
        return user is null ? Result<UserDto>.Fail("User not found") : Result<UserDto>.Ok(user.ToDto());
    }

    public async Task<Result<UserDto>> CreateUserAsync(CreateUserRequest request, Guid actorId, string? ipAddress, CancellationToken ct = default)
    {
        if (!IsValidPassword(request.Password))
            return Result<UserDto>.Fail("Password must be at least 8 characters with 1 uppercase and 1 number");

        if (await db.Users.AnyAsync(u => u.Email == request.Email.ToLower(), ct))
            return Result<UserDto>.Fail("Email already exists");

        if (await db.Users.AnyAsync(u => u.EmployeeId == request.EmployeeId, ct))
            return Result<UserDto>.Fail("Employee ID already exists");

        var user = new User
        {
            EmployeeId = request.EmployeeId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            MiddleName = request.MiddleName,
            Email = request.Email.ToLower(),
            Phone = request.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
            OrganizationId = request.OrganizationId,
            DepartmentId = request.DepartmentId,
            PositionId = request.PositionId,
            Role = request.Role,
            Language = request.Language
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "USER_CREATED", "User", user.Id, JsonSerializer.Serialize(new { user.Email }), ipAddress, ct);

        return await GetUserByIdAsync(user.Id, ct);
    }

    public async Task<Result<UserDto>> UpdateUserAsync(Guid id, UpdateUserRequest request, Guid actorId, string? ipAddress, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return Result<UserDto>.Fail("User not found");

        if (await db.Users.AnyAsync(u => u.Email == request.Email.ToLower() && u.Id != id, ct))
            return Result<UserDto>.Fail("Email already exists");

        if (await db.Users.AnyAsync(u => u.EmployeeId == request.EmployeeId && u.Id != id, ct))
            return Result<UserDto>.Fail("Employee ID already exists");

        user.EmployeeId = request.EmployeeId;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.MiddleName = request.MiddleName;
        user.Email = request.Email.ToLower();
        user.Phone = request.Phone;
        user.OrganizationId = request.OrganizationId;
        user.DepartmentId = request.DepartmentId;
        user.PositionId = request.PositionId;
        user.Role = request.Role;
        user.Language = request.Language;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "USER_UPDATED", "User", user.Id, null, ipAddress, ct);

        return await GetUserByIdAsync(id, ct);
    }

    public async Task<Result<bool>> DeactivateUserAsync(Guid id, Guid actorId, string? ipAddress, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return Result<bool>.Fail("User not found");
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "USER_DEACTIVATED", "User", user.Id, null, ipAddress, ct);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> ActivateUserAsync(Guid id, Guid actorId, string? ipAddress, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return Result<bool>.Fail("User not found");
        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "USER_ACTIVATED", "User", user.Id, null, ipAddress, ct);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> ResetPasswordAsync(Guid id, string newPassword, Guid actorId, string? ipAddress, CancellationToken ct = default)
    {
        if (!IsValidPassword(newPassword))
            return Result<bool>.Fail("Password must be at least 8 characters with 1 uppercase and 1 number");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return Result<bool>.Fail("User not found");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, 12);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "PASSWORD_RESET", "User", user.Id, null, ipAddress, ct);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> IsEmailUniqueAsync(string email, Guid? excludeId, CancellationToken ct = default)
    {
        var exists = await db.Users.AnyAsync(u => u.Email == email.ToLower() && u.Id != excludeId, ct);
        return Result<bool>.Ok(!exists);
    }

    public async Task<Result<bool>> IsEmployeeIdUniqueAsync(string employeeId, Guid? excludeId, CancellationToken ct = default)
    {
        var exists = await db.Users.AnyAsync(u => u.EmployeeId == employeeId && u.Id != excludeId, ct);
        return Result<bool>.Ok(!exists);
    }

    public async Task<byte[]> ExportUsersCsvAsync(CancellationToken ct = default)
    {
        var users = await GetUserQuery().OrderBy(u => u.LastName).ToListAsync(ct);
        var sb = new StringBuilder();
        sb.AppendLine("EmployeeId,FirstName,LastName,Email,Organization,Department,Position,Role,Status,LastLogin");
        foreach (var u in users)
        {
            sb.AppendLine(string.Join(",",
                Csv(u.EmployeeId), Csv(u.FirstName), Csv(u.LastName), Csv(u.Email),
                Csv(u.Organization?.Name), Csv(u.Department?.Name), Csv(u.Position?.Name),
                u.Role.ToString(), u.IsActive ? "Active" : "Inactive",
                u.LastLoginAt?.ToString("o", CultureInfo.InvariantCulture) ?? ""));
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private IQueryable<User> GetUserQuery() => db.Users
        .Include(u => u.Organization)
        .Include(u => u.Department)
        .Include(u => u.Position);

    private static bool IsValidPassword(string password) =>
        password.Length >= 8 && password.Any(char.IsUpper) && password.Any(char.IsDigit);

    private static string Csv(string? value) => $"\"{(value ?? "").Replace("\"", "\"\"")}\"";
}
