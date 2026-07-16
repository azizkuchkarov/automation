using ATG.Platform.Domain.Entities;

namespace ATG.Platform.Infrastructure.Hr;

/// <summary>
/// Builds structured data for the formal bilingual business trip order (приказ).
/// </summary>
public static class HrBusinessTripOrderBodyBuilder
{
    public static HrBusinessTripOrderDocumentModel BuildModel(
        IReadOnlyList<HrBusinessTripRequestDetail> details,
        DateTime orderDate,
        string orderNumber)
    {
        var basisRows = new List<BilingualRow>();
        var sections = new List<HrBusinessTripOrderMemoSection>();
        var sectionNum = 1;

        foreach (var detail in details)
        {
            sections.Add(BuildMemoSection(detail, sectionNum));
            sectionNum++;

            var dept = detail.Document.Department;
            var deptEn = dept?.GetName("en") ?? dept?.Name ?? "";
            var deptRuGenitive = GetDepartmentGenitive(dept);
            var memoDateEn = detail.RequestDate.ToString("dd.MM.yyyy");
            var memoDateRu = detail.RequestDate.ToString("dd.MM.yyyy") + " г.";
            basisRows.Add(new BilingualRow(
                $"Basis: Memo of {deptEn} dated {memoDateEn}.",
                $"Основание: Служебная записка ({deptRuGenitive}) от {memoDateRu}"));
        }

        string? purposeIntroEn = null;
        string? purposeIntroRu = null;
        if (details.Count == 1)
        {
            var only = details[0];
            var purposeEn = string.IsNullOrWhiteSpace(only.PurposeEn) ? only.PurposeRu : only.PurposeEn;
            purposeIntroEn = $"Due to production necessity, with the purpose of {purposeEn},";
            purposeIntroRu = $"В связи с производственной необходимостью, с целью {only.PurposeRu},";
        }

        return new HrBusinessTripOrderDocumentModel(
            orderNumber,
            orderDate,
            purposeIntroEn,
            purposeIntroRu,
            sections,
            basisRows,
            details.Count + 1,
            details.Count + 2);
    }

    public static HrBusinessTripOrderDocumentModel BuildModel(
        IReadOnlyList<HrBusinessTripRequestDetail> details,
        DateTime orderDate) =>
        BuildModel(details, orderDate, details[0].OrderNumber ?? "HBO-0000-000");

    private static HrBusinessTripOrderMemoSection BuildMemoSection(HrBusinessTripRequestDetail detail, int sectionNum)
    {
        var travelers = detail.Travelers.OrderBy(t => t.SortOrder).Select(t => new HrBusinessTripOrderTravelerLine(
            HrBusinessTripTextBuilder.BuildTravelerLineEn(
                t.FullNameEn, t.PositionEn, t.FullNameRu, t.PositionRu),
            HrBusinessTripTextBuilder.BuildTravelerLineRu(t.FullNameRu, t.PositionRu))).ToList();

        return new HrBusinessTripOrderMemoSection(
            sectionNum,
            string.IsNullOrWhiteSpace(detail.PurposeEn) ? detail.PurposeRu : detail.PurposeEn,
            detail.PurposeRu,
            string.IsNullOrWhiteSpace(detail.PlaceEn) ? detail.PlaceRu : detail.PlaceEn,
            detail.PlaceRu,
            detail.DateFrom,
            detail.DateTo,
            detail.DaysCount,
            travelers);
    }

    public static string GetDepartmentGenitive(Department? department)
    {
        if (department is null) return "";
        if (!string.IsNullOrWhiteSpace(department.NameGenitive))
            return department.NameGenitive.Trim();
        return department.Name.Trim();
    }
}
