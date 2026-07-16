using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Application.DTOs;

public record HrBusinessTripTravelerDto(
    Guid Id,
    string FullNameRu,
    string? FullNameEn,
    string PositionRu,
    string? PositionEn,
    int SortOrder,
    string DisplayRu,
    string DisplayEn,
    string? CertificateNumber,
    bool HasCertificate,
    DateTime? CertificateDeliveredAt,
    Guid? UserId);

public record HrBusinessTripApproverDto(
    Guid Id,
    Guid UserId,
    string UserName,
    string? PositionRu,
    string? PositionEn,
    HrBusinessTripApprovalRole Role,
    HrLeaveApproverStatus Status,
    int SortOrder,
    DateTime? DecidedAt,
    string? Comment);

public record HrBusinessTripTimelineEventDto(
    Guid Id,
    string Action,
    string ActorName,
    string? Details,
    DateTime CreatedAt);

public record HrBusinessTripPermissionsDto(
    bool CanCreate,
    bool CanEdit,
    bool CanSubmit,
    bool CanHrReview,
    bool CanApprove,
    bool CanEimzoApprove,
    bool CanIssueOrder,
    bool CanGenerateCertificates,
    bool CanDeliverCertificates,
    bool CanReject);

public record HrBusinessTripSignatureDto(
    Guid Id,
    string Kind,
    string SignerName,
    string? SignerPinpp,
    DateTime SignedAt,
    string? CertificateSerial);

public record HrBusinessTripSigningPackageDto(
    string JsonBase64,
    string PdfBase64,
    string PayloadSha256,
    string Number);

public record HrBusinessTripRequestDto(
    Guid Id,
    string Number,
    DocumentStatus Status,
    HrBusinessTripPhase Phase,
    DateTime RequestDate,
    string PurposeRu,
    string? PurposeEn,
    DateTime DateFrom,
    DateTime DateTo,
    int DaysCount,
    string PlaceRu,
    string? PlaceEn,
    string AuthorName,
    string DepartmentName,
    string DepartmentNameEn,
    string OrganizationName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? OrderNumber,
    DateTime? OrderIssuedAt,
    bool HasMemoPdf,
    bool HasOrderPdf,
    bool HasOrderSigned,
    bool HasCertificates,
    bool AllCertificatesDelivered,
    bool IsTravelerView,
    Guid? MyTravelerId,
    IReadOnlyList<HrBusinessTripTravelerDto> Travelers,
    IReadOnlyList<HrBusinessTripApproverDto> Approvers,
    IReadOnlyList<HrBusinessTripTimelineEventDto> Timeline,
    IReadOnlyList<HrBusinessTripSignatureDto> Signatures,
    HrBusinessTripPermissionsDto Permissions);

public record HrBusinessTripListItemDto(
    Guid Id,
    string Number,
    HrBusinessTripPhase Phase,
    string DepartmentName,
    string DepartmentNameEn,
    DateTime RequestDate,
    DateTime DateFrom,
    DateTime DateTo,
    string PlaceRu,
    int TravelerCount,
    DateTime CreatedAt,
    bool HasMyCertificate);

public record CreateHrBusinessTripTravelerRequest(
    string FullNameRu,
    string? FullNameEn,
    string PositionRu,
    string? PositionEn,
    Guid? UserId = null);

public record CreateHrBusinessTripRequestRequest(
    DateTime RequestDate,
    string PurposeRu,
    string? PurposeEn,
    DateTime DateFrom,
    DateTime DateTo,
    string PlaceRu,
    string? PlaceEn,
    IReadOnlyList<CreateHrBusinessTripTravelerRequest> Travelers);

public record UpdateHrBusinessTripRequestRequest(
    DateTime RequestDate,
    string PurposeRu,
    string? PurposeEn,
    DateTime DateFrom,
    DateTime DateTo,
    string PlaceRu,
    string? PlaceEn,
    IReadOnlyList<CreateHrBusinessTripTravelerRequest> Travelers);

public record HrBusinessTripApprovalRequest(string? Comment, string? JsonPkcs7 = null, string? PdfPkcs7 = null);

public record IssueHrBusinessTripOrderRequest(IReadOnlyList<Guid> RequestIds);

public record HrBusinessTripOrderResultDto(
    string OrderNumber,
    DateTime OrderIssuedAt,
    IReadOnlyList<Guid> RequestIds);

public record HrBusinessTripColleagueDto(
    Guid Id,
    string FullNameRu,
    string? FullNameEn,
    string PositionRu,
    string? PositionEn);
