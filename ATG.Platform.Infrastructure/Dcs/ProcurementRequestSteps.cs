namespace ATG.Platform.Infrastructure.Dcs;



public static class ProcurementRequestSteps

{

    public const int TotalSteps = 10;



    public static IReadOnlyList<ProcurementStepDefinition> Definitions { get; } =

    [

        new(1,

            "Проверка наличия запрашиваемой позиции в утвержденном Плане закупок на 2026 год.",

            "Verify availability of the requested item in the approved 2026 Procurement Plan."),

        new(2,

            "Проверка нормативного запаса материально-технических ресурсов.",

            "Verify normative stock levels of material and technical resources."),

        new(3,

            "Проверка складских остатков в системе EAM и дополнительная сверка фактических остатков в системе 1С.",

            "Check warehouse balances in EAM and reconcile actual stock in 1C."),

        new(4,

            "Анализ наличия аналогов, взаимозаменяемых и альтернативных позиций в системах EAM и 1С.",

            "Analyze analogs, interchangeable and alternative items in EAM and 1C."),

        new(5,

            "Проверка наличия требуемой продукции в электронном магазине и возможности ее приобретения через электронные торговые площадки.",

            "Check e-shop availability and electronic trading platform procurement options."),

        new(6,

            "Проверка наличия выделенного бюджета, соответствия статьи затрат и финансового обеспечения закупки.",

            "Verify allocated budget, cost article alignment and financial coverage."),

        new(7,

            "Разработка технического задания с учетом технических, нормативных, эксплуатационных и производственных требований.",

            "Develop technical assignment per technical, regulatory and operational requirements."),

        new(8,

            "Инициирование и согласование технического задания с руководителем подразделения и профильными специалистами.",

            "Initiate and coordinate TA approval with department head and specialists."),

        new(9,

            "Инициирование и согласование листа запроса материалов (ЛЗМ) / листа запроса услуг (ЛЗУ) с руководством и профильными специалистами.",

            "Initiate and coordinate MR/SR approval with management and specialists."),

        new(10,

            "Передача согласованного пакета документов для дальнейшего проведения закупочной процедуры в установленном порядке.",

            "Transfer the approved document package for procurement processing."),

    ];

}



public record ProcurementStepDefinition(int Number, string TitleRu, string TitleEn);


