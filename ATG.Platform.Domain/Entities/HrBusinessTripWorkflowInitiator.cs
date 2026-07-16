namespace ATG.Platform.Domain.Entities;

public class HrBusinessTripWorkflowInitiator
{
    public Guid Id { get; set; }
    public Guid TierId { get; set; }
    public Guid UserId { get; set; }

    public HrBusinessTripWorkflowTier Tier { get; set; } = null!;
    public User User { get; set; } = null!;
}
