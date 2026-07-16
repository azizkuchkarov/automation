using System.Globalization;
using System.Text.RegularExpressions;
using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ATG.Platform.Infrastructure.Dcs;

/// <summary>ATG-CD-DOM-{VARIANT}-{yy}-{seq:D3} e.g. ATG-CD-DOM-ESHOP-26-001</summary>
public static class ContractsDomRegistrationNumberGenerator
{
    public const string PrefixMarker = "ATG-CD-DOM-";

    public static string VariantCode(ContractsDomProcurementVariant variant) => variant switch
    {
        ContractsDomProcurementVariant.EShop => "ESHOP",
        ContractsDomProcurementVariant.ElectronicAuction => "AUC",
        ContractsDomProcurementVariant.QuotationRequest => "QRS",
        ContractsDomProcurementVariant.DirectContract => "DPS",
        ContractsDomProcurementVariant.SmallValue => "SPS",
        _ => variant.ToString().ToUpperInvariant(),
    };

    public static string BuildPrefix(ContractsDomProcurementVariant variant, int? year = null)
    {
        var yy = (year ?? DateTime.UtcNow.Year) % 100;
        return $"{PrefixMarker}{VariantCode(variant)}-{yy:D2}-";
    }

    public static async Task<string> GenerateNextAsync(
        AppDbContext db, ContractsDomProcurementVariant variant, CancellationToken ct = default)
    {
        var prefix = BuildPrefix(variant);
        var numbers = await db.ProcurementRequestDetails.AsNoTracking()
            .Where(d => d.ContractsDomContractRegistrationNumber != null
                && d.ContractsDomContractRegistrationNumber.StartsWith(prefix))
            .Select(d => d.ContractsDomContractRegistrationNumber!)
            .ToListAsync(ct);

        var max = 0;
        foreach (var number in numbers)
        {
            var match = Regex.Match(number, @"-(\d{3})$");
            if (match.Success && int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var seq))
                max = Math.Max(max, seq);
        }

        return $"{prefix}{(max + 1):D3}";
    }
}
