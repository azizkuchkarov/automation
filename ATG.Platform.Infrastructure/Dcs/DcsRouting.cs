using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Infrastructure.Dcs;

public static class DcsRouting
{
    /// <summary>Clerk who registers incoming letters into the system.</summary>
    public const string IncomingRegistrarEmail = "m.yugay@atg.uz";

    public static string? GetRegistrarEmail(DocumentType type) =>
        type switch
        {
            DocumentType.Incoming => IncomingRegistrarEmail,
            _ => null
        };

    public static string? ResolveDepartmentCode(DocumentType type, string organizationCode)
    {
        var org = organizationCode.ToUpperInvariant();
        return type switch
        {
            DocumentType.Incoming or DocumentType.Outgoing or DocumentType.Memo
                or DocumentType.MinutesOfMeeting or DocumentType.Order => org switch
            {
                "HO" => "HO-DCPR-CLER",
                "BMGMC" => "BMGMC-DCPR",
                _ => "BMGMC-DCPR"
            },
            DocumentType.TechnicalAssignment => org switch
            {
                "HO" => "HO-CPROC",
                "BMGMC" => "BMGMC-CPROC",
                _ => "BMGMC-CPROC"
            },
            DocumentType.MaterialServiceRequisition or DocumentType.SupplySection => org switch
            {
                "HO" => "HO-CPROC-DOM",
                "BMGMC" => "BMGMC-CPROC",
                _ => "BMGMC-CPROC"
            },
            DocumentType.Contract => org switch
            {
                "HO" => "HO-CPROC-CADM",
                "BMGMC" => "BMGMC-CPROC",
                _ => "BMGMC-CPROC"
            },
            DocumentType.Marketing => org switch
            {
                "HO" => "HO-MKT-MKT",
                "BMGMC" => "BMGMC-DCPR",
                _ => "BMGMC-DCPR"
            },
            DocumentType.Payment => org switch
            {
                "HO" => "HO-FINPLAN",
                "BMGMC" => "BMGMC-DCPR",
                _ => "BMGMC-DCPR"
            },
            DocumentType.Accounting => org switch
            {
                "HO" => "HO-ACCT",
                "BMGMC" => "BMGMC-DCPR",
                _ => "BMGMC-DCPR"
            },
            DocumentType.ProcurementRequest => org switch
            {
                "HO" => "HO-CPROC-DOM",
                "BMGMC" => "BMGMC-TECH",
                _ => "BMGMC-TECH"
            },
            _ => null
        };
    }

    public static string NumberPrefix(DocumentType type) => type switch
    {
        DocumentType.Incoming => "OTHER-LI",
        DocumentType.Outgoing => "OUT",
        DocumentType.Memo => "MEMO",
        DocumentType.MinutesOfMeeting => "MOM",
        DocumentType.Order => "ORD",
        DocumentType.TechnicalAssignment => "TA",
        DocumentType.MaterialServiceRequisition => "MR",
        DocumentType.Marketing => "MKT",
        DocumentType.Contract => "CON",
        DocumentType.Payment => "PAY",
        DocumentType.Accounting => "ACC",
        DocumentType.SupplySection => "SUP",
        DocumentType.ProcurementRequest => "ATG-REQ",
        _ => "DOC"
    };

    public static IReadOnlyList<DcsTypeInfo> Types { get; } =
    [
        new(DocumentType.Incoming, "Incoming", "Входящие", "office", "Inbox", "atg-blue", IncomingRegistrarEmail),
        new(DocumentType.Outgoing, "Outgoing", "Исходящие", "office", "Send", "atg-teal"),
        new(DocumentType.Memo, "Memo", "Служебные записки", "office", "FileText", "violet"),
        new(DocumentType.MinutesOfMeeting, "Minutes of Meetings", "Протоколы заседаний", "office", "Users", "indigo"),
        new(DocumentType.Order, "Orders", "Приказы", "office", "ScrollText", "orange"),
        new(DocumentType.TechnicalAssignment, "TA", "Технические задания", "procurement", "ClipboardList", "atg-blue"),
        new(DocumentType.ProcurementRequest, "Request", "Заявка", "procurement", "FilePlus", "sky"),
        new(DocumentType.MaterialServiceRequisition, "MR / SR", "MR / SR", "procurement", "Package", "teal"),
        new(DocumentType.Marketing, "Marketing", "Маркетинг", "procurement", "Megaphone", "pink"),
        new(DocumentType.Contract, "Contract", "Контракты", "procurement", "FileSignature", "emerald"),
        new(DocumentType.Payment, "Payment", "Оплата", "procurement", "CreditCard", "amber"),
        new(DocumentType.Accounting, "Accounting", "Бухгалтерия", "procurement", "Calculator", "slate"),
        new(DocumentType.SupplySection, "Supply Section", "Снабжение", "procurement", "Truck", "purple"),
    ];
}

public record DcsTypeInfo(
    DocumentType Type,
    string NameEn,
    string NameRu,
    string Section,
    string Icon,
    string Color,
    string? RegistrarEmail = null);
