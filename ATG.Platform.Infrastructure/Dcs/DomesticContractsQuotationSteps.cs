namespace ATG.Platform.Infrastructure.Dcs;

public static class DomesticContractsQuotationSteps
{
    public const int TotalSteps = 8;
    public const int FirstOperationalStep = 3;

    public static IReadOnlyList<ContractsDomStepDefinition> Definitions { get; } =
    [
        DomesticContractsEshopSteps.Definitions[0],
        DomesticContractsEshopSteps.Definitions[1],

        new(3, false,
            "Размещение объявления (запрос предложений)",
            "Quotation request announcement",
            "Исполнитель размещает объявление о запросе предложений на специализированном электронном портале.",
            "Executor posts a quotation request announcement on the specialized portal.",
            null, null, RequiresUpload: true),

        new(4, false,
            "Представление предложений (2 рабочих дня)",
            "Proposal submission (2 business days)",
            "В течение 2 рабочих дней поставщики представляют свои предложения.",
            "Suppliers submit proposals within 2 business days.",
            null, null),

        new(5, false,
            "Выбор поставщика",
            "Supplier selection",
            "Исполнитель определяет поставщика, чьё предложение наиболее соответствует ТЗ и является наиболее экономически выгодным.",
            "Executor selects the supplier whose proposal best matches the TA and is most economical.",
            null, null),

        new(6, false,
            "Заключение договора",
            "Contract conclusion",
            "С выбранным поставщиком заключается договор.",
            "Contract is concluded with the selected supplier.",
            null, null, RequiresUpload: true),

        new(7, false,
            "Исполнение договора",
            "Contract execution",
            "Договор исполняется в установленном порядке.",
            "Contract is executed in the established manner.",
            null, null),

        new(8, false,
            "Завершение и регистрация",
            "Completion & registration",
            "Обязательства по договору исполнены. Договор регистрируется и закрывается.",
            "Contract obligations fulfilled. Contract registered and closed.",
            null, null, RequiresRegistration: true),
    ];
}
