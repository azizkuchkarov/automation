using System.Globalization;
using System.Text.RegularExpressions;
using ATG.Platform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ATG.Platform.Infrastructure.Dcs;

public static class MarketingRegistrationNumberGenerator
{
    public const string PrefixMarker = "ATG-CP-";

    private static readonly Regex RfqSequencePattern = new(
        @"^ATG-CP-(?:MT|SR)-LO-(?:\d{2}|\d{4})-(\d{3})$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>RFQ numbers always use ATG-CP-MT-LO-{yy}- regardless of MR/SR.</summary>
    public static string BuildPrefix(int? year = null)
    {
        var yyyy = year ?? DateTime.UtcNow.Year;
        var yy = yyyy % 100;
        return $"ATG-CP-MT-LO-{yy:D2}-";
    }

    public static bool IsRfqRegistrationNumber(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.StartsWith(PrefixMarker, StringComparison.Ordinal);

    public static bool IsCurrentRfqNumber(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.StartsWith(BuildPrefix(), StringComparison.Ordinal);

    public static async Task<string> GenerateNextAsync(AppDbContext db, CancellationToken ct = default)
    {
        var prefix = BuildPrefix();
        var numbers = await db.MarketingRecords.AsNoTracking()
            .Where(r => r.PortalNumber != null && r.PortalNumber.StartsWith(PrefixMarker))
            .Select(r => r.PortalNumber!)
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
        var match = RfqSequencePattern.Match(number);
        if (!match.Success)
            return false;

        return int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out seq);
    }
}
