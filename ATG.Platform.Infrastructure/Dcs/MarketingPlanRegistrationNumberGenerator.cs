using System.Globalization;
using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ATG.Platform.Infrastructure.Dcs;

public static class MarketingPlanRegistrationNumberGenerator
{
    public static string BuildPrefix(MarketingPlanRegistrationMethod method, int? year = null)
    {
        var yyyy = year ?? DateTime.UtcNow.Year;
        var code = method switch
        {
            MarketingPlanRegistrationMethod.Tender => "TS",
            MarketingPlanRegistrationMethod.BestOfferSelection => "SBP",
            MarketingPlanRegistrationMethod.LocalProcurement => "LPS",
            MarketingPlanRegistrationMethod.SmallProcurement => "SPS",
            MarketingPlanRegistrationMethod.QuotationRequest => "QRS",
            MarketingPlanRegistrationMethod.DirectContract => "DPS",
            _ => throw new ArgumentOutOfRangeException(nameof(method)),
        };
        return $"ATG-{code}-{yyyy}-";
    }

    public static async Task<string> GenerateNextAsync(
        AppDbContext db, MarketingPlanRegistrationMethod method, CancellationToken ct = default)
    {
        var prefix = BuildPrefix(method);
        var numbers = await db.MarketingProcurementPlans.AsNoTracking()
            .Where(p => p.RegistrationNumber != null && p.RegistrationNumber.StartsWith(prefix))
            .Select(p => p.RegistrationNumber!)
            .ToListAsync(ct);

        var max = 0;
        foreach (var number in numbers)
        {
            var suffix = number[prefix.Length..];
            if (int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out var seq))
                max = Math.Max(max, seq);
        }

        return $"{prefix}{(max + 1):D3}";
    }

    public static ProcurementMethodType ToProcurementMethod(MarketingPlanRegistrationMethod method) =>
        method switch
        {
            MarketingPlanRegistrationMethod.Tender => ProcurementMethodType.Tender,
            MarketingPlanRegistrationMethod.BestOfferSelection => ProcurementMethodType.BestOffer,
            MarketingPlanRegistrationMethod.LocalProcurement => ProcurementMethodType.LocalAuction,
            MarketingPlanRegistrationMethod.SmallProcurement => ProcurementMethodType.SmallValue,
            MarketingPlanRegistrationMethod.QuotationRequest => ProcurementMethodType.Rfp,
            MarketingPlanRegistrationMethod.DirectContract => ProcurementMethodType.DirectContract,
            _ => throw new ArgumentOutOfRangeException(nameof(method)),
        };
}
