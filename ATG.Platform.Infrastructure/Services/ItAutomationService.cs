using ATG.Platform.Application.Common;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ATG.Platform.Infrastructure.Services;

public class ItAutomationService(
    AppDbContext db,
    IAuditService audit,
    INotificationService notifications) : IItAutomationService
{
    public const int ExpiryWarningMonths = 3;

    public async Task<Result<ItAutomationHubDto>> GetHubAsync(CancellationToken ct = default)
    {
        await EnsureRolesSeededAsync(ct);
        var now = DateTime.UtcNow.Date;
        var warnUntil = now.AddMonths(ExpiryWarningMonths);
        var assets = await db.ItAssets.AsNoTracking().ToListAsync(ct);
        var roles = await db.ItAutomationRoleAssignments.AsNoTracking()
            .Include(r => r.ResponsibleUser)
            .ToListAsync(ct);
        var roleMap = roles.ToDictionary(r => r.Category, r => r.ResponsibleUser?.FullName);

        var summaries = Enum.GetValues<ItAssetCategory>().Select(cat =>
        {
            var items = assets.Where(a => a.Category == cat).ToList();
            return new ItAssetCategorySummaryDto(
                cat.ToString(),
                items.Count,
                items.Count(a => a.ExpiresAt.HasValue && a.ExpiresAt.Value.Date >= now && a.ExpiresAt.Value.Date <= warnUntil),
                items.Count(a => a.ExpiresAt.HasValue && a.ExpiresAt.Value.Date < now),
                roleMap.GetValueOrDefault(cat));
        }).ToList();

        return Result<ItAutomationHubDto>.Ok(new ItAutomationHubDto(
            summaries,
            summaries.Sum(s => s.ExpiringSoon)));
    }

    public async Task<Result<IReadOnlyList<ItAssetDto>>> ListAsync(
        string? category, int? planYear, CancellationToken ct = default)
    {
        var query = db.ItAssets.AsNoTracking()
            .Include(a => a.ResponsibleUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(category)
            && Enum.TryParse<ItAssetCategory>(category, true, out var cat))
            query = query.Where(a => a.Category == cat);

        if (planYear is > 0)
            query = query.Where(a => a.PlanYear == planYear);

        var items = await query
            .OrderBy(a => a.Category)
            .ThenBy(a => a.ExpiresAt ?? DateTime.MaxValue)
            .ThenBy(a => a.NameEn)
            .ToListAsync(ct);

        return Result<IReadOnlyList<ItAssetDto>>.Ok(items.Select(Map).ToList());
    }

    public async Task<Result<ItAssetDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var item = await db.ItAssets.AsNoTracking()
            .Include(a => a.ResponsibleUser)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
        return item is null
            ? Result<ItAssetDto>.Fail("Asset not found")
            : Result<ItAssetDto>.Ok(Map(item));
    }

    public async Task<Result<ItAssetDto>> CreateAsync(
        Guid actorId, CreateItAssetRequest request, string? ip, CancellationToken ct = default)
    {
        if (!Enum.TryParse<ItAssetCategory>(request.Category, true, out var category))
            return Result<ItAssetDto>.Fail("Invalid category");
        if (string.IsNullOrWhiteSpace(request.NameRu) && string.IsNullOrWhiteSpace(request.NameEn))
            return Result<ItAssetDto>.Fail("Name is required");

        var status = ParseStatus(request.Status) ?? ItAssetStatus.Active;
        var entity = new ItAsset
        {
            Id = Guid.NewGuid(),
            Category = category,
            NameRu = (request.NameRu ?? "").Trim(),
            NameEn = (request.NameEn ?? request.NameRu ?? "").Trim(),
            Quantity = NullIfWhite(request.Quantity),
            Term = NullIfWhite(request.Term),
            BudgetCode = NullIfWhite(request.BudgetCode),
            BudgetAmount = request.BudgetAmount,
            Currency = NullIfWhite(request.Currency),
            ResponsibleUserId = request.ResponsibleUserId,
            StartsAt = request.StartsAt?.Date,
            ExpiresAt = request.ExpiresAt?.Date,
            ContractNumber = NullIfWhite(request.ContractNumber),
            ContractDate = request.ContractDate?.Date,
            Cost = request.Cost,
            Status = status,
            Note = NullIfWhite(request.Note),
            PlanYear = request.PlanYear > 0 ? request.PlanYear : DateTime.UtcNow.Year,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.ItAssets.Add(entity);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ItAssetCreated", "ItAsset", entity.Id, entity.NameEn, ip, ct);

        return await GetAsync(entity.Id, ct);
    }

    public async Task<Result<ItAssetDto>> UpdateAsync(
        Guid id, Guid actorId, UpdateItAssetRequest request, string? ip, CancellationToken ct = default)
    {
        var entity = await db.ItAssets.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (entity is null) return Result<ItAssetDto>.Fail("Asset not found");
        if (string.IsNullOrWhiteSpace(request.NameRu) && string.IsNullOrWhiteSpace(request.NameEn))
            return Result<ItAssetDto>.Fail("Name is required");

        entity.NameRu = (request.NameRu ?? "").Trim();
        entity.NameEn = (request.NameEn ?? request.NameRu ?? "").Trim();
        entity.Quantity = NullIfWhite(request.Quantity);
        entity.Term = NullIfWhite(request.Term);
        entity.BudgetCode = NullIfWhite(request.BudgetCode);
        entity.BudgetAmount = request.BudgetAmount;
        entity.Currency = NullIfWhite(request.Currency);
        entity.ResponsibleUserId = request.ResponsibleUserId;
        entity.StartsAt = request.StartsAt?.Date;
        entity.ExpiresAt = request.ExpiresAt?.Date;
        entity.ContractNumber = NullIfWhite(request.ContractNumber);
        entity.ContractDate = request.ContractDate?.Date;
        entity.Cost = request.Cost;
        entity.Status = ParseStatus(request.Status) ?? entity.Status;
        entity.Note = NullIfWhite(request.Note);
        entity.PlanYear = request.PlanYear > 0 ? request.PlanYear : entity.PlanYear;
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ItAssetUpdated", "ItAsset", entity.Id, entity.NameEn, ip, ct);
        return await GetAsync(entity.Id, ct);
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, Guid actorId, string? ip, CancellationToken ct = default)
    {
        var entity = await db.ItAssets.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (entity is null) return Result<bool>.Fail("Asset not found");
        db.ItAssets.Remove(entity);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ItAssetDeleted", "ItAsset", id, entity.NameEn, ip, ct);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<ItAutomationRolesAdminDto>> GetRolesAdminAsync(Guid actorId, CancellationToken ct = default)
    {
        if (!await IsAdminAsync(actorId, ct))
            return Result<ItAutomationRolesAdminDto>.Fail("Access denied");

        await EnsureRolesSeededAsync(ct);
        var roles = await db.ItAutomationRoleAssignments.AsNoTracking()
            .Include(r => r.ResponsibleUser)
            .ToListAsync(ct);

        var candidates = await db.Users.AsNoTracking()
            .Include(u => u.Department)
            .Where(u => u.IsActive)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Take(500)
            .Select(u => new ItAutomationCandidateUserDto(
                u.Id,
                u.FullName,
                u.Email,
                u.Department != null ? u.Department.Name : null))
            .ToListAsync(ct);

        var dto = roles
            .OrderBy(r => r.Category)
            .Select(MapRole)
            .ToList();

        return Result<ItAutomationRolesAdminDto>.Ok(new ItAutomationRolesAdminDto(dto, candidates));
    }

    public async Task<Result<ItAutomationRoleDto>> UpdateRoleAsync(
        Guid actorId, string category, UpdateItAutomationRoleRequest request, string? ip, CancellationToken ct = default)
    {
        if (!await IsAdminAsync(actorId, ct))
            return Result<ItAutomationRoleDto>.Fail("Access denied");
        if (!Enum.TryParse<ItAssetCategory>(category, true, out var cat))
            return Result<ItAutomationRoleDto>.Fail("Invalid category");

        await EnsureRolesSeededAsync(ct);
        var role = await db.ItAutomationRoleAssignments
            .Include(r => r.ResponsibleUser)
            .FirstAsync(r => r.Category == cat, ct);

        if (request.ResponsibleUserId.HasValue)
        {
            var userExists = await db.Users.AnyAsync(u => u.Id == request.ResponsibleUserId && u.IsActive, ct);
            if (!userExists) return Result<ItAutomationRoleDto>.Fail("User not found");
        }

        role.ResponsibleUserId = request.ResponsibleUserId;
        role.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "ItAutomationRoleUpdated", "ItAutomationRole", null, cat.ToString(), ip, ct);

        await db.Entry(role).Reference(r => r.ResponsibleUser).LoadAsync(ct);
        return Result<ItAutomationRoleDto>.Ok(MapRole(role));
    }

    public async Task ProcessExpiryWarningsAsync(CancellationToken ct = default)
    {
        await EnsureRolesSeededAsync(ct);
        var now = DateTime.UtcNow.Date;
        var warnUntil = now.AddMonths(ExpiryWarningMonths);
        var roles = await db.ItAutomationRoleAssignments.AsNoTracking().ToListAsync(ct);
        var roleMap = roles.ToDictionary(r => r.Category, r => r.ResponsibleUserId);

        var assets = await db.ItAssets
            .Where(a => a.ExpiresAt != null
                && a.ExpiresAt.Value.Date >= now
                && a.ExpiresAt.Value.Date <= warnUntil
                && a.Status != ItAssetStatus.Cancelled
                && a.Status != ItAssetStatus.Suspended)
            .ToListAsync(ct);

        foreach (var asset in assets)
        {
            var recipientId = asset.ResponsibleUserId ?? roleMap.GetValueOrDefault(asset.Category);
            if (!recipientId.HasValue) continue;

            // Avoid daily spam — notify at most once per 20 days per asset
            if (asset.LastExpiryWarningAt.HasValue
                && asset.LastExpiryWarningAt.Value > DateTime.UtcNow.AddDays(-20))
                continue;

            var name = string.IsNullOrWhiteSpace(asset.NameEn) ? asset.NameRu : asset.NameEn;
            await notifications.NotifyItAssetExpiryWarningAsync(
                recipientId.Value,
                name,
                asset.ExpiresAt!.Value,
                asset.Id,
                asset.Category.ToString(),
                ct);
            asset.LastExpiryWarningAt = DateTime.UtcNow;
        }

        if (assets.Count > 0)
            await db.SaveChangesAsync(ct);
    }

    private async Task EnsureRolesSeededAsync(CancellationToken ct)
    {
        var existing = await db.ItAutomationRoleAssignments.Select(r => r.Category).ToListAsync(ct);
        foreach (var cat in Enum.GetValues<ItAssetCategory>())
        {
            if (existing.Contains(cat)) continue;
            db.ItAutomationRoleAssignments.Add(new ItAutomationRoleAssignment
            {
                Category = cat,
                UpdatedAt = DateTime.UtcNow,
            });
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task<bool> IsAdminAsync(Guid actorId, CancellationToken ct)
    {
        var role = await db.Users.AsNoTracking()
            .Where(u => u.Id == actorId)
            .Select(u => u.Role)
            .FirstOrDefaultAsync(ct);
        return role is UserRole.SuperAdmin or UserRole.HOTopManager;
    }

    private static ItAssetDto Map(ItAsset a)
    {
        var now = DateTime.UtcNow.Date;
        int? days = a.ExpiresAt.HasValue ? (int)(a.ExpiresAt.Value.Date - now).TotalDays : null;
        var warning = days is >= 0 and <= ExpiryWarningMonths * 31;
        return new ItAssetDto(
            a.Id,
            a.Category.ToString(),
            a.NameRu,
            a.NameEn,
            a.Quantity,
            a.Term,
            a.BudgetCode,
            a.BudgetAmount,
            a.Currency,
            a.ResponsibleUserId,
            a.ResponsibleUser?.FullName,
            a.StartsAt,
            a.ExpiresAt,
            a.ContractNumber,
            a.ContractDate,
            a.Cost,
            a.Status.ToString(),
            a.Note,
            a.PlanYear,
            days,
            warning);
    }

    private static ItAutomationRoleDto MapRole(ItAutomationRoleAssignment r)
    {
        var (titleRu, titleEn, descRu, descEn) = CategoryMeta(r.Category);
        return new ItAutomationRoleDto(
            r.Category.ToString(),
            titleRu,
            titleEn,
            descRu,
            descEn,
            r.ResponsibleUserId,
            r.ResponsibleUser?.FullName,
            r.ResponsibleUser?.Email);
    }

    public static (string TitleRu, string TitleEn, string DescRu, string DescEn) CategoryMeta(ItAssetCategory cat) =>
        cat switch
        {
            ItAssetCategory.License => (
                "Лицензии", "Licenses",
                "Ответственный за программные лицензии и подписки (MS 365, Visio, Adobe и др.)",
                "Responsible for software licenses and subscriptions (MS 365, Visio, Adobe, etc.)"),
            ItAssetCategory.Service => (
                "Услуги", "Services",
                "Ответственный за IT-услуги и сопровождение инфраструктуры",
                "Responsible for IT services and infrastructure support"),
            ItAssetCategory.MobileService => (
                "Мобильная связь", "Mobile Services",
                "Ответственный за мобильную связь (Ucell, Unitel, Uzmobile и др.)",
                "Responsible for mobile operator services (Ucell, Unitel, Uzmobile, etc.)"),
            ItAssetCategory.GovernmentService => (
                "Государственные услуги", "Government Services",
                "Ответственный за гос. разрешения, E-IMZO, радиосвязь и лицензии связи",
                "Responsible for government permits, E-IMZO, radio and telecom licenses"),
            ItAssetCategory.Equipment => (
                "Оборудование", "Equipment",
                "Ответственный за закупку IT-оборудования и комплектующих",
                "Responsible for IT equipment and spare parts purchases"),
            _ => (cat.ToString(), cat.ToString(), "", ""),
        };

    private static ItAssetStatus? ParseStatus(string? status) =>
        string.IsNullOrWhiteSpace(status) ? null
        : Enum.TryParse<ItAssetStatus>(status, true, out var s) ? s : null;

    private static string? NullIfWhite(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
