using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

/// <summary>
/// Admin-configured manager for a procurement workflow area.
/// Manager assigns engineers from <see cref="EngineerDepartmentId"/>.
/// </summary>
public class ProcurementWorkflowRoleAssignment
{
    public ProcurementWorkflowRoleKey RoleKey { get; set; }
    public Guid? ManagerUserId { get; set; }
    public Guid? EngineerDepartmentId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? ManagerUser { get; set; }
    public Department? EngineerDepartment { get; set; }
}
