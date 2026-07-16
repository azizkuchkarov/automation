using ATG.Platform.Domain.Entities;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ATG.Platform.Infrastructure.Hr;

public static class HrBusinessTripPresentationPdfGenerator
{
    public static byte[] Generate(
        HrBusinessTripRequestDetail detail,
        IReadOnlyList<HrLeavePdfStamp> stamps,
        string verificationUrl)
    {
        HrPdfFontRegistrar.EnsureRegistered();
        var qrBytes = CreateQrPng(verificationUrl);

        return QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                HrBusinessTripMemoLayout.BuildPage(
                    page,
                    body => HrBusinessTripDocumentBodyRenderer.Render(body, detail),
                    stamps);
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
