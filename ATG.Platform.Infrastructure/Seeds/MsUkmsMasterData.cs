using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Infrastructure.Seeds;

/// <summary>Regional station MS / UKMS — bilingual RU/EN.</summary>
public static class MsUkmsMasterData
{
    public const string OrganizationCode = "MS-UKMS";
    public const string OrganizationNameRu = "Региональная станция MS / UKMS";
    public const string OrganizationNameEn = "Regional station MS / UKMS";

    public static readonly string[] LegacyOrganizationCodes = ["MS", "UKMS"];

    public static readonly HoDepartment[] Departments =
    [
        new("MS-UKMS-EXEC", "Руководство", "Leadership"),
        new("MS-UKMS-ENG", "Инженерная служба", "Engineering"),
        new("MS-UKMS-OPS", "Эксплуатация", "Operations"),
    ];

    public static readonly string[] LegacyDepartmentCodes =
        ["MS-UKMS-ADM", "MS-UKMS-SAF", "MS-UKMS-IT"];

    public static readonly StationStaffMember[] Staff =
    [
        new("ATG-416", "Вэйвэй", "Линь", null, "Weiwei", "Lin", null, "linweiwei@atg.uz", "94 655 02 46", "6111, 9205", OrganizationCode, "MS-UKMS-EXEC",
            "Региональный менеджер MS/UKMS", "Regional Manager of MS/UKMS", "NACHALNIK", UserRole.BMGMCNachalnikiOtdeli),
        new("ATG-417", "Махмуд", "Рахманов", null, "Makhmud", "Rakhmanov", null, "m.rakhmanov@atg.uz", "93 382 05 55", "9113", OrganizationCode, "MS-UKMS-EXEC",
            "Первый заместитель регионального менеджера MS/UKMS", "First Deputy Regional Manager of MS/UKMS", "MANAGER", UserRole.BMGMCNachalnikiOtdeli),
        new("ATG-418", "Евгений", "Коломейчук", null, "Evgeniy", "Kolomeychuk", null, "e.kolomeychuk@atg.uz", "94 660 26 90", "6113", OrganizationCode, "MS-UKMS-EXEC",
            "Заместитель регионального менеджера MS/UKMS", "Deputy Regional Manager of MS/UKMS", "MANAGER", UserRole.BMGMCNachalnikiOtdeli),
        new("ATG-419", "Шерзод", "Худойбердиев", null, "Sherzod", "Khudoyberdiev", null, "sh.khudoyberdiev@atg.uz", "94 660 80 84", "9016", OrganizationCode, "MS-UKMS-ENG",
            "Инженер КИПиА", "Automatic Equipments System Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-420", "Нуратдин", "Ахмедов", null, "Nuratdin", "Akhmedov", null, "n.akhmedov@atg.uz", "94 660 21 66", "6005", OrganizationCode, "MS-UKMS-ENG",
            "Инженер-механик по оборудованию", "Mechanics equipment engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-421", "Улугбек", "Хакимов", null, "Ulugbek", "Khakimov", null, "u.khakimov@atg.uz", "94 660 26 05", "9022", OrganizationCode, "MS-UKMS-ENG",
            "Инженер-механик по оборудованию", "Mechanics equipment engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-422", "Зехниддин", "Файзуллаев", null, "Zekhniddin", "Fayzullaev", null, "z.fayzullaev@atg.uz", "94 655 02 61", "9010", OrganizationCode, "MS-UKMS-ENG",
            "Инженер-электрик", "Electrical Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-423", "Алишер", "Атажанов", null, "Alisher", "Atajanov", null, "a.atajanov@atg.uz", "94 660 24 75", "6001", OrganizationCode, "MS-UKMS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-424", "Олег", "Ермолаев", null, "Oleg", "Ermolaev", null, "o.ermolaev@atg.uz", "94 660 26 93", "6001, 6666", OrganizationCode, "MS-UKMS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-425", "Дамир", "Абусев", null, "Damir", "Abusev", null, "d.abusev@atg.uz", "94 660 25 80", "6001", OrganizationCode, "MS-UKMS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-426", "Аброр", "Мингбоев", null, "Abror", "Mingboev", null, "a.mingboev@atg.uz", "94 660 80 77", "6001, 6109", OrganizationCode, "MS-UKMS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-427", "Сухроб", "Саидбахадурханов", null, "Sukhrob", "Saidbakhadurkhanov", null, "s.saidbakhadurkhanov@atg.uz", "94 660 23 91", "6001, 6109", OrganizationCode, "MS-UKMS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-428", "Акмал", "Халилов", null, "Akmal", "Khalilov", null, "ak.khalilov@atg.uz", "94 660 24 79", "9001", OrganizationCode, "MS-UKMS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-429", "Азамат", "Атоев", null, "Azamat", "Atoev", null, "a.atoev@atg.uz", "94 660 81 21", "9001", OrganizationCode, "MS-UKMS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-430", "Мурод", "Максудов", null, "Murod", "Makhsudov", null, "m.makhsudov@atg.uz", "94 660 60 31", "9001", OrganizationCode, "MS-UKMS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-431", "Бахтиёр", "Раджабов", null, "Bakhtiyor", "Radjabov", null, "b.radjabov@atg.uz", "94 660 01 26", "9001", OrganizationCode, "MS-UKMS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-432", "Жихан", "Файзуллаев", null, "Jikhan", "Fayzullaev", null, "j.fayzullaev@atg.uz", "94 660 26 54", "9001", OrganizationCode, "MS-UKMS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-433", "Илхом", "Сагатов", null, "Ilkhom", "Sagatov", null, "i.sagatov@atg.uz", "94 660 01 16", "9001", OrganizationCode, "MS-UKMS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-434", "Хаким", "Бобобеков", null, "Khakim", "Bobobekov", null, "kh.bobobekov@atg.uz", "94 660 61 71", "6001, 6109", OrganizationCode, "MS-UKMS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-435", "Рустамжон", "Усманов", null, "Rustamjon", "Usmanov", null, "r.usmanov@atg.uz", "94 660 30 68", "6001, 6666", OrganizationCode, "MS-UKMS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-436", "Ойбек", "Исрафилов", null, "Oybek", "Israfilov", null, "o.israfilov@atg.uz", "94 660 05 49", "9001", OrganizationCode, "MS-UKMS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-437", "Рахим", "Камалов", null, "Rakhim", "Kamalov", null, "ra.kamalov@atg.uz", "94 660 03 13", "6001", OrganizationCode, "MS-UKMS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
        new("ATG-438", "Азизхон", "Кулдашев", null, "Azizkhon", "Kuldashev", null, "a.kuldashev@atg.uz", "50 333 05 31", "9001", OrganizationCode, "MS-UKMS-OPS",
            "Сменный инженер", "Shift Engineer", "ENGINEER", UserRole.StationEngineer),
    ];

    public static readonly StationDefinition Definition = new(
        OrganizationCode, OrganizationNameRu, OrganizationNameEn,
        LegacyOrganizationCodes, Departments, LegacyDepartmentCodes, Staff);
}
