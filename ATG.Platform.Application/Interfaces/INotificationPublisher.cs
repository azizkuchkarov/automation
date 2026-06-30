using ATG.Platform.Application.DTOs;

namespace ATG.Platform.Application.Interfaces;

public interface INotificationPublisher
{
    Task PublishAsync(Guid userId, NotificationDto notification, int unreadCount, CancellationToken ct = default);
    Task PublishUnreadCountAsync(Guid userId, int unreadCount, CancellationToken ct = default);
}
