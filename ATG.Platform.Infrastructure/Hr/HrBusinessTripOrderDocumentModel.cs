using ATG.Platform.Domain.Entities;

namespace ATG.Platform.Infrastructure.Hr;

public record BilingualRow(string En, string Ru);

public record HrBusinessTripOrderDocumentModel(
    string OrderNumber,
    DateTime OrderDate,
    string? PurposeIntroEn,
    string? PurposeIntroRu,
    IReadOnlyList<HrBusinessTripOrderMemoSection> Memoranda,
    IReadOnlyList<BilingualRow> BasisRows,
    int AccountingSectionNum,
    int ReportSectionNum,
    string? VerificationUrl = null);

public record HrBusinessTripOrderMemoSection(
    int SectionNum,
    string PurposeEn,
    string PurposeRu,
    string PlaceEn,
    string PlaceRu,
    DateTime DateFrom,
    DateTime DateTo,
    int DaysCount,
    IReadOnlyList<HrBusinessTripOrderTravelerLine> Travelers);

public record HrBusinessTripOrderTravelerLine(
    string LineEn,
    string LineRu);
