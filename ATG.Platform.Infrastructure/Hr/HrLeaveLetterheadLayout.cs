using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ATG.Platform.Infrastructure.Hr;

/// <summary>ATG corporate letterhead — logo header, bilingual footer contacts.</summary>
public static class HrLeaveLetterheadLayout
{
    public const string AtgBlue = "#1B4F8C";

    private const float HorizontalPadding = 52;
    private const float LogoHeight = 54;
    private const float FooterFontSize = 8.5f;

    private static byte[]? _logoBytes;

    private const string FooterRu =
        "СП ООО «Asia Trans Gas»\n" +
        "Республика Узбекистан,\n" +
        "г. Ташкент, 100100, ул. Нукус, 2Б.\n" +
        "Тел.:(998 71) 203-22-00, E-mail: info@atg.uz";

    private const string FooterEn =
        "JV «Asia Trans Gas» LLC\n" +
        "2B Nukus str., Tashkent,\n" +
        "100100, Republic of Uzbekistan\n" +
        "Tel.:(998 71) 203-22-00, E-mail: info@atg.uz";

    public static void BuildPage(
        PageDescriptor page,
        Action<IContainer> renderBody,
        IReadOnlyList<HrLeavePdfStamp>? stamps = null)
    {
        page.Size(PageSizes.A4);
        page.MarginTop(24);
        page.MarginBottom(18);
        page.MarginHorizontal(HorizontalPadding);
        page.DefaultTextStyle(x => x.FontSize(11).FontColor(AtgBlue));

        page.Header().Column(header =>
        {
            header.Item()
                .Background(Colors.White)
                .PaddingBottom(4)
                .Height(LogoHeight + 4)
                .AlignLeft()
                .Image(GetLogoBytes())
                .FitHeight();

            header.Item().PaddingTop(8).Column(lines =>
            {
                lines.Item().LineHorizontal(2.5f).LineColor(AtgBlue);
                lines.Item().PaddingTop(3).LineHorizontal(1f).LineColor(AtgBlue);
            });
        });

        page.Footer().Column(footer =>
        {
            if (stamps is { Count: > 0 })
            {
                footer.Item().Row(row =>
                {
                    foreach (var stamp in stamps)
                        row.RelativeItem().PaddingHorizontal(3).Element(c => HrLeavePdfStampRenderer.Render(c, stamp));
                });
                footer.Item().PaddingTop(10);
            }

            footer.Item().LineHorizontal(1f).LineColor(AtgBlue);
            footer.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Element(left => RenderFooterBlock(left, FooterRu, alignRight: false));
                row.ConstantItem(20);
                row.RelativeItem().Element(right => RenderFooterBlock(right, FooterEn, alignRight: true));
            });
        });

        page.Content()
            .PaddingTop(12)
            .PaddingBottom(stamps is { Count: > 0 } ? 8 : 4)
            .Element(renderBody);
    }

    private static void RenderFooterBlock(IContainer container, string text, bool alignRight)
    {
        var block = container.Text(text)
            .FontSize(FooterFontSize)
            .FontColor(AtgBlue)
            .LineHeight(1.25f);

        if (alignRight)
            block.AlignRight();
    }

    public static byte[] GetLogoBytes()
    {
        if (_logoBytes is not null) return _logoBytes;

        var assembly = typeof(HrLeaveLetterheadLayout).Assembly;
        const string resourceName = "ATG.Platform.Infrastructure.Hr.Assets.atg-logo.png";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Embedded logo not found: {resourceName}");

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        _logoBytes = memory.ToArray();
        return _logoBytes;
    }
}
