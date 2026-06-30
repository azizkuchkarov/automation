using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Application.DTOs;

public record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string? Body,
    string? EntityType,
    Guid? EntityId,
    string? ActionUrl,
    bool IsRead,
    DateTime CreatedAt);

public record NotificationUnreadCountDto(int Count);
