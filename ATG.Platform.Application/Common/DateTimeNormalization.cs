namespace ATG.Platform.Application.Common;

public static class DateTimeNormalization
{
    public static DateTime ToUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

    public static DateTime? ToUtc(DateTime? value) => value is null ? null : ToUtc(value.Value);
}
