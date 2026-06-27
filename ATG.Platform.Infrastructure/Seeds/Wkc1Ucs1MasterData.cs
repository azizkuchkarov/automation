using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Infrastructure.Seeds;

/// <summary>Regional station WKC1 / UCS1 — bilingual RU/EN.</summary>
public static class Wkc1Ucs1MasterData
{
    public const string OrganizationCode = "WKC1-UCS1";
    public const string OrganizationNameRu = "Региональная станция WKC1 / UCS1";
    public const string OrganizationNameEn = "Regional station WKC1 / UCS1";

    public static readonly string[] LegacyOrganizationCodes = ["WKC1", "UCS1"];

    public static readonly HoDepartment[] Departments =
    [
        new("WKC1-UCS1-EXEC", "Руководство", "Leadership"),
        new("WKC1-UCS1-ENG", "Инженерная служба", "Engineering"),
        new("WKC1-UCS1-OPS", "Эксплуатация", "Operations"),
    ];

    public static readonly string[] LegacyDepartmentCodes =
        ["WKC1-UCS1-ADM", "WKC1-UCS1-SAF", "WKC1-UCS1-IT"];

    public static readonly StationStaffMember[] Staff =
    [
        // Руководство / Leadership
        new("ATG-315", "Чжэньчжунь", "Дин", null, "Zhenjun", "Ding", null, "dingzhenjun@atg.uz", "94 660 80 73", "3111, 1202", OrganizationCode, "WKC1-UCS1-EXEC",
            "Региональный менеджер WKC1/UCS1", "Regional Manager of WKC1/UCS1", "NACHALNIK", UserRole.BMGMCNachalnikiOtdeli),
        new("ATG-316", "Рафаэль", "Канислямов", null, "Rafael", "Kanislyamov", null, "r.kanislyamov@atg.uz", "98 180 30 30", "3113, 1111, 1330", OrganizationCode, "WKC1-UCS1-EXEC",
            "Первый заместитель регионального менеджера WKC1/UCS1", "First Deputy Regional Manager of WKC1/UCS1", "MANAGER", UserRole.BMGMCNachalnikiOtdeli),
        new("ATG-317", "Максим", "Игамратов", null, "Maksim", "Igamratov", null, "m.igamratov@atg.uz", "94 660 01 13", "3113", OrganizationCode, "WKC1-UCS1-EXEC",
            "Заместитель регионального менеджера WKC1/UCS1", "Deputy Regional Manager of WKC1/UCS1", "MANAGER", UserRole.BMGMCNachalnikiOtdeli),
        new("ATG-318", "Рустам", "Ганиев", null, "Rustam", "Ganiev", null, "r.ganiev@atg.uz", "94 660 30 58", "1113", OrganizationCode, "WKC1-UCS1-EXEC",
            "Заместитель регионального менеджера WKC1/UCS1", "Deputy Regional Manager of WKC1/UCS1", "MANAGER", UserRole.BMGMCNachalnikiOtdeli),

        // Инженерная служба / Engineering
        new("ATG-319", "Чжаомин", "Ли", null, "Chaoming", "Li", null, "lichaoming@atg.uz", "94 660 30 47", "3115, 1208", OrganizationCode, "WKC1-UCS1-ENG",
            "Инженер по компрессорам", "Compressors engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-320", "Уктам", "Буронов", null, "Uktam", "Buronov", null, "u.buronov@atg.uz", "94 660 30 57", "1014", OrganizationCode, "WKC1-UCS1-ENG",
            "Инженер по компрессорам", "Compressors engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-321", "Шамсиддин", "Мансуров", null, "Shamsiddin", "Mansurov", null, "sh.mansurov@atg.uz", "93 180 09 13", "3077", OrganizationCode, "WKC1-UCS1-ENG",
            "Инженер по компрессорам", "Compressors engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-322", "Цзя", "Ду", null, "Jia", "Du", null, "dujia@atg.uz", "50 105 06 53", "1016, 1219", OrganizationCode, "WKC1-UCS1-ENG",
            "Инженер по компрессорам", "Compressors engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-323", "Чжиюн", "Бай", null, "Zhiyong", "Bai", null, "baizhiyong@atg.uz", "94 660 30 15", "3115, 1305", OrganizationCode, "WKC1-UCS1-ENG",
            "Инженер-механик по оборудованию", "Mechanics equipment engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-324", "Расим", "Юсупов", null, "Rasim", "Yusupov", null, "r.yusupov@atg.uz", "94 655 02 59", "1014", OrganizationCode, "WKC1-UCS1-ENG",
            "Инженер-механик по оборудованию", "Mechanics equipment engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-325", "Аскар", "Бозоров", null, "Askar", "Bozorov", null, "a.bozorov@atg.uz", "94 660 30 53", "3077", OrganizationCode, "WKC1-UCS1-ENG",
            "Инженер-механик по оборудованию", "Mechanics equipment engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-326", "Вэй", "Сюэ", null, "Wei", "Xue", null, "xuewei@atg.uz", "94 660 30 38", "1066, 1201", OrganizationCode, "WKC1-UCS1-ENG",
            "Инженер КИПиА", "Automatic Equipments System Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-327", "Самад", "Джураев", null, "Samad", "Djuraev", null, "s.djuraev@atg.uz", "94 655 03 22", "1014", OrganizationCode, "WKC1-UCS1-ENG",
            "Инженер КИПиА", "Automatic Equipments System Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-328", "Бахтиёр", "Кахрамонов", null, "Bukhtiyor", "Kakhramonov", null, "b.kahramonov@atg.uz", "93 111 00 62", "3077", OrganizationCode, "WKC1-UCS1-ENG",
            "Инженер КИПиА", "Automatic Equipments System Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-329", "Хаонан", "Ма", null, "Haonan", "Ma", null, "mahaonan@atg.uz", "50 333 53 21", "3115", OrganizationCode, "WKC1-UCS1-ENG",
            "Инженер КИПиА", "Automatic Equipments System Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-330", "Жунхань", "Ли", null, "Ronghan", "Li", null, "lironghan@atg.uz", "50 333 53 20", "3115", OrganizationCode, "WKC1-UCS1-ENG",
            "Инженер КИПиА", "Automatic Equipments System Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-331", "Фаррух", "Ибрагимов", null, "Farrukh", "Ibragimov", null, "fa.ibragimov@atg.uz", null, "1014", OrganizationCode, "WKC1-UCS1-ENG",
            "Инженер КИПиА", "Automatic Equipments System Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-332", "Цзунцзян", "Цзан", null, "Zongqiang", "Zang", null, "zangzongqiang@atg.uz", "94 660 30 13", "3115, 1210", OrganizationCode, "WKC1-UCS1-ENG",
            "Инженер-электрик", "Electrical Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-333", "Жунчжан", "Янь", null, "Rongzhang", "Yan", null, "yanrongzhang@atg.uz", "94 660 30 41", "1066, 1211", OrganizationCode, "WKC1-UCS1-ENG",
            "Инженер-электрик", "Electrical Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-334", "Ойбек", "Розиков", null, "Oybek", "Rozikov", null, "o.rozikov@atg.uz", "94 660 30 23", "1020", OrganizationCode, "WKC1-UCS1-ENG",
            "Инженер-электрик", "Electrical Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-335", "Шорасул", "Амонов", null, "Shorasul", "Amonov", null, "sh.amonov@atg.uz", "94 660 90 07", "3006", OrganizationCode, "WKC1-UCS1-ENG",
            "Инженер-электрик", "Electrical Engineer", "ENGINEER", UserRole.StationEngineer),

        // Эксплуатация / Operations (shift engineers)
        new("ATG-336", "Алохон", "Вахобов", null, "Alohon", "Vahobov", null, "a.vahobov@atg.uz", "94 660 26 40", "3001", OrganizationCode, "WKC1-UCS1-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-337", "Садриддин", "Саидов", null, "Sadrididin", "Saidov", null, "s.saidov@atg.uz", "94 660 24 81", "1001", OrganizationCode, "WKC1-UCS1-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-338", "Абдулазиз", "Абдуганиев", null, "Abdulaziz", "Abduganiev", null, "a.abduganiev@atg.uz", "94 660 50 15", "3001", OrganizationCode, "WKC1-UCS1-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-339", "Наримон", "Бекчанов", null, "Narimon", "Bekchanov", null, "n.bekchanov@atg.uz", "94 660 30 62", "3001", OrganizationCode, "WKC1-UCS1-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-340", "Роман", "Динкеев", null, "Roman", "Dinkeev", null, "r.dinkeev@atg.uz", "94 655 03 23", "1001", OrganizationCode, "WKC1-UCS1-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-341", "Максим", "Пафнутов", null, "Maksim", "Pafnutov", null, "m.pafnutov@atg.uz", "94 660 26 03", "3001", OrganizationCode, "WKC1-UCS1-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-342", "Улугбек", "Шадиев", null, "Ulugbek", "Shadiev", null, "u.shadiev@atg.uz", "94 655 02 43", "1001", OrganizationCode, "WKC1-UCS1-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-343", "Азизбек", "Афаков", null, "Azizbek", "Afakov", null, "a.afakov@atg.uz", "94 660 78 71", "3001", OrganizationCode, "WKC1-UCS1-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-344", "Ачилло", "Очилов", null, "Amillo", "Ochilov", null, "a.ochilov@atg.uz", "94 660 26 84", "3001", OrganizationCode, "WKC1-UCS1-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-345", "Азизбек", "Ражаббоев", null, "Azizbek", "Rajabboev", null, "a.rajabboev@atg.uz", "94 660 91 81", "3001", OrganizationCode, "WKC1-UCS1-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-346", "Ахмаджон", "Сайфидинов", null, "Akhmadjon", "Sayfidinov", null, "a.sayfidinov@atg.uz", "94 660 01 12", "1001", OrganizationCode, "WKC1-UCS1-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-347", "Шавкатжон", "Холиков", null, "Shavkatjon", "Kholikov", null, "sh.kholikov@atg.uz", "94 660 26 48", "1001", OrganizationCode, "WKC1-UCS1-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-348", "Окилжон", "Исмоилов", null, "Okiljon", "Ismoilov", null, "o.ismoilov@atg.uz", "94 660 91 96", "1001", OrganizationCode, "WKC1-UCS1-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-349", "Шахром", "Икромов", null, "Shakhrom", "Ikromov", null, "sh.ikromov@atg.uz", "50 333 05 30", "1001", OrganizationCode, "WKC1-UCS1-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-350", "Обид", "Шаропов", null, "Obid", "Sharopov", null, "ob.sharopov@atg.uz", "94 660 71 05", "1001", OrganizationCode, "WKC1-UCS1-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-351", "Артур", "Нуруллаев", null, "Artur", "Nurullaev", null, "a.nurullaev@atg.uz", "93 641 70 31", "3001", OrganizationCode, "WKC1-UCS1-OPS",
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
