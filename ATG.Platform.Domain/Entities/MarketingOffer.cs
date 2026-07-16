using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

public class MarketingOffer
{
    public Guid Id { get; set; }
    public Guid MarketingRecordId { get; set; }
    public string CompanyName { get; set; } = "";
    public decimal? OfferAmount { get; set; }
    public string Currency { get; set; } = "UZS";
    public bool VatIncluded { get; set; }
    public bool DeliveryIncluded { get; set; }
    public string? WarrantyTerms { get; set; }
    public DateOnly? OfferDate { get; set; }
    public DateOnly? OfferValidityDate { get; set; }
    public string? ContactInfo { get; set; }
    public bool? MeetsTzRequirements { get; set; }
    public string? RejectionReason { get; set; }
    public bool IsAffiliated { get; set; }
    public string? AffiliationNote { get; set; }
    public MarketingOfferSource Source { get; set; } = MarketingOfferSource.Manual;
    public string? AttachmentKey { get; set; }
    public MarketingInitiatorReviewStatus InitiatorReviewStatus { get; set; } = MarketingInitiatorReviewStatus.Pending;
    public Guid? InitiatorReviewedById { get; set; }
    public DateTime? InitiatorReviewedAt { get; set; }
    public string? InitiatorReviewComment { get; set; }
    public MarketingInitiatorReviewStatus EngineerReviewStatus { get; set; } = MarketingInitiatorReviewStatus.Pending;
    public Guid? EngineerReviewedById { get; set; }
    public DateTime? EngineerReviewedAt { get; set; }
    public string? EngineerReviewComment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public MarketingRecord Record { get; set; } = null!;
}
