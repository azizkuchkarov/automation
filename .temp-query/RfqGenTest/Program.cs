using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Dcs;

var fill = new RfqDocumentFillRequest(
    TasRequisitionType.MaterialRequest,
    "ATG-CP-MT-LO-2026-099",
    new DateOnly(2026, 7, 15),
    new DateOnly(2026, 7, 15),
    "Test subject for RFQ",
    "Test subject EN",
    "engineer@atg.uz",
    "Dmitriy Temirov");

var bytes = RfqWordDocumentGenerator.Generate(fill);
var outPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".temp-query", "rfq-test-output.docx");
Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
File.WriteAllBytes(outPath, bytes);
Console.WriteLine($"Wrote {bytes.Length} bytes to {Path.GetFullPath(outPath)}");
