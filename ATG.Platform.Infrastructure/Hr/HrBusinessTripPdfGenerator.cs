using ATG.Platform.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ATG.Platform.Infrastructure.Hr;

public static class HrBusinessTripPdfGenerator
{
    public static byte[] Generate(
        HrBusinessTripRequestDetail detail,
        IReadOnlyDictionary<Guid, User>? approverUsers = null)
    {
        HrPdfFontRegistrar.EnsureRegistered();

        return QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                HrBusinessTripMemoLayout.BuildPage(
                    page,
                    body => HrBusinessTripDocumentBodyRenderer.Render(body, detail, approverUsers));
            });
        }).GeneratePdf();
    }
}
