using ATG.Platform.API.Hubs;
using ATG.Platform.Application.DTOs;
using ATG.Platform.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace ATG.Platform.API.Services;

public class NotificationPublisher(IHubContext<NotificationHub> hub) : INotificationPublisher
{
    public Task PublishAsync(Guid userId, NotificationDto notification, int unreadCount, CancellationToken ct = default) =>
        hub.Clients.Group(NotificationHub.UserGroup(userId))
            .SendAsync("notification", notification, unreadCount, ct);

    public Task PublishUnreadCountAsync(Guid userId, int unreadCount, CancellationToken ct = default) =>
        hub.Clients.Group(NotificationHub.UserGroup(userId))
            .SendAsync("unreadCount", unreadCount, ct);
}
