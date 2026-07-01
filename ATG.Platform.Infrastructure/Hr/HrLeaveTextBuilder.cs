using ATG.Platform.Domain.Entities;
using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Infrastructure.Hr;

public static class HrLeaveTextBuilder
{
    public static (string Ru, string En) BuildItemText(HrLeaveRequestItem item, string periodLabel)
    {
        var from = item.DateFrom?.ToString("d MMMM yyyy") ?? "";
        var to = item.DateTo?.ToString("d MMMM yyyy") ?? "";
        var fromEn = item.DateFrom?.ToString("MMMM d, yyyy") ?? "";
        var toEn = item.DateTo?.ToString("MMMM d, yyyy") ?? "";

        return item.Type switch
        {
            HrLeaveItemType.RegularLeave => (
                $"Прошу предоставить мне трудовой отпуск за период {periodLabel} с {from} по {to}.",
                $"You are kindly requested to provide me regular leave for the period of {periodLabel} from {fromEn} till {toEn}."),
            HrLeaveItemType.CompensationDays => (
                $"Неиспользованные {item.DaysCount} дополнительных дней отпуска за период {periodLabel} прошу заменить денежной компенсацией.",
                $"Provide me with monetary compensation for {item.DaysCount} unused additional days of vacation for the period of {periodLabel}."),
            HrLeaveItemType.UnpaidLeave => (
                $"Прошу предоставить мне отпуск без сохранения заработной платы с {from} по {to}.",
                $"You are kindly requested to provide me unpaid leave from {fromEn} till {toEn}."),
            HrLeaveItemType.PartialPayLeave => (
                $"Прошу предоставить мне отпуск с частичным сохранением заработной платы с {from} по {to}.",
                $"You are kindly requested to provide me leave with partial pay starting from {fromEn} till {toEn}."),
            HrLeaveItemType.FinancialAid => (
                $"Прошу Вас предоставить мне материальную помощь к трудовому отпуску за период {periodLabel}.",
                $"You are kindly requested to provide me financial aid due to regular leave for the period of {periodLabel}."),
            _ => (item.NoteRu ?? "", item.NoteEn ?? "")
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
            HrLeaveItemType.UnpaidLeave => $"Unpaid leave request",
            HrLeaveItemType.PartialPayLeave => $"Partial pay leave {periodLabel}",
            HrLeaveItemType.FinancialAid => $"Financial aid {periodLabel}",
            _ => $"Leave request {periodLabel}"
        };
    }
}
