namespace ATG.Platform.Infrastructure.Seeds;

/// <summary>
/// Tashkent Head Office business trip approval chains from PPTX
/// «Список согласовывающих лиц служебной заявки командировки».
/// </summary>
public static class HoBusinessTripWorkflowDefinitions
{
    public const string Fdgd = "m.azizov@atg.uz";
    public const string Gd = "liuzhiguang@atg.uz";

    public sealed record TierDef(
        string Key,
        string TitleRu,
        string TitleEn,
        int Priority,
        string[]? InitiatorEmails,
        bool CatchAllStaff,
        bool PrependsSectionManager,
        string[] StepEmails);

    public sealed record DeptDef(
        string Code,
        string TitleRu,
        string TitleEn,
        TierDef[] Tiers);

    public static readonly DeptDef[] All =
    [
        Hr(),
        Sec(),
        Ac(),
        FinPlan(),
        Acct(),
        EngCon(),
        NewPrj(),
        ItDig(),
        Mkt(),
        Cproc(),
        Dcpr(),
        Qhse(),
        GasM(),
        Legal(),
        Adm(),
        Exec(),
    ];

    private static DeptDef Hr() => new(
        "HO-HR",
        "Департамент по кадрам",
        "HR Department",
        [
            Tier("Manager", "Начальник департамента", "Department manager", 80,
                ["v.khusenov@atg.uz"], Fdgd, Gd),
            Tier("DeputyManager", "Заместитель начальника", "Deputy manager", 70,
                ["lishuo@atg.uz"], "v.khusenov@atg.uz", Fdgd, Gd),
            Tier("Staff", "Сотрудники", "Staff", 10, null, true,
                "lishuo@atg.uz", "v.khusenov@atg.uz", Fdgd, Gd),
        ]);

    private static DeptDef Sec() => new(
        "HO-SEC",
        "Служба безопасности",
        "Security Service",
        [
            Tier("Manager", "Начальник службы", "Service manager", 80,
                ["f.maksudov@atg.uz"], Fdgd, Gd),
            Tier("Staff", "Сотрудники", "Staff", 10,
                ["d.djapparov@atg.uz"],
                "f.maksudov@atg.uz", Fdgd, Gd),
        ]);

    private static DeptDef Ac() => new(
        "HO-AC",
        "Служба противодействия коррупции",
        "Anti-corruption Service",
        [
            Tier("ChiefCoordinator", "Главный координатор", "Chief coordinator", 80,
                ["lizhaoyu@atg.uz"], Fdgd, Gd),
            Tier("Staff", "Сотрудники", "Staff", 10,
                ["a.oripov@atg.uz"],
                "lizhaoyu@atg.uz", Fdgd, Gd),
        ]);

    private static DeptDef FinPlan() => new(
        "HO-FINPLAN",
        "Департамент финансов и планирования",
        "Finance and Planning Department",
        [
            Tier("Manager", "Начальник департамента", "Department manager", 80,
                ["wangfeng@atg.uz"], "li.yong@atg.uz", Fdgd, Gd),
            Tier("DeputyManager", "Заместитель начальника", "Deputy manager", 70,
                ["j.ibodov@atg.uz"], "wangfeng@atg.uz", "li.yong@atg.uz", Fdgd, Gd),
            Tier("Staff", "Сотрудники", "Staff", 10, null, true,
                "j.ibodov@atg.uz", "wangfeng@atg.uz", "li.yong@atg.uz", Fdgd, Gd),
        ]);

    private static DeptDef Acct() => new(
        "HO-ACCT",
        "Бухгалтерский центр",
        "Accounting Center",
        [
            Tier("Manager", "Начальник центра", "Center manager", 80,
                ["zhangpeng@atg.uz"], "r.karshibaev@atg.uz", Fdgd, Gd),
            Tier("DeputyManager", "Заместитель начальника", "Deputy manager", 70,
                ["a.safarov@atg.uz"], "zhangpeng@atg.uz", "r.karshibaev@atg.uz", Fdgd, Gd),
            Tier("Staff", "Сотрудники", "Staff", 10, null, true,
                "a.safarov@atg.uz", "zhangpeng@atg.uz", "r.karshibaev@atg.uz", Fdgd, Gd),
        ]);

    private static DeptDef EngCon() => new(
        "HO-ENGCON",
        "Департамент инжиниринга и строительства",
        "Engineering and Construction Department",
        [
            Tier("Manager", "Начальник департамента", "Department manager", 80,
                ["j.yunusov@atg.uz"], "yuyaoguo@atg.uz", "s.latipov@atg.uz", Fdgd, Gd),
            Tier("Staff", "Сотрудники", "Staff", 10, null, true,
                "j.yunusov@atg.uz", "yuyaoguo@atg.uz", "s.latipov@atg.uz", Fdgd, Gd),
        ]);

    private static DeptDef NewPrj() => new(
        "HO-NEWPRJ",
        "Департамент новых проектов",
        "New Projects Department",
        [
            Tier("Manager", "Начальник департамента", "Department manager", 80,
                ["zhuruihua@atg.uz"], "rm.magzumov@atg.uz", "tianaimin@atg.uz", Fdgd, Gd),
            Tier("Staff", "Сотрудники", "Staff", 10, null, true,
                "zhuruihua@atg.uz", "rm.magzumov@atg.uz", "tianaimin@atg.uz", Fdgd, Gd),
        ]);

    private static DeptDef ItDig() => new(
        "HO-ITDIG",
        "Департамент ИТ и цифровизации",
        "IT & Digitalization Department",
        [
            Tier("DeputyManager", "Заместитель начальника", "Deputy manager", 70,
                ["a.lebedev@atg.uz"], Fdgd, Gd),
            Tier("Staff", "Сотрудники", "Staff", 10, null, true,
                "a.lebedev@atg.uz", Fdgd, Gd),
        ]);

    private static DeptDef Mkt() => new(
        "HO-MKT",
        "Департамент маркетинга и тендерного управления",
        "Marketing and Tender Management Department",
        [
            Tier("Manager", "Начальник департамента", "Department manager", 80,
                ["pangshubao@atg.uz"], Fdgd, Gd),
            Tier("DeputyManager", "Заместитель начальника", "Deputy manager", 70,
                ["j.zokirov@atg.uz"], "pangshubao@atg.uz", Fdgd, Gd),
            Tier("SectionManager", "Начальник секции / тендерная служба", "Section manager / tender secretariat", 60,
                ["a.madrakhimov@atg.uz", "i.kogay@atg.uz", "s.kim@atg.uz"],
                "j.zokirov@atg.uz", "pangshubao@atg.uz", Fdgd, Gd),
            Tier("Staff", "Сотрудники маркетинговой секции", "Marketing section staff", 10, null, true,
                "a.madrakhimov@atg.uz", "j.zokirov@atg.uz", "pangshubao@atg.uz", Fdgd, Gd),
        ]);

    private static DeptDef Cproc() => new(
        "HO-CPROC",
        "Департамент договоров и закупок",
        "Contracts and Procurement Department",
        [
            Tier("Manager", "Начальник департамента", "Department manager", 80,
                ["f.asadov@atg.uz"], Fdgd, Gd),
            Tier("DeputyManager", "Заместитель начальника", "Deputy manager", 70,
                ["wangweijian@atg.uz"], "f.asadov@atg.uz", Fdgd, Gd),
            Tier("SectionManager", "Начальники секций и специалисты", "Section managers and specialists", 60,
                ["zhaomao@atg.uz", "r.avezov@atg.uz", "i.raimjanov@atg.uz", "o.tulyaganov@atg.uz", "l.buzrukova@atg.uz", "a.yodgorov@atg.uz"],
                "wangweijian@atg.uz", "f.asadov@atg.uz", Fdgd, Gd),
            Tier("Staff", "Сотрудники секций", "Section staff", 10, null, true, true,
                "wangweijian@atg.uz", "f.asadov@atg.uz", Fdgd, Gd),
        ]);

    private static DeptDef Dcpr() => new(
        "HO-DCPR",
        "Департамент по документообороту и связям с общественностью",
        "Document Control and Public Relations Department",
        [
            Tier("Manager", "Начальник департамента", "Department manager", 80,
                ["xiaozheng@atg.uz"], "cuihaibo@atg.uz", "j.rakhmatullaev@atg.uz", Fdgd, Gd),
            Tier("DeputyManager", "Заместитель начальника", "Deputy manager", 70,
                ["n.kim@atg.uz"], "xiaozheng@atg.uz", "cuihaibo@atg.uz", "j.rakhmatullaev@atg.uz", Fdgd, Gd),
            Tier("Staff", "Сотрудники", "Staff", 10, null, true,
                "n.kim@atg.uz", "xiaozheng@atg.uz", "cuihaibo@atg.uz", "j.rakhmatullaev@atg.uz", Fdgd, Gd),
        ]);

    private static DeptDef Qhse() => new(
        "HO-QHSE",
        "Департамент QHSE",
        "QHSE Department",
        [
            Tier("Manager", "Начальник департамента", "Department manager", 80,
                ["z.mannanova@atg.uz"], Fdgd, Gd),
            Tier("DeputyManager", "Заместитель начальника", "Deputy manager", 70,
                ["jingang@atg.uz"], "z.mannanova@atg.uz", Fdgd, Gd),
            Tier("Staff", "Сотрудники", "Staff", 10, null, true,
                "jingang@atg.uz", "z.mannanova@atg.uz", Fdgd, Gd),
        ]);

    private static DeptDef GasM() => new(
        "HO-GASM",
        "Департамент по учёту газа",
        "Gas Metering Department",
        [
            Tier("Manager", "Начальник департамента", "Department manager", 80,
                ["qiuzhonghua@atg.uz"], Fdgd, Gd),
            Tier("DeputyManager", "Заместитель начальника", "Deputy manager", 70,
                ["n.ismailov@atg.uz"], "qiuzhonghua@atg.uz", Fdgd, Gd),
            Tier("Staff", "Сотрудники", "Staff", 10, null, true,
                "n.ismailov@atg.uz", "qiuzhonghua@atg.uz", Fdgd, Gd),
        ]);

    private static DeptDef Legal() => new(
        "HO-LEGAL",
        "Юридический департамент",
        "Legal Department",
        [
            Tier("Manager", "Начальник департамента", "Department manager", 80,
                ["h.narbaev@atg.uz"], Fdgd, Gd),
            Tier("DeputyManager", "Заместитель начальника", "Deputy manager", 70,
                ["zhouliang@atg.uz"], "h.narbaev@atg.uz", Fdgd, Gd),
            Tier("Staff", "Сотрудники", "Staff", 10, null, true,
                "zhouliang@atg.uz", "h.narbaev@atg.uz", Fdgd, Gd),
        ]);

    private static DeptDef Adm() => new(
        "HO-ADM",
        "Департамент по административно-хозяйственным вопросам",
        "Administrative and Utility Issues Department",
        [
            Tier("Manager", "Начальник департамента", "Department manager", 80,
                ["khur.zakirov@atg.uz"], "cuihaibo@atg.uz", "j.rakhmatullaev@atg.uz", Fdgd, Gd),
            Tier("DeputyManager", "Заместитель начальника", "Deputy manager", 70,
                ["wangzilong@atg.uz"], "khur.zakirov@atg.uz", "cuihaibo@atg.uz", "j.rakhmatullaev@atg.uz", Fdgd, Gd),
            Tier("Staff", "Сотрудники", "Staff", 10, null, true,
                "wangzilong@atg.uz", "khur.zakirov@atg.uz", "cuihaibo@atg.uz", "j.rakhmatullaev@atg.uz", Fdgd, Gd),
        ]);

    private static DeptDef Exec() => new(
        "HO-EXEC",
        "Руководство",
        "Leaders / Top management",
        [
            Tier("DeputyGdEngineering", "ЗГД по инжинирингу и строительству", "Deputy GD for Engineering", 75,
                ["s.latipov@atg.uz"], Fdgd, Gd),
            Tier("ChiefInspectorExecutive", "Главный инспектор", "Chief inspector", 72,
                ["yuyaoguo@atg.uz"], "s.latipov@atg.uz", Fdgd, Gd),
            Tier("DeputyGdNewProjects", "ЗГД по новым проектам", "Deputy GD for New Projects", 75,
                ["tianaimin@atg.uz"], Fdgd, Gd),
            Tier("DeputyGdAdministrative", "ЗГД по административным вопросам", "Deputy GD for Administrative Affairs", 75,
                ["j.rakhmatullaev@atg.uz"], Fdgd, Gd),
            Tier("TopManagement", "Топ-менеджмент", "Top management", 68,
                ["liuzhiguang@atg.uz", "s.latipov@atg.uz", "tianaimin@atg.uz", "li.yong@atg.uz", "j.rakhmatullaev@atg.uz", "cuihaibo@atg.uz", "r.karshibaev@atg.uz"],
                Fdgd, Gd),
            Tier("ChiefInspectorGd", "Главный инспектор (Магзумов)", "Chief inspector (Magzumov)", 71,
                ["rm.magzumov@atg.uz"], "tianaimin@atg.uz", Fdgd, Gd),
            Tier("GeneralDirector", "Генеральный директор", "General Director", 90,
                ["liuzhiguang@atg.uz"]),
        ]);

    private static TierDef Tier(
        string key,
        string titleRu,
        string titleEn,
        int priority,
        string[]? initiators,
        bool catchAll,
        params string[] steps) =>
        new(key, titleRu, titleEn, priority, initiators, catchAll, false, steps);

    private static TierDef Tier(
        string key,
        string titleRu,
        string titleEn,
        int priority,
        string[] initiators,
        params string[] steps) =>
        new(key, titleRu, titleEn, priority, initiators, false, false, steps);

    private static TierDef Tier(
        string key,
        string titleRu,
        string titleEn,
        int priority,
        string[]? initiators,
        bool catchAll,
        bool prependsSectionManager,
        params string[] steps) =>
        new(key, titleRu, titleEn, priority, initiators, catchAll, prependsSectionManager, steps);
}
