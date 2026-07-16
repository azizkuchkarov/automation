using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Dcs;
using NPOI.XWPF.UserModel;

var bytes = PlanWordDocumentGenerator.Generate(new PlanDocumentFillRequest(
    MarketingPlanRegistrationMethod.BestOfferSelection,
    "ATG-SBP-2026-001",
    DateOnly.FromDateTime(DateTime.UtcNow.Date)));

var outPath = Path.Combine(AppContext.BaseDirectory, "test-plan-sbp.docx");
await File.WriteAllBytesAsync(outPath, bytes);

var origPath = Path.Combine(AppContext.BaseDirectory, "Dcs", "Templates", "Plans", "PlanSbp.docx");
var origSize = File.Exists(origPath) ? new FileInfo(origPath).Length : 0;
var hasNumber = false;
using (var verify = new MemoryStream(bytes))
using (var doc = new XWPFDocument(verify))
{
    var text = string.Concat(doc.Paragraphs.Select(p => p.ParagraphText));
    hasNumber = text.Contains("ATG-SBP-2026-001", StringComparison.Ordinal);
}
Console.WriteLine($"Bytes: {bytes.Length} (orig {origSize}), PK: {bytes[0] == 0x50 && bytes[1] == 0x4B}, Number: {hasNumber}");
Console.WriteLine($"Written: {outPath}");
