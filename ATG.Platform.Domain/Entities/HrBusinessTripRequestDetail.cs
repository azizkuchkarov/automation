using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

public class HrBusinessTripRequestDetail
{
    public Guid DocumentId { get; set; }
    public HrBusinessTripPhase Phase { get; set; } = HrBusinessTripPhase.Draft;
    public DateTime RequestDate { get; set; }
    public string PurposeRu { get; set; } = "";
    public string? PurposeEn { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int DaysCount { get; set; }
    public string PlaceRu { get; set; } = "";
    public string? PlaceEn { get; set; }
    public string? PdfStorageKey { get; set; }
    public string? PdfSignedStorageKey { get; set; }
    public string? PdfPresentationStorageKey { get; set; }
    public string? SigningPayloadHash { get; set; }
    public DateTime? EimzoCompletedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? OrderNumber { get; set; }
    public DateTime? OrderIssuedAt { get; set; }
    public string? OrderDocxStorageKey { get; set; }

    public Document Document { get; set; } = null!;
    public ICollection<HrBusinessTripTraveler> Travelers { get; set; } = [];
    public ICollection<HrBusinessTripApprover> Approvers { get; set; } = [];
    public ICollection<HrBusinessTripSignature> Signatures { get; set; } = [];
}
