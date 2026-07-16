using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Infrastructure.Hr;

public static class HrLeaveTextBuilder
{
    public static (string Ru, string En) BuildItemText(HrLeaveRequestItem item, string periodLabel)
    {
        var from = FormatDate(item.DateFrom);
        var to = FormatDate(item.DateTo);
        var periodRu = FormatPeriodRu(periodLabel);
        var periodEn = FormatPeriodEn(periodLabel);
        var noteRu = FormatNoteSuffix(item.NoteRu);
        var noteEn = FormatNoteSuffix(item.NoteEn);

        return item.Type switch
        {
            HrLeaveItemType.RegularLeave => (
                $"Прошу Вас предоставить мне очередной трудовой отпуск за период {periodRu} с {from} по {to}{noteRu}",
                $"You are kindly requested to provide me regular leave for the period of {periodEn} from {from} to {to}{noteEn}"),
            HrLeaveItemType.CompensationDays => (
                $"Неиспользованные {item.DaysCount} дополнительных дней отпуска за период {periodRu} прошу заменить денежной компенсацией{noteRu}",
                $"Provide me with monetary compensation for {item.DaysCount} unused additional days of vacation for the period of {periodEn}{noteEn}"),
            HrLeaveItemType.UnpaidLeave => (
                $"Прошу Вас предоставить мне отпуск без сохранения заработной платы с {from} по {to}{noteRu}",
                $"You are kindly requested to provide me unpaid leave from {from} to {to}{noteEn}"),
            HrLeaveItemType.PartialPayLeave => (
                $"Прошу Вас предоставить мне отпуск с частичным сохранением заработной платы с {from} по {to}{noteRu}",
                $"You are kindly requested to provide me leave with partial pay from {from} to {to}{noteEn}"),
            HrLeaveItemType.FinancialAid => (
                $"Прошу Вас предоставить мне материальную помощь к трудовому отпуску за период {periodRu}{noteRu}",
                $"You are kindly requested to provide me financial aid due to regular leave for the period of {periodEn}{noteEn}"),
            _ => (item.NoteRu ?? "", item.NoteEn ?? "")
        };
    }

    public static (string Ru, string En) BuildDocumentTitlePair(HrLeaveItemType? primaryType)
    {
        return primaryType switch
        {
            HrLeaveItemType.RegularLeave => (
                "Заявление на трудовой отпуск /",
                "Application for regular leave"),
            HrLeaveItemType.CompensationDays => (
                "Заявление о денежной компенсации /",
                "Application for monetary compensation"),
            HrLeaveItemType.UnpaidLeave => (
                "Заявление на отпуск без сохранения заработной платы /",
                "Application for unpaid leave"),
            HrLeaveItemType.PartialPayLeave => (
                "Заявление на отпуск с частичным сохранением заработной платы /",
                "Application for leave with partial pay"),
            HrLeaveItemType.FinancialAid => (
                "Заявление о материальной помощи /",
                "Application for financial aid"),
            _ => (
                "Заявление /",
                "Application")
        };
    }

    public static string BuildDocumentTitle(IReadOnlyList<HrLeaveRequestItem> items, string periodLabel)
    {
        var primary = items.OrderBy(i => i.SortOrder).FirstOrDefault();
        if (primary is null) return $"Leave request {periodLabel}";
        return primary.Type switch
        {
            HrLeaveItemType.RegularLeave => $"Leave request {periodLabel}",
            HrLeaveItemType.CompensationDays => $"Vacation compensation {periodLabel}",
            HrLeaveItemType.UnpaidLeave => "Unpaid leave request",
            HrLeaveItemType.PartialPayLeave => $"Partial pay leave {periodLabel}",
            HrLeaveItemType.FinancialAid => $"Financial aid {periodLabel}",
            _ => $"Leave request {periodLabel}"
        };
    }

    private static string FormatDate(DateTime? date) => date?.ToString("dd.MM.yyyy") ?? "";

    private static string FormatPeriodRu(string periodLabel)
    {
        var trimmed = periodLabel.Trim();
        return trimmed.Contains("гг", StringComparison.OrdinalIgnoreCase) ? trimmed : $"{trimmed} гг.";
    }

    private static string FormatPeriodEn(string periodLabel)
    {
        var trimmed = periodLabel.Trim();
        return trimmed.Contains("year", StringComparison.OrdinalIgnoreCase) ? trimmed : $"{trimmed} years";
    }

    private static string FormatNoteSuffix(string? note)
    {
        if (string.IsNullOrWhiteSpace(note)) return ".";
        var trimmed = note.Trim().TrimStart(',', '.', ' ');
        return trimmed.EndsWith('.') ? $" {trimmed}" : $" {trimmed}.";
    }
}
