using System.Globalization;
using ATG.Platform.Domain.Enums;
using NPOI.XWPF.UserModel;

namespace ATG.Platform.Infrastructure.Dcs;

public record RfqDocumentFillRequest(
    TasRequisitionType RequisitionType,
    string RegistrationNumber,
    DateOnly DocumentDate,
    DateOnly CommercialProposalDeadline,
    string SubjectRu,
    string? SubjectEn,
    string MarketingEngineerEmail,
    string MarketingEngineerName);

public static class RfqWordDocumentGenerator
{
    private const int FontSizePoints = 10;

    public static byte[] Generate(RfqDocumentFillRequest data)
    {
        var templateName = data.RequisitionType == TasRequisitionType.ServiceRequest
            ? "RfqService.docx"
            : "RfqMaterial.docx";
        var templatePath = Path.Combine(AppContext.BaseDirectory, "Dcs", "Templates", templateName);
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"RFQ template not found: {templateName}", templatePath);

        var dateText = data.DocumentDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        var deadlineText = data.CommercialProposalDeadline.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        var subjectRu = data.SubjectRu.Trim();
        var subjectEn = string.IsNullOrWhiteSpace(data.SubjectEn) ? subjectRu : data.SubjectEn.Trim();
        var email = data.MarketingEngineerEmail.Trim();
        var engineerName = data.MarketingEngineerName.Trim();

        var replacements = data.RequisitionType == TasRequisitionType.ServiceRequest
            ? BuildServiceReplacements(data.RegistrationNumber, dateText, deadlineText, subjectRu, subjectEn, email, engineerName)
            : BuildMaterialReplacements(data.RegistrationNumber, dateText, deadlineText, subjectRu, subjectEn, email, engineerName);

        using var input = File.OpenRead(templatePath);
        using var doc = new XWPFDocument(input);

        foreach (var replacement in replacements)
            ReplaceAll(doc, replacement);

        using var output = new MemoryStream();
        doc.Write(output);
        return output.ToArray();
    }

    private static RfqReplacement[] BuildMaterialReplacements(
        string registrationNumber, string dateText, string deadlineText,
        string subjectRu, string subjectEn, string email, string engineerName) =>
    [
        new("ATG-CP-MT-LO-26-_____", registrationNumber, Style: RfqTextStyle.Emphasis10),
        new("ATG-CP-MT-LO-2026-_____", registrationNumber, Style: RfqTextStyle.Emphasis10),
        new("__.__.2026г.", $"{dateText}г."),
        new("__.__.2026", dateText, Style: RfqTextStyle.Emphasis10, ExactParagraph: true),
        new("no later than ____.____.2026 on the following terms:", $"no later than {deadlineText} on the following terms:"),
        new("no later than ____.____.2026.", $"no later than {deadlineText}."),
        new("просит в срок до ____.____.2026 года", $"просит в срок до {deadlineText} года"),
        new("Тема: Запрос на закупку товара ____________", $"Тема: Запрос на закупку товара {subjectRu}", Style: RfqTextStyle.Emphasis10),
        new("Subject: Request for purchase of goods ____________", $"Subject: Request for purchase of goods {subjectEn}", Style: RfqTextStyle.Emphasis10),
        new("for purchase of goods ____________", $"for purchase of goods {subjectEn}", Style: RfqTextStyle.Emphasis10),
        new("____________;", $"{email};"),
        new("____________, +998 71 203 22 00 (доб.____)", $"{engineerName}, +998 71 203 22 00 (доб.____)"),
        new("_Ф.И.О_", engineerName),
    ];

    private static RfqReplacement[] BuildServiceReplacements(
        string registrationNumber, string dateText, string deadlineText,
        string subjectRu, string subjectEn, string email, string engineerName) =>
    [
        new("ATG-CP-MT-LO-26-_____", registrationNumber, Style: RfqTextStyle.Emphasis10),
        new("ATG-CP-MT-LO-2026-_____", registrationNumber, Style: RfqTextStyle.Emphasis10),
        new("ATG-CP-SR-LO-26-_____", registrationNumber, Style: RfqTextStyle.Emphasis10),
        new("ATG-CP-SR-LO-2026-_____", registrationNumber, Style: RfqTextStyle.Emphasis10),
        new("__.__.2026г.", $"{dateText}г."),
        new("__.__.2026", dateText, Style: RfqTextStyle.Emphasis10, ExactParagraph: true),
        new("no later than ____.____.2026 on the following terms:", $"no later than {deadlineText} on the following terms:"),
        new("no later than ____.____.2026.", $"no later than {deadlineText}."),
        new("просит в срок до ____.____.2026 года", $"просит в срок до {deadlineText} года"),
        new("Тема: Запрос на закупку услуг ____________", $"Тема: Запрос на закупку услуг {subjectRu}", Style: RfqTextStyle.Emphasis10),
        new("Subject: Request for Procurement of Services ____________", $"Subject: Request for Procurement of Services {subjectEn}", Style: RfqTextStyle.Emphasis10),
        new("____________;", $"{email};"),
        new("____________, +998 71 203 22 00 (доб.____)", $"{engineerName}, +998 71 203 22 00 (доб.____)"),
        new("_Ф.И.О_", engineerName),
    ];

    private enum RfqTextStyle { None, Emphasis10 }

    private readonly record struct RfqReplacement(
        string Old,
        string New,
        RfqTextStyle Style = RfqTextStyle.None,
        bool ExactParagraph = false);

    private static void ReplaceAll(XWPFDocument doc, RfqReplacement replacement)
    {
        if (string.IsNullOrEmpty(replacement.Old) || replacement.Old == replacement.New)
            return;

        foreach (var paragraph in doc.Paragraphs)
            ReplaceInParagraph(paragraph, replacement);

        foreach (var table in doc.Tables)
        {
            foreach (var row in table.Rows)
            {
                foreach (var cell in row.GetTableCells())
                {
                    foreach (var paragraph in cell.Paragraphs)
                        ReplaceInParagraph(paragraph, replacement);
                }
            }
        }

        foreach (var header in doc.HeaderList)
        {
            foreach (var paragraph in header.Paragraphs)
                ReplaceInParagraph(paragraph, replacement);
        }

        foreach (var footer in doc.FooterList)
        {
            foreach (var paragraph in footer.Paragraphs)
                ReplaceInParagraph(paragraph, replacement);
        }
    }

    private static void ReplaceInParagraph(XWPFParagraph paragraph, RfqReplacement replacement)
    {
        var full = paragraph.ParagraphText;
        if (string.IsNullOrEmpty(full) || !full.Contains(replacement.Old, StringComparison.Ordinal))
            return;

        if (replacement.ExactParagraph && !string.Equals(full, replacement.Old, StringComparison.Ordinal))
            return;

        var replaced = full.Replace(replacement.Old, replacement.New);
        if (string.IsNullOrEmpty(replaced))
        {
            for (var i = paragraph.Runs.Count - 1; i >= 0; i--)
                paragraph.RemoveRun(i);
            return;
        }

        var templateSize = GetTemplateFontSize(paragraph);
        var templateBold = GetTemplateBold(paragraph);

        for (var i = paragraph.Runs.Count - 1; i >= 0; i--)
            paragraph.RemoveRun(i);

        var run = paragraph.CreateRun();
        run.SetText(replaced);
        ApplyStyle(run, replacement.Style, templateSize, templateBold);
    }

    private static int? GetTemplateFontSize(XWPFParagraph paragraph)
    {
        foreach (var run in paragraph.Runs)
        {
            var size = run.FontSize;
            if (size > 0)
                return (int)Math.Round(size);
        }

        return null;
    }

    private static bool GetTemplateBold(XWPFParagraph paragraph)
    {
        foreach (var run in paragraph.Runs)
        {
            if (run.IsBold)
                return true;
        }

        return false;
    }

    private static void ApplyStyle(XWPFRun run, RfqTextStyle style, int? templateSize, bool templateBold)
    {
        switch (style)
        {
            case RfqTextStyle.Emphasis10:
                run.IsBold = true;
                run.FontSize = FontSizePoints;
                run.SetFontFamily("Arial", FontCharRange.None);
                break;
            default:
                if (templateSize is > 0)
                    run.FontSize = templateSize.Value;
                if (templateBold)
                    run.IsBold = true;
                break;
        }
    }
}
