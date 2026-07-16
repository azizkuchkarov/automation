namespace ATG.Platform.Infrastructure.Dcs;

public static class DomesticContractsSmallValueSteps
{
    public const int TotalSteps = 9;
    public const int FirstOperationalStep = 3;

    public static IReadOnlyList<ContractsDomStepDefinition> Definitions { get; } =
    [
        DomesticContractsEshopSteps.Definitions[0],
        DomesticContractsEshopSteps.Definitions[1],

        new(3, false,
            "Рассмотрение стратегии, ТЗ и коммерческих предложений",
            "Review strategy, TA and proposals",
            "Исполнитель проверяет стратегию закупки, техническое задание и коммерческие предложения. При несоответствиях заявка возвращается в маркетинг на доработку.",
            "Executor reviews the procurement strategy, technical assignment and commercial proposals. If inconsistencies are found, the request is returned to Marketing for rework.",
            null, null, AllowsReturnToMarketing: true),

        new(4, false,
            "Процесс заключения договора",
            "Contract conclusion process",
            "Исполнитель организует процесс заключения договора и при необходимости загружает сопровождающий документ.",
            "Executor manages the contract conclusion process and may upload the supporting document.",
            null, null, RequiresUpload: true),

        new(5, false,
            "Процесс оплаты (предоплата при необходимости)",
            "Payment process (prepayment if needed)",
            "Исполнитель инициирует оплату, если условия договора предусматривают предоплату.",
            "Executor initiates the payment process when prepayment is required by the contract.",
            null, null),

        new(6, false,
            "Ожидание доставки",
            "Await delivery",
            "Укажите плановый срок доставки товаров/услуг и дождитесь фактической поставки.",
            "Set the planned goods/services delivery date and wait for actual delivery.",
            null, null, RequiresScheduleDate: true,
            ScheduleLabelRu: "Плановая дата доставки",
            ScheduleLabelEn: "Planned delivery date",
            ScheduleHintRu: "Используйте календарь для указания срока доставки.",
            ScheduleHintEn: "Use the calendar to set the delivery due date."),

        new(7, false,
            "Согласование товара инициатором",
            "Goods approval by initiator",
            "Инициатор подтверждает соответствие поставленного товара (работ, услуг).",
            "The initiator confirms the delivered goods/services are acceptable.",
            null, null, RequiresApprovers: true),

        new(8, false,
            "Приём счёт-фактуры и подача заявки на оплату",
            "Invoice acceptance and payment request",
            "Специалист логистики и таможенного оформления, заведующий складом и бухгалтерия принимают счёт-фактуру. После этого подаётся заявка на оплату.",
            "Logistics/customs, warehouse and accounting approve the invoice. After that a payment request is submitted.",
            null, null, RequiresApprovers: true),

        new(9, false,
            "Оплата и завершение",
            "Payment and completion",
            "После получения заявки бухгалтер производит оплату. Для завершения загрузите документ, подтверждающий оплату, и нажмите завершить.",
            "After receiving the payment request, accounting performs the payment. Upload the payment confirmation document and press complete.",
            null, null, RequiresUpload: true, RequiresRegistration: true),
    ];
}
