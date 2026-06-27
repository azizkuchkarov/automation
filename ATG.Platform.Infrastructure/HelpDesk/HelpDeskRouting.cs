using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Infrastructure.HelpDesk;

public static class HelpDeskRouting
{
    /// <summary>Maps category + requester org to target department code.</summary>
    public static string? ResolveDepartmentCode(TicketCategory category, string organizationCode)
    {
        var org = organizationCode.ToUpperInvariant();
        return category switch
        {
            TicketCategory.IT => org switch
            {
                "HO" => "HO-ITDIG",
                "BMGMC" => "BMGMC-ITDIG",
                _ => "BMGMC-ITDIG"
            },
            TicketCategory.Administration => org switch
            {
                "HO" => "HO-ADM-ADMIN",
                "BMGMC" => "BMGMC-ADM",
                _ => "BMGMC-ADM"
            },
            TicketCategory.Accountant => org switch
            {
                "HO" => "HO-ACCT",
                "BMGMC" => "BMGMC-ACCT",
                _ => "BMGMC-ACCT"
            },
            TicketCategory.Transport => org switch
            {
                "HO" => "HO-ADM-TRANS",
                "BMGMC" => "BMGMC-TRANS",
                _ => "BMGMC-TRANS"
            },
            TicketCategory.TravelTickets => org switch
            {
                "HO" => "HO-ADM-ADMIN",
                "BMGMC" => "BMGMC-ADM",
                _ => "BMGMC-ADM"
            },
            TicketCategory.Translator => org switch
            {
                "HO" => "HO-DCPR-TRNS",
                "BMGMC" => "BMGMC-DCPR",
                _ => "BMGMC-DCPR"
            },
            _ => null
        };
    }

    public static IReadOnlyList<HelpDeskCategoryInfo> Categories { get; } =
    [
        new(TicketCategory.IT, "IT Section", "IT отдел", "Monitor", "atg-blue"),
        new(TicketCategory.Administration, "Administration", "Администрирование", "Building2", "atg-teal"),
        new(TicketCategory.Accountant, "Accountant", "Бухгалтерия", "Calculator", "emerald"),
        new(TicketCategory.Transport, "Transport", "Транспорт", "Truck", "atg-purple"),
        new(TicketCategory.TravelTickets, "Travel Tickets", "Командировки", "Plane", "atg-amber"),
        new(TicketCategory.Translator, "Translator", "Переводчик", "Languages", "cyan"),
    ];
}

public record HelpDeskCategoryInfo(
    TicketCategory Category,
    string NameEn,
    string NameRu,
    string Icon,
    string Color);
