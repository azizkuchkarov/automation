using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

/// <summary>
/// Admin-configured default responsible user for an IT Automation category.
/// </summary>
public class ItAutomationRoleAssignment
{
    public ItAssetCategory Category { get; set; }
    public Guid? ResponsibleUserId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? ResponsibleUser { get; set; }
}
