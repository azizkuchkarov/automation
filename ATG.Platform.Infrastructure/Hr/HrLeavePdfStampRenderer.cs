using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ATG.Platform.Infrastructure.Hr;

public static class HrLeavePdfStampRenderer
{
    public static void Render(IContainer container, HrLeavePdfStamp stamp)
    {
        string bg;
        Color border;
        Color titleColor;
        switch (stamp.Style)
        {
            case HrLeaveStampStyle.Sent:
                bg = Colors.Grey.Lighten3;
                border = Colors.Grey.Medium;
                titleColor = Colors.Grey.Darken2;
                break;
            case HrLeaveStampStyle.Reviewed:
                bg = "#E3F2FD";
                border = Colors.Blue.Lighten2;
                titleColor = Colors.Blue.Darken2;
                break;
            case HrLeaveStampStyle.Approved:
                bg = "#B2EBF2";
                border = Colors.Teal.Medium;
                titleColor = Colors.Teal.Darken3;
                break;
            default:
                bg = Colors.Grey.Lighten4;
                border = Colors.Grey.Medium;
                titleColor = Colors.Black;
                break;
        }

        container
            .Border(1).BorderColor(border)
            .Background(bg)
            .Padding(8)
            .Column(col =>
            {
                col.Item().AlignCenter().Text(stamp.Title).Bold().FontSize(10).FontColor(titleColor);
                col.Item().PaddingTop(4).Text($"№ {stamp.DocumentNumber}").FontSize(7);
                col.Item().Text(FormatStampTime(stamp.SignedAt)).FontSize(7);
                col.Item().PaddingTop(3).Text(stamp.SignerName.ToUpperInvariant()).FontSize(7).Bold();
                if (!string.IsNullOrWhiteSpace(stamp.SignerPinpp))
                    col.Item().Text($"PINFL: {stamp.SignerPinpp}").FontSize(6);
                col.Item().PaddingTop(2).Text(stamp.Operator).FontSize(6).FontColor(Colors.Grey.Darken1);
                if (!string.IsNullOrWhiteSpace(stamp.IpAddress))
                    col.Item().Text(stamp.IpAddress).FontSize(6).FontColor(Colors.Grey.Darken1);
            });
    }

    private static string FormatStampTime(DateTime value) =>
        value.ToLocalTime().ToString("yyyy.MM.dd HH:mm:ss");
}
