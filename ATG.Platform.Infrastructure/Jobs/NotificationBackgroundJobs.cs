using ATG.Platform.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ATG.Platform.Infrastructure.Jobs;

public class NotificationBackgroundJobs(INotificationService notifications, ILogger<NotificationBackgroundJobs> logger)
{
    public async Task CheckPendingApprovalRemindersAsync()
    {
        logger.LogInformation("Running pending approval reminder check");
        await notifications.ProcessApprovalRemindersAsync();
    }
}
