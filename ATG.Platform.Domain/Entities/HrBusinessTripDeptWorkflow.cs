namespace ATG.Platform.Domain.Entities;

/// <summary>
/// Per-department business trip approval workflow for an organization (Tashkent HO).
/// </summary>
public class HrBusinessTripDeptWorkflow
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string DepartmentCode { get; set; } = "";
    public string TitleRu { get; set; } = "";
    public string TitleEn { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Organization Organization { get; set; } = null!;
    public ICollection<HrBusinessTripWorkflowTier> Tiers { get; set; } = [];
}
