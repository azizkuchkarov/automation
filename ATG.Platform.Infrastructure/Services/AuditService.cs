using System.Text.Json;
using ATG.Platform.Application.Common;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Application.Mappings;
using ATG.Platform.Domain.Entities;
using ATG.Platform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ATG.Platform.Infrastructure.Services;

public class AuditService(AppDbContext db) : IAuditService
{
    public async Task LogAsync(Guid? userId, string action, string? entityType, Guid? entityId, string? details, string? ipAddress, CancellationToken ct = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            IpAddress = ipAddress
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<Result<PagedResult<AuditLogDto>>> GetLogsAsync(int page, int pageSize, Guid? userId, string? action, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var query = db.AuditLogs.Include(a => a.User).AsQueryable();

        if (userId.HasValue) query = query.Where(a => a.UserId == userId);
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(a => a.Action == action);
        if (from.HasValue) query = query.Where(a => a.CreatedAt >= from);
        if (to.HasValue) query = query.Where(a => a.CreatedAt <= to);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return Result<PagedResult<AuditLogDto>>.Ok(new PagedResult<AuditLogDto>(
            items.Select(i => i.ToDto()).ToList(), total, page, pageSize));
    }

    public async Task<Result<DashboardStatsDto>> GetDashboardStatsAsync(CancellationToken ct = default)
    {
        var stats = new DashboardStatsDto(
            await db.Users.CountAsync(ct),
            await db.Users.CountAsync(u => u.IsActive, ct),
            await db.Organizations.CountAsync(o => o.IsActive, ct),
            await db.Departments.CountAsync(d => d.IsActive, ct));
        return Result<DashboardStatsDto>.Ok(stats);
    }
}
