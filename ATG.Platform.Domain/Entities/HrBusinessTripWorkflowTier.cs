namespace ATG.Platform.Domain.Entities;

public class HrBusinessTripWorkflowTier
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public string TierKey { get; set; } = "";
    public string TitleRu { get; set; } = "";
    public string TitleEn { get; set; } = "";
    /// <summary>Higher value = matched before lower tiers.</summary>
    public int MatchPriority { get; set; }
    public bool CatchAllStaff { get; set; }
    /// <summary>Contracts dept: prepend section manager based on author's subsection.</summary>
    public bool PrependsSectionManager { get; set; }

    public HrBusinessTripDeptWorkflow Workflow { get; set; } = null!;
    public ICollection<HrBusinessTripWorkflowInitiator> Initiators { get; set; } = [];
    public ICollection<HrBusinessTripWorkflowStep> Steps { get; set; } = [];
}
