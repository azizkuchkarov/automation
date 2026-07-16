using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;

namespace ATG.Platform.Infrastructure.Hr;

/// <summary>
/// Formal bilingual business trip ORDER / ПРИКАЗ PDF — framed body with left, centre and right borders.
/// </summary>
public static class HrBusinessTripOrderPdfGenerator
{
    private const string BodyFont = HrBusinessTripMemoLayout.BodyFont;
    private const float BodyFontSize = HrBusinessTripMemoLayout.BodyFontSize;
    private const float TitleFontSize = 14f;
    private const float SubtitleFontSize = 10f;
    private const float HeaderToBoxGap = 12f;
    private const float BoxPaddingH = 10f;
    private const float BoxPaddingV = 8f;
    private const float SectionSpacing = 10f;
    private const float BorderWidth = 0.75f;
    private const float QrSize = 72f;
    private static readonly string BorderColor = Colors.Black;

    public static byte[] Generate(HrBusinessTripOrderDocumentModel model)
    {
        HrPdfFontRegistrar.EnsureRegistered();
        var qrPng = string.IsNullOrWhiteSpace(model.VerificationUrl)
            ? null
            : CreateQrPng(model.VerificationUrl);

        return QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                HrBusinessTripMemoLayout.BuildPage(page, body =>
                {
                    body.DefaultTextStyle(x => x
                            .FontFamily(BodyFont)
                            .FontSize(BodyFontSize)
                            .FontColor(Colors.Black)
                            .LineHeight(1.35f))
                        .Column(col =>
                        {
                            col.Item().AlignCenter().Text($"_______ № {FormatOrderDisplayNumber(model.OrderNumber)}")
                                .FontFamily(BodyFont)
                                .FontSize(BodyFontSize);

                            col.Item().PaddingTop(14).Row(header =>
                            {
                                header.RelativeItem().Column(left =>
                                {
                                    left.Item().AlignCenter().Text("ORDER")
                                        .FontFamily(BodyFont).FontSize(TitleFontSize).Bold();
                                    left.Item().AlignCenter().Text("Sending to business trip")
                                        .FontFamily(BodyFont).FontSize(SubtitleFontSize);
                                });

                                header.RelativeItem().Column(right =>
                                {
                                    right.Item().AlignCenter().Text("ПРИКАЗ")
                                        .FontFamily(BodyFont).FontSize(TitleFontSize).Bold();
                                    right.Item().AlignCenter().Text("О командировании")
                                        .FontFamily(BodyFont).FontSize(SubtitleFontSize);
                                });
                            });

                            col.Item()
                                .PaddingTop(HeaderToBoxGap)
                                .BorderTop(BorderWidth)
                                .BorderLeft(BorderWidth)
                                .BorderRight(BorderWidth)
                                .BorderBottom(BorderWidth)
                                .BorderColor(BorderColor)
                                .Row(box =>
                                {
                                    box.RelativeItem()
                                        .BorderRight(BorderWidth)
                                        .BorderColor(BorderColor)
                                        .PaddingHorizontal(BoxPaddingH)
                                        .PaddingVertical(BoxPaddingV)
                                        .Element(c => RenderEnglishBody(c, model));

                                    box.RelativeItem()
                                        .PaddingHorizontal(BoxPaddingH)
                                        .PaddingVertical(BoxPaddingV)
                                        .Element(c => RenderRussianBody(c, model));
                                });

                            col.Item().PaddingTop(28).Row(sig =>
                            {
                                sig.RelativeItem().Column(left =>
                                {
                                    left.Item().Text("Mr. Liu Zhiguang / Г-н Лю Чжигуан")
                                        .FontFamily(BodyFont).FontSize(BodyFontSize);
                                    left.Item().PaddingTop(2).Text("General Director JV «Asia Trans Gas» LLC")
                                        .FontFamily(BodyFont).FontSize(BodyFontSize);
                                    left.Item().PaddingTop(1).Text("Генеральный директор СП ООО «Asia Trans Gas»")
                                        .FontFamily(BodyFont).FontSize(BodyFontSize);
                                });

                                sig.ConstantItem(QrSize + 8).AlignRight().AlignMiddle().Column(qr =>
                                {
                                    if (qrPng is not null)
                                        qr.Item().Width(QrSize).Height(QrSize).Image(qrPng).FitArea();
                                });
                            });
                        });
                });
            });
        }).GeneratePdf();
    }

    private static void RenderEnglishBody(IContainer container, HrBusinessTripOrderDocumentModel model)
    {
        container.Column(col =>
        {
            col.Spacing(SectionSpacing);

            col.Item().Row(r =>
            {
                r.RelativeItem().Text("Tashkent").FontFamily(BodyFont).FontSize(BodyFontSize);
                r.RelativeItem().AlignRight().Text(HrBusinessTripTextBuilder.FormatDateEn(model.OrderDate))
                    .FontFamily(BodyFont).FontSize(BodyFontSize);
            });

            if (model.PurposeIntroEn is not null)
            {
                col.Item().Text(model.PurposeIntroEn).FontFamily(BodyFont).FontSize(BodyFontSize).Justify();
            }

            col.Item().PaddingTop(4).AlignCenter().Text("I ORDER")
                .FontFamily(BodyFont).FontSize(BodyFontSize).Bold();

            foreach (var section in model.Memoranda)
                RenderMemoSectionEn(col, section);

            col.Item().Text(
                    $"§{model.AccountingSectionNum}. Accounting Center to effect payment of business trip expenses according to \"Regulation on business trips of employees of JV \"Asia Trans Gas\" dated 04.09.2023.")
                .FontFamily(BodyFont).FontSize(BodyFontSize).Justify();

            col.Item().Text(
                    $"§{model.ReportSectionNum}. The business trip report shall be submitted within 3 days upon arrival.")
                .FontFamily(BodyFont).FontSize(BodyFontSize).Justify();

            foreach (var basis in model.BasisRows)
            {
                col.Item().Text(basis.En).FontFamily(BodyFont).FontSize(BodyFontSize).Justify();
            }
        });
    }

    private static void RenderRussianBody(IContainer container, HrBusinessTripOrderDocumentModel model)
    {
        container.Column(col =>
        {
            col.Spacing(SectionSpacing);

            col.Item().Row(r =>
            {
                r.RelativeItem().Text("г. Ташкент").FontFamily(BodyFont).FontSize(BodyFontSize);
                r.RelativeItem().AlignRight()
                    .Text(model.OrderDate.ToString("d MMMM yyyy", new System.Globalization.CultureInfo("ru-RU")) + " г.")
                    .FontFamily(BodyFont).FontSize(BodyFontSize);
            });

            if (model.PurposeIntroRu is not null)
            {
                col.Item().Text(model.PurposeIntroRu).FontFamily(BodyFont).FontSize(BodyFontSize).Justify();
            }

            col.Item().PaddingTop(4).AlignCenter().Text("ПРИКАЗЫВАЮ")
                .FontFamily(BodyFont).FontSize(BodyFontSize).Bold();

            foreach (var section in model.Memoranda)
                RenderMemoSectionRu(col, section);

            col.Item().Text(
                    $"§{model.AccountingSectionNum}. Центральной бухгалтерии произвести оплату командировочных расходов согласно Положению о служебных командировках работников СП ООО «Asia Trans Gas» от 04.09.2023 года.")
                .FontFamily(BodyFont).FontSize(BodyFontSize).Justify();

            col.Item().Text(
                    $"§{model.ReportSectionNum}. Отчет о проделанной работе представить в течение 3 дней со дня приезда.")
                .FontFamily(BodyFont).FontSize(BodyFontSize).Justify();

            foreach (var basis in model.BasisRows)
            {
                col.Item().Text(basis.Ru).FontFamily(BodyFont).FontSize(BodyFontSize).Justify();
            }
        });
    }

    private static void RenderMemoSectionEn(ColumnDescriptor col, HrBusinessTripOrderMemoSection section)
    {
        var from = section.DateFrom.ToString("dd.MM.yyyy");
        var to = section.DateTo.ToString("dd.MM.yyyy");
        var daysEn = HrBusinessTripTextBuilder.BuildDaysEn(section.DaysCount);

        if (section.Travelers.Count <= 1)
        {
            var traveler = section.Travelers.FirstOrDefault();
            var lineEn = traveler?.LineEn ?? "";
            col.Item().Text(
                    $"§{section.SectionNum}. For the purpose of {section.PurposeEn}, to send {lineEn} of JV «Asia Trans Gas» LLC to business trip to {section.PlaceEn} for the period of {daysEn} from {from} till {to}.")
                .FontFamily(BodyFont).FontSize(BodyFontSize).Justify();
            return;
        }

        col.Item().Text(
                $"§{section.SectionNum}. For the purpose of {section.PurposeEn}, to send following employees of JV «Asia Trans Gas» LLC to business trip to {section.PlaceEn} for the period of {daysEn} from {from} till {to}:")
            .FontFamily(BodyFont).FontSize(BodyFontSize).Justify();

        var index = 1;
        foreach (var traveler in section.Travelers)
        {
            col.Item().PaddingLeft(12).Text($"{section.SectionNum}.{index} {traveler.LineEn};")
                .FontFamily(BodyFont).FontSize(BodyFontSize);
            index++;
        }
    }

    private static void RenderMemoSectionRu(ColumnDescriptor col, HrBusinessTripOrderMemoSection section)
    {
        var fromRu = section.DateFrom.ToString("dd.MM.yyyy") + " г.";
        var toRu = section.DateTo.ToString("dd.MM.yyyy") + " г.";
        var daysRu = HrBusinessTripTextBuilder.BuildDaysRu(section.DaysCount);

        if (section.Travelers.Count <= 1)
        {
            var traveler = section.Travelers.FirstOrDefault();
            var lineRu = traveler?.LineRu ?? "";
            col.Item().Text(
                    $"§{section.SectionNum}. С целью {section.PurposeRu}, командировать {lineRu} СП ООО «Asia Trans Gas» в {section.PlaceRu} на {daysRu} с {fromRu} по {toRu}.")
                .FontFamily(BodyFont).FontSize(BodyFontSize).Justify();
            return;
        }

        col.Item().Text(
                $"§{section.SectionNum}. С целью {section.PurposeRu}, командировать следующих сотрудников СП ООО «Asia Trans Gas» в {section.PlaceRu} на {daysRu} с {fromRu} по {toRu}:")
            .FontFamily(BodyFont).FontSize(BodyFontSize).Justify();

        var index = 1;
        foreach (var traveler in section.Travelers)
        {
            col.Item().PaddingLeft(12).Text($"{section.SectionNum}.{index} {traveler.LineRu};")
                .FontFamily(BodyFont).FontSize(BodyFontSize);
            index++;
        }
    }

    private static string FormatOrderDisplayNumber(string orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber)) return "____-bt";

        var parts = orderNumber.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 3
            && string.Equals(parts[0], "HBO", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(parts[^1], out var seq))
            return $"{seq}-bt";

        return orderNumber;
    }

    private static byte[] CreateQrPng(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        return new PngByteQRCode(data).GetGraphic(4);
    }
}
