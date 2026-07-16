namespace ATG.Platform.Infrastructure.Seeds;

/// <summary>
/// Local/dev test accounts that take priority in routing when present in the database.
/// </summary>
public static class DevTestAccounts
{
    public const string MarketingSectionHeadEmail = "user2@atg.uz";
    public const string ContractsDepartmentHeadEmail = "user3@atg.uz";
    public const string ContractsIntSectionHeadEmail = "user4@atg.uz";
    public const string ContractsDomSectionHeadEmail = "user5@atg.uz";
    public const string ContractsIntEngineerEmail = "user6@atg.uz";
    public const string ContractsDomEngineerEmail = "user7@atg.uz";
    public const string TenderSecretariatEmail = "user8@atg.uz";
    public const string TranslationSectionHeadEmail = "user9@atg.uz";
    public const string TranslationTestEngineerEmail = "user10@atg.uz";
    /// <summary>Real HO-FINPLAN head used for payment handoff when no dedicated dev account is assigned.</summary>
    public const string PaymentSectionHeadEmail = "j.ibodov@atg.uz";
    public const string PaymentSpecialistEmail = "j.ibodov@atg.uz";

    /// <summary>Legacy alias — INT section head may assign themselves as engineer in dev.</summary>
    public const string ContractsSectionHeadEmail = ContractsIntSectionHeadEmail;

    public static bool IsContractsSectionHeadEmail(string? email) =>
        email is not null && (
            email.Equals(ContractsIntSectionHeadEmail, StringComparison.OrdinalIgnoreCase)
            || email.Equals(ContractsDomSectionHeadEmail, StringComparison.OrdinalIgnoreCase));
}
