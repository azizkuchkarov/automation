using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Infrastructure.Hr;

public static class HrBusinessTripSigningPayloadBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    public static string BuildCanonicalJson(HrBusinessTripRequestDetail detail)
    {
        var payload = new SigningPayloadV1(
            V: 1,
            RequestId: detail.DocumentId,
            Number: detail.Document.Number,
            RequestDate: detail.RequestDate.ToString("yyyy-MM-dd"),
            DateFrom: detail.DateFrom.ToString("yyyy-MM-dd"),
            DateTo: detail.DateTo.ToString("yyyy-MM-dd"),
            DaysCount: detail.DaysCount,
            PurposeRu: detail.PurposeRu,
            PurposeEn: detail.PurposeEn,
            PlaceRu: detail.PlaceRu,
            PlaceEn: detail.PlaceEn,
            AuthorName: detail.Document.Author.FullName,
            AuthorPinpp: detail.Document.Author.Pinpp,
            DepartmentName: detail.Document.Department?.Name ?? "",
            OrganizationCode: detail.Document.Organization.Code,
            Travelers: detail.Travelers.OrderBy(t => t.SortOrder).Select(t => new SigningTravelerV1(
                t.FullNameRu,
                t.FullNameEn,
                t.PositionRu,
                t.PositionEn)).ToList(),
            Approvers: detail.Approvers
                .Where(a => a.Status == HrLeaveApproverStatus.Approved)
                .OrderBy(a => a.SortOrder)
                .Select(a => new SigningApproverV1(
                    a.Role.ToString(),
                    a.User?.FullName ?? "",
                    a.DecidedAt?.ToString("O")))
                .ToList());

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    public static string ToBase64(string canonicalJson) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(canonicalJson));

    public static string ComputeSha256Hex(string canonicalJson)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalJson));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private sealed record SigningPayloadV1(
        int V,
        Guid RequestId,
        string Number,
        string RequestDate,
        string DateFrom,
        string DateTo,
        int DaysCount,
        string PurposeRu,
        string? PurposeEn,
        string PlaceRu,
        string? PlaceEn,
        string AuthorName,
        string? AuthorPinpp,
        string DepartmentName,
        string OrganizationCode,
        IReadOnlyList<SigningTravelerV1> Travelers,
        IReadOnlyList<SigningApproverV1> Approvers);

    private sealed record SigningTravelerV1(
        string FullNameRu,
        string? FullNameEn,
        string PositionRu,
        string? PositionEn);

    private sealed record SigningApproverV1(
        string Role,
        string UserName,
        string? DecidedAt);
}
