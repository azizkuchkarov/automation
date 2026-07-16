using System.Globalization;
using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Infrastructure.Dcs;

public record PlanDocumentFillRequest(
    MarketingPlanRegistrationMethod Method,
    string RegistrationNumber,
    DateOnly DocumentDate);

public static class PlanWordDocumentGenerator
{
    public static bool HasTemplate(MarketingPlanRegistrationMethod method) =>
        method != MarketingPlanRegistrationMethod.Tender;

    public static byte[] Generate(PlanDocumentFillRequest data)
    {
        if (!HasTemplate(data.Method))
            throw new InvalidOperationException("This procurement method has no Word template.");

        var templateName = data.Method switch
        {
            MarketingPlanRegistrationMethod.BestOfferSelection => "PlanSbp.docx",
            MarketingPlanRegistrationMethod.LocalProcurement => "PlanLps.docx",
            MarketingPlanRegistrationMethod.SmallProcurement => "PlanSps.docx",
            MarketingPlanRegistrationMethod.QuotationRequest => "PlanQrs.docx",
            MarketingPlanRegistrationMethod.DirectContract => "PlanDps.docx",
            _ => throw new ArgumentOutOfRangeException(nameof(data.Method)),
        };

        var templatePath = Path.Combine(AppContext.BaseDirectory, "Dcs", "Templates", "Plans", templateName);
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Plan template not found: {templateName}", templatePath);

        var prefix = MarketingPlanRegistrationNumberGenerator.BuildPrefix(data.Method);
        var placeholder = $"{prefix}___";
        var dateText = data.DocumentDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

        return WordZipTextReplacer.FillTemplate(templatePath,
        [
            (placeholder, data.RegistrationNumber),
            ($"{prefix}000", data.RegistrationNumber),
            ("__.__.2026г.", $"{dateText}г."),
            ("__.__.2026", dateText),
        ]);
    }
}
