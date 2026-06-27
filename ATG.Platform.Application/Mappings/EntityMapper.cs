using ATG.Platform.Application.DTOs;
using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Application.Mappings;

public static class EntityMapper
{
    public static UserDto ToDto(this User user) => new(
        user.Id,
        user.EmployeeId,
        user.FirstName,
        user.LastName,
        user.MiddleName,
        user.FullName,
        user.FirstNameEn,
        user.LastNameEn,
        user.MiddleNameEn,
        user.FullNameEn,
        user.JobTitleRu,
        user.JobTitleEn,
        user.Email,
        user.Phone,
        user.OrganizationId,
        user.Organization?.Name ?? "",
        user.Organization?.Code ?? "",
        user.DepartmentId,
        user.Department?.Name,
        user.Department?.NameEn,
        user.PositionId,
        user.Position?.Name,
        user.Role,
        user.IsActive,
        user.Language,
        user.LastLoginAt,
        user.CreatedAt);

    public static OrganizationDto ToDto(this Organization org, int userCount = 0, IReadOnlyList<OrganizationDto>? children = null) => new(
        org.Id,
        org.Name,
        org.Code,
        org.ParentId,
        org.OrgType,
        org.IsActive,
        userCount,
        children ?? []);

    public static DepartmentDto ToDto(this Department dept) => new(
        dept.Id,
        dept.OrganizationId,
        dept.Organization?.Name ?? "",
        dept.Name,
        dept.NameEn,
        dept.Code,
        dept.ParentId,
        dept.IsActive);

    public static PositionDto ToDto(this Position pos) => new(pos.Id, pos.Name, pos.Code, pos.IsActive);

    public static AuditLogDto ToDto(this AuditLog log) => new(
        log.Id,
        log.UserId,
        log.User != null ? log.User.FullName : null,
        log.Action,
        log.EntityType,
        log.EntityId,
        log.Details,
        log.IpAddress,
        log.CreatedAt);
}
