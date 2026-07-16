namespace ATG.Platform.Domain.Entities;

public class HrBusinessTripTraveler
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string FullNameRu { get; set; } = "";
    public string? FullNameEn { get; set; }
    public string PositionRu { get; set; } = "";
    public string? PositionEn { get; set; }
    public int SortOrder { get; set; }
    public Guid? UserId { get; set; }
    public string? CertificateNumber { get; set; }
    public string? CertificateStorageKey { get; set; }
    public DateTime? CertificateDeliveredAt { get; set; }

    public HrBusinessTripRequestDetail Request { get; set; } = null!;
}
