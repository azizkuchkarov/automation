using System.Globalization;
using System.Reflection;
using ATG.Platform.Domain.Entities;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace ATG.Platform.Infrastructure.Hr;

public static class HrBusinessTripCertificateGenerator
{
    private static readonly CultureInfo RuCulture = CultureInfo.GetCultureInfo("ru-RU");

    public static byte[] Generate(
        HrBusinessTripRequestDetail detail,
        HrBusinessTripTraveler traveler,
        string? passportSeries = null,
        string? passportNumber = null)
    {
        var certNumber = BuildCertificateNumber(detail, traveler);
        using var template = OpenTemplateStream();
        using var workbook = new XSSFWorkbook(template);
        var sheet = workbook.GetSheetAt(0);
        workbook.SetSheetName(0, SanitizeSheetName(traveler.FullNameRu));

        var orderDisplay = FormatOrderDisplayNumber(detail.OrderNumber);
        var orderDate = detail.OrderIssuedAt ?? DateTime.UtcNow;
        var placeRu = detail.PlaceRu.Trim();
        var placeEn = string.IsNullOrWhiteSpace(detail.PlaceEn) ? placeRu : detail.PlaceEn.Trim();
        var placeDisplay = string.Equals(placeRu, placeEn, StringComparison.OrdinalIgnoreCase)
            ? placeRu
            : $"{placeRu}/{placeEn}";
        var nameEn = string.IsNullOrWhiteSpace(traveler.FullNameEn) ? traveler.FullNameRu : traveler.FullNameEn.Trim();
        var positionRu = traveler.PositionRu.Trim();
        var positionEn = string.IsNullOrWhiteSpace(traveler.PositionEn) ? positionRu : traveler.PositionEn.Trim();
        var positionDisplay = string.Equals(positionRu, positionEn, StringComparison.OrdinalIgnoreCase)
            ? positionRu
            : $"{positionRu} / {positionEn}";

        SetCell(sheet, "B1", $"Командировочное удостоверение/Business trip reference  №{certNumber}");
        SetCell(sheet, "E2", traveler.FullNameRu.Trim());
        SetCell(sheet, "E3", nameEn);
        SetWrappedMergedCell(sheet, "D4", positionDisplay);
        ClearCell(sheet, "B7");
        SetCell(sheet, "D5", $"командированному  СП ООО \"Asia Trans Gas\" в {placeRu}");
        SetCell(sheet, "D6", $"who was sent to business trip by JV \"Asia Trans Gas\" LLC to {placeEn}");
        SetCell(sheet, "D7", placeDisplay);
        SetCell(sheet, "F8", $"Пр.№{orderDisplay}-Uz от {orderDate.ToString("dd.MM.yyyy", RuCulture)} г. ");
        SetCell(sheet, "F10", BuildTripPeriodText(detail));
        SetCell(sheet, "D12", BuildPassportLine(passportSeries, passportNumber));
        SetCell(sheet, "A37", detail.PurposeRu.Trim());
        if (!string.IsNullOrWhiteSpace(detail.PurposeEn))
            SetCell(sheet, "A38", detail.PurposeEn.Trim());

        var fromText = FormatRussianDate(detail.DateFrom, includeYearWord: true);
        var toText = FormatRussianDate(detail.DateTo, includeYearWord: false);
        SetCell(sheet, "F43", $"с/from {fromText}  по/till {toText}");

        using var output = new MemoryStream();
        workbook.Write(output, leaveOpen: true);
        return output.ToArray();
    }

    private static Stream OpenTemplateStream()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("BusinessTripCertificate.xlsx", StringComparison.OrdinalIgnoreCase));
        if (resourceName is not null)
            return assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException("Certificate template resource stream is empty.");

        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "Hr", "Templates", "BusinessTripCertificate.xlsx"),
            Path.Combine(baseDir, "Dcs", "Templates", "BusinessTripCertificate.xlsx"),
        };
        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return File.OpenRead(path);
        }

        throw new FileNotFoundException("Business trip certificate template was not found.");
    }

    private static void SetCell(ISheet sheet, string address, string value)
    {
        var (rowIndex, colIndex) = ResolveWritableCell(sheet, address);
        var row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
        var cell = row.GetCell(colIndex) ?? row.CreateCell(colIndex);

        // Template cells use inlineStr; SetCellValue alone leaves stale <is><t> text that Excel still displays.
        if (cell is XSSFCell xssfCell)
        {
            xssfCell.SetBlank();
            xssfCell.SetCellValue(new XSSFRichTextString(value));
            return;
        }

        cell.SetBlank();
        cell.SetCellValue(value);
    }

    private static void SetWrappedMergedCell(ISheet sheet, string address, string value, float rowHeightPoints = 36f)
    {
        SetCell(sheet, address, value);
        var (rowIndex, colIndex) = ResolveWritableCell(sheet, address);
        var row = sheet.GetRow(rowIndex);
        var cell = row?.GetCell(colIndex);
        if (row is null || cell is null) return;

        var style = sheet.Workbook.CreateCellStyle();
        style.CloneStyleFrom(cell.CellStyle);
        style.WrapText = true;
        style.VerticalAlignment = VerticalAlignment.Top;
        style.Alignment = HorizontalAlignment.Center;
        cell.CellStyle = style;

        if (row.Height < 1 || row.HeightInPoints < rowHeightPoints)
            row.HeightInPoints = rowHeightPoints;
    }

    private static void ClearCell(ISheet sheet, string address)
    {
        var (rowIndex, colIndex) = ResolveWritableCell(sheet, address);
        var row = sheet.GetRow(rowIndex);
        var cell = row?.GetCell(colIndex);
        if (cell is null) return;
        cell.SetBlank();
    }

    private static (int RowIndex, int ColIndex) ResolveWritableCell(ISheet sheet, string address)
    {
        var rowIndex = GetRowIndex(address);
        var colIndex = GetColumnIndex(address);

        for (var i = 0; i < sheet.NumMergedRegions; i++)
        {
            var region = sheet.GetMergedRegion(i);
            if (rowIndex < region.FirstRow || rowIndex > region.LastRow
                || colIndex < region.FirstColumn || colIndex > region.LastColumn)
                continue;

            return (region.FirstRow, region.FirstColumn);
        }

        return (rowIndex, colIndex);
    }

    private static int GetRowIndex(string address)
    {
        var letters = new string(address.TakeWhile(char.IsLetter).ToArray());
        var digits = address[letters.Length..];
        return int.Parse(digits, CultureInfo.InvariantCulture) - 1;
    }

    private static int GetColumnIndex(string address)
    {
        var letters = new string(address.TakeWhile(char.IsLetter).ToArray());
        var col = 0;
        foreach (var ch in letters)
            col = col * 26 + (char.ToUpperInvariant(ch) - 'A' + 1);
        return col - 1;
    }

    public static string BuildCertificateNumber(HrBusinessTripRequestDetail detail, HrBusinessTripTraveler traveler)
    {
        var orderDisplay = FormatOrderDisplayNumber(detail.OrderNumber);
        var travelerIndex = detail.Travelers
            .OrderBy(t => t.SortOrder)
            .ToList()
            .FindIndex(t => t.Id == traveler.Id) + 1;
        return travelerIndex <= 1 ? orderDisplay : $"{orderDisplay}-{travelerIndex}";
    }

    private static string FormatOrderDisplayNumber(string? orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber)) return "___-bt";

        var parts = orderNumber.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 3
            && string.Equals(parts[0], "HBO", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(parts[^1], out var seq))
            return $"{seq}-bt";

        return orderNumber;
    }

    private static string BuildTripPeriodText(HrBusinessTripRequestDetail detail)
    {
        var daysText = FormatRussianDays(detail.DaysCount);
        var fromText = FormatRussianDate(detail.DateFrom, includeYearWord: true);
        var toText = FormatRussianDate(detail.DateTo, includeYearWord: false);
        return $"{daysText} с {fromText} по {toText}";
    }

    private static string FormatRussianDays(int days)
    {
        var mod10 = days % 10;
        var mod100 = days % 100;
        if (mod100 is >= 11 and <= 14)
            return $"{days} дней";

        return mod10 switch
        {
            1 => $"{days} день",
            2 or 3 or 4 => $"{days} дня",
            _ => $"{days} дней",
        };
    }

    private static string FormatRussianDate(DateTime date, bool includeYearWord)
    {
        var month = RuCulture.DateTimeFormat.MonthGenitiveNames[date.Month - 1];
        return includeYearWord
            ? $"{date.Day} {month} {date.Year} года"
            : $"{date.Day} {month} {date.Year} г";
    }

    private static string BuildPassportLine(string? series, string? number)
    {
        if (!string.IsNullOrWhiteSpace(series) && !string.IsNullOrWhiteSpace(number))
            return $"Действительно при предъявлении паспорта серия  {series.Trim().ToUpperInvariant()} {number.Trim()}";
        return "Действительно при предъявлении паспорта серия  ________________";
    }

    private static string SanitizeSheetName(string name)
    {
        var invalid = new[] { '\\', '/', '*', '?', ':', '[', ']' };
        var cleaned = new string(name.Select(ch => invalid.Contains(ch) ? ' ' : ch).ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
            cleaned = "Certificate";
        return cleaned.Length <= 31 ? cleaned : cleaned[..31];
    }
}
