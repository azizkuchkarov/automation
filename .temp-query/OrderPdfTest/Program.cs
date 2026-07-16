using ATG.Platform.Domain.Entities;
using ATG.Platform.Infrastructure.Hr;

var travelers = new List<HrBusinessTripTraveler>
{
    new()
    {
        FullNameRu = "Кучкаров Азиз Шухратович",
        FullNameEn = "Kuchkarov Aziz Shukhratovich",
        PositionRu = "Специалист по ИТ (старший специалист)",
        PositionEn = "IT Officer (Senior specialist)",
        SortOrder = 0,
    },
};

var detail = new HrBusinessTripRequestDetail
{
    DocumentId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
    RequestDate = new DateTime(2026, 7, 8),
    PurposeRu = "проверка оборудования",
    PurposeEn = "equipment inspection",
    PlaceRu = "г. Бухара, Навои, Кашкадарья",
    PlaceEn = "Bukhara, Navoi, Kashkadarya",
    DateFrom = new DateTime(2026, 7, 10),
    DateTo = new DateTime(2026, 7, 12),
    DaysCount = 3,
    Travelers = travelers,
    Document = new Document
    {
        Number = "HBT-2026-010",
        Department = new Department
        {
            Name = "Департамент по информационным технологиям и цифровизации",
            NameEn = "IT & Digitalization Department",
            NameGenitive = "Департамента по информационным технологиям и цифровизации",
        },
    },
};

var model = HrBusinessTripOrderBodyBuilder.BuildModel([detail], new DateTime(2026, 7, 8), "HBO-2026-003")
    with { VerificationUrl = "http://localhost:3000/api/hr/business-trips/11111111-1111-1111-1111-111111111111/public/order-pdf" };
var bytes = HrBusinessTripOrderPdfGenerator.Generate(model);
var path = @"C:\Users\a.kuchkarov\Desktop\automation\.temp-query\prikaz-middle-line.pdf";
File.WriteAllBytes(path, bytes);
Console.WriteLine($"Wrote {bytes.Length} -> {path}");
