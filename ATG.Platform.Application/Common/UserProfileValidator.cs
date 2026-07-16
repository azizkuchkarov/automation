namespace ATG.Platform.Application.Common;

public static class UserProfileValidator
{
    public static string? ValidatePinpp(string? pinpp)
    {
        var normalized = pinpp?.Trim() ?? "";
        if (normalized.Length != 14 || !normalized.All(char.IsDigit))
            return "PINPP must be a 14-digit number";
        return null;
    }

    public static string? ValidatePassport(string? series, string? number)
    {
        var normalizedSeries = series?.Trim().ToUpperInvariant() ?? "";
        var normalizedNumber = number?.Trim() ?? "";

        if (normalizedSeries.Length != 2 || !normalizedSeries.All(char.IsLetter))
            return "Passport series must be 2 letters";

        if (normalizedNumber.Length < 7 || normalizedNumber.Length > 9 || !normalizedNumber.All(char.IsDigit))
            return "Passport number must be 7–9 digits";

        return null;
    }

    public static (string Pinpp, string PassportSeries, string PassportNumber) Normalize(
        string pinpp, string passportSeries, string passportNumber) =>
        (pinpp.Trim(), passportSeries.Trim().ToUpperInvariant(), passportNumber.Trim());
}
