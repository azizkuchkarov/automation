namespace ATG.Platform.Infrastructure.Dcs;

public static class DomesticContractsEshopSteps
{
    public const int TotalSteps = 9;
    public const int FirstOperationalStep = 3;

    public static IReadOnlyList<ContractsDomStepDefinition> Definitions { get; } =
    [
        Meta(1, "Передача ЛЗМ/ЛЗУ начальнику отдела локальных закупок",
            "Handover to Domestic Procurement Section head",
            "Начальник Департамента по контрактам и закупкам направляет заявку начальнику отдела локальных закупок.",
            "Contracts Department head routes the request to the Domestic Procurement Section head."),
        Meta(2, "Распределение заявки исполнителю",
            "Assignment to executor",
            "Начальник отдела локальных закупок назначает исполнителя (или выполняет самостоятельно).",
            "Domestic section head assigns an executor or performs the work personally."),

        new(3, false,
            "Рассмотрение стратегии, ТЗ и коммерческих предложений",
            "Review strategy, TA and proposals",
            "Исполнитель проверяет стратегию закупки, техническое задание и коммерческие предложения. При несоответствиях заявка возвращается в маркетинг на доработку.",
            "Executor reviews the procurement strategy, technical assignment and commercial proposals. If inconsistencies are found, the request is returned to Marketing for rework.",
            null, null, AllowsReturnToMarketing: true),

        new(4, false,
            "Поиск лота в E-shop и согласование с инициатором",
            "Find lot in E-shop and align with initiator",
            "Исполнитель подбирает соответствующий лот в электронном магазине и согласовывает его с инициатором.",
            "Executor finds the matching e-shop lot and coordinates it with the initiator.",
            null, null, RequiresApprovers: true),

        new(5, false,
            "Запрос цены (2 рабочих дня)",
            "Price request (2 business days)",
            "Укажите дату запроса цены. Система рассчитает двухдневный рабочий срок ожидания ответа.",
            "Set the price request date. The system will calculate the 2-business-day response window.",
            null, null, RequiresScheduleDate: true,
            ScheduleLabelRu: "Дата запроса цены",
            ScheduleLabelEn: "Price request date",
            ScheduleHintRu: "После указания даты запускается счётчик 2 рабочих дней.",
            ScheduleHintEn: "After setting the date, a 2-business-day counter starts."),

        new(6, false,
            "Формирование договора",
            "Contract formation",
            "Исполнитель формирует и загружает договор с выбранным поставщиком.",
            "Executor prepares and uploads the contract with the selected supplier.",
            null, null, RequiresUpload: true),

        new(7, false,
            "Ожидание поставки",
            "Await delivery",
            "Укажите плановый срок поставки товаров/услуг. При расторжении договора процесс возвращается к шагу согласования лота.",
            "Set the planned delivery date for goods/services. If the contract is terminated, the process returns to the lot coordination step.",
            null, null, RequiresScheduleDate: true,
            ScheduleLabelRu: "Плановая дата поставки",
            ScheduleLabelEn: "Planned delivery date",
            ScheduleHintRu: "Используйте календарь для указания срока поставки.",
            ScheduleHintEn: "Use the calendar to set the delivery due date.",
            AllowsTerminationRollback: true,
            RollbackStepNumber: 4),

        new(8, false,
            "Согласование товара инициатором",
            "Goods approval by initiator",
            "Инициатор подтверждает соответствие поставленного товара (работ, услуг).",
            "The initiator confirms the delivered goods/services are acceptable.",
            null, null, RequiresApprovers: true),

        new(9, false,
            "Приём счёт-фактуры, оплата и закрытие сделки",
            "Invoice acceptance, payment and deal closure",
            "Специалист логистики и таможенного оформления, заведующий складом и бухгалтерия принимают счёт-фактуру. После оплаты загрузите подписанную счёт-фактуру и завершите сделку.",
            "Logistics/customs, warehouse and accounting approve the invoice. After payment upload the signed invoice and close the deal.",
            null, null, RequiresUpload: true, RequiresApprovers: true, RequiresRegistration: true),
    ];

    private static ContractsDomStepDefinition Meta(int n, string ru, string en, string hintRu, string hintEn) =>
        new(n, false, ru, en, hintRu, hintEn, null, null);
}
