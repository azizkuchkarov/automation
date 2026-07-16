using System.Globalization;
using System.Text.RegularExpressions;
using ATG.Platform.Domain.Enums;
using ATG.Platform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ATG.Platform.Infrastructure.Dcs;

/// <summary>
/// Contract registration numbers: ATG-CD-INT-{VARIANT}-{yy}-{seq:D3}
/// Example: ATG-CD-INT-SBP-26-001
/// </summary>
public static class ContractsIntRegistrationNumberGenerator
{
    public const string PrefixMarker = "ATG-CD-INT-";

    private static readonly Regex SequencePattern = new(
        @"^ATG-CD-INT-(?:SBP|TP|DFC)-(?:\d{2}|\d{4})-(\d{3})$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string VariantCode(ContractsIntProcurementVariant variant) => variant switch
    {
        ContractsIntProcurementVariant.Sbp => "SBP",
        ContractsIntProcurementVariant.Tender => "TP",
        ContractsIntProcurementVariant.DirectForeignContract => "DFC",
        _ => variant.ToString().ToUpperInvariant(),
    };

    public static string BuildPrefix(ContractsIntProcurementVariant variant, int? year = null)
    {
        var yyyy = year ?? DateTime.UtcNow.Year;
        var yy = yyyy % 100;
        return $"{PrefixMarker}{VariantCode(variant)}-{yy:D2}-";
    }

    public static async Task<string> GenerateNextAsync(
        AppDbContext db, ContractsIntProcurementVariant variant, CancellationToken ct = default)
    {
        var prefix = BuildPrefix(variant);
        var numbers = await db.ProcurementRequestDetails.AsNoTracking()
            .Where(d => d.ContractsIntContractRegistrationNumber != null
                && d.ContractsIntContractRegistrationNumber.StartsWith(prefix))
            .Select(d => d.ContractsIntContractRegistrationNumber!)
            .ToListAsync(ct);

        var max = 0;
        foreach (var number in numbers)
        {
            if (TryExtractSequence(number, out var seq))
                max = Math.Max(max, seq);
        }

        return $"{prefix}{(max + 1):D3}";
    }

    private static bool TryExtractSequence(string number, out int seq)
    {
        seq = 0;
        var match = SequencePattern.Match(number);
        if (!match.Success)
            return false;
        return int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out seq);
    }
}
