using ATG.Platform.Domain.Entities;
using ATG.Platform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ATG.Platform.Infrastructure.Seeds;

public static class DepartmentSeederHelper
{
    public static async Task<Dictionary<string, Guid>> SyncDepartmentsAsync(
        AppDbContext db,
        Guid organizationId,
        IReadOnlyList<HoDepartment> departments,
        IReadOnlyList<string> legacyCodes,
        CancellationToken ct = default)
    {
        var deptIds = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var entities = new Dictionary<string, Department>(StringComparer.OrdinalIgnoreCase);

        foreach (var dept in departments)
        {
            var existing = await db.Departments
                .FirstOrDefaultAsync(d => d.OrganizationId == organizationId && d.Code == dept.Code, ct);

            if (existing is null)
            {
                existing = new Department
                {
                    OrganizationId = organizationId,
                    Code = dept.Code,
                    Name = dept.NameRu,
                    NameEn = dept.NameEn,
                    IsActive = dept.Active
                };
                db.Departments.Add(existing);
            }
            else
            {
                existing.Name = dept.NameRu;
                existing.NameEn = dept.NameEn;
                existing.IsActive = dept.Active;
            }

            await db.SaveChangesAsync(ct);
            entities[dept.Code] = existing;
            deptIds[dept.Code] = existing.Id;
        }

        foreach (var dept in departments)
        {
            if (string.IsNullOrEmpty(dept.ParentCode)) continue;
            if (!entities.TryGetValue(dept.Code, out var child)) continue;
            if (!entities.TryGetValue(dept.ParentCode, out var parent)) continue;
            child.ParentId = parent.Id;
        }

        await db.SaveChangesAsync(ct);

        foreach (var legacyCode in legacyCodes)
        {
            var legacy = await db.Departments
                .FirstOrDefaultAsync(d => d.OrganizationId == organizationId && d.Code == legacyCode, ct);
            if (legacy is not null)
                legacy.IsActive = false;
        }

        await db.SaveChangesAsync(ct);
        return deptIds;
    }
}
