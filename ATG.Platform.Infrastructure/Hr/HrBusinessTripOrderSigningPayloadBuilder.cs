using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ATG.Platform.Domain.Entities;

namespace ATG.Platform.Infrastructure.Hr;

public static class HrBusinessTripOrderSigningPayloadBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    public static string BuildCanonicalJson(
        string orderNumber,
        DateTime orderIssuedAt,
        IReadOnlyList<HrBusinessTripRequestDetail> memoranda)
    {
        var payload = new OrderSigningPayloadV1(
            V: 1,
            Kind: "business_trip_order",
            OrderNumber: orderNumber,
            OrderDate: orderIssuedAt.ToString("yyyy-MM-dd"),
            OrganizationCode: memoranda[0].Document.Organization.Code,
            Memoranda: memoranda.Select(d => new OrderMemoV1(
                d.DocumentId,
                d.Document.Number,
                d.RequestDate.ToString("yyyy-MM-dd"),
                d.Document.Department?.Name ?? "",
                d.PurposeRu,
                d.PlaceRu,
                d.DateFrom.ToString("yyyy-MM-dd"),
                d.DateTo.ToString("yyyy-MM-dd"),
                d.DaysCount,
                d.Travelers.OrderBy(t => t.SortOrder).Select(t => t.FullNameRu).ToList())).ToList());

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    public static string ToBase64(string canonicalJson) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(canonicalJson));

    public static string ComputeSha256Hex(string canonicalJson)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalJson));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private sealed record OrderSigningPayloadV1(
        int V,
        string Kind,
        string OrderNumber,
        string OrderDate,
        string OrganizationCode,
        IReadOnlyList<OrderMemoV1> Memoranda);

    private sealed record OrderMemoV1(
        Guid RequestId,
        string Number,
        string RequestDate,
        string DepartmentName,
        string PurposeRu,
        string PlaceRu,
        string DateFrom,
        string DateTo,
        int DaysCount,
        IReadOnlyList<string> TravelerNames);
}
