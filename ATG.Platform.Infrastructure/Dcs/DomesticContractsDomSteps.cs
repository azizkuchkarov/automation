using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Infrastructure.Dcs;

public static class DomesticContractsDomSteps
{
    public static int FirstOperationalStep(ContractsDomProcurementVariant variant) => variant switch
    {
        ContractsDomProcurementVariant.EShop => DomesticContractsEshopSteps.FirstOperationalStep,
        ContractsDomProcurementVariant.ElectronicAuction => DomesticContractsAuctionSteps.FirstOperationalStep,
        ContractsDomProcurementVariant.QuotationRequest => DomesticContractsQuotationSteps.FirstOperationalStep,
        ContractsDomProcurementVariant.DirectContract => DomesticContractsDirectContractSteps.FirstOperationalStep,
        ContractsDomProcurementVariant.SmallValue => DomesticContractsSmallValueSteps.FirstOperationalStep,
        _ => 3,
    };

    public static bool IsSupported(ContractsDomProcurementVariant variant) => true;

    public static int TotalSteps(ContractsDomProcurementVariant variant) => variant switch
    {
        ContractsDomProcurementVariant.EShop => DomesticContractsEshopSteps.TotalSteps,
        ContractsDomProcurementVariant.ElectronicAuction => DomesticContractsAuctionSteps.TotalSteps,
        ContractsDomProcurementVariant.QuotationRequest => DomesticContractsQuotationSteps.TotalSteps,
        ContractsDomProcurementVariant.DirectContract => DomesticContractsDirectContractSteps.TotalSteps,
        ContractsDomProcurementVariant.SmallValue => DomesticContractsSmallValueSteps.TotalSteps,
        _ => 0,
    };

    public static IReadOnlyList<ContractsDomStepDefinition> GetDefinitions(ContractsDomProcurementVariant variant) =>
        variant switch
        {
            ContractsDomProcurementVariant.EShop => DomesticContractsEshopSteps.Definitions,
            ContractsDomProcurementVariant.ElectronicAuction => DomesticContractsAuctionSteps.Definitions,
            ContractsDomProcurementVariant.QuotationRequest => DomesticContractsQuotationSteps.Definitions,
            ContractsDomProcurementVariant.DirectContract => DomesticContractsDirectContractSteps.Definitions,
            ContractsDomProcurementVariant.SmallValue => DomesticContractsSmallValueSteps.Definitions,
            _ => [],
        };

    public static string VariantLabelRu(ContractsDomProcurementVariant variant) => variant switch
    {
        ContractsDomProcurementVariant.EShop => "Закупка методом E-shop",
        ContractsDomProcurementVariant.ElectronicAuction => "Электронный аукцион",
        ContractsDomProcurementVariant.QuotationRequest => "Запрос предложений",
        ContractsDomProcurementVariant.DirectContract => "Прямой договор",
        ContractsDomProcurementVariant.SmallValue => "Упрощённая процедура (малая стоимость)",
        _ => variant.ToString(),
    };

    public static string VariantLabelEn(ContractsDomProcurementVariant variant) => variant switch
    {
        ContractsDomProcurementVariant.EShop => "E-shop procurement",
        ContractsDomProcurementVariant.ElectronicAuction => "Electronic auction",
        ContractsDomProcurementVariant.QuotationRequest => "Quotation request",
        ContractsDomProcurementVariant.DirectContract => "Direct contract",
        ContractsDomProcurementVariant.SmallValue => "Simplified small-value procedure",
        _ => variant.ToString(),
    };
}
