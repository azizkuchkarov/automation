namespace ATG.Platform.Infrastructure.Dcs;



public static class ProcurementRequestSteps

{

    public const int TotalSteps = 7;



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

            "Проверка наличия выделенного бюджета, соответствия статьи затрат и финансового обеспечения закупки.",

            "Verify allocated budget, cost article alignment and financial coverage."),

        new(6,

            "Инициирование и согласование листа запроса материалов (ЛЗМ) / листа запроса услуг (ЛЗУ) с руководством и профильными специалистами.",

            "Initiate and coordinate MR/SR approval with management and specialists."),

        new(7,

            "Передача согласованного пакета документов для дальнейшего проведения закупочной процедуры в установленном порядке.",

            "Transfer the approved document package for procurement processing."),

    ];

}



public record ProcurementStepDefinition(int Number, string TitleRu, string TitleEn);

