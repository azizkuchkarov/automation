using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Application.DTOs;

public record ItAssetDto(
    Guid Id,
    string Category,
    string NameRu,
    string NameEn,
    string? Quantity,
    string? Term,
    string? BudgetCode,
    decimal? BudgetAmount,
    string? Currency,
    Guid? ResponsibleUserId,
    string? ResponsibleUserName,
    DateTime? StartsAt,
    DateTime? ExpiresAt,
    string? ContractNumber,
    DateTime? ContractDate,
    decimal? Cost,
    string Status,
    string? Note,
    int PlanYear,
    int? DaysUntilExpiry,
    bool ExpiryWarning);

public record ItAssetCategorySummaryDto(
    string Category,
    int Total,
    int ExpiringSoon,
    int Expired,
    string? ResponsibleUserName);

public record ItAutomationHubDto(
    IReadOnlyList<ItAssetCategorySummaryDto> Categories,
    int ExpiringSoonTotal);

public record CreateItAssetRequest(
    string Category,
    string NameRu,
    string NameEn,
    string? Quantity,
    string? Term,
    string? BudgetCode,
    decimal? BudgetAmount,
    string? Currency,
    Guid? ResponsibleUserId,
    DateTime? StartsAt,
    DateTime? ExpiresAt,
    string? ContractNumber,
    DateTime? ContractDate,
    decimal? Cost,
    string? Status,
    string? Note,
    int PlanYear);

public record UpdateItAssetRequest(
    string NameRu,
    string NameEn,
    string? Quantity,
    string? Term,
    string? BudgetCode,
    decimal? BudgetAmount,
    string? Currency,
    Guid? ResponsibleUserId,
    DateTime? StartsAt,
    DateTime? ExpiresAt,
    string? ContractNumber,
    DateTime? ContractDate,
    decimal? Cost,
    string? Status,
    string? Note,
    int PlanYear);

public record ItAutomationRoleDto(
    string Category,
    string TitleRu,
    string TitleEn,
    string DescriptionRu,
    string DescriptionEn,
    Guid? ResponsibleUserId,
    string? ResponsibleUserName,
    string? ResponsibleUserEmail);

public record ItAutomationRolesAdminDto(
    IReadOnlyList<ItAutomationRoleDto> Roles,
    IReadOnlyList<ItAutomationCandidateUserDto> Candidates);

public record ItAutomationCandidateUserDto(
    Guid Id,
    string FullName,
    string Email,
    string? DepartmentName);

public record UpdateItAutomationRoleRequest(Guid? ResponsibleUserId);
