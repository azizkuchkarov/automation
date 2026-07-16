using ATG.Platform.Domain.Entities;

namespace ATG.Platform.Infrastructure.Hr;

public static class HrBusinessTripOrderDocumentGenerator
{
    public static byte[] Generate(
        IReadOnlyList<HrBusinessTripRequestDetail> details,
        string orderNumber,
        DateTime orderDate,
        string? verificationUrl = null)
    {
        if (details.Count == 0)
            throw new ArgumentException("At least one memorandum is required", nameof(details));

        var model = HrBusinessTripOrderBodyBuilder.BuildModel(details, orderDate, orderNumber)
            with { VerificationUrl = verificationUrl };
        return HrBusinessTripOrderPdfGenerator.Generate(model);
    }

    public static string ResolveTemplatePath(string templateFileName) =>
        Path.Combine(AppContext.BaseDirectory, "Hr", "Templates", templateFileName);
}
