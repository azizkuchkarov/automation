using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Hr;

var detail = new HrBusinessTripRequestDetail
{
    DocumentId = Guid.NewGuid(),
    PurposeRu = "участия в переговорах с контрагентом",
    PurposeEn = "participation in negotiations with the counterparty",
    PlaceRu = "Пекин (КНР)",
    PlaceEn = "Beijing city (CPR)",
    DateFrom = new DateTime(2026, 4, 10),
    DateTo = new DateTime(2026, 4, 15),
    DaysCount = 6,
    OrderNumber = "HBO-2026-003",
    OrderIssuedAt = new DateTime(2026, 4, 8),
    Document = new Document { Number = "HBT-2026-001" },
};

var traveler = new HrBusinessTripTraveler
{
    Id = Guid.NewGuid(),
    FullNameRu = "Иванов Иван Иванович",
    FullNameEn = "Ivan Ivanov",
    PositionRu = "инженер",
    PositionEn = "engineer",
    SortOrder = 0,
};
detail.Travelers.Add(traveler);

var bytes = HrBusinessTripCertificateGenerator.Generate(detail, traveler);
var outPath = Path.Combine(AppContext.BaseDirectory, "certificate-test.xlsx");
await File.WriteAllBytesAsync(outPath, bytes);
Console.WriteLine($"Wrote {outPath} ({bytes.Length} bytes)");
