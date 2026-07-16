using ATG.Platform.Domain.Entities;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ATG.Platform.Infrastructure.Hr;

public static class HrLeavePdfGenerator
{
    static HrLeavePdfGenerator() => QuestPDF.Settings.License = LicenseType.Community;

    public static byte[] Generate(HrLeaveRequestDetail detail, string? verificationUrl = null)
    {
        byte[]? qrPng = string.IsNullOrWhiteSpace(verificationUrl) ? null : CreateQrPng(verificationUrl);

        return QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                HrLeaveLetterheadLayout.BuildPage(
                    page,
                    body => HrLeaveDocumentBodyRenderer.Render(body, detail, qrPng: qrPng));
            });
        }).GeneratePdf();
    }

    private static byte[] CreateQrPng(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        return new PngByteQRCode(data).GetGraphic(4);
    }
}
