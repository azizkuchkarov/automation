using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Infrastructure.Seeds;

/// <summary>Regional station WKC2 / GCS — bilingual RU/EN.</summary>
public static class Wkc2GcsMasterData
{
    public const string OrganizationCode = "WKC2-GCS";
    public const string OrganizationNameRu = "Региональная станция WKC2 / GCS";
    public const string OrganizationNameEn = "Regional station WKC2 / GCS";

    public static readonly string[] LegacyOrganizationCodes = ["WKC2", "GCS"];

    public static readonly HoDepartment[] Departments =
    [
        new("WKC2-GCS-EXEC", "Руководство", "Leadership"),
        new("WKC2-GCS-ENG", "Инженерная служба", "Engineering"),
        new("WKC2-GCS-OPS", "Эксплуатация", "Operations"),
    ];

    public static readonly string[] LegacyDepartmentCodes =
        ["WKC2-GCS-ADM", "WKC2-GCS-SAF", "WKC2-GCS-IT"];

    public static readonly StationStaffMember[] Staff =
    [
        // Руководство / Leadership
        new("ATG-352", "Вэй", "Лю", null, "Wei", "Liu", null, "liuwei@atg.uz", "94 660 26 38", "7111, 8147", OrganizationCode, "WKC2-GCS-EXEC",
            "Региональный менеджер WKC2/GCS", "Regional Manager of WKC2/GCS", "NACHALNIK", UserRole.BMGMCNachalnikiOtdeli),
        new("ATG-353", "Замир", "Мамедов", null, "Zamir", "Mamedov", null, "z.mamedov@atg.uz", "94 660 26 65", "7113", OrganizationCode, "WKC2-GCS-EXEC",
            "Первый заместитель регионального менеджера WKC2/GCS", "First Deputy Regional Manager of WKC2/GCS", "MANAGER", UserRole.BMGMCNachalnikiOtdeli),
        new("ATG-354", "Чжэнь", "Юй", null, "Zhen", "Yu", null, "yuzhen@atg.uz", "94 888 01 67", "7112", OrganizationCode, "WKC2-GCS-EXEC",
            "Заместитель регионального менеджера WKC2/GCS", "Deputy Regional Manager of WKC2/GCS", "MANAGER", UserRole.BMGMCNachalnikiOtdeli),
        new("ATG-355", "Уткир", "Раджабов", null, "Utkir", "Radjabov", null, "u.radjabov@atg.uz", "94 660 26 89", "8113", OrganizationCode, "WKC2-GCS-EXEC",
            "Заместитель регионального менеджера WKC2/GCS", "Deputy Regional Manager of WKC2/GCS", "MANAGER", UserRole.BMGMCNachalnikiOtdeli),
        new("ATG-356", "Феруз", "Жалилов", null, "Feruz", "Jalilov", null, "f.jalilov@atg.uz", "94 660 21 32", "8113", OrganizationCode, "WKC2-GCS-EXEC",
            "Заместитель начальника WKC2/GCS", "Deputy Manager of WKC2/GCS", "MANAGER", UserRole.BMGMCNachalnikiOtdeli),
        new("ATG-357", "Ган", "Лю", null, "Gang", "Liu", null, "liugang@atg.uz", "93 505 65 46", "8111", OrganizationCode, "WKC2-GCS-EXEC",
            "Заместитель регионального менеджера WKC2/GCS", "Deputy Regional Manager of WKC2/GCS", "MANAGER", UserRole.BMGMCNachalnikiOtdeli),

        // Инженерная служба / Engineering
        new("ATG-358", "Фахриддин", "Утаганов", null, "Fahriddin", "Utaganov", null, "f.utaganov@atg.uz", "94 660 26 95", "7013", OrganizationCode, "WKC2-GCS-ENG",
            "Инженер по компрессорам", "Compressors engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-359", "Лунфэй", "Чжан", null, "Longfei", "Zhang", null, "zhanglongfei@atg.uz", "50 333 01 12", "8116, 8035", OrganizationCode, "WKC2-GCS-ENG",
            "Инженер по компрессорам", "Compressors engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-360", "Линьсун", "Ван", null, "Linsong", "Wang", null, "wanglinsong@atg.uz", "50 333 03 25", "7003", OrganizationCode, "WKC2-GCS-ENG",
            "Инженер по компрессорам", "Compressors engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-361", "Шавкат", "Абдулманов", null, "Shavkat", "Abdulmanov", null, "sh.abdulmanov@atg.uz", "94 660 21 50", "8002", OrganizationCode, "WKC2-GCS-ENG",
            "Инженер-механик по оборудованию", "Mechanics equipment engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-362", "Мухаммаджон", "Тураев", null, "Muhammadjon", "Turaev", null, "m.turaev@atg.uz", "94 660 23 12", "7013", OrganizationCode, "WKC2-GCS-ENG",
            "Инженер-механик по оборудованию", "Mechanics equipment engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-363", "Дун", "Хуан", null, "Dong", "Huang", null, "huangdong@atg.uz", "94 660 61 75", "7112, 8145", OrganizationCode, "WKC2-GCS-ENG",
            "Инженер КИПиА", "Automatic equipment system engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-364", "Абдухаким", "Муминов", null, "Abduhakim", "Muminov", null, "a.muminov@atg.uz", "94 660 98 43", "7012", OrganizationCode, "WKC2-GCS-ENG",
            "Инженер КИПиА", "Automatic Equipments System Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-365", "Бо", "Ван", null, "Bo", "Wang", null, "wangbo@atg.uz", "94 660 90 98", "8116", OrganizationCode, "WKC2-GCS-ENG",
            "Инженер КИПиА", "Automatic equipment system engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-366", "Уктам", "Ёдгоров", null, "Uktam", "Yodgorov", null, "u.yodgorov@atg.uz", "50 177 16 52", "8117", OrganizationCode, "WKC2-GCS-ENG",
            "Инженер КИПиА", "Automatic equipment system engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-367", "Кобил", "Фаттоев", null, "Kobil", "Fattoev", null, "k.fattoev@atg.uz", "94 660 80 85", "7012", OrganizationCode, "WKC2-GCS-ENG",
            "Инженер-электрик", "Electrical Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-368", "Хуэй", "Чжан", null, "Hui", "Zhang", null, "zhanghui@atg.uz", "93 111 25 33", "8116, 8152", OrganizationCode, "WKC2-GCS-ENG",
            "Инженер-электрик", "Electrical Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-369", "Синлинь", "Юй", null, "Xinglin", "Yu", null, "yuxinglin@atg.uz", "50 177 64 08", "7003", OrganizationCode, "WKC2-GCS-ENG",
            "Инженер-электрик", "Electrical Engineer", "ENGINEER", UserRole.StationEngineer),

        // Эксплуатация / Operations
        new("ATG-370", "Саидамирихон", "Ахмедов", null, "Saidamirkhon", "Akhmedov", null, "s.akhmedov@atg.uz", "94 660 26 91", "7001", OrganizationCode, "WKC2-GCS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-371", "Георгий", "Ким", null, "Georgiy", "Kim", null, "g.kim@atg.uz", "93 111 28 87", "8001", OrganizationCode, "WKC2-GCS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-372", "Рустам", "Исхаков", null, "Rustam", "Iskhakov", null, "r.iskhakov@atg.uz", "94 660 30 63", "7001", OrganizationCode, "WKC2-GCS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-373", "Акбар", "Курьязов", null, "Akbar", "Kuryazov", null, "a.kuryazov@atg.uz", "94 655 02 54", "7001", OrganizationCode, "WKC2-GCS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-374", "Шерзодхужа", "Анваров", null, "Sherzodhuja", "Anvarov", null, "sh.anvarov@atg.uz", "94 660 50 17", "8001", OrganizationCode, "WKC2-GCS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-375", "Шухрат", "Исмоилов", null, "Shukhrat", "Ismoilov", null, "sh.ismoilov@atg.uz", "94 660 59 12", "8001", OrganizationCode, "WKC2-GCS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-376", "Эркинжон", "Сафаров", null, "Erkinjon", "Safarov", null, "e.safarov@atg.uz", "94 660 59 11", "8001", OrganizationCode, "WKC2-GCS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-377", "Сардор", "Умаров", null, "Sardor", "Umarov", null, "s.umarov@atg.uz", "94 655 02 56", "7001", OrganizationCode, "WKC2-GCS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-378", "Мирфайзжон", "Наимов", null, "Mirfayzjon", "Naimov", null, "m.naimov@atg.uz", "94 660 03 73", "8001", OrganizationCode, "WKC2-GCS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-379", "Рашид", "Холиков", null, "Rashid", "Kholikov", null, "r.kholikov@atg.uz", "94 660 92 32", "8001", OrganizationCode, "WKC2-GCS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-380", "Хусниддин", "Пулатов", null, "Khusniddin", "Pulatov", null, "k.pulatov@atg.uz", "94 660 95 10", "7001", OrganizationCode, "WKC2-GCS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-381", "Сохибжон", "Салимов", null, "Sokhibjon", "Salimov", null, "s.salimov@atg.uz", "94 660 91 01", "8001", OrganizationCode, "WKC2-GCS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-382", "Мухаммадбек", "Камалов", null, "Mukhammadbek", "Kamalov", null, "m.kamalov@atg.uz", "50 333 05 29", "7001", OrganizationCode, "WKC2-GCS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-383", "Дилшод", "Акромов", null, "Dilshod", "Akromov", null, "d.akromov@atg.uz", "50 590 45 39", "8001", OrganizationCode, "WKC2-GCS-OPS",
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
