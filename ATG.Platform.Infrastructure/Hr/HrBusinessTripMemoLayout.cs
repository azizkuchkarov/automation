using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ATG.Platform.Infrastructure.Hr;

/// <summary>Business-trip memorandum page: ATG logo header only (no contact footer).</summary>
public static class HrBusinessTripMemoLayout
{
    public const string BodyFont = "Arial";
    public const float BodyFontSize = 10f;
    public const string AtgBlue = HrLeaveLetterheadLayout.AtgBlue;

    private const float HorizontalPadding = 52;
    private const float LogoHeight = 54;

    public static void BuildPage(
        PageDescriptor page,
        Action<IContainer> renderBody,
        IReadOnlyList<HrLeavePdfStamp>? stamps = null)
    {
        page.Size(PageSizes.A4);
        page.MarginTop(24);
        page.MarginBottom(stamps is { Count: > 0 } ? 28 : 36);
        page.MarginHorizontal(HorizontalPadding);
        page.DefaultTextStyle(x => x
            .FontFamily(BodyFont)
            .FontSize(BodyFontSize)
            .FontColor(Colors.Black));

        page.Header().Column(header =>
        {
            header.Item()
                .Background(Colors.White)
                .PaddingBottom(4)
                .Height(LogoHeight + 4)
                .AlignLeft()
                .Image(HrLeaveLetterheadLayout.GetLogoBytes())
                .FitHeight();

            header.Item().PaddingTop(8).Column(lines =>
            {
                lines.Item().LineHorizontal(2.5f).LineColor(AtgBlue);
                lines.Item().PaddingTop(3).LineHorizontal(1f).LineColor(AtgBlue);
            });
        });

        if (stamps is { Count: > 0 })
        {
            page.Footer().PaddingTop(6).Row(row =>
            {
                foreach (var stamp in stamps)
                    row.RelativeItem().PaddingHorizontal(3).Element(c => HrLeavePdfStampRenderer.Render(c, stamp));
            });
        }
        else
        {
            page.Footer().Height(1);
        }

        page.Content()
            .PaddingTop(14)
            .PaddingBottom(stamps is { Count: > 0 } ? 8 : 4)
            .Element(renderBody);
    }
}
