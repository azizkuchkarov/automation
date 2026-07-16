using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

/// <summary>
/// Tracked IT license / service / equipment item with optional expiry for reminders.
/// </summary>
public class ItAsset
{
    public Guid Id { get; set; }
    public ItAssetCategory Category { get; set; }
    public string NameRu { get; set; } = "";
    public string NameEn { get; set; } = "";
    public string? Quantity { get; set; }
    public string? Term { get; set; }
    public string? BudgetCode { get; set; }
    public decimal? BudgetAmount { get; set; }
    public string? Currency { get; set; }
    public Guid? ResponsibleUserId { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ContractNumber { get; set; }
    public DateTime? ContractDate { get; set; }
    public decimal? Cost { get; set; }
    public ItAssetStatus Status { get; set; } = ItAssetStatus.Active;
    public string? Note { get; set; }
    public int PlanYear { get; set; }
    public DateTime? LastExpiryWarningAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? ResponsibleUser { get; set; }
}
