using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Infrastructure.Dcs;

public static class InternationalContractsIntSteps
{
    public static int FirstOperationalStep(ContractsIntProcurementVariant variant) => variant switch
    {
        ContractsIntProcurementVariant.DirectForeignContract => InternationalContractsDirectForeignSteps.FirstOperationalStep,
        _ => InternationalContractsSbpSteps.FirstOperationalStep,
    };

    public static bool IsSupported(ContractsIntProcurementVariant variant) =>
        variant is ContractsIntProcurementVariant.Sbp
            or ContractsIntProcurementVariant.Tender
            or ContractsIntProcurementVariant.DirectForeignContract;

    public static int TotalSteps(ContractsIntProcurementVariant variant) => variant switch
    {
        ContractsIntProcurementVariant.Sbp => InternationalContractsSbpSteps.TotalSteps,
        ContractsIntProcurementVariant.Tender => InternationalContractsTenderSteps.TotalSteps,
        ContractsIntProcurementVariant.DirectForeignContract => InternationalContractsDirectForeignSteps.TotalSteps,
        _ => 0,
    };

    public static IReadOnlyList<ContractsIntStepDefinition> GetDefinitions(ContractsIntProcurementVariant variant) =>
        variant switch
        {
            ContractsIntProcurementVariant.Sbp => InternationalContractsSbpSteps.Definitions,
            ContractsIntProcurementVariant.Tender => InternationalContractsTenderSteps.Definitions,
            ContractsIntProcurementVariant.DirectForeignContract => InternationalContractsDirectForeignSteps.Definitions,
            _ => [],
        };

    public static string VariantLabelRu(ContractsIntProcurementVariant variant) => variant switch
    {
        ContractsIntProcurementVariant.Sbp => "Отбор наилучших предложений (SBP)",
        ContractsIntProcurementVariant.Tender => "Тендер (TP)",
        ContractsIntProcurementVariant.DirectForeignContract => "Прямой контракт с иностранными поставщиками",
        _ => variant.ToString(),
    };

    public static string VariantLabelEn(ContractsIntProcurementVariant variant) => variant switch
    {
        ContractsIntProcurementVariant.Sbp => "Selection of Best Proposals (SBP)",
        ContractsIntProcurementVariant.Tender => "Tender (TP)",
        ContractsIntProcurementVariant.DirectForeignContract => "Direct contract with foreign suppliers",
        _ => variant.ToString(),
    };
}
