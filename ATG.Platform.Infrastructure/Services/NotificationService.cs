using ATG.Platform.Application.Common;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using ATG.Platform.Application.Options;
using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ATG.Platform.Infrastructure.Services;

public class NotificationService(
    AppDbContext db,
    INotificationPublisher publisher,
    IOptions<NotificationOptions> options,
    ILogger<NotificationService> logger) : INotificationService
{
    private static readonly ProcurementApproverRole[] ApproverRoleOrder =
    [
        ProcurementApproverRole.Initiator,
        ProcurementApproverRole.TasManager,
        ProcurementApproverRole.BmgmcTopManager,
        ProcurementApproverRole.SectionHead,
        ProcurementApproverRole.TopManager,
    ];

    public Task NotifyDcsApprovalRequiredAsync(
        Guid recipientId, string documentNumber, string documentTitle, Guid documentId, CancellationToken ct = default) =>
        NotifyLocalizedAsync(
            recipientId,
            NotificationType.DcsApprovalRequired,
            $"Approval required: {documentNumber}",
            $"Требуется согласование: {documentNumber}",
            documentTitle,
            "Document",
            documentId,
            $"/automation/documents/{documentId}",
            ct);

    public Task NotifyMarketingPlanApprovalRequiredAsync(
        Guid recipientId, string documentNumber, Guid documentId, CancellationToken ct = default) =>
        NotifyLocalizedAsync(
            recipientId,
            NotificationType.MarketingPlanApprovalRequired,
            $"Marketing plan approval: {documentNumber}",
            $"Согласование плана закупки: {documentNumber}",
            null,
            "Document",
            documentId,
            $"/automation/documents/{documentId}",
            ct);

    public async Task NotifyTaskAssignedAsync(
        Guid recipientId, string taskNumber, string taskTitle, Guid taskId,
        TaskSource source, Guid? externalId, CancellationToken ct = default)
    {
        var actionUrl = source == TaskSource.DCS && externalId is not null
            ? $"/automation/documents/{externalId}"
            : "/tasks/list";

        await NotifyLocalizedAsync(
            recipientId,
            NotificationType.TaskAssigned,
            $"New task: {taskNumber}",
            $"Новая задача: {taskNumber}",
            taskTitle,
            "WorkTask",
            taskId,
            actionUrl,
            ct);
    }

    public Task NotifyTicketAssignedAsync(
        Guid recipientId, string ticketNumber, string ticketTitle, Guid ticketId, CancellationToken ct = default) =>
        NotifyLocalizedAsync(
            recipientId,
            NotificationType.TicketAssigned,
            $"Ticket assigned: {ticketNumber}",
            $"Тикет назначен: {ticketNumber}",
            ticketTitle,
            "Ticket",
            ticketId,
            $"/helpdesk/tickets/{ticketId}",
            ct);

    public Task NotifyDcsApprovalRejectedAsync(
        Guid recipientId, string documentNumber, Guid documentId, CancellationToken ct = default) =>
        NotifyLocalizedAsync(
            recipientId,
            NotificationType.DcsApprovalRejected,
            $"Request rejected: {documentNumber}",
            $"Заявка отклонена: {documentNumber}",
            null,
            "Document",
            documentId,
            $"/automation/documents/{documentId}",
            ct);

    public async Task ProcessApprovalRemindersAsync(CancellationToken ct = default)
    {
        var opts = options.Value;
        var staleBefore = DateTime.UtcNow.AddDays(-opts.ApprovalReminderAfterDays);
        var cooldownAfter = DateTime.UtcNow.AddHours(-opts.ApprovalReminderCooldownHours);

        var dcsPending = await db.ProcurementRequestDetails.AsNoTracking()
            .Include(d => d.Document)
            .Include(d => d.Approvers)
            .Where(d => d.Phase == ProcurementRequestPhase.AwaitingApproval && d.Document.UpdatedAt <= staleBefore)
            .ToListAsync(ct);

        foreach (var detail in dcsPending)
        {
            var approver = detail.Approvers
                .Where(a => a.Status == ProcurementApproverStatus.Pending)
                .OrderBy(a => Array.IndexOf(ApproverRoleOrder, a.Role))
                .FirstOrDefault();
            if (approver is null) continue;

            if (await WasRemindedRecentlyAsync(
                    approver.UserId, detail.DocumentId, NotificationType.DcsApprovalReminder, cooldownAfter, ct))
                continue;

            await NotifyLocalizedAsync(
                approver.UserId,
                NotificationType.DcsApprovalReminder,
                $"Reminder: approve {detail.Document.Number}",
                $"Напоминание: согласуйте {detail.Document.Number}",
                detail.Document.Title,
                "Document",
                detail.DocumentId,
                $"/automation/documents/{detail.DocumentId}",
                ct);
        }

        var planPending = await db.ProcurementRequestDetails.AsNoTracking()
            .Include(d => d.Document)
            .Include(d => d.MarketingPlanApprovers)
            .Where(d => d.Phase == ProcurementRequestPhase.Marketing
                && d.MarketingCurrentStep == 8
                && d.MarketingPlanApprovalSubmittedAt != null
                && d.Document.UpdatedAt <= staleBefore)
            .ToListAsync(ct);

        foreach (var detail in planPending)
        {
            var approver = detail.MarketingPlanApprovers
                .Where(a => a.Status == ProcurementApproverStatus.Pending)
                .OrderBy(a => a.SortOrder)
                .FirstOrDefault();
            if (approver is null) continue;

            if (await WasRemindedRecentlyAsync(
                    approver.UserId, detail.DocumentId, NotificationType.MarketingPlanApprovalReminder, cooldownAfter, ct))
                continue;

            await NotifyLocalizedAsync(
                approver.UserId,
                NotificationType.MarketingPlanApprovalReminder,
                $"Reminder: marketing plan {detail.Document.Number}",
                $"Напоминание: план закупки {detail.Document.Number}",
                detail.Document.Title,
                "Document",
                detail.DocumentId,
                $"/automation/documents/{detail.DocumentId}",
                ct);
        }
    }

    public async Task<Result<PagedResult<NotificationDto>>> GetInboxAsync(
        Guid actorId, bool unreadOnly, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.UserNotifications.AsNoTracking().Where(n => n.UserId == actorId);
        if (unreadOnly) query = query.Where(n => !n.IsRead);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto(
                n.Id, n.Type, n.Title, n.Body, n.EntityType, n.EntityId, n.ActionUrl, n.IsRead, n.CreatedAt))
            .ToListAsync(ct);

        return Result<PagedResult<NotificationDto>>.Ok(new PagedResult<NotificationDto>(items, total, page, pageSize));
    }

    public async Task<Result<NotificationUnreadCountDto>> GetUnreadCountAsync(Guid actorId, CancellationToken ct = default)
    {
        var count = await db.UserNotifications.CountAsync(n => n.UserId == actorId && !n.IsRead, ct);
        return Result<NotificationUnreadCountDto>.Ok(new NotificationUnreadCountDto(count));
    }

    public async Task<Result<bool>> MarkReadAsync(Guid id, Guid actorId, CancellationToken ct = default)
    {
        var notification = await db.UserNotifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == actorId, ct);
        if (notification is null) return Result<bool>.Fail("Notification not found");

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await db.SaveChangesAsync(ct);
            await PushUnreadCountAsync(actorId, ct);
        }

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> MarkAllReadAsync(Guid actorId, CancellationToken ct = default)
    {
        await db.UserNotifications
            .Where(n => n.UserId == actorId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
        await PushUnreadCountAsync(actorId, ct);
        return Result<bool>.Ok(true);
    }

    private async Task NotifyLocalizedAsync(
        Guid recipientId,
        NotificationType type,
        string titleEn,
        string titleRu,
        string? body,
        string? entityType,
        Guid? entityId,
        string? actionUrl,
        CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == recipientId && u.IsActive, ct);
        if (user is null) return;

        var useEn = user.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        var title = useEn ? titleEn : titleRu;
        await CreateAndPushAsync(recipientId, type, title, body, entityType, entityId, actionUrl, ct);
    }

    private async Task<bool> WasRemindedRecentlyAsync(
        Guid userId, Guid entityId, NotificationType type, DateTime since, CancellationToken ct) =>
        await db.UserNotifications.AsNoTracking().AnyAsync(n =>
            n.UserId == userId
            && n.EntityId == entityId
            && n.Type == type
            && n.CreatedAt >= since, ct);

    private async Task CreateAndPushAsync(
        Guid userId,
        NotificationType type,
        string title,
        string? body,
        string? entityType,
        Guid? entityId,
        string? actionUrl,
        CancellationToken ct)
    {
        try
        {
            var notification = new UserNotification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = type,
                Title = title,
                Body = body,
                EntityType = entityType,
                EntityId = entityId,
                ActionUrl = actionUrl,
            };
            db.UserNotifications.Add(notification);
            await db.SaveChangesAsync(ct);

            var unread = await db.UserNotifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);
            var dto = new NotificationDto(
                notification.Id, notification.Type, notification.Title, notification.Body,
                notification.EntityType, notification.EntityId, notification.ActionUrl,
                notification.IsRead, notification.CreatedAt);
            await publisher.PublishAsync(userId, dto, unread, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to create notification for user {UserId}", userId);
        }
    }

    private async Task PushUnreadCountAsync(Guid userId, CancellationToken ct)
    {
        try
        {
            var unread = await db.UserNotifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);
            await publisher.PublishUnreadCountAsync(userId, unread, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to push unread count for user {UserId}", userId);
        }
    }
}
