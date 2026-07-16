namespace ATG.Platform.Infrastructure.Dcs;

public static class DomesticContractsDirectContractSteps
{
    public const int TotalSteps = 4;
    public const int FirstOperationalStep = 3;

    public static IReadOnlyList<ContractsDomStepDefinition> Definitions { get; } =
    [
        DomesticContractsEshopSteps.Definitions[0],
        DomesticContractsEshopSteps.Definitions[1],

        new(3, false,
            "Заключение прямого договора",
            "Direct contract conclusion",
            "Исполнитель заключает договор непосредственно с поставщиком.",
            "Executor concludes a contract directly with the supplier.",
            null, null, RequiresUpload: true),

        new(4, false,
            "Передача в отдел управления контрактами",
            "Handover to Contracts Administration",
            "После подписания договор передаётся в отдел управления контрактами (HO-CPROC-CADM) для сопровождения и исполнения.",
            "After signing the contract is transferred to Contracts Administration (HO-CPROC-CADM) for follow-up and execution.",
            null, null, RequiresContractsAdmin: true, RequiresRegistration: true),
    ];
}
