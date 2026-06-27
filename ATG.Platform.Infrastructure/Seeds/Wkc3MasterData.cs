using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Infrastructure.Seeds;

/// <summary>Regional station WKC3 — bilingual RU/EN.</summary>
public static class Wkc3MasterData
{
    public const string OrganizationCode = "WKC3";
    public const string OrganizationNameRu = "Региональная станция WKC3";
    public const string OrganizationNameEn = "Regional station WKC3";

    public static readonly string[] LegacyOrganizationCodes = Array.Empty<string>();

    public static readonly HoDepartment[] Departments =
    [
        new("WKC3-EXEC", "Руководство", "Leadership"),
        new("WKC3-ENG", "Инженерная служба", "Engineering"),
        new("WKC3-OPS", "Эксплуатация", "Operations"),
    ];

    public static readonly string[] LegacyDepartmentCodes =
        ["WKC3-ADM", "WKC3-SAF", "WKC3-IT"];

    public static readonly StationStaffMember[] Staff =
    [
        // Руководство / Leadership
        new("ATG-384", "Сюэфэн", "Бай", null, "Xuefeng", "Bai", null, "baixuefeng@atg.uz", "94 660 50 51", "5112, 5216", OrganizationCode, "WKC3-EXEC",
            "Начальник WKC3", "Manager of WKC3", "NACHALNIK", UserRole.BMGMCNachalnikiOtdeli),
        new("ATG-385", "Фусинь", "Чэнь", null, "Fuxin", "Chen", null, "chenfuxin@atg.uz", "93 111 26 90", "5112", OrganizationCode, "WKC3-EXEC",
            "Заместитель начальника КС-3", "Deputy Manager of WKC-3", "MANAGER", UserRole.BMGMCNachalnikiOtdeli),
        new("ATG-386", "Сохибжон", "Рахмонов", null, "Sokhibjon", "Rakhmonov", null, "s.rahmonov@atg.uz", "94 660 30 59", "5113", OrganizationCode, "WKC3-EXEC",
            "Заместитель начальника КС-3", "Deputy Manager of WKC-3", "MANAGER", UserRole.BMGMCNachalnikiOtdeli),
        new("ATG-387", "Анвар", "Турдиев", null, "Anvar", "Turdiev", null, "a.turdiev@atg.uz", "94 660 26 20", "5113", OrganizationCode, "WKC3-EXEC",
            "Заместитель начальника КС-3", "Deputy Manager of WKC-3", "MANAGER", UserRole.BMGMCNachalnikiOtdeli),

        // Инженерная служба / Engineering
        new("ATG-388", "Хамзабек", "Гиёсов", null, "Hamzabek", "Giyosov", null, "h.giyosov@atg.uz", "94 660 21 67", "5013", OrganizationCode, "WKC3-ENG",
            "Инженер по компрессорам", "Compressors engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-389", "Юнцин", "Дун", null, "Yongqing", "Dong", null, "dongyongqing@atg.uz", "94 660 26 88", "5112, 5218", OrganizationCode, "WKC3-ENG",
            "Инженер по компрессорам", "Compressors engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-390", "Нодиржон", "Абдуллоев", null, "Nodirjon", "Abdulloev", null, "n.abdulloev@atg.uz", "94 660 08 66", "5013", OrganizationCode, "WKC3-ENG",
            "Инженер-механик по оборудованию", "Mechanics equipment engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-391", "Фан", "Чжан", null, "Fan", "Zhang", null, "zhangfan@atg.uz", null, "5005", OrganizationCode, "WKC3-ENG",
            "Инженер-механик по оборудованию", "Mechanics equipment engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-392", "Миржон", "Насруллаев", null, "Mirjon", "Nasrullaev", null, "m.nasrullaev@atg.uz", "94 660 30 24", "5013", OrganizationCode, "WKC3-ENG",
            "Инженер КИПиА", "Automatic Equipments System Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-393", "Бао", "Чжан", null, "Bao", "Zhang", null, "zhangbao@atg.uz", "94 660 74 21", "5005, 5230", OrganizationCode, "WKC3-ENG",
            "Инженер КИПиА", "Automatic Equipments System Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-394", "Вахиджан", "Умарбаев", null, "Vakhidjan", "Umarbaev", null, "v.umarbaev@atg.uz", "94 660 30 50", "5013", OrganizationCode, "WKC3-ENG",
            "Инженер-электрик", "Electrical Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-395", "Вуган", "Фань", null, "Wugang", "Fan", null, "fanwugang@atg.uz", "50 105 06 57", "5005", OrganizationCode, "WKC3-ENG",
            "Инженер-электрик", "Electrical Engineer", "ENGINEER", UserRole.StationEngineer),

        // Эксплуатация / Operations
        new("ATG-396", "Хаким", "Закиров", null, "Khakim", "Zakirov", null, "kh.zakirov@atg.uz", "94 660 26 23", "5001", OrganizationCode, "WKC3-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-397", "Шарафаддин", "Хаитов", null, "Sharafaddin", "Khaitov", null, "sh.khaitov@atg.uz", "94 655 02 48", "5001", OrganizationCode, "WKC3-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-398", "Абдулло", "Кулдашев", null, "Abdullo", "Kuldashev", null, "ab.kuldashev@atg.uz", "94 660 56 07", "5001", OrganizationCode, "WKC3-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-399", "Мирзобек", "Рузимов", null, "Mirzobek", "Ruzimov", null, "m.ruzimov@atg.uz", "94 660 50 18", "5001", OrganizationCode, "WKC3-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-400", "Шухрат", "Дуров", null, "Shukhrat", "Durov", null, "sh.durov@atg.uz", "94 660 26 49", "5001", OrganizationCode, "WKC3-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-401", "Иброхим", "Кузиев", null, "Ibrokhim", "Kuziev", null, "i.quziyev@atg.uz", "94 620 56 52", "5001", OrganizationCode, "WKC3-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-402", "Асадбек", "Касимов", null, "Asadbek", "Kasimov", null, "a.kasimov@atg.uz", "93 007 52 66", "5001", OrganizationCode, "WKC3-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
    ];

    public static readonly StationDefinition Definition = new(
        OrganizationCode,
        OrganizationNameRu,
        OrganizationNameEn,
        LegacyOrganizationCodes,
        Departments,
        LegacyDepartmentCodes,
        Staff);
}
