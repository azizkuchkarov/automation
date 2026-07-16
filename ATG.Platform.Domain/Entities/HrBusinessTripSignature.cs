using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

public class HrBusinessTripSignature
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid SignerUserId { get; set; }
    public HrLeaveSignatureKind Kind { get; set; }
    public HrBusinessTripApprovalRole ApproverRole { get; set; }
    public string Pkcs7Base64 { get; set; } = "";
    public string PayloadSha256 { get; set; } = "";
    public string? CertificateSerial { get; set; }
    public string? SignerPinpp { get; set; }
    public string? SignerCn { get; set; }
    public string? SignerTin { get; set; }
    public DateTime SignedAt { get; set; }
    public string? StorageKey { get; set; }

    public HrBusinessTripRequestDetail Request { get; set; } = null!;
    public User Signer { get; set; } = null!;
}
