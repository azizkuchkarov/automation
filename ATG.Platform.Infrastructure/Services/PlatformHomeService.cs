using ATG.Platform.Application.Common;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ATG.Platform.Infrastructure.Services;

public class PlatformHomeService(AppDbContext db) : IPlatformHomeService
{
    private const int HrParallelGroup = 0;
    private const int SequentialGroup = 1;

    private static readonly TicketStatus[] AssigneeActionStatuses =
    [
        TicketStatus.Open,
        TicketStatus.Assigned,
        TicketStatus.Accepted,
        TicketStatus.InProgress,
    ];

    public async Task<Result<HomeModuleCountsDto>> GetModuleCountsAsync(Guid actorId, CancellationToken ct = default)
    {
        var actor = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == actorId && u.IsActive, ct);
        if (actor is null)
            return Result<HomeModuleCountsDto>.Fail("User not found");

        var hr = await CountHrPendingAsync(actorId, ct);
        var helpDesk = await CountHelpDeskPendingAsync(actor, ct);
        var automation = await CountPendingWorkTasksAsync(actorId, TaskSource.DCS, ct);
        var tasks = await CountPendingWorkTasksAsync(actorId, TaskSource.Manual, ct);
        var itAutomation = await CountItAutomationExpiringAsync(actorId, ct);

        return Result<HomeModuleCountsDto>.Ok(new HomeModuleCountsDto(
            Admin: 0,
            Automation: automation,
            ItAutomation: itAutomation,
            HelpDesk: helpDesk,
            Hr: hr,
            Tasks: tasks));
    }

    private async Task<int> CountItAutomationExpiringAsync(Guid actorId, CancellationToken ct)
    {
        var now = DateTime.UtcNow.Date;
        var warnUntil = now.AddMonths(3);
        var roleCats = await db.ItAutomationRoleAssignments.AsNoTracking()
            .Where(r => r.ResponsibleUserId == actorId)
            .Select(r => r.Category)
            .ToListAsync(ct);

        return await db.ItAssets.AsNoTracking()
            .Where(a => a.ExpiresAt != null
                && a.ExpiresAt.Value.Date >= now
                && a.ExpiresAt.Value.Date <= warnUntil
                && a.Status != ItAssetStatus.Cancelled
                && a.Status != ItAssetStatus.Suspended
                && (a.ResponsibleUserId == actorId
                    || (a.ResponsibleUserId == null && roleCats.Contains(a.Category))))
            .CountAsync(ct);
    }

    private async Task<int> CountHrPendingAsync(Guid actorId, CancellationToken ct) =>
        await db.HrLeaveRequestDetails.AsNoTracking()
            .Where(d => d.Phase == HrLeaveRequestPhase.HrReview || d.Phase == HrLeaveRequestPhase.AwaitingApproval)
            .Where(d => d.Approvers.Any(a =>
                a.UserId == actorId
                && a.Status == HrLeaveApproverStatus.Pending
                && (
                    (d.Phase == HrLeaveRequestPhase.HrReview && a.ApprovalGroup == HrParallelGroup)
                    || (d.Phase == HrLeaveRequestPhase.AwaitingApproval
                        && a.ApprovalGroup == SequentialGroup
                        && !d.Approvers.Any(b =>
                            b.ApprovalGroup == SequentialGroup
                            && b.Status == HrLeaveApproverStatus.Pending
                            && b.SortOrder < a.SortOrder)))))
            .CountAsync(ct);

    private async Task<int> CountHelpDeskPendingAsync(User actor, CancellationToken ct)
    {
        var query = db.Tickets.AsNoTracking()
            .Where(t => t.Status != TicketStatus.Closed
                && t.Status != TicketStatus.Cancelled
                && t.Status != TicketStatus.Done);

        if (actor.DepartmentId is Guid deptId)
        {
            return await query.CountAsync(t =>
                (t.AssigneeId == actor.Id && AssigneeActionStatuses.Contains(t.Status))
                || (t.TargetDepartmentId == deptId && t.Status == TicketStatus.Open), ct);
        }

        return await query.CountAsync(t =>
            t.AssigneeId == actor.Id && AssigneeActionStatuses.Contains(t.Status), ct);
    }

    private Task<int> CountPendingWorkTasksAsync(Guid actorId, TaskSource source, CancellationToken ct) =>
        db.WorkTasks.AsNoTracking().CountAsync(t =>
            t.AssigneeId == actorId
            && t.Source == source
            && (t.Status == WorkTaskStatus.New || t.Status == WorkTaskStatus.InProgress), ct);
}
