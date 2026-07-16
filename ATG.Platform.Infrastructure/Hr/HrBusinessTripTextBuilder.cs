namespace ATG.Platform.Infrastructure.Hr;

public static class HrBusinessTripTextBuilder
{
    public static string BuildTravelerLineRu(string fullNameRu, string positionRu) =>
        $"{fullNameRu.Trim()}, {positionRu.Trim()}";

    public static string BuildTravelerLineEn(string? fullNameEn, string? positionEn, string fullNameRu, string positionRu)
    {
        var name = string.IsNullOrWhiteSpace(fullNameEn) ? fullNameRu.Trim() : fullNameEn.Trim();
        var position = string.IsNullOrWhiteSpace(positionEn) ? positionRu.Trim() : positionEn.Trim();
        return $"{name}, {position}";
    }

    public static string BuildDaysRu(int days) => days switch
    {
        1 => "1 день",
        >= 2 and <= 4 => $"{days} дня",
        _ => $"{days} дней"
    };

    public static string BuildDaysEn(int days) => days == 1 ? "1 day" : $"{days} days";

    public static string FormatDateRu(DateTime date) =>
        date.ToString("d MMMM yyyy", new System.Globalization.CultureInfo("ru-RU")) + " г.";

    public static string FormatDateEn(DateTime date) =>
        date.ToString("MMMM d, yyyy", System.Globalization.CultureInfo.InvariantCulture);

    public static int ComputeDaysInclusive(DateTime from, DateTime to)
    {
        var start = from.Date;
        var end = to.Date;
        if (end < start) return 0;
        return (end - start).Days + 1;
    }
}
