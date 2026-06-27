using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Infrastructure.Seeds;

/// <summary>Regional station UCS3 — bilingual RU/EN.</summary>
public static class Ucs3MasterData
{
    public const string OrganizationCode = "UCS3";
    public const string OrganizationNameRu = "Региональная станция UCS3";
    public const string OrganizationNameEn = "Regional station UCS3";

    public static readonly string[] LegacyOrganizationCodes = Array.Empty<string>();

    public static readonly HoDepartment[] Departments =
    [
        new("UCS3-EXEC", "Руководство", "Leadership"),
        new("UCS3-ENG", "Инженерная служба", "Engineering"),
        new("UCS3-OPS", "Эксплуатация", "Operations"),
    ];

    public static readonly string[] LegacyDepartmentCodes =
        ["UCS3-ADM", "UCS3-SAF", "UCS3-IT"];

    public static readonly StationStaffMember[] Staff =
    [
        new("ATG-403", "Хайхао", "Ван", null, "Haihao", "Wang", null, "wanghaihao@atg.uz", "94 660 23 97", "4111, 4002", OrganizationCode, "UCS3-EXEC",
            "Начальник UCS3", "UCS3 Manager", "NACHALNIK", UserRole.BMGMCNachalnikiOtdeli),
        new("ATG-404", "Икром", "Эшмурадов", null, "Ikrom", "Eshmuradov", null, "i.eshmuradov@atg.uz", "94 888 17 50", "4113", OrganizationCode, "UCS3-EXEC",
            "Заместитель начальника UCS3", "Deputy Manager", "MANAGER", UserRole.BMGMCNachalnikiOtdeli),
        new("ATG-405", "Бахтийор", "Мурадов", null, "Bakhtiyor", "Muradov", null, "b.muradov@atg.uz", "94 660 80 87", "4113", OrganizationCode, "UCS3-EXEC",
            "Заместитель начальника UCS3", "Deputy Manager", "MANAGER", UserRole.BMGMCNachalnikiOtdeli),
        new("ATG-406", "Акбар", "Мамуров", null, "Akbar", "Mamurov", null, "a.mamurov@atg.uz", "94 655 02 53", "4033", OrganizationCode, "UCS3-ENG",
            "Инженер по компрессорам", "Compressors engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-407", "Чэнь", "Хань", null, "Chen", "Han", null, "hanchen@atg.uz", "94 660 61 73", "4015", OrganizationCode, "UCS3-ENG",
            "Инженер по компрессорам", "Compressors engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-408", "Фэнци", "Чжоу", null, "Fengqi", "Zhou", null, "zhoufengqi@atg.uz", "94 655 07 50", "4009, 4237", OrganizationCode, "UCS3-ENG",
            "Инженер КИПиА", "Automatic Equipments System Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-409", "Шерзод", "Жумаев", null, "Sherzod", "Jumaev", null, "sh.jumaev@atg.uz", "50 177 68 36", "4033", OrganizationCode, "UCS3-ENG",
            "Инженер КИПиА", "Automatic Equipments System Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-410", "Уткур", "Кенжаев", null, "Utkur", "Kenjaev", null, "u.kenjaev@atg.uz", "94 660 26 01", "4033", OrganizationCode, "UCS3-ENG",
            "Инженер-электрик", "Electrical Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-411", "Сяолян", "Фэн", null, "Xiaoliang", "Feng", null, "fengxiaoliang@atg.uz", "94 660 30 45", "4009, 4216", OrganizationCode, "UCS3-ENG",
            "Инженер-электрик", "Electrical Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-412", "Алишер", "Бабаев", null, "Alisher", "Babaev", null, "a.babaev@atg.uz", "94 660 24 76", "4001", OrganizationCode, "UCS3-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-413", "Шарафутдин", "Абдураимов", null, "Sharafutdin", "Abduraimov", null, "sh.abduraimov@atg.uz", "94 660 50 16", "4001", OrganizationCode, "UCS3-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-414", "Хасан", "Каримов", null, "Khasan", "Karimov", null, "kh.karimov@atg.uz", "94 660 01 15", "4001", OrganizationCode, "UCS3-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-415", "Хумоюн", "Абдурахманов", null, "Khumoyun", "Abdurakhmanov", null, "kh.abdurakhmanov@atg.uz", "94 660 50 21", "4001", OrganizationCode, "UCS3-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
    ];

    public static readonly StationDefinition Definition = new(
        OrganizationCode, OrganizationNameRu, OrganizationNameEn,
        LegacyOrganizationCodes, Departments, LegacyDepartmentCodes, Staff);
}
