namespace ATG.Platform.Domain.Entities;

public class Department
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    /// <summary>Russian name in genitive case (родительный падеж), e.g. for «Служебная записка …».</summary>
    public string? NameGenitive { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Organization Organization { get; set; } = null!;
    public Department? Parent { get; set; }
    public ICollection<Department> Children { get; set; } = [];
    public ICollection<User> Users { get; set; } = [];

    public string GetName(string locale) =>
        locale.StartsWith("en", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(NameEn)
            ? NameEn
            : Name;
}
