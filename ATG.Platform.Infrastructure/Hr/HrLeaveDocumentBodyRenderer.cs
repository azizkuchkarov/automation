using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Seeds;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ATG.Platform.Infrastructure.Hr;

/// <summary>Bilingual leave application body (RU/EN columns in header, centered title, body paragraphs).</summary>
public static class HrLeaveDocumentBodyRenderer
{
    private const string BodyFont = "Times New Roman";

    public static void Render(IContainer container, HrLeaveRequestDetail detail, bool showSignedBanner = false, byte[]? qrPng = null)
    {
        var doc = detail.Document;
        var items = detail.Items.OrderBy(i => i.SortOrder).ToList();
        var primaryType = items.FirstOrDefault()?.Type;
        var (titleRu, titleEn) = HrLeaveTextBuilder.BuildDocumentTitlePair(primaryType);
        var header = BuildHeader(detail);
        var documentDate = FormatDocumentDate(detail.RequestDate);
        var registrationNumber = doc.Number;

        container.DefaultTextStyle(x => x.FontFamily(BodyFont).FontSize(11).FontColor(Colors.Black)).Column(col =>
        {
            col.Spacing(0);

            if (showSignedBanner)
            {
                col.Item().PaddingBottom(10).Background(Colors.Green.Lighten4).Border(1).BorderColor(Colors.Green.Lighten2)
                    .PaddingVertical(7).PaddingHorizontal(10).AlignCenter()
                    .Text("Статус: электронно подписан / Electronically signed")
                    .FontFamily(BodyFont).FontSize(9.5f).FontColor(Colors.Green.Darken3);
            }

            col.Item().PaddingBottom(16).Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Spacing(3);
                    left.Item().Text($"Регистрационный номер: {registrationNumber}");
                    left.Item().Text($"Число: {documentDate}");
                });
                row.ConstantItem(12);
                row.RelativeItem().Column(right =>
                {
                    right.Spacing(4);
                    if (qrPng is not null)
                        right.Item().AlignRight().Width(64).Height(64).Image(qrPng);
                    right.Item().AlignRight().Text($"Registration №: {registrationNumber}");
                    right.Item().AlignRight().Text($"Date: {documentDate}");
                });
            });

            col.Item().Row(row =>
            {
                row.RelativeItem().Element(left => RenderHeaderColumn(left, header.RuTo, header.RuFrom));
                row.ConstantItem(16);
                row.RelativeItem().Element(right => RenderHeaderColumn(right, header.EnTo, header.EnFrom));
            });

            col.Item().PaddingTop(22).AlignCenter().Column(title =>
            {
                title.Spacing(2);
                title.Item().Text(titleRu);
                title.Item().Text(titleEn);
            });

            foreach (var item in items)
            {
                var (ru, en) = HrLeaveTextBuilder.BuildItemText(item, detail.PeriodLabel);
                col.Item().PaddingTop(18).Text(ru).Justify();
                col.Item().PaddingTop(10).Text(en).Justify();
            }
        });
    }

    private static void RenderHeaderColumn(IContainer container, string toBlock, string fromBlock)
    {
        container.Column(col =>
        {
            col.Spacing(14);
            col.Item().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontFamily(BodyFont).FontSize(11).FontColor(Colors.Black));
                text.Line(toBlock);
            });
            col.Item().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontFamily(BodyFont).FontSize(11).FontColor(Colors.Black));
                text.Line(fromBlock);
            });
        });
    }

    private static DocumentHeader BuildHeader(HrLeaveRequestDetail detail)
    {
        var doc = detail.Document;
        var author = doc.Author;
        var department = doc.Department;
        var org = doc.Organization;
        var gd = detail.Approvers
            .Where(a => a.Role == HrLeaveApprovalRole.GeneralDirector)
            .Select(a => a.User)
            .FirstOrDefault();

        var (orgRu, orgEn) = ResolveRecipientOrganization(org);
        var (directorRu, directorEn) = ResolveDirectorTitle(org);
        var gdNameRu = gd is null ? "________________" : FormatGdNameRu(gd);
        var gdNameEn = gd is null ? "________________" : FormatGdNameEn(gd);

        var authorName = FormatAuthorName(author, english: false);
        var authorNameEn = FormatAuthorName(author, english: true);
        var jobRu = author.GetJobTitle("ru") ?? author.Position?.Name ?? "";
        var jobEn = author.GetJobTitle("en") ?? author.Position?.Name ?? jobRu;
        var deptRu = department?.Name ?? "";
        var deptEn = department?.GetName("en") ?? deptRu;

        var ruFrom = string.IsNullOrWhiteSpace(jobRu)
            ? $"От: {authorName},"
            : $"От: {authorName},\n{jobRu} {deptRu}".Trim();

        var enFrom = string.IsNullOrWhiteSpace(jobEn)
            ? $"From: {authorNameEn}"
            : $"From: {authorNameEn}\n{jobEn} of {deptEn}".Trim();

        return new DocumentHeader(
            RuTo: $"Кому: {directorRu}\n{orgRu}\n{gdNameRu}",
            EnTo: $"To: {directorEn}\nof {orgEn}\n{gdNameEn}",
            RuFrom: ruFrom,
            EnFrom: enFrom);
    }

    private static string FormatAuthorName(User author, bool english)
    {
        if (english && !string.IsNullOrWhiteSpace(author.FirstNameEn))
            return $"{author.LastNameEn} {author.FirstNameEn}".Trim();

        return $"{author.LastName} {author.FirstName}".Trim();
    }

    private static string FormatGdNameRu(User gd)
    {
        var name = gd.FullName.Trim();
        return name.StartsWith("Г-ну", StringComparison.OrdinalIgnoreCase) ? name : $"Г-ну {name}";
    }

    private static string FormatGdNameEn(User gd)
    {
        var name = string.IsNullOrWhiteSpace(gd.FullNameEn) ? gd.FullName : gd.FullNameEn.Trim();
        return name.StartsWith("Mr.", StringComparison.OrdinalIgnoreCase) ? name : $"Mr. {name}";
    }

    private static (string Ru, string En) ResolveRecipientOrganization(Organization org)
    {
        if (org.Code == BmgmcMasterData.OrganizationCode)
        {
            return (
                "Бухарского ОУМГ",
                "Bukhara Main Gas Management Center");
        }

        return (
            "СП ООО «Asia Trans Gas»",
            "JV «Asia Trans Gas» LLC");
    }

    private static (string Ru, string En) ResolveDirectorTitle(Organization org)
    {
        if (org.Code == BmgmcMasterData.OrganizationCode)
        {
            return (
                "Генеральному менеджеру по эксплуатации",
                "General Manager for operation");
        }

        return (
            "Генеральному Директору",
            "General Director");
    }

    private static string FormatDocumentDate(DateTime value) =>
        value.ToString("dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);

    private sealed record DocumentHeader(string RuTo, string EnTo, string RuFrom, string EnFrom);
}
