using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Infrastructure.Hr;

public static class HrLeaveSigningPayloadBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    public static string BuildCanonicalJson(HrLeaveRequestDetail detail)
    {
        var payload = new SigningPayloadV1(
            V: 1,
            RequestId: detail.DocumentId,
            Number: detail.Document.Number,
            PeriodLabel: detail.PeriodLabel,
            RequestDate: detail.RequestDate.ToString("yyyy-MM-dd"),
            AuthorName: detail.Document.Author.FullName,
            AuthorPinpp: detail.Document.Author.Pinpp,
            DepartmentName: detail.Document.Department.Name,
            OrganizationCode: detail.Document.Organization.Code,
            Items: detail.Items.OrderBy(i => i.SortOrder).Select(i => new SigningItemV1(
                i.Type.ToString(),
                i.DateFrom?.ToString("yyyy-MM-dd"),
                i.DateTo?.ToString("yyyy-MM-dd"),
                i.DaysCount,
                HrLeaveTextBuilder.BuildItemText(i, detail.PeriodLabel).Ru,
                HrLeaveTextBuilder.BuildItemText(i, detail.PeriodLabel).En)).ToList(),
            Approvers: detail.Approvers
                .Where(a => a.Status == HrLeaveApproverStatus.Approved)
                .OrderBy(a => a.ApprovalGroup).ThenBy(a => a.SortOrder)
                .Select(a => new SigningApproverV1(
                    a.Role.ToString(),
                    a.User.FullName,
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
        string PeriodLabel,
        string RequestDate,
        string AuthorName,
        string? AuthorPinpp,
        string DepartmentName,
        string OrganizationCode,
        IReadOnlyList<SigningItemV1> Items,
        IReadOnlyList<SigningApproverV1> Approvers);

    private sealed record SigningItemV1(
        string Type,
        string? DateFrom,
        string? DateTo,
        int? DaysCount,
        string TextRu,
        string TextEn);

    private sealed record SigningApproverV1(
        string Role,
        string UserName,
        string? DecidedAt);
}
