using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Application.DTOs;

public record UserDto(
    Guid Id,
    string? EmployeeId,
    string FirstName,
    string LastName,
    string? MiddleName,
    string FullName,
    string FirstNameEn,
    string LastNameEn,
    string? MiddleNameEn,
    string FullNameEn,
    string? JobTitleRu,
    string? JobTitleEn,
    string Email,
    string? Phone,
    Guid OrganizationId,
    string OrganizationName,
    string OrganizationCode,
    Guid? DepartmentId,
    string? DepartmentName,
    string? DepartmentNameEn,
    Guid? PositionId,
    string? PositionName,
    UserRole Role,
    bool IsActive,
    string Language,
    DateTime? LastLoginAt,
    DateTime CreatedAt);

public record CreateUserRequest(
    string EmployeeId,
    string FirstName,
    string LastName,
    string? MiddleName,
    string Email,
    string? Phone,
    Guid OrganizationId,
    Guid? DepartmentId,
    Guid? PositionId,
    UserRole Role,
    string Language,
    string? Password,
    bool UseLdap = false);

public record ImportUsersRequest(Guid OrganizationId, IReadOnlyList<ImportUserRow> Users);

public record ImportUserRow(
    string EmployeeId,
    string FirstName,
    string LastName,
    string? MiddleName,
    string Email,
    string? Phone,
    string DepartmentCode,
    string PositionCode,
    UserRole Role,
    string Language);

public record ImportUsersResult(int Created, int Failed, IReadOnlyList<string> Errors);

public record UpdateUserRequest(
    string EmployeeId,
    string FirstName,
    string LastName,
    string? MiddleName,
    string Email,
    string? Phone,
    Guid OrganizationId,
    Guid? DepartmentId,
    Guid? PositionId,
    UserRole Role,
    string Language);

public record LoginRequest(string Email, string Password);

public record LoginResponse(string AccessToken, string RefreshToken, UserDto User);

public record OrganizationDto(
    Guid Id,
    string Name,
    string Code,
    Guid? ParentId,
    OrgType OrgType,
    bool IsActive,
    int UserCount,
    IReadOnlyList<OrganizationDto> Children);

public record DepartmentDto(
    Guid Id,
    Guid OrganizationId,
    string OrganizationName,
    string Name,
    string NameEn,
    string Code,
    Guid? ParentId,
    bool IsActive);

public record DepartmentHierarchyDto(
    Guid Id,
    string Name,
    string NameEn,
    string Code,
    bool IsActive,
    int UserCount,
    int TotalUserCount,
    IReadOnlyList<DepartmentHierarchyDto> Children);

public record OrgHierarchyDto(
    Guid Id,
    string Name,
    string Code,
    OrgType OrgType,
    bool IsActive,
    int UserCount,
    int TotalUserCount,
    IReadOnlyList<DepartmentHierarchyDto> Departments,
    IReadOnlyList<OrgHierarchyDto> Children);

public record PositionDto(Guid Id, string Name, string Code, bool IsActive);

public record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string? UserName,
    string Action,
    string? EntityType,
    Guid? EntityId,
    string? Details,
    string? IpAddress,
    DateTime CreatedAt);

public record DashboardStatsDto(
    int TotalUsers,
    int ActiveUsers,
    int Organizations,
    int Departments);
