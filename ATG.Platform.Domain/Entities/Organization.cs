using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

public class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public OrgType OrgType { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Organization? Parent { get; set; }
    public ICollection<Organization> Children { get; set; } = [];
    public ICollection<Department> Departments { get; set; } = [];
    public ICollection<User> Users { get; set; } = [];
}
