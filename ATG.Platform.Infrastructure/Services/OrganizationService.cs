using ATG.Platform.Application.Common;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Application.Mappings;
using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ATG.Platform.Infrastructure.Services;

public class OrganizationService(AppDbContext db) : IOrganizationService
{
    public async Task<Result<IReadOnlyList<OrganizationDto>>> GetTreeAsync(CancellationToken ct = default)
    {
        var orgs = await db.Organizations.Where(o => o.IsActive).ToListAsync(ct);
        var userCounts = await db.Users.GroupBy(u => u.OrganizationId)
            .Select(g => new { OrgId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OrgId, x => x.Count, ct);

        var roots = orgs.Where(o => o.ParentId == null).Select(o => BuildTree(o, orgs, userCounts)).ToList();
        return Result<IReadOnlyList<OrganizationDto>>.Ok(roots);
    }

    public async Task<Result<OrganizationDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var org = await db.Organizations.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (org is null) return Result<OrganizationDto>.Fail("Organization not found");
        var count = await db.Users.CountAsync(u => u.OrganizationId == id, ct);
        return Result<OrganizationDto>.Ok(org.ToDto(count));
    }

    public async Task<Result<OrganizationDto>> CreateAsync(string name, string code, Guid? parentId, OrgType orgType, CancellationToken ct = default)
    {
        if (await db.Organizations.AnyAsync(o => o.Code == code, ct))
            return Result<OrganizationDto>.Fail("Code already exists");

        var org = new Organization { Name = name, Code = code, ParentId = parentId, OrgType = orgType };
        db.Organizations.Add(org);
        await db.SaveChangesAsync(ct);
        return Result<OrganizationDto>.Ok(org.ToDto());
    }

    public async Task<Result<OrganizationDto>> UpdateAsync(Guid id, string name, string code, CancellationToken ct = default)
    {
        var org = await db.Organizations.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (org is null) return Result<OrganizationDto>.Fail("Organization not found");

        if (await db.Organizations.AnyAsync(o => o.Code == code && o.Id != id, ct))
            return Result<OrganizationDto>.Fail("Code already exists");

        org.Name = name;
        org.Code = code;
        await db.SaveChangesAsync(ct);
        var count = await db.Users.CountAsync(u => u.OrganizationId == id, ct);
        return Result<OrganizationDto>.Ok(org.ToDto(count));
    }

    private static OrganizationDto BuildTree(Organization org, List<Organization> all, Dictionary<Guid, int> counts)
    {
        var children = all.Where(o => o.ParentId == org.Id)
            .Select(o => BuildTree(o, all, counts)).ToList();
        return org.ToDto(counts.GetValueOrDefault(org.Id), children);
    }
}

public class DepartmentService(AppDbContext db) : IDepartmentService
{
    public async Task<Result<IReadOnlyList<DepartmentDto>>> GetAllAsync(Guid? orgId, CancellationToken ct = default)
    {
        var query = db.Departments.Include(d => d.Organization).Where(d => d.IsActive).AsQueryable();
        if (orgId.HasValue) query = query.Where(d => d.OrganizationId == orgId);
        var items = await query.OrderBy(d => d.Name).ToListAsync(ct);
        return Result<IReadOnlyList<DepartmentDto>>.Ok(items.Select(i => i.ToDto()).ToList());
    }

    public async Task<Result<DepartmentDto>> CreateAsync(Guid orgId, string name, string code, CancellationToken ct = default)
    {
        var dept = new Department { OrganizationId = orgId, Name = name, Code = code };
        db.Departments.Add(dept);
        await db.SaveChangesAsync(ct);
        await db.Entry(dept).Reference(d => d.Organization).LoadAsync(ct);
        return Result<DepartmentDto>.Ok(dept.ToDto());
    }

    public async Task<Result<DepartmentDto>> UpdateAsync(Guid id, string name, string code, CancellationToken ct = default)
    {
        var dept = await db.Departments.Include(d => d.Organization).FirstOrDefaultAsync(d => d.Id == id, ct);
        if (dept is null) return Result<DepartmentDto>.Fail("Department not found");
        dept.Name = name;
        dept.Code = code;
        await db.SaveChangesAsync(ct);
        return Result<DepartmentDto>.Ok(dept.ToDto());
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var dept = await db.Departments.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (dept is null) return Result<bool>.Fail("Department not found");
        dept.IsActive = false;
        await db.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }
}

public class PositionService(AppDbContext db) : IPositionService
{
    public async Task<Result<IReadOnlyList<PositionDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await db.Positions.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync(ct);
        return Result<IReadOnlyList<PositionDto>>.Ok(items.Select(i => i.ToDto()).ToList());
    }

    public async Task<Result<PositionDto>> CreateAsync(string name, string code, CancellationToken ct = default)
    {
        var pos = new Position { Name = name, Code = code };
        db.Positions.Add(pos);
        await db.SaveChangesAsync(ct);
        return Result<PositionDto>.Ok(pos.ToDto());
    }

    public async Task<Result<PositionDto>> UpdateAsync(Guid id, string name, string code, CancellationToken ct = default)
    {
        var pos = await db.Positions.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pos is null) return Result<PositionDto>.Fail("Position not found");
        pos.Name = name;
        pos.Code = code;
        await db.SaveChangesAsync(ct);
        return Result<PositionDto>.Ok(pos.ToDto());
    }
}
