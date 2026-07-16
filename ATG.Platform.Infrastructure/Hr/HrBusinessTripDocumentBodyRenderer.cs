using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Seeds;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ATG.Platform.Infrastructure.Hr;

public static class HrBusinessTripDocumentBodyRenderer
{
    private const string BodyFont = HrBusinessTripMemoLayout.BodyFont;
    private const float BodyFontSize = HrBusinessTripMemoLayout.BodyFontSize;
    private const float LabelWidth = 80f;

    public static void Render(
        IContainer container,
        HrBusinessTripRequestDetail detail,
        IReadOnlyDictionary<Guid, User>? approverUsers = null)
    {
        var department = detail.Document.Department;
        var org = detail.Document.Organization;
        var deptRu = string.IsNullOrWhiteSpace(department?.Name)
            ? "Название департамента"
            : department!.Name.Trim();
        var deptEn = string.IsNullOrWhiteSpace(department?.GetName("en"))
            ? "Department name"
            : department!.GetName("en")!.Trim();
        var (orgRu, orgEn) = ResolveOrganization(org);
        var (directorRu, directorEn, gdNameRu, gdNameEn) = ResolveRecipient();

        container.DefaultTextStyle(x => x
            .FontFamily(BodyFont)
            .FontSize(BodyFontSize)
            .FontColor(Colors.Black)
            .LineHeight(1.35f)).Column(col =>
        {
            col.Spacing(0);

            // Кому / To — 3 lines, RU / EN on the same line
            RenderAddressBlock(col, "Кому / To:",
            [
                $"{directorRu}/ {directorEn}",
                $"{orgRu} /{orgEn}",
                $"{gdNameRu} / {gdNameEn}",
            ]);

            col.Item().PaddingTop(14);
            // От / From — 2 lines, RU / EN on the same line
            RenderAddressBlock(col, "От / From:",
            [
                $"{deptRu} / {deptEn}",
                $"{orgRu} /{orgEn}",
            ]);

            // Centered title
            col.Item().PaddingTop(22).PaddingBottom(16).AlignCenter().Column(title =>
            {
                title.Spacing(2);
                title.Item().AlignCenter().Text("СЛУЖЕБНАЯ ЗАПИСКА")
                    .FontFamily(BodyFont).FontSize(BodyFontSize).SemiBold();
                title.Item().AlignCenter().Text("APPLICATION")
                    .FontFamily(BodyFont).FontSize(BodyFontSize).SemiBold();
            });

            // Travelers request block — bordered, professional
            col.Item().Border(0.75f).BorderColor(Colors.Black).Padding(10).Column(box =>
            {
                box.Item().AlignCenter().Text(
                        "Прошу Вашего разрешения на командирование следующих лиц /")
                    .FontFamily(BodyFont).FontSize(BodyFontSize);
                box.Item().PaddingTop(2).AlignCenter().Text(
                        "Your consent is kindly requested to dispatch for business trip the following persons:")
                    .FontFamily(BodyFont).FontSize(BodyFontSize);

                box.Item().PaddingTop(8).LineHorizontal(0.75f).LineColor(Colors.Black);

                var travelers = detail.Travelers.OrderBy(t => t.SortOrder).ToList();
                if (travelers.Count == 0)
                {
                    box.Item().PaddingTop(8)
                        .Text(Bilingual("1. ФИО, должность", "Surname & name, position"))
                        .FontFamily(BodyFont).FontSize(BodyFontSize);
                }
                else
                {
                    for (var i = 0; i < travelers.Count; i++)
                    {
                        var traveler = travelers[i];
                        var lineRu = HrBusinessTripTextBuilder.BuildTravelerLineRu(
                            traveler.FullNameRu, traveler.PositionRu);
                        var lineEn = HrBusinessTripTextBuilder.BuildTravelerLineEn(
                            traveler.FullNameEn, traveler.PositionEn,
                            traveler.FullNameRu, traveler.PositionRu);
                        box.Item().PaddingTop(i == 0 ? 8 : 6)
                            .Text(Bilingual($"{i + 1}. {lineRu}", lineEn))
                            .FontFamily(BodyFont).FontSize(BodyFontSize);
                    }
                }
            });

            // Trip details
            col.Item().PaddingTop(16).Border(0.75f).BorderColor(Colors.Black).Column(details =>
            {
                var purposeEn = string.IsNullOrWhiteSpace(detail.PurposeEn)
                    ? detail.PurposeRu.Trim()
                    : detail.PurposeEn.Trim();
                RenderDetailRow(details,
                    "Цель командирования:\nPurpose of trip:",
                    Bilingual(detail.PurposeRu.Trim(), purposeEn),
                    drawBottomBorder: true);

                var fromRu = HrBusinessTripTextBuilder.FormatDateRu(detail.DateFrom);
                var toRu = HrBusinessTripTextBuilder.FormatDateRu(detail.DateTo);
                var fromEn = HrBusinessTripTextBuilder.FormatDateEn(detail.DateFrom);
                var toEn = HrBusinessTripTextBuilder.FormatDateEn(detail.DateTo);
                var daysRu = HrBusinessTripTextBuilder.BuildDaysRu(detail.DaysCount);
                var daysEn = HrBusinessTripTextBuilder.BuildDaysEn(detail.DaysCount);
                RenderDetailRow(details,
                    "Сроки командирования:\nDuration of trip:",
                    Bilingual($"С {fromRu} по {toRu} ({daysRu})", $"From {fromEn} to {toEn} ({daysEn})"),
                    drawBottomBorder: true);

                var placeRu = detail.PlaceRu.Trim();
                var placeEn = string.IsNullOrWhiteSpace(detail.PlaceEn) ? placeRu : detail.PlaceEn.Trim();
                RenderDetailRow(details,
                    "Место командирования:\nPlace of trip:",
                    Bilingual(placeRu, placeEn),
                    drawBottomBorder: false);
            });

            // Signatures
            col.Item().PaddingTop(18).Border(0.75f).BorderColor(Colors.Black).Column(sig =>
            {
                var rows = BuildSignatureRows(detail, approverUsers).ToList();
                for (var i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    var item = sig.Item();
                    if (i < rows.Count - 1)
                        item = item.BorderBottom(0.75f).BorderColor(Colors.Black);

                    item.Row(r =>
                    {
                        r.RelativeItem(1.5f).Padding(7)
                            .Text(row.Position).FontFamily(BodyFont).FontSize(BodyFontSize);
                        r.RelativeItem(0.85f).BorderLeft(0.75f).BorderRight(0.75f).BorderColor(Colors.Black)
                            .Padding(7).AlignCenter()
                            .Text(row.Signature).FontFamily(BodyFont).FontSize(BodyFontSize);
                        r.RelativeItem(1.35f).Padding(7)
                            .Text(row.Name).FontFamily(BodyFont).FontSize(BodyFontSize);
                    });
                }
            });
        });
    }

    /// <summary>RU / then EN on the next line — used everywhere in the memo.</summary>
    private static string Bilingual(string ru, string en) => $"{ru.Trim()} /\n{en.Trim()}";

    private static void RenderAddressBlock(ColumnDescriptor col, string label, IReadOnlyList<string> lines)
    {
        col.Item().Row(row =>
        {
            row.ConstantItem(LabelWidth).AlignTop()
                .Text(label).FontFamily(BodyFont).FontSize(BodyFontSize).SemiBold();
            row.RelativeItem().PaddingLeft(10).Column(c =>
            {
                c.Spacing(3);
                foreach (var line in lines)
                    c.Item().Text(line).FontFamily(BodyFont).FontSize(BodyFontSize);
            });
        });
    }

    private static void RenderDetailRow(
        ColumnDescriptor parent, string label, string value, bool drawBottomBorder)
    {
        var item = parent.Item();
        if (drawBottomBorder)
            item = item.BorderBottom(0.75f).BorderColor(Colors.Black);

        item.Row(row =>
        {
            row.RelativeItem(1.1f).Padding(8)
                .Text(label).FontFamily(BodyFont).FontSize(BodyFontSize);
            row.RelativeItem(1.9f).BorderLeft(0.75f).BorderColor(Colors.Black).Padding(8)
                .Text(value).FontFamily(BodyFont).FontSize(BodyFontSize);
        });
    }

    private static IEnumerable<SignatureRow> BuildSignatureRows(
        HrBusinessTripRequestDetail detail,
        IReadOnlyDictionary<Guid, User>? approverUsers)
    {
        User? ResolveUser(HrBusinessTripApprover approver) =>
            approver.User
            ?? (approverUsers is not null && approverUsers.TryGetValue(approver.UserId, out var user)
                ? user
                : null);

        var managerApprovers = detail.Approvers
            .Where(a => a.Role is HrBusinessTripApprovalRole.DeputyDepartmentHead
                or HrBusinessTripApprovalRole.DepartmentHead)
            .OrderBy(a => a.SortOrder)
            .ToList();

        for (var i = 0; i < 2; i++)
        {
            if (i < managerApprovers.Count)
            {
                var approver = managerApprovers[i];
                var user = ResolveUser(approver);
                if (user is null)
                {
                    yield return new SignatureRow(
                        Bilingual("Должность руководителя", "Position of manager"),
                        Bilingual("Подпись", "signature"),
                        Bilingual("г-н / г-жа ФИО", "Surname & name"));
                    continue;
                }

                var posRu = user.GetJobTitle("ru") ?? user.Position?.Name ?? "Должность руководителя";
                var posEn = user.GetJobTitle("en") ?? user.Position?.Name ?? "Position of manager";
                var signed = approver.Status == HrLeaveApproverStatus.Approved;
                yield return new SignatureRow(
                    Bilingual(posRu, posEn),
                    signed ? "✓" : Bilingual("Подпись", "signature"),
                    Bilingual(FormatPersonRu(user), FormatPersonEn(user)));
            }
            else
            {
                yield return new SignatureRow(
                    Bilingual("Должность руководителя", "Position of manager"),
                    Bilingual("Подпись", "signature"),
                    Bilingual("г-н / г-жа ФИО", "Surname & name"));
            }
        }

        var fdgd = detail.Approvers.FirstOrDefault(a => a.Role == HrBusinessTripApprovalRole.FirstDeputyGeneralDirector);
        var fdgdUser = fdgd is null ? null : ResolveUser(fdgd);
        var fdgdSigned = fdgd?.Status == HrLeaveApproverStatus.Approved;
        var fdgdNameRu = fdgdUser is not null ? FormatPersonRu(fdgdUser) : "г-н Азизов М.А.";
        var fdgdNameEn = fdgdUser is not null ? FormatPersonEn(fdgdUser) : "Mr. Azizov M.A.";
        yield return new SignatureRow(
            Bilingual("Первый Заместитель Генерального Директора", "First Deputy General Director"),
            fdgdSigned ? "✓" : Bilingual("Подпись", "signature"),
            Bilingual(fdgdNameRu, fdgdNameEn));
    }

    private static string FormatPersonRu(User user)
    {
        var name = $"{user.LastName} {user.FirstName}".Trim();
        return name.StartsWith("г-", StringComparison.OrdinalIgnoreCase) ? name : $"г-н {name}";
    }

    private static string FormatPersonEn(User user)
    {
        var name = string.IsNullOrWhiteSpace(user.FullNameEn) ? user.FullName : user.FullNameEn;
        return name.StartsWith("Mr.", StringComparison.OrdinalIgnoreCase) ? name : $"Mr. {name}";
    }

    private static (string Ru, string En) ResolveOrganization(Organization? org)
    {
        if (org?.Code == BmgmcMasterData.OrganizationCode)
            return ("Бухарского ОУМГ", "Bukhara Main Gas Management Center");

        return ("СП ООО «Asia Trans Gas»", "JV «Asia Trans Gas» LLC");
    }

    private static (string DirectorRu, string DirectorEn, string GdNameRu, string GdNameEn) ResolveRecipient() =>
        (
            "Генеральному директору",
            "General Director",
            "г-ну Лю Чжигуан",
            "Mr. Liu Zhiguang");

    private sealed record SignatureRow(string Position, string Signature, string Name);
}
