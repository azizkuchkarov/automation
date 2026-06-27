using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Infrastructure.Dcs;

public static class MarketingDeadlineService
{
    public static readonly IReadOnlyDictionary<MarketingRequestCategory, int> CategoryWorkingDays =
        new Dictionary<MarketingRequestCategory, int>
        {
            [MarketingRequestCategory.Category1] = 10,
            [MarketingRequestCategory.Category2] = 15,
            [MarketingRequestCategory.Category3] = 20,
            [MarketingRequestCategory.Category4] = 25,
        };

    public static int GetWorkingDaysForCategory(MarketingRequestCategory category) =>
        CategoryWorkingDays[category];

    public static DateOnly CalculateDeadline(DateOnly startDate, MarketingRequestCategory category)
    {
        var workingDays = GetWorkingDaysForCategory(category);
        var current = startDate;
        var counted = 0;

        while (counted < workingDays)
        {
            current = current.AddDays(1);
            if (current.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                counted++;
        }

        return current;
    }

    public static int GetRemainingWorkingDays(DateOnly deadline, DateOnly? from = null)
    {
        var today = from ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (today >= deadline) return 0;

        var days = 0;
        var current = today;
        while (current < deadline)
        {
            current = current.AddDays(1);
            if (current.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                days++;
        }

        return days;
    }

    public static int GetWorkingDaysSince(DateTime startUtc, DateOnly? from = null)
    {
        var start = DateOnly.FromDateTime(startUtc);
        var today = from ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (today <= start) return 0;

        var days = 0;
        var current = start;
        while (current < today)
        {
            current = current.AddDays(1);
            if (current.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                days++;
        }

        return days;
    }

    public static string GetDeadlineColor(DateOnly deadline) =>
        GetRemainingWorkingDays(deadline) switch
        {
            <= 0 => "red",
            <= 3 => "orange",
            <= 7 => "yellow",
            _ => "green",
        };
}
