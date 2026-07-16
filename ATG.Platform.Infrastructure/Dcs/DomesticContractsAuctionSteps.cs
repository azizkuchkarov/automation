namespace ATG.Platform.Infrastructure.Dcs;

public static class DomesticContractsAuctionSteps
{
    public const int TotalSteps = 9;
    public const int FirstOperationalStep = 3;

    public static IReadOnlyList<ContractsDomStepDefinition> Definitions { get; } =
    [
        DomesticContractsEshopSteps.Definitions[0],
        DomesticContractsEshopSteps.Definitions[1],

        new(3, false,
            "Размещение объявления на портале",
            "Portal announcement",
            "Исполнитель размещает объявление об электронном аукционе на специализированном электронном портале.",
            "Executor posts the electronic auction announcement on the specialized portal.",
            null, null, RequiresUpload: true),

        new(4, false,
            "Проведение аукциона (не менее 5 рабочих дней)",
            "Auction period (min. 5 business days)",
            "Срок проведения торгов составляет не менее 5 рабочих дней.",
            "Trading period is at least 5 business days.",
            null, null),

        new(5, false,
            "Определение победителя и заключение договора",
            "Winner selection & contract",
            "По результатам аукциона определяется победитель с наиболее низкой ценой и заключается договор.",
            "Auction winner (lowest price) is determined and contract is signed.",
            null, null, RequiresUpload: true),

        new(6, false,
            "Поставка по договору",
            "Delivery per contract",
            "Поставщик осуществляет поставку в соответствии с условиями договора.",
            "Supplier delivers per contract terms.",
            null, null),

        new(7, false,
            "Счёт-фактура поставщика",
            "Supplier invoice",
            "После поставки поставщик выставляет счёт-фактуру.",
            "Supplier issues invoice after delivery.",
            null, null, RequiresUpload: true),

        new(8, false,
            "Приёмка и подписание счёт-фактуры",
            "Acceptance & invoice signing",
            "Ответственное лицо подтверждает приёмку и подписывает счёт-фактуру.",
            "Responsible person confirms acceptance and signs the invoice.",
            null, null, RequiresApprovers: true),

        new(9, false,
            "Оплата и закрытие договора",
            "Payment & contract closure",
            "Исполнитель инициирует оплату поставщику. Договор исполнен и закрыт.",
            "Executor initiates supplier payment. Contract fulfilled and closed.",
            null, null, RequiresUpload: true, RequiresRegistration: true),
    ];
}
