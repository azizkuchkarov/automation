namespace ATG.Platform.Application.Options;

public class NotificationOptions
{
    public const string SectionName = "Notifications";

    public int ApprovalReminderAfterDays { get; set; } = 2;
    public int ApprovalReminderCooldownHours { get; set; } = 24;
}
