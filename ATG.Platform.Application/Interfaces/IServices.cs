using ATG.Platform.Application.Common;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Application.Interfaces;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default);
    Task<Result<(string AccessToken, string RefreshToken)>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<Result<bool>> LogoutAsync(string refreshToken, CancellationToken ct = default);
    Task<Result<UserDto>> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}

public interface IUserService
{
    Task<Result<PagedResult<UserDto>>> GetUsersAsync(int page, int pageSize, string? search, Guid? orgId, UserRole? role, bool? isActive, CancellationToken ct = default);
    Task<Result<UserDto>> GetUserByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<UserDto>> CreateUserAsync(CreateUserRequest request, Guid actorId, string? ipAddress, CancellationToken ct = default);
    Task<Result<UserDto>> UpdateUserAsync(Guid id, UpdateUserRequest request, Guid actorId, string? ipAddress, CancellationToken ct = default);
    Task<Result<bool>> DeactivateUserAsync(Guid id, Guid actorId, string? ipAddress, CancellationToken ct = default);
    Task<Result<bool>> ActivateUserAsync(Guid id, Guid actorId, string? ipAddress, CancellationToken ct = default);
    Task<Result<bool>> ResetPasswordAsync(Guid id, string newPassword, Guid actorId, string? ipAddress, CancellationToken ct = default);
    Task<Result<bool>> IsEmailUniqueAsync(string email, Guid? excludeId, CancellationToken ct = default);
    Task<Result<bool>> IsEmployeeIdUniqueAsync(string employeeId, Guid? excludeId, CancellationToken ct = default);
    Task<byte[]> ExportUsersCsvAsync(CancellationToken ct = default);
}

public interface IOrganizationService
{
    Task<Result<IReadOnlyList<OrganizationDto>>> GetTreeAsync(CancellationToken ct = default);
    Task<Result<OrganizationDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<OrganizationDto>> CreateAsync(string name, string code, Guid? parentId, OrgType orgType, CancellationToken ct = default);
    Task<Result<OrganizationDto>> UpdateAsync(Guid id, string name, string code, CancellationToken ct = default);
}

public interface IDepartmentService
{
    Task<Result<IReadOnlyList<DepartmentDto>>> GetAllAsync(Guid? orgId, CancellationToken ct = default);
    Task<Result<DepartmentDto>> CreateAsync(Guid orgId, string name, string code, CancellationToken ct = default);
    Task<Result<DepartmentDto>> UpdateAsync(Guid id, string name, string code, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IPositionService
{
    Task<Result<IReadOnlyList<PositionDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<PositionDto>> CreateAsync(string name, string code, CancellationToken ct = default);
    Task<Result<PositionDto>> UpdateAsync(Guid id, string name, string code, CancellationToken ct = default);
}

public interface IAuditService
{
    Task LogAsync(Guid? userId, string action, string? entityType, Guid? entityId, string? details, string? ipAddress, CancellationToken ct = default);
    Task<Result<PagedResult<AuditLogDto>>> GetLogsAsync(int page, int pageSize, Guid? userId, string? action, DateTime? from, DateTime? to, CancellationToken ct = default);
    Task<Result<DashboardStatsDto>> GetDashboardStatsAsync(CancellationToken ct = default);
}

public interface IJwtService
{
    string GenerateAccessToken(User user);
}
